using CategorizationAgent.Agents;
using CategorizationAgent.DTOs;
using System.Text.Json;

namespace CategorizationAgent.Services;

public class CategoryActionService
{
    private readonly Dictionary<int, string> _handlingSummaries = LoadHandlingSummaries();

    private static Dictionary<int, string> LoadHandlingSummaries()
    {
        var ruleFilePath = Path.Combine(AppContext.BaseDirectory, "KnowledgeBase", InquiryClassificationAgent.RULE_FILE_NAME);

        if (!File.Exists(ruleFilePath))
        {
            Console.WriteLine($"Warning: Category rule file not found at {ruleFilePath}");
            return new Dictionary<int, string>();
        }

        try
        {
            var json = File.ReadAllText(ruleFilePath);
            var rules = JsonSerializer.Deserialize<List<CategoryRuleItem>>(json);
            return rules?.ToDictionary(r => r.Id, r => r.HandlingSummary) ?? new Dictionary<int, string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading rules: {ex.Message}");
            return new Dictionary<int, string>();
        }
    }

    public CategoryActionResponse GetCategoryAction(ClassificationResult result)
    {
        var response = new CategoryActionResponse
        {
            InquiryId = result.InquiryId,
            UserId = result.UserId,
            IsSuccess = false,
            ResponseMessage = string.Empty
        };

        if (_handlingSummaries.TryGetValue(result.CategoryId, out var handlingSummary))
        {
            response.IsSuccess = true;
            response.ResponseMessage = $"[개발자 작업 도움말] {handlingSummary}";
            Console.WriteLine($"[카테고리 액션 조회 완료] InquiryId: {result.InquiryId}, CategoryId: {result.CategoryId}");
        }
        else
        {
            response.ResponseMessage = $"처리방법 정보를 찾을 수 없습니다. (Category ID: {result.CategoryId})";
            Console.WriteLine($"[카테고리 액션 조회 실패] InquiryId: {result.InquiryId}, CategoryId: {result.CategoryId} - 처리방법 없음");
        }

        return response;
    }
}

public class CategoryActionResponse : CategoryActionResponseBase
{
}