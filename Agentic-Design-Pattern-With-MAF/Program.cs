using System.ComponentModel;
using Microsoft.Agents.AI;
using OllamaSharp;
using Microsoft.Extensions.AI;

var chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "phi4-mini");

AIAgent agent = new ChatClientAgent(
    chatClient,
    instructions: "You are a helpful assistant",
    name: "Joker",
    tools: [AIFunctionFactory.Create(GetWeather)]);

Console.WriteLine(await agent.RunAsync("What is the weather like in Amsterdam?"));

// AgentThread thread = agent.GetNewThread();
// Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate.", thread));
// Console.WriteLine(await agent.RunAsync("Now add some emojis to the joke and tell it in the voice of a pirate's parrot.", thread));


[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";