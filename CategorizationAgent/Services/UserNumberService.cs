using CategorizationAgent.Data;
using CategorizationAgent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CategorizationAgent.Services;

public class UserNumberService(LogDbContext context)
{
    public async Task<UserNumber?> GetByIdAsync(int id) =>
        await context.UserNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<UserNumber?> GetByUserIdAsync(string userId) =>
        await context.UserNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
}

