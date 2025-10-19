using System;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OllamaSharp;
using OpenAI;

var chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "phi4-mini-reasoning");

AIAgent agent = new ChatClientAgent(
    chatClient,
    instructions: "You are good at telling jokes",
    name: "Joker");

Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));