using BetterGenshinImpact.ViewModel;

namespace BetterGenshinImpact.UnitTest.GameTaskTests.MapMaskTests;

/// <summary>
/// 点位类型搜索与方案比较
/// </summary>
public class MapLabelSearchTests
{
    [Theory]
    [InlineData("Baú Comum", "Baús", "bau", true)]
    [InlineData("Fragmento de Cristal", "Minérios", "CRISTAL", true)]
    [InlineData("Pedaço de Ferro", "Minérios", "minerio", true)]
    [InlineData("Anemoculus", "Oculi", "cristal", false)]
    [InlineData("水晶块", "矿物", "水晶", true)]
    public void MatchesIgnoresCaseAndAccents(string name, string category, string query, bool expected)
    {
        Assert.Equal(expected, MapLabelSearch.Matches(name, category, MapLabelSearch.Normalize(query)));
    }

    [Fact]
    public void SameSetIgnoresOrderAndDuplicates()
    {
        Assert.True(MapLabelSelection.SameSet(["a", "b", "c"], ["c", "a", "b", "a"]));
        Assert.False(MapLabelSelection.SameSet(["a", "b"], ["a", "b", "c"]));
        Assert.True(MapLabelSelection.SameSet([], []));
    }
}
