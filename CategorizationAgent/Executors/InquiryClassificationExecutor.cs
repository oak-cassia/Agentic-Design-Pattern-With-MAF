using CategorizationAgent.DTOs;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

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

        // AIAgent를 ChatClientAgent로 캐스팅하여 구조화된 출력 기능을 사용합니다.
        if (agent is not ChatClientAgent chatAgent)
        {
            throw new InvalidOperationException("InquiryClassificationExecutor requires a ChatClientAgent.");
        }

        foreach (var inquiry in inquiries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // 문의 내용을 Agent에 전달
                var agentInput = $"문의 ID: {inquiry.Id}\n사용자: {inquiry.UserId}\n내용: {inquiry.Description}";
                
                // CRITICAL FIX:
                // Create a NEW thread for each inquiry to ensure context isolation.
                // If we reuse a thread, the agent remembers previous inquiries, 
                // polluting context and wasting tokens.
                var isolatedThread = chatAgent.GetNewThread();

                // AIAgent 실행 (구조화된 출력 요청)
                // Pass the isolatedThread to RunAsync
                var response = await chatAgent.RunAsync<ClassificationResult>(
                    agentInput, 
                    isolatedThread, 
                    cancellationToken: cancellationToken);
                
                var classificationResult = response.Result;

                // AI가 생성하지 않는 컨텍스트 정보(ID, 원본 내용)를 채웁니다.
                classificationResult.InquiryId = inquiry.Id;
                classificationResult.InquiryDescription = inquiry.Description ?? "";
                classificationResult.UserId = inquiry.UserId ?? string.Empty;

                results.Add(classificationResult);
                
                Console.WriteLine($"[분류 완료] ID: {inquiry.Id}, 카테고리: {classificationResult.CategoryName}");

                // Optional: Explicitly delete the thread if the underlying storage requires cleanup
                // await chatAgent.DeleteThreadAsync(isolatedThread.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[분류 실패] ID: {inquiry.Id}, 오류: {ex.Message}");
                
                // 실패한 경우 기본 결과 추가
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