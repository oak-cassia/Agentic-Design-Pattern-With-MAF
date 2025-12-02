using CategorizationAgent.Data;
using CategorizationAgent.Data.Models;
using CategorizationAgent.Enums;
using Microsoft.EntityFrameworkCore;

namespace CategorizationAgent.Services;

public class MailboxService(LogDbContext context)
{
    public async Task<MailStatus?> CheckMailStatusAsync(long userId, int messageId)
    {
        MailboxLog? log = await context.MailboxLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.MessageId == messageId);

        return log?.MailState;
    }
}