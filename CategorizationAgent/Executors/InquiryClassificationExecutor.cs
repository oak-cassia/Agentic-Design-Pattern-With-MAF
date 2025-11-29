using CategorizationAgent.DTOs;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using System.Text.Json;

namespace CategorizationAgent.Executors;

/// <summary>
/// Inquiry 리스트를 받아서 각 문의를 InquiryClassificationAgent에 전달하고
/// 분류 결과를 수집하여 반환하는 Executor
/// </summary>
public class InquiryClassificationExecutor(AIAgent agent) : Executor<List<Inquiry>, List<ClassificationResult>>("InquiryClassificationExecutor")
{
    public async override ValueTask<List<ClassificationResult>> HandleAsync(
        List<Inquiry> inquiries, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        var results = new List<ClassificationResult>();

        foreach (var inquiry in inquiries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // 문의 내용을 Agent에 전달
                var agentInput = $"문의 ID: {inquiry.Id}\n사용자: {inquiry.UserId}\n내용: {inquiry.Description}";
                
                // AIAgent 실행
                var response = await agent.RunAsync(agentInput);
                
                // 응답에서 텍스트 추출
                var responseText = response.Text;
                
                // JSON 파싱 시도
                var classificationResult = ParseClassificationResponse(responseText, inquiry);
                results.Add(classificationResult);
                
                var inquiryId = inquiry.Id.ToString();
                var categoryName = classificationResult.CategoryNameKo;
                Console.WriteLine($"[분류 완료] ID: {inquiryId}, 카테고리: {categoryName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[분류 실패] ID: {inquiry.Id}, 오류: {ex.Message}");
                
                // 실패한 경우 기본 결과 추가
                results.Add(new ClassificationResult
                {
                    InquiryId = inquiry.Id,
                    CategoryId = 99,
                    CategoryNameKo = "기타/분류 불가",
                    CategoryNameEn = "OtherOrUnknown",
                    Confidence = 0.0,
                    IsMultiLabel = false,
                    SubCategories = new List<string>(),
                    Reason = $"분류 중 오류 발생: {ex.Message}",
                    Keywords = new List<string>()
                });
            }
        }

        return results;
    }

    private ClassificationResult ParseClassificationResponse(string responseText, Inquiry inquiry)
    {
        try
        {
            // JSON 부분만 추출 (```json ... ``` 형태인 경우 처리)
            var jsonText = ExtractJson(responseText);
            
            var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            return new ClassificationResult
            {
                InquiryId = inquiry.Id,
                CategoryId = root.GetProperty("category_id").GetInt32(),
                CategoryNameKo = root.GetProperty("category_name_ko").GetString() ?? "",
                CategoryNameEn = root.GetProperty("category_name_en").GetString() ?? "",
                Confidence = root.GetProperty("confidence").GetDouble(),
                IsMultiLabel = root.GetProperty("is_multi_label").GetBoolean(),
                SubCategories = JsonSerializer.Deserialize<List<string>>(
                    root.GetProperty("sub_categories").GetRawText()) ?? new List<string>(),
                Reason = root.GetProperty("reason").GetString() ?? "",
                Keywords = JsonSerializer.Deserialize<List<string>>(
                    root.GetProperty("keywords").GetRawText()) ?? new List<string>()
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"JSON 파싱 실패. 응답: {responseText}", ex);
        }
    }

    private string ExtractJson(string text)
    {
        // ```json ... ``` 형태 처리
        var startMarker = "```json";
        var endMarker = "```";
        
        var startIndex = text.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (startIndex >= 0)
        {
            startIndex += startMarker.Length;
            var endIndex = text.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex > startIndex)
            {
                return text.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }

        // JSON 마커가 없으면 원본 반환
        return text.Trim();
    }
}

