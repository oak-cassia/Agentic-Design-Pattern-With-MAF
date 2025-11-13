using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

// 1) Ollama IChatClient 설정
var ollama = new OllamaApiClient(new Uri("http://localhost:11434"));
ollama.SelectedModel = "phi4-mini";

builder.Services.AddChatClient(ollama); // IChatClient 등록

// 2) 기본 에이전트 등록
builder.AddAIAgent(
    "writer",
    "You write short stories (max 300 words) about the given topic. " + "Write in a clear, engaging style.",
    ollama
);

builder.AddAIAgent(
    "editor",
    "You edit short stories to improve grammar and style. " + "Ensure the story stays under 300 words and keep the original meaning.",
    ollama
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
    ollama
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