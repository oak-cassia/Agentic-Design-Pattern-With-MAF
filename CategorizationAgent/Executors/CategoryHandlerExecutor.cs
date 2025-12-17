using CategorizationAgent.DTOs;
using CategorizationAgent.Services;
using Microsoft.Agents.AI.Workflows;

namespace CategorizationAgent.Executors;

public class CategoryHandlerExecutor(
    BeginnerRewardService beginnerRewardService,
    CategoryActionService categoryActionService)
    : Executor<List<ClassificationResult>, List<CategoryActionResponseBase>>("CategoryHandlerExecutor")
{
    public async override ValueTask<List<CategoryActionResponseBase>> HandleAsync(
        List<ClassificationResult> classificationResults,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var responses = new List<CategoryActionResponseBase>();

        foreach (var result in classificationResults)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                CategoryActionResponseBase response;

                if (result.CategoryId == 1)
                {
                    response = await beginnerRewardService.CheckRewardStatusAsync(result, cancellationToken);
                }
                else
                {
                    response = categoryActionService.GetCategoryAction(result);
                }

                responses.Add(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[카테고리 처리 실패] InquiryId: {result.InquiryId}, CategoryId: {result.CategoryId}, 오류: {ex.Message}");

                responses.Add(new CategoryActionResponse
                {
                    InquiryId = result.InquiryId,
                    UserId = result.UserId,
                    IsSuccess = false,
                    ResponseMessage = $"처리 중 오류가 발생했습니다: {ex.Message}"
                });
            }
        }

        return responses;
    }
}