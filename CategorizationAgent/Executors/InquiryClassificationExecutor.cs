using CategorizationAgent.DTOs;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace CategorizationAgent.Executors;

public class InquiryClassificationExecutor : Executor<List<Inquiry>, List<ClassificationResult>>
{
    private readonly ChatClientAgent _chatAgent;

    public InquiryClassificationExecutor(AIAgent agent) : base("InquiryClassificationExecutor")
    {
        if (agent is not ChatClientAgent chatAgent)
        {
            throw new ArgumentException("InquiryClassificationExecutor requires a ChatClientAgent.", nameof(agent));
        }

        _chatAgent = chatAgent;
    }

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
                var agentInput = $"문의 ID: {inquiry.Id}\n사용자: {inquiry.UserId}\n내용: {inquiry.Description}";

                var isolatedThread = _chatAgent.GetNewThread();

                var response = await _chatAgent.RunAsync<ClassificationResult>(
                    agentInput,
                    isolatedThread,
                    cancellationToken: cancellationToken);

                var classificationResult = response.Result;

                classificationResult.InquiryId = inquiry.Id;
                classificationResult.InquiryDescription = inquiry.Description ?? "";
                classificationResult.UserId = inquiry.UserId ?? string.Empty;

                results.Add(classificationResult);

                Console.WriteLine($"[분류 완료] ID: {inquiry.Id}, 카테고리: {classificationResult.CategoryName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[분류 실패] ID: {inquiry.Id}, 오류: {ex.Message}");

                results.Add(new ClassificationResult
                {
                    InquiryId = inquiry.Id,
                    InquiryDescription = inquiry.Description ?? "",
                    UserId = inquiry.UserId ?? string.Empty,
                    CategoryId = 99,
                    CategoryName = "기타/분류 불가",
                    Confidence = 0.0,
                    Reason = $"분류 중 오류 발생: {ex.Message}",
                    Keywords = new List<string>()
                });
            }
        }

        return results;
    }
}