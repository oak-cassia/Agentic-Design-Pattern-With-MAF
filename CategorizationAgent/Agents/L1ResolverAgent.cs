using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace CategorizationAgent.Agents;

public static class L1ResolverAgent
{
    public const string NAME = "l1-resolver";

    private const string INSTRUCTIONS = """
                                        You craft final responses for L1 inquiries using provided knowledge base context. 
                                        If insufficient context, escalate as L2.
                                        """;

    public static IHostedAgentBuilder AddL1ResolverAgent(this IHostApplicationBuilder builder)
    {
        return builder.AddAIAgent(NAME, (sp, _) =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();

            return new ChatClientAgent(chatClient, INSTRUCTIONS, NAME);
        });
    }
}