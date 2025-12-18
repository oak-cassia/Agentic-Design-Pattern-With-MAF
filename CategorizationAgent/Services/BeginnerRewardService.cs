using CategorizationAgent.DTOs;
using CategorizationAgent.Enums;

namespace CategorizationAgent.Services;

public class BeginnerRewardService(IServiceScopeFactory scopeFactory)
{
    private const int BEGINNER_REWARD_MESSAGE_ID = 1;

    public async Task<BeginnerRewardStatusResponse> CheckRewardStatusAsync(
        ClassificationResult result,
        CancellationToken cancellationToken = default)
    {
        var response = new BeginnerRewardStatusResponse
        {
            InquiryId = result.InquiryId,
            UserId = result.UserId
        };

        using var scope = scopeFactory.CreateScope();

        var userNumberService = scope.ServiceProvider.GetRequiredService<UserNumberService>();
        var mailboxService = scope.ServiceProvider.GetRequiredService<MailboxService>();

        try
        {
            var userNumber = await userNumberService.GetByUserIdAsync(result.UserId);

            if (userNumber == null)
            {
                response.IsSuccess = false;
                response.ResponseMessage = "ERROR: 사용자 정보를 찾을 수 없습니다. 개발자의 확인이 필요합니다.";
                Console.WriteLine($"[보상 상태 확인 실패] InquiryId: {result.InquiryId}, UserId: {result.UserId} - 사용자 정보 없음");
                return response;
            }

            response.UserNumberId = userNumber.Id;

            var (mailStatus, message) = await CheckMailStatusAsync(mailboxService, userNumber.Id);
            response.MailStatus = mailStatus;
            response.ResponseMessage = message;
            response.IsSuccess = !message.StartsWith("ERROR");

            Console.WriteLine($"[보상 상태 확인 완료] InquiryId: {result.InquiryId}, UserId: {result.UserId}, Status: {mailStatus}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[보상 상태 확인 실패] InquiryId: {result.InquiryId}, 오류: {ex.Message}");

            response.IsSuccess = false;
            response.MailStatus = null;
            response.ResponseMessage = $"시스템 오류가 발생했습니다: {ex.Message}";
        }

        return response;
    }

    private async Task<(MailStatus? status, string message)> CheckMailStatusAsync(
        MailboxService mailboxService,
        int userNumberId)
    {
        try
        {
            var mailStatus = await mailboxService.CheckMailStatusAsync(userNumberId, BEGINNER_REWARD_MESSAGE_ID);

            var message = mailStatus switch
            {
                MailStatus.Received => "[수령 로그 존재] 이미 수령한 보상입니다.",
                MailStatus.Expired => "[만료 로그 존재] 우편함 만료로 삭제되었습니다.",
                MailStatus.Pending => "[우편 존재] 우편함 확인 안내 부탁드립니다.",
                null => "[우편 로그 없음] 보상을 실제로 보낸 적이 있는지 확인 필요",
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