using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using CategorizationAgent.Agents;
using CategorizationAgent.Executors;
using CategorizationAgent.Data;
using CategorizationAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// 기본 인프라 및 DB 서비스 등록
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<LogDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddTransient<MailboxService>();
builder.Services.AddTransient<UserNumberService>();
builder.Services.AddSingleton<CsvService>();
builder.Services.AddTransient<BeginnerRewardService>();
builder.Services.AddTransient<CategoryActionService>();

// AI 클라이언트 설정
var apiKey = builder.Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OpenAI API key is not set.");

IChatClient chatClient = new OpenAIClient(apiKey)
    .GetChatClient("gpt-5-nano")
    .AsIChatClient();

builder.Services.AddChatClient(chatClient);

// 에이전트 등록
builder.AddInquiryClassificationAgent();
builder.AddL1ResolverAgent();
builder.AddNotificationAgent();

// 분류 및 확인용 워크플로우 등록
builder.AddWorkflow("inquiry-classification-workflow", (sp, key) =>
{
    var csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Sample", "inquiries.csv");
    var csvService = sp.GetRequiredService<CsvService>();

    var csvReader = new SimpleInquiryReadExecutor(csvFilePath, csvService);
    var classificationAgent = sp.GetRequiredKeyedService<AIAgent>(InquiryClassificationAgent.NAME);
    var classifier = new InquiryClassificationExecutor(classificationAgent);
    var printer = new ClassificationResultPrinterExecutor();

    var workflowBuilder = new WorkflowBuilder(csvReader);
    workflowBuilder.WithName(key);

    workflowBuilder
        .AddEdge(csvReader, classifier)
        .AddEdge(classifier, printer)
        .WithOutputFrom(printer);

    return workflowBuilder.Build();
});

// 실제 목표로하는 워크플로우
builder.AddWorkflow("run-classification-workflow", (sp, key) =>
{
    var csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Sample", "inquiries.csv");
    var csvService = sp.GetRequiredService<CsvService>();
    var classificationAgent = sp.GetRequiredKeyedService<AIAgent>(InquiryClassificationAgent.NAME);
    var beginnerRewardService = sp.GetRequiredService<BeginnerRewardService>();
    var categoryActionService = sp.GetRequiredService<CategoryActionService>();

    var csvReadExecutor = new SimpleInquiryReadExecutor(csvFilePath, csvService);
    var classificationExecutor = new InquiryClassificationExecutor(classificationAgent);
    var categoryHandlerExecutor = new CategoryHandlerExecutor(beginnerRewardService, categoryActionService);
    var inquiryStatusUpdateExecutor = new InquiryStatusUpdateExecutor(csvFilePath, csvService);

    var workflowBuilder = new WorkflowBuilder(csvReadExecutor);
    workflowBuilder.WithName(key);

    workflowBuilder
        .AddEdge(csvReadExecutor, classificationExecutor)
        .AddEdge(classificationExecutor, categoryHandlerExecutor)
        .AddEdge(categoryHandlerExecutor, inquiryStatusUpdateExecutor)
        .WithOutputFrom(inquiryStatusUpdateExecutor);

    return workflowBuilder.Build();
});

// DevUI 및 호스팅 서비스 설정
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

// 파이프라인 및 엔드포인트 설정
app.UseHttpsRedirection();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (app.Environment.IsDevelopment())
{
    app.MapDevUI();
}

// 워크플로우 실행 엔드포인트
app.MapGet("/run-classification", async (
    [FromKeyedServices("inquiry-classification-workflow")]
    Workflow workflow) =>
{
    try
    {
        Console.WriteLine("\n🚀 문의 분류 워크플로우를 시작합니다...\n");

        await using var run = await InProcessExecution.RunAsync(workflow, "");

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
        Console.WriteLine($"\n❌ 오류 발생: {ex.Message}");
        return Results.Problem(detail: ex.Message);
    }
});

app.MapGet("/run-classification-with-action", async (
    [FromKeyedServices("run-classification-workflow")]
    Workflow workflow) =>
{
    try
    {
        Console.WriteLine("\n🚀 문의 분류 및 액션 처리 워크플로우를 시작합니다...\n");

        await using var run = await InProcessExecution.RunAsync(workflow, "");

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
        Console.WriteLine($"\n❌ 오류 발생: {ex.Message}");
        return Results.Problem(detail: ex.Message);
    }
});