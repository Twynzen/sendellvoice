using Microsoft.EntityFrameworkCore;
using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Interfaces;

namespace SendellVoice.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Agent entity.
/// </summary>
public class AgentRepository : BaseRepository<Agent>, IAgentRepository
{
    public AgentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Agent>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.IsAvailable && a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Agent>> GetBySkillAsync(string skill, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.IsActive && a.Skills.Contains(skill))
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Agent>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.IsActive && a.Department == department)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Agent?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);
    }
}
