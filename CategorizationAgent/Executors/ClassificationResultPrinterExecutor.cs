using CategorizationAgent.Agents;
using CategorizationAgent.DTOs;
using Microsoft.Agents.AI.Workflows;
using System.Text.Json;

namespace CategorizationAgent.Executors;

/// <summary>
/// 분류 결과 리스트를 받아서 카테고리 ID에 맞는 처리방법을 콘솔에 출력하는 Executor
/// </summary>
public class ClassificationResultPrinterExecutor() : Executor<List<ClassificationResult>, string>("ClassificationResultPrinterExecutor")
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

    public override ValueTask<string> HandleAsync(List<ClassificationResult> results, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("📋 문의 분류 결과 및 처리방법");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"총 {results.Count}건의 문의가 분류되었습니다.\n");

        foreach (var result in results)
        {
            Console.WriteLine($"[문의 ID: {result.InquiryId}]");
            Console.WriteLine($"  📝 문의 내용: {result.InquiryDescription}");
            Console.WriteLine($"\n  ✅ 분류 결과: {result.CategoryName} (ID: {result.CategoryId})");
            
            if (HandlingSummaries.TryGetValue(result.CategoryId, out var handlingSummary))
            {
                Console.WriteLine($"\n  📌 처리방법:");
                Console.WriteLine($"  {handlingSummary}");
            }
            else
            {
                Console.WriteLine($"  ⚠️  처리방법 정보를 찾을 수 없습니다. (Category ID: {result.CategoryId})");
            }
            
            Console.WriteLine();
        }

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("✅ 분류 작업 완료");
        Console.WriteLine(new string('=', 80) + "\n");

        return ValueTask.FromResult($"분류 완료: {results.Count}건");
    }
}