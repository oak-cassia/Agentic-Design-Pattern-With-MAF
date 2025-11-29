using CategorizationAgent.DTOs;
using Microsoft.Agents.AI.Workflows;

namespace CategorizationAgent.Executors;

/// <summary>
/// 분류 결과 리스트를 받아서 콘솔에 출력하는 Executor
/// </summary>
public class ClassificationResultPrinterExecutor() : Executor<List<ClassificationResult>, string>("ClassificationResultPrinterExecutor")
{
    public override ValueTask<string> HandleAsync(List<ClassificationResult> results, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("📋 문의 분류 결과 리스트");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"총 {results.Count}건의 문의가 분류되었습니다.\n");

        foreach (var result in results)
        {
            var inquiryId = result.InquiryId.ToString();
            var categoryId = result.CategoryId.ToString();
            var confidence = result.Confidence.ToString("P2");
            var multiLabel = result.IsMultiLabel
                ? "예"
                : "아니오";

            Console.WriteLine($"[문의 ID: {inquiryId}]");
            Console.WriteLine($"  ├─ 카테고리 ID: {categoryId}");
            Console.WriteLine($"  ├─ 카테고리 (한글): {result.CategoryNameKo}");
            Console.WriteLine($"  ├─ 카테고리 (영문): {result.CategoryNameEn}");
            Console.WriteLine($"  ├─ 신뢰도: {confidence}");
            Console.WriteLine($"  ├─ 복합 문의: {multiLabel}");

            if (result.SubCategories.Any())
            {
                var subCategories = string.Join(", ", result.SubCategories.ToArray());
                Console.WriteLine($"  ├─ 하위 카테고리: {subCategories}");
            }

            if (result.Keywords.Any())
            {
                var keywords = string.Join(", ", result.Keywords.ToArray());
                Console.WriteLine($"  ├─ 키워드: {keywords}");
            }

            Console.WriteLine($"  └─ 이유: {result.Reason}");
            Console.WriteLine();
        }

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("✅ 분류 작업 완료");
        Console.WriteLine(new string('=', 80) + "\n");

        return ValueTask.FromResult($"분류 완료: {results.Count}건");
    }
}