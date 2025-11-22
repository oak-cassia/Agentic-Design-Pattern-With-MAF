using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace CategorizationAgent.Agents;

public static class NotificationAgent
{
    public const string NAME = "notification-agent";

    private const string INSTRUCTIONS = """
                                        You summarize pending L2 inquiries for human reviewers in Slack-ready bullet points.
                                        """;

    public static IHostedAgentBuilder AddNotificationAgent(this IHostApplicationBuilder builder)
    {
        return builder.AddAIAgent(NAME, (sp, _) =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();

            return new ChatClientAgent(chatClient, INSTRUCTIONS, NAME);
        });
    }
}

