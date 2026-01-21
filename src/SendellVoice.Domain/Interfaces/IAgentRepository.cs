using SendellVoice.Domain.Entities;

namespace SendellVoice.Domain.Interfaces;

/// <summary>
/// Repository interface for Agent entity operations.
/// </summary>
public interface IAgentRepository : IRepository<Agent>
{
    Task<IReadOnlyList<Agent>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Agent>> GetBySkillAsync(string skill, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Agent>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default);
    Task<Agent?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
