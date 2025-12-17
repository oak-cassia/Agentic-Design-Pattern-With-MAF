using CategorizationAgent.DTOs;
using CategorizationAgent.Enums;
using CategorizationAgent.Services;
using Microsoft.Agents.AI.Workflows;

namespace CategorizationAgent.Executors;

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
            var updates = responses.ToDictionary(
                r => r.InquiryId,
                r => (
                    r.IsSuccess
                        ? InquiryStatus.Resolved
                        : InquiryStatus.OnHold,
                    r.ResponseMessage
                )
            );

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