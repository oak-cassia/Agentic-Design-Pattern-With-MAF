using CategorizationAgent.Enums;

namespace CategorizationAgent.DTOs;

public class BeginnerRewardStatusResponse : CategoryActionResponseBase
{
    public MailStatus? MailStatus { get; set; }
}