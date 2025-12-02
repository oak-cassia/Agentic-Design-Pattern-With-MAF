using CategorizationAgent.DTOs;
using CategorizationAgent.Enums;
using CategorizationAgent.Services;
using Microsoft.Agents.AI.Workflows;

namespace CategorizationAgent.Executors;

/// <summary>
/// CategoryActionResponseBase 응답을 받아 inquiries.csv 파일의 status와 responseMessage를 업데이트하는 Executor
/// </summary>
public class InquiryStatusUpdateExecutor(string filePath, CsvService csvService) 
    : Executor<List<CategoryActionResponseBase>, bool>("InquiryStatusUpdateExecutor")
{
    public async override ValueTask<bool> HandleAsync(
        List<CategoryActionResponseBase> responses,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[InquiryStatusUpdateExecutor] 시작 - {responses.Count}개 응답 처리");

        try
        {
            // 1. 응답을 CsvService가 필요로 하는 형식으로 변환
            var updates = responses.ToDictionary(
                r => r.InquiryId,
                r => (
                    r.IsSuccess ? InquiryStatus.Resolved : InquiryStatus.OnHold,
                    r.ResponseMessage
                )
            );

            // 2. CsvService를 통해 업데이트
            var result = await csvService.UpdateInquiriesAsync(filePath, updates, cancellationToken);

            Console.WriteLine($"[InquiryStatusUpdateExecutor] 완료");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InquiryStatusUpdateExecutor] 오류 발생: {ex.Message}");
            throw;
        }
    }
}

