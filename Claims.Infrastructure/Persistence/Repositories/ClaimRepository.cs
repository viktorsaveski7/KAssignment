using Claims.Application.Common.Interfaces;
using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence.Repositories;

public sealed class ClaimRepository : IClaimRepository
{
    private readonly ClaimsContext _context;

    public ClaimRepository(ClaimsContext context) => _context = context;

    public async Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Claims.ToListAsync(cancellationToken);

    public async Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _context.Claims.SingleOrDefaultAsync(claim => claim.Id == id, cancellationToken);

    public async Task AddAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        _context.Claims.Add(claim);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        _context.Claims.Remove(claim);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
