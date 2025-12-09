using CategorizationAgent.DTOs;
using CategorizationAgent.Enums;
using Microsoft.Extensions.DependencyInjection; // 필수 추가

namespace CategorizationAgent.Services;

/// <summary>
/// 초보자 가이드 보상 수령 여부를 확인하는 서비스
/// </summary>
public class BeginnerRewardService(IServiceScopeFactory scopeFactory) // ✅ 변경: 개별 서비스 대신 팩토리 주입
{
    private const int BEGINNER_REWARD_MESSAGE_ID = 1;

    /// <summary>
    /// 초보자 보상 상태를 확인하고 응답을 생성합니다.
    /// </summary>
    public async Task<BeginnerRewardStatusResponse> CheckRewardStatusAsync(
        ClassificationResult result,
        CancellationToken cancellationToken = default)
    {
        var response = new BeginnerRewardStatusResponse
        {
            InquiryId = result.InquiryId,
            UserId = result.UserId
        };

        // ✅ 중요: 여기서 Scope를 생성합니다. (DB 연결 시작)
        using var scope = scopeFactory.CreateScope();
        
        // Scope 안에서 필요한 서비스들을 꺼냅니다.
        var userNumberService = scope.ServiceProvider.GetRequiredService<UserNumberService>();
        var mailboxService = scope.ServiceProvider.GetRequiredService<MailboxService>();

        try
        {
            // 1. UserId로 UserNumber 조회
            var userNumber = await userNumberService.GetByUserIdAsync(result.UserId);
            
            if (userNumber == null)
            {
                response.IsSuccess = false;
                response.ResponseMessage = "ERROR: 사용자 정보를 찾을 수 없습니다. 개발자의 확인이 필요합니다.";
                Console.WriteLine($"[보상 상태 확인 실패] InquiryId: {result.InquiryId}, UserId: {result.UserId} - 사용자 정보 없음");
                return response;
            }

            response.UserNumberId = userNumber.Id;

            // 2. 메일함 상태 확인 및 응답 메시지 생성
            // (mailboxService를 인자로 넘겨줍니다)
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

        return response; // 여기서 using 블록이 끝나며 DB 연결도 닫힙니다.
    }

    /// <summary>
    /// 메일함 상태를 확인하는 메서드
    /// </summary>
    private async Task<(MailStatus? status, string message)> CheckMailStatusAsync(
        MailboxService mailboxService, // ✅ 변경: 메서드 인자로 서비스를 받음
        int userNumberId)
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