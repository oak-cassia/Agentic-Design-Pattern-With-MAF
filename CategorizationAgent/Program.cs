using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

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

// DI 컨테이너에 등록 (선택 사항이지만 다른 곳에서 주입받아 쓸 때 유용, 설계 고민)
builder.Services.AddChatClient(chatClient);

// 2) 기본 에이전트 등록
builder.AddAIAgent(
    "writer",
    "You write short stories (max 300 words) about the given topic. " + "Write in a clear, engaging style.",
    chatClient
);

builder.AddAIAgent(
    "editor",
    "You edit short stories to improve grammar and style. " + "Ensure the story stays under 300 words and keep the original meaning.",
    chatClient
);

// 3) 선택: 툴 하나 추가 (예: 포맷팅)
[Description("Formats the story for display.")]
string FormatStory(
    [Description("Title of the story.")] string title,
    [Description("Body of the story.")] string story)
    => $"""
        **Title**: {title}
        **Date**: {DateTime.Today:yyyy-MM-dd}

        {story}
        """;

builder.AddAIAgent(
    "formatter",
    "You format stories for display; you do not change the content.",
    chatClient
).WithAITools(AIFunctionFactory.Create(FormatStory, name: "format_story"));

// 4) 워크플로우 등록: writer → editor → formatter 순차 실행
builder.AddWorkflow("story-workflow", (sp, key) =>
    {
        var writer = sp.GetRequiredKeyedService<AIAgent>("writer");
        var editor = sp.GetRequiredKeyedService<AIAgent>("editor");
        var formatter = sp.GetRequiredKeyedService<AIAgent>("formatter");

        // 순차 워크플로우 구성
        // AgentWorkflowBuilder.BuildSequential(workflowName, params AIAgent[])
        return AgentWorkflowBuilder.BuildSequential(
            workflowName: key,
            writer,
            editor,
            formatter
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