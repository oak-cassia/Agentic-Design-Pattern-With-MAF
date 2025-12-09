using CategorizationAgent.DTOs;
using CategorizationAgent.Services;
using Microsoft.Agents.AI.Workflows;

namespace CategorizationAgent.Executors;

/// <summary>
/// 분류 결과를 받아서 카테고리 ID에 따라 적절한 처리를 수행하는 통합 Executor
/// - CategoryId == 1: 초보자 보상 상태 확인 (BeginnerRewardService)
/// - CategoryId == 2~8, 99: 카테고리별 처리 방법 조회 (CategoryActionService)
/// </summary>
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

                // CategoryId에 따라 다른 처리
                if (result.CategoryId == 1)
                {
                    // 초보자 보상 상태 확인
                    response = await beginnerRewardService.CheckRewardStatusAsync(result, cancellationToken);
                }
                else
                {
                    // 일반 카테고리 처리 방법 조회
                    response = categoryActionService.GetCategoryAction(result);
                }

                responses.Add(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[카테고리 처리 실패] InquiryId: {result.InquiryId}, CategoryId: {result.CategoryId}, 오류: {ex.Message}");
                
                // 실패한 경우 기본 응답 추가
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

