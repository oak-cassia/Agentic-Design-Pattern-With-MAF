using CategorizationAgent.Agents;
using CategorizationAgent.DTOs;
using System.Text.Json;

namespace CategorizationAgent.Services;

/// <summary>
/// 카테고리별 처리 방법을 제공하는 서비스
/// </summary>
public class CategoryActionService
{
    private readonly Dictionary<int, string> _handlingSummaries;

    public CategoryActionService()
    {
        _handlingSummaries = LoadHandlingSummaries();
    }

    /// <summary>
    /// 카테고리 규칙 파일에서 처리 방법을 로드합니다.
    /// </summary>
    private static Dictionary<int, string> LoadHandlingSummaries()
    {
        var ruleFilePath = Path.Combine(Directory.GetCurrentDirectory(), "KnowledgeBase", InquiryClassificationAgent.RULE_FILE_NAME);
        
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

    /// <summary>
    /// 카테고리 ID에 해당하는 처리 방법을 조회합니다.
    /// </summary>
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
            response.ResponseMessage = handlingSummary;
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

/// <summary>
/// 카테고리별 액션 실행 결과 응답 클래스
/// </summary>
public class CategoryActionResponse : CategoryActionResponseBase
{
}

