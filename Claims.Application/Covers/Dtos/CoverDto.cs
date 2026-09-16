using Claims.Domain.Entities;
using Claims.Domain.Enums;

namespace Claims.Application.Covers.Dtos;

public record CoverDto(
    string Id,
    DateTime StartDate,
    DateTime EndDate,
    CoverType Type,
    decimal Premium)
{
    public static CoverDto FromEntity(Cover cover) =>
        new(cover.Id, cover.StartDate, cover.EndDate, cover.Type, cover.Premium);
}
