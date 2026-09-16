using Claims.Domain.Enums;

namespace Claims.Domain.Entities;

public class Claim
{
    private Claim()
    {
        Id = string.Empty;
        CoverId = string.Empty;
        Name = string.Empty;
    }

    private Claim(string id, string coverId, string name, ClaimType type, DateTime created, decimal damageCost)
    {
        Id = id;
        CoverId = coverId;
        Name = name;
        Type = type;
        Created = created;
        DamageCost = damageCost;
    }

    public string Id { get; private set; }

    public string CoverId { get; private set; }

    public DateTime Created { get; private set; }

    public string Name { get; private set; }

    public ClaimType Type { get; private set; }

    public decimal DamageCost { get; private set; }

    public static Claim Create(string coverId, string name, ClaimType type, DateTime created, decimal damageCost) =>
        new(Guid.NewGuid().ToString(), coverId, name, type, created, damageCost);
}
