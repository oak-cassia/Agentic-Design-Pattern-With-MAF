using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI; // DevUI 사용 시
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
// (사용자 정의 네임스페이스)
using CategorizationAgent.Agents;
using CategorizationAgent.Executors;
using CategorizationAgent.Data;
using CategorizationAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. 기본 인프라 및 DB 서비스 등록
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<LogDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddTransient<MailboxService>();
builder.Services.AddTransient<UserNumberService>();
builder.Services.AddSingleton<CsvService>(); // CSV 서비스 등록
builder.Services.AddTransient<BeginnerRewardService>(); // 초보자 보상 서비스
builder.Services.AddTransient<CategoryActionService>(); // 카테고리 액션 서비스
// Executor가 상태를 가지지 않는다면 Scoped/Singleton으로 등록 가능

// ============================================================
// 2. AI 클라이언트 설정 (표준 패턴)
// ============================================================
// IChatClient를 DI 컨테이너에 등록하여 모든 Agent가 이를 공유하도록 합니다.
var apiKey = builder.Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OpenAI API key is not set.");

// OpenAI 클라이언트 설정
IChatClient chatClient = new OpenAIClient(apiKey)
    .GetChatClient("gpt-5-nano") // 모델명 지정
    .AsIChatClient();

// 프레임워크 표준 확장 메서드를 사용하여 ChatClient 등록
builder.Services.AddChatClient(chatClient);

// ============================================================
// 3. 에이전트(Agent) 등록
// ============================================================
// 사용자 정의 확장 메서드(AddInquiryClassificationAgent 등)가 내부적으로 
// builder.AddAIAgent(...)를 호출한다고 가정합니다.
// 만약 직접 등록한다면 아래와 같은 형태가 됩니다:
// builder.AddAIAgent("InquiryClassificationAgent", instructions: "...");

builder.AddInquiryClassificationAgent();
builder.AddL1ResolverAgent();
builder.AddNotificationAgent();

// ============================================================
// 4. 워크플로우(Workflow) 등록
// ============================================================
builder.AddWorkflow("inquiry-classification-workflow", (sp, key) =>
{
    // 1. 필요한 리소스 준비
    var csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Sample", "inquiries.csv");
    var csvService = sp.GetRequiredService<CsvService>();

    // 2. Executor 인스턴스 생성 (CsvService 주입)
    var csvReader = new SimpleInquiryReadExecutor(csvFilePath, csvService);

    // Keyed Service로 등록된 Agent를 가져와서 Executor에 주입
    var classificationAgent = sp.GetRequiredKeyedService<AIAgent>(InquiryClassificationAgent.NAME);
    var classifier = new InquiryClassificationExecutor(classificationAgent);

    var printer = new ClassificationResultPrinterExecutor();

    // 3. 워크플로우 빌드
    var workflowBuilder = new WorkflowBuilder(csvReader);
    workflowBuilder.WithName(key);

    workflowBuilder
        .AddEdge(csvReader, classifier)
        .AddEdge(classifier, printer)
        .WithOutputFrom(printer);

    return workflowBuilder.Build();
});
// .AddAsAIAgent(); // 이 워크플로우를 다른 워크플로우의 하위 에이전트로 쓸 때만 주석 해제

builder.AddWorkflow("run-classification-workflow", (sp, key) =>
{
    // 1. 필요한 리소스 준비
    var csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Sample", "inquiries.csv");

    var csvService = sp.GetRequiredService<CsvService>();
    var classificationAgent = sp.GetRequiredKeyedService<AIAgent>(InquiryClassificationAgent.NAME);
    var beginnerRewardService = sp.GetRequiredService<BeginnerRewardService>();
    var categoryActionService = sp.GetRequiredService<CategoryActionService>();

    // 2. Executor 인스턴스 생성
    var csvReadExecutor = new SimpleInquiryReadExecutor(csvFilePath, csvService);
    var classificationExecutor = new InquiryClassificationExecutor(classificationAgent);
    var categoryHandlerExecutor = new CategoryHandlerExecutor(beginnerRewardService, categoryActionService);
    var inquiryStatusUpdateExecutor = new InquiryStatusUpdateExecutor(csvFilePath, csvService);

    // 3. 워크플로우 빌드
    var workflowBuilder = new WorkflowBuilder(csvReadExecutor);
    workflowBuilder.WithName(key);

    workflowBuilder
        .AddEdge(csvReadExecutor, classificationExecutor)
        .AddEdge(classificationExecutor, categoryHandlerExecutor)
        .AddEdge(categoryHandlerExecutor, inquiryStatusUpdateExecutor)
        .WithOutputFrom(inquiryStatusUpdateExecutor);

    return workflowBuilder.Build();
});

// ============================================================
// 5. DevUI 및 호스팅 서비스 설정 (표준 패턴)
// ============================================================
// 이 서비스들은 DevUI 및 에이전트 상태 관리에 필수적입니다.
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

// ============================================================
// 6. 파이프라인 및 엔드포인트 설정
// ============================================================
app.UseHttpsRedirection();

// DevUI 및 OpenAI 호환 엔드포인트 매핑
app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (app.Environment.IsDevelopment())
{
    app.MapDevUI(); // /devui 경로로 접근 가능
}

// 워크플로우 실행 엔드포인트
app.MapGet("/run-classification", async (
    [FromKeyedServices("inquiry-classification-workflow")]
    Workflow workflow) =>
{
    try
    {
        Console.WriteLine("\n🚀 문의 분류 워크플로우를 시작합니다...\n");

        // 스트리밍 실행 또는 일반 실행
        await using var run = await InProcessExecution.RunAsync(workflow, "");

        // 실행 결과 로그 출력 (이벤트 기반)
        foreach (var evt in run.NewEvents)
        {
            if (evt is ExecutorCompletedEvent executorComplete)
            {
                Console.WriteLine($"✓ {executorComplete.ExecutorId} 완료");
            }
        }

        Console.WriteLine("\n✅ 워크플로우 실행이 완료되었습니다.\n");
        return Results.Ok(new { message = "워크플로우 실행 완료", success = true });
    }
    catch (Exception ex)
    {
        // 실제 프로덕션에서는 로거(ILogger)를 사용하세요.
        Console.WriteLine($"\n❌ 오류 발생: {ex.Message}");
        return Results.Problem(detail: ex.Message);
    }
});

// run-classification-workflow 실행 엔드포인트
app.MapGet("/run-classification-with-action", async (
    [FromKeyedServices("run-classification-workflow")]
    Workflow workflow) =>
{
    try
    {
        Console.WriteLine("\n🚀 문의 분류 및 액션 처리 워크플로우를 시작합니다...\n");

        // 스트리밍 실행 또는 일반 실행
        await using var run = await InProcessExecution.RunAsync(workflow, "");

        // 실행 결과 로그 출력 (이벤트 기반)
        foreach (var evt in run.NewEvents)
        {
            if (evt is ExecutorCompletedEvent executorComplete)
            {
                Console.WriteLine($"✓ {executorComplete.ExecutorId} 완료");
            }
        }

        Console.WriteLine("\n✅ 워크플로우 실행이 완료되었습니다.\n");
        return Results.Ok(new { message = "분류 및 액션 처리 워크플로우 실행 완료", success = true });
    }
    catch (Exception ex)
    {
        // 실제 프로덕션에서는 로거(ILogger)를 사용하세요.
        Console.WriteLine($"\n❌ 오류 발생: {ex.Message}");
        return Results.Problem(detail: ex.Message);
    }
});