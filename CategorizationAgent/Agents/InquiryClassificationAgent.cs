using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Data;
using System.Text.Json;

namespace CategorizationAgent.Agents;

// JSON 데이터 구조 정의 (변경 없음)
public static class InquiryClassificationAgent
{
    public const string NAME = "inquiry-classifier";
    public const string RULE_FILE_NAME = "CsCategoryRule.json";

    // 프롬프트: 구조화된 출력 형식을 강제할 필요 없이 분류 작업 자체에 집중하도록 지시합니다.
    public const string INSTRUCTIONS =
        """
        당신은 고객 문의 분류 전문가입니다.
        제공된 [전체 카테고리 규칙 목록]을 참고하여, 입력으로 들어오는 문의 내용이 어느 카테고리에 가장 적합한지 판단하세요.
        판단 근거, 신뢰도, 키워드를 포함하여 답변해야 합니다.
        """;

    // JSON 데이터를 미리 로드
    private static readonly List<CategoryRuleItem> CategoryRules = LoadCategoryRules();

    private static List<CategoryRuleItem> LoadCategoryRules()
    {
        var ruleFilePath = Path.Combine(Directory.GetCurrentDirectory(), "KnowledgeBase", RULE_FILE_NAME);

        if (!File.Exists(ruleFilePath))
        {
            Console.WriteLine($"Warning: Category rule file not found at {ruleFilePath}");
            return new List<CategoryRuleItem>();
        }

        try
        {
            var json = File.ReadAllText(ruleFilePath);
            var rules = JsonSerializer.Deserialize<List<CategoryRuleItem>>(json);
            return rules ?? new List<CategoryRuleItem>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading rules: {ex.Message}");
            return new List<CategoryRuleItem>();
        }
    }

    // [수정됨] 검색(필터링) 없이 전체 리스트를 반환하는 함수
    private static Task<IEnumerable<TextSearchProvider.TextSearchResult>> GetAllRulesAsContextAsync(string query, CancellationToken cancellationToken)
    {
        var results = new List<TextSearchProvider.TextSearchResult>();

        // query(사용자 질문) 내용은 무시하고, 로드된 모든 룰을 변환하여 반환합니다.
        foreach (var item in CategoryRules)
        {
            results.Add(new TextSearchProvider.TextSearchResult
            {
                SourceName = item.NameKo,
                SourceLink = $"ID:{item.Id}",
                // AI가 전체 목록을 이해하기 좋도록 포맷팅합니다.
                Text = $"""
                        [Category ID: {item.Id}]
                        Name: {item.NameKo} / {item.NameEn}
                        Description: {item.Description}
                        Key Points: {item.HandlingSummary}
                        ---
                        """
            });
        }

        return Task.FromResult<IEnumerable<TextSearchProvider.TextSearchResult>>(results);
    }

    public static IHostedAgentBuilder AddInquiryClassificationAgent(this IHostApplicationBuilder builder)
    {
        return builder.AddAIAgent(NAME, (sp, key) =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();

            TextSearchProviderOptions textSearchOptions = new()
            {
                SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
                RecentMessageMemoryLimit = 5
            };

            // ChatClientAgent 생성
            return chatClient.CreateAIAgent(
                new ChatClientAgentOptions()
                {
                    Instructions = INSTRUCTIONS,
                    Name = NAME,
                    // GetAllRulesAsContextAsync 함수를 연결합니다.
                    AIContextProviderFactory = ctx =>
                        new TextSearchProvider(GetAllRulesAsContextAsync, ctx.SerializedState, ctx.JsonSerializerOptions, textSearchOptions)
                }
            );
        });
    }
}