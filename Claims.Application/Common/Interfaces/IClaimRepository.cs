using Claims.Domain.Entities;

namespace Claims.Application.Common.Interfaces;

public interface IClaimRepository
{
    Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task AddAsync(Claim claim, CancellationToken cancellationToken = default);

    Task DeleteAsync(Claim claim, CancellationToken cancellationToken = default);
}
