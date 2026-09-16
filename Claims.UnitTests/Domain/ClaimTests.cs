using Claims.Domain.Entities;
using Claims.Domain.Enums;

namespace Claims.UnitTests.Domain;

public class ClaimTests
{
    private static readonly DateOnly Created = new(2027, 2, 1);

    [Fact]
    public void Create_assigns_a_unique_identifier()
    {
        var first = Claim.Create("cover-1", "Hull damage", ClaimType.Collision, Created, 500m);
        var second = Claim.Create("cover-1", "Hull damage", ClaimType.Collision, Created, 500m);

        Assert.False(string.IsNullOrWhiteSpace(first.Id));
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Create_stores_the_supplied_values()
    {
        var claim = Claim.Create("cover-7", "Fire in engine room", ClaimType.Fire, Created, 1_234.56m);

        Assert.Equal("cover-7", claim.CoverId);
        Assert.Equal("Fire in engine room", claim.Name);
        Assert.Equal(ClaimType.Fire, claim.Type);
        Assert.Equal(Created, claim.Created);
        Assert.Equal(1_234.56m, claim.DamageCost);
    }
}
