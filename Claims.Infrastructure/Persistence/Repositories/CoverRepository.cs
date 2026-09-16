using Claims.Application.Common.Interfaces;
using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence.Repositories;

public sealed class CoverRepository : ICoverRepository
{
    private readonly ClaimsContext _context;

    public CoverRepository(ClaimsContext context) => _context = context;

    public async Task<IReadOnlyList<Cover>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Covers.ToListAsync(cancellationToken);

    public async Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _context.Covers.SingleOrDefaultAsync(cover => cover.Id == id, cancellationToken);

    public async Task AddAsync(Cover cover, CancellationToken cancellationToken = default)
    {
        _context.Covers.Add(cover);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Cover cover, CancellationToken cancellationToken = default)
    {
        _context.Covers.Remove(cover);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
