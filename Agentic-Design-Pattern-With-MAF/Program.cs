using System.ComponentModel;
using Microsoft.Agents.AI;
using OllamaSharp;
using Microsoft.Extensions.AI;

#pragma warning disable MEAI001

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

// --------------------
AIFunction weatherFunction = AIFunctionFactory.Create(GetWeather);
AIFunction approvalRequiredWeatherFunction = new ApprovalRequiredAIFunction(weatherFunction);


var chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "phi4-mini");

AIAgent agent = new ChatClientAgent(
    chatClient,
    instructions: "You are a helpful assistant",
    name: "Joker",
    tools: [approvalRequiredWeatherFunction]);
AgentThread thread = agent.GetNewThread();
AgentRunResponse response = await agent.RunAsync("What is the weather like in Amsterdam?", thread);

var functionApprovalRequests = response.Messages
    .SelectMany(x => x.Contents)
    .OfType<FunctionApprovalRequestContent>()
    .ToList();

FunctionApprovalRequestContent requestContent = functionApprovalRequests.First();
Console.WriteLine($"We require approval to execute '{requestContent.FunctionCall.Name}'");

var approvalMessage = new ChatMessage(ChatRole.User, [requestContent.CreateResponse(true)]);
Console.WriteLine(await agent.RunAsync(approvalMessage, thread));

// AgentThread thread = agent.GetNewThread();
// Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate.", thread));
// Console.WriteLine(await agent.RunAsync("Now add some emojis to the joke and tell it in the voice of a pirate's parrot.", thread));

#pragma warning restore MEAI001