using CategorizationAgent.Data;
using CategorizationAgent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CategorizationAgent.Services;

public class MailboxService(LogDbContext context)
{
    public async Task<string> CheckMailStatusAsync(long userId, string messageId)
    {
        MailboxLog? log = await context.MailboxLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.MessageId == messageId);

        if (log == null)
        {
            return "ERROR: 로그가 존재하지 않습니다. 개발자의 확인이 필요합니다.";
        }

        return log.MailState switch
        {
            1 => "INFO: 이미 수령한 보상입니다.",
            2 => "INFO: 만료되거나 삭제된 우편입니다.",
            _ => "INFO: 수령 가능한 상태이거나 처리되지 않았습니다."
        };
    }
}