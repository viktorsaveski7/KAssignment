using Claims.Domain.Entities;

namespace Claims.Application.Common.Interfaces;

/// <summary>Persistence operations for covers.</summary>
public interface ICoverRepository
{
    Task<IReadOnlyList<Cover>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task AddAsync(Cover cover, CancellationToken cancellationToken = default);

    Task DeleteAsync(Cover cover, CancellationToken cancellationToken = default);
}
