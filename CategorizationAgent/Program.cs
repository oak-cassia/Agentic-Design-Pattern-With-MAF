using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using CategorizationAgent.Agents;

var builder = WebApplication.CreateBuilder(args);

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

builder.AddInquiryClassificationAgent();
builder.AddL1ResolverAgent();
builder.AddNotificationAgent();

// 4) 워크플로우 등록: router → resolver → notifier 순차 실행
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

// 5) OpenAI 호환 엔드포인트 (원하면 DevUI도 같이)
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

app.UseHttpsRedirection();

// 필요하면 DevUI도

if (app.Environment.IsDevelopment())
{
    app.MapDevUI();
}

app.Run();