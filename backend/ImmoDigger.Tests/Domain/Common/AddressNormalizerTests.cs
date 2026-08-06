using ImmoDigger.Domain.Common;

namespace ImmoDigger.Tests.Domain.Common;

public class AddressNormalizerTests
{
    [Fact]
    public void Normalize_IsCaseInsensitive()
    {
        var a = AddressNormalizer.Normalize("Avenue Coghen", "1180", "Uccle");
        var b = AddressNormalizer.Normalize("AVENUE COGHEN", "1180", "UCCLE");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Normalize_ExpandsCommonStreetAbbreviations()
    {
        var abbreviated = AddressNormalizer.Normalize("Av. Coghen", "1180", "Uccle");
        var expanded = AddressNormalizer.Normalize("Avenue Coghen", "1180", "Uccle");

        Assert.Equal(expanded, abbreviated);
    }

    [Fact]
    public void Normalize_IgnoresAccentsAndPunctuation()
    {
        var a = AddressNormalizer.Normalize("Chaussée de Haecht, 12", "1030", "Schaerbeek");
        var b = AddressNormalizer.Normalize("Chaussee de Haecht 12", "1030", "Schaerbeek");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Normalize_DifferentAddresses_ProduceDifferentKeys()
    {
        var a = AddressNormalizer.Normalize("Avenue Coghen", "1180", "Uccle");
        var b = AddressNormalizer.Normalize("Rue Wayez", "1070", "Anderlecht");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Normalize_DifferentPostalCode_ProducesDifferentKey_EvenWithSameStreetName()
    {
        var a = AddressNormalizer.Normalize("Rue de la Source", "1060", "Saint-Gilles");
        var b = AddressNormalizer.Normalize("Rue de la Source", "1000", "Bruxelles");

        Assert.NotEqual(a, b);
    }
}
