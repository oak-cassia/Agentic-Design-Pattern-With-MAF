using CategorizationAgent.Agents;
using CategorizationAgent.DTOs;
using Microsoft.Agents.AI.Workflows;
using System.Text.Json;

namespace CategorizationAgent.Executors;

/// <summary>
/// 분류 결과를 받아서 카테고리 ID에 맞는 대응법을 CategoryActionResponseBase로 반환하는 Executor
/// </summary>
public class CategoryActionResponseExecutor() : Executor<ClassificationResult, CategoryActionResponseBase>("CategoryActionResponseExecutor")
{
    private static readonly Dictionary<int, string> HandlingSummaries = LoadHandlingSummaries();

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

    public override ValueTask<CategoryActionResponseBase> HandleAsync(ClassificationResult result, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var response = new CategoryActionResponse
        {
            InquiryId = result.InquiryId,
            UserId = result.UserId ?? string.Empty,
            // UserNumberId = result.UserNumberId,
            IsSuccess = false,
            ResponseMessage = string.Empty
        };

        if (HandlingSummaries.TryGetValue(result.CategoryId, out var handlingSummary))
        {
            response.IsSuccess = true;
            response.ResponseMessage = handlingSummary;
        }
        else
        {
            response.ResponseMessage = $"처리방법 정보를 찾을 수 없습니다. (Category ID: {result.CategoryId})";
        }

        return ValueTask.FromResult<CategoryActionResponseBase>(response);
    }
}

/// <summary>
/// 카테고리별 액션 실행 결과 응답 클래스
/// </summary>
public class CategoryActionResponse : CategoryActionResponseBase
{
}

