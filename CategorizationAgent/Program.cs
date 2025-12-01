using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using CategorizationAgent.Agents;
using CategorizationAgent.Executors;
using CategorizationAgent.Data;
using CategorizationAgent.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<LogDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<MailboxService>();

// ---------------------------------------------------------
// 1) OpenAI 설정으로 변경
// ---------------------------------------------------------
// 실제 키는 환경 변수나 UserSecrets에서 가져오는 것을 권장합니다.
// dotnet user-secrets로 설정한 값은 builder.Configuration["OpenAI:ApiKey"]로 읽을 수 있습니다.
var apiKey = builder.Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException(
        "OpenAI API key is not set. Set 'OpenAI:ApiKey' via dotnet user-secrets or 'OPENAI_API_KEY' environment variable.");
}

OpenAIClient openAiClient = new OpenAIClient(apiKey);

IChatClient chatClient = openAiClient.GetChatClient("gpt-5-nano").AsIChatClient();

builder.Services.AddChatClient(chatClient);

// CSV 기반 Inquiry Executor 등록
var csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "inquiries.csv");

builder.AddInquiryClassificationAgent();
builder.AddL1ResolverAgent();
builder.AddNotificationAgent();

// 문의 분류 워크플로우: CSV 읽기 → 분류 → 출력
builder.AddWorkflow("inquiry-classification-workflow", (sp, key) =>
    {
        // 1. CSV 파일 읽기 Executor
        var csvReader = new SimpleInquiryReadExecutor(csvFilePath);

        // 2. 분류 Executor - AIAgent 전달
        var classificationAgent = sp.GetRequiredKeyedService<AIAgent>(InquiryClassificationAgent.NAME);
        var classifier = new InquiryClassificationExecutor(classificationAgent);

        // 3. 결과 출력 Executor
        var printer = new ClassificationResultPrinterExecutor();

        // 워크플로우 빌드: csvReader → classifier → printer
        
        var workflowBuilder = new WorkflowBuilder(csvReader);
        
        workflowBuilder.WithName(key);
        workflowBuilder.AddEdge(csvReader, classifier);
        workflowBuilder.AddEdge(classifier, printer);
        workflowBuilder.WithOutputFrom(printer);

        return workflowBuilder.Build();
    })
    .AddAsAIAgent(); // ← 워크플로우 자체를 하나의 AIAgent로 등록

// 4) 원래 워크플로우 등록: router → resolver → notifier 순차 실행
builder.AddWorkflow("cs-workflow", (sp, key) =>
    {
        var classificator = sp.GetRequiredKeyedService<AIAgent>(InquiryClassificationAgent.NAME);
        var resolver = sp.GetRequiredKeyedService<AIAgent>(L1ResolverAgent.NAME);
        var notifier = sp.GetRequiredKeyedService<AIAgent>(NotificationAgent.NAME);

        return AgentWorkflowBuilder.BuildSequential(
            workflowName: key,
            classificator,
            resolver,
            notifier
        );
    })
    .AddAsAIAgent(); // ← 워크플로우 자체를 하나의 AIAgent로 등록

// 5) OpenAI 호환 엔드포인트 및 Tracing 설정
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

// 워크플로우 테스트 엔드포인트 추가
app.MapGet("/run-classification", async (IServiceProvider sp) =>
{
    try
    {
        Console.WriteLine("\n🚀 문의 분류 워크플로우를 시작합니다...\n");
        
        var workflow = sp.GetRequiredKeyedService<Workflow>("inquiry-classification-workflow");
        
        // 워크플로우 실행 (입력은 빈 문자열, SimpleInquiryReadExecutor가 내부 _filePath 사용)
        await using var run = await InProcessExecution.RunAsync(workflow, "");
        
        // 이벤트 처리
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
        Console.WriteLine($"\n❌ 워크플로우 실행 중 오류 발생: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return Results.Problem(detail: ex.Message, title: "워크플로우 실행 오류");
    }
});

app.UseHttpsRedirection();

// DevUI 초기 지원이라 아직 잘 안됨
app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (app.Environment.IsDevelopment())
{
    app.MapDevUI();
}

app.Run();