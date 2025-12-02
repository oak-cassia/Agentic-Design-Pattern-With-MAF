using CategorizationAgent.Enums;

namespace CategorizationAgent.DTOs;

/// <summary>
/// 초보자 가이드 보상 상태 확인 응답 DTO
/// CategoryActionResponseBase를 상속하여 표준화된 응답 형식을 제공합니다.
/// </summary>
public class BeginnerRewardStatusResponse : CategoryActionResponseBase
{
    /// <summary>
    /// 메일함 상태
    /// </summary>
    public MailStatus? MailStatus { get; set; }
}

