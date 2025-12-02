using CategorizationAgent.DTOs;
using CategorizationAgent.Enums;
using CategorizationAgent.Services;
using Microsoft.Agents.AI.Workflows;

namespace CategorizationAgent.Executors;

/// <summary>
/// 초보자 가이드 보상 수령 여부를 확인하는 Executor
/// ClassificationResult 리스트를 받아 각 문의에 대해 메일함 상태를 확인하고 응답을 생성합니다.
/// </summary>
public class BeginnerRewardStatusExecutor(
    UserNumberService userNumberService,
    MailboxService mailboxService) 
    : Executor<List<ClassificationResult>, List<BeginnerRewardStatusResponse>>("BeginnerRewardStatusExecutor")
{
    private const int BEGINNER_REWARD_MESSAGE_ID = 1; // 초보자 가이드 보상 메시지 ID

    public async override ValueTask<List<BeginnerRewardStatusResponse>> HandleAsync(
        List<ClassificationResult> classificationResults,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var responses = new List<BeginnerRewardStatusResponse>();

        foreach (var result in classificationResults)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var response = new BeginnerRewardStatusResponse
                {
                    InquiryId = result.InquiryId,
                    UserId = result.UserId
                };

                // 1. UserId로 UserNumber 조회
                var userNumber = await userNumberService.GetByUserIdAsync(result.UserId);
                
                if (userNumber == null)
                {
                    response.IsSuccess = false;
                    response.ResponseMessage = "ERROR: 사용자 정보를 찾을 수 없습니다. 개발자의 확인이 필요합니다.";
                    responses.Add(response);
                    Console.WriteLine($"[보상 상태 확인 실패] InquiryId: {result.InquiryId}, UserId: {result.UserId} - 사용자 정보 없음");
                    continue;
                }

                response.UserNumberId = userNumber.Id;

                // 2. 메일함 상태 확인 및 응답 메시지 생성
                var (mailStatus, message) = await CheckMailStatusAsync(userNumber.Id);
                response.MailStatus = mailStatus;
                response.ResponseMessage = message;
                response.IsSuccess = !message.StartsWith("ERROR");

                responses.Add(response);
                
                Console.WriteLine($"[보상 상태 확인 완료] InquiryId: {result.InquiryId}, UserId: {result.UserId}, Status: {mailStatus}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[보상 상태 확인 실패] InquiryId: {result.InquiryId}, 오류: {ex.Message}");
                
                responses.Add(new BeginnerRewardStatusResponse
                {
                    InquiryId = result.InquiryId,
                    UserId = result.UserId,
                    UserNumberId = 0,
                    IsSuccess = false,
                    MailStatus = null,
                    ResponseMessage = $"시스템 오류가 발생했습니다: {ex.Message}"
                });
            }
        }

        return responses;
    }

    /// <summary>
    /// 메일함 상태를 확인하는 메서드
    /// UserNumber.Id를 사용하여 MailboxLog에서 mail_state를 확인합니다.
    /// </summary>
    private async Task<(MailStatus? status, string message)> CheckMailStatusAsync(int userNumberId)
    {
        try
        {
            var mailStatus = await mailboxService.CheckMailStatusAsync(userNumberId, BEGINNER_REWARD_MESSAGE_ID);
            
            var message = mailStatus switch
            {
                MailStatus.Received => "이미 수령한 보상입니다.",
                MailStatus.Expired => "우편함 만료로 삭제되었습니다.",
                MailStatus.Pending => "우편함 확인 안내 부탁드립니다.",
                null => "보상을 실제로 보낸 적이 있는지 확인 필요",
                _ => "알 수 없는 상태입니다."
            };
            
            return (mailStatus, message);
        }
        catch (Exception ex)
        {
            return (null, $"ERROR: 메일 상태 확인 중 오류 발생 - {ex.Message}");
        }
    }
}

