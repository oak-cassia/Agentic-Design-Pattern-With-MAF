using Microsoft.Agents.AI;
using OllamaSharp;
using Microsoft.Extensions.AI;
using Agentic_Design_Pattern_With_MAF.Services;
using CategorizationAgent.DTOs;

#pragma warning disable MEAI001

// 1) 날씨 툴 준비
AIFunction weatherFunction = AIFunctionFactory.Create(WeatherService.GetWeather);
AIFunction approvalRequiredWeatherFunction = new ApprovalRequiredAIFunction(weatherFunction);

// 2) 공통 LLM 클라이언트
var chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "phi4-mini");

// 3) 날씨 전용 에이전트
ChatOptions chatOptions = new()
{
    ResponseFormat = ChatResponseFormat.ForJsonSchema<WeatherInfo>(),
    Tools = [approvalRequiredWeatherFunction]
};

ChatClientAgentOptions chatClientAgentOptions = new()
{
    Name = "Weather Agent",
    Instructions = "You are a weather assistant. You ONLY answer weather questions with tools.",
    ChatOptions = chatOptions,
};

AIAgent weatherAgent = chatClient.CreateAIAgent(
    chatClientAgentOptions
);

// AIAgent weatherAgent = new ChatClientAgent(
//     chatClient,
//     instructions: "You are a weather assistant. You ONLY answer weather questions with tools.",
//     name: "WeatherAgent",
//     tools: [approvalRequiredWeatherFunction]);

// 4) 일반 대화 에이전트
AIAgent chatAgent = new ChatClientAgent(
    chatClient,
    instructions: "You are a helpful assistant for general questions.",
    name: "GeneralAgent");

// 5) 간단 라우터
AIAgent RouteAsync(string userInput)
{
    // 규칙 기반: 'weather', '날씨', '기온', '비와?' 등 포함되면 날씨 에이전트로
    // 필요하면 여기다 정규식, 키워드 테이블, LLM 분류 넣으면 됨
    string lower = userInput.ToLowerInvariant();

    if (lower.Contains("weather") ||
        lower.Contains("forecast"))
    {
        return weatherAgent;
    }

    return chatAgent;
}

// ---------------- 실제 사용부 ----------------
// Console.Write("사용자 입력: ");
var userText = "use Tool. What is the weather like in Amsterdam?";

// 1) 라우팅해서 어떤 에이전트 쓸지 결정
AIAgent agent = RouteAsync(userText);

// 2) 스레드 만들고 실행
AgentThread thread = agent.GetNewThread();
AgentRunResponse response = await agent.RunAsync(userText, thread);

// 3) 만약 그 에이전트가 승인 필요한 툴을 사용하려 했다면 처리
var functionApprovalRequests = response.Messages
    .SelectMany(x => x.Contents)
    .OfType<FunctionApprovalRequestContent>()
    .ToList();

if (functionApprovalRequests.Count > 0)
{
    FunctionApprovalRequestContent requestContent = functionApprovalRequests.First();
    Console.WriteLine($"We require approval to execute '{requestContent.FunctionCall.Name}'");

    Console.Write("함수 실행을 승인하시겠습니까? (y/n): ");
    var userInput = Console.ReadLine()?.ToLower();
    bool isApproved = userInput == "y" || userInput == "yes";

    var approvalMessage = new ChatMessage(ChatRole.User, [requestContent.CreateResponse(isApproved)]);
    Console.WriteLine(await agent.RunAsync(approvalMessage, thread));
}
else
{
    // 승인 요청이 없는 일반 대화면 여기서 끝
    Console.WriteLine(response.Messages.Last().ToString());
}

#pragma warning restore MEAI001