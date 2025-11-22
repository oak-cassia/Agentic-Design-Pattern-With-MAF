using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting; // ChatClientAgent가 있는 네임스페이스
using Microsoft.Extensions.AI;

namespace CategorizationAgent.Agents;

public static class InquiryRouterAgent
{
    // 1. 설정값과 프롬프트는 이곳에 숨깁니다. (책임 분리)
    public const string NAME = "inquiry-router";

    private const string INSTRUCTIONS = """
                                        너는 문의 분류 전문가야. 
                                        사용자의 질문을 [기술지원, 환불, 일반문의] 중 하나로만 답변해줘.
                                        """;

    public static IHostedAgentBuilder AddInquiryRouterAgent(this IHostApplicationBuilder builder)
    {
        return builder.AddAIAgent(NAME, (sp, _) =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();

            return new ChatClientAgent(chatClient, INSTRUCTIONS, NAME);
        });
    }
}