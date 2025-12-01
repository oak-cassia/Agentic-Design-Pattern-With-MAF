using CategorizationAgent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CategorizationAgent.Data;

public class LogDbContext(DbContextOptions<LogDbContext> options) : DbContext(options)
{
    public DbSet<MailboxLog> MailboxLogs => Set<MailboxLog>();
    public DbSet<UserNumber> UserNumbers => Set<UserNumber>();
}
