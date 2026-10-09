using Pokedex.Api.Domain;
using Pokedex.Api.Tests.Fixtures;

namespace Pokedex.Api.Tests.Domain;

public class TypeEffectivenessTests
{
    [Fact]
    public void Gengar_is_immune_to_normal_and_fighting_and_takes_a_quarter_from_poison_and_bug()
    {
        var gengar = WeaknessesOf("ghost", "poison");

        Assert.Equal(0, gengar["normal"]);
        Assert.Equal(0, gengar["fighting"]);
        Assert.Equal(0.25, gengar["poison"]);
        Assert.Equal(0.25, gengar["bug"]);
        Assert.Equal(2, gengar["ground"]);
        Assert.Equal(2, gengar["psychic"]);
        Assert.Equal(2, gengar["ghost"]);
        Assert.Equal(2, gengar["dark"]);
    }

    [Fact]
    public void Bulbasaur_takes_a_quarter_from_grass()
    {
        var bulbasaur = WeaknessesOf("grass", "poison");

        Assert.Equal(0.25, bulbasaur["grass"]);
        Assert.Equal(2, bulbasaur["fire"]);
        Assert.Equal(2, bulbasaur["psychic"]);
    }

    [Theory]
    [InlineData("electric", new[] { "water", "flying" })] // Gyarados
    [InlineData("rock", new[] { "fire", "flying" })] // Charizard
    [InlineData("ice", new[] { "dragon", "ground" })] // Garchomp
    public void Double_weakness_on_both_types_is_x4(string attacking, string[] defending)
    {
        Assert.Equal(4, WeaknessesOf(defending)[attacking]);
    }

    [Fact]
    public void An_immunity_cancels_a_weakness_of_the_other_type()
    {
        // Sableye: Fantasma recibe x2 de Psíquico, pero Siniestro es inmune (x0). 2 × 0 = 0
        Assert.Equal(0, WeaknessesOf("dark", "ghost")["psychic"]);
    }

    [Fact]
    public void Only_non_neutral_multipliers_are_listed()
    {
        var pikachu = WeaknessesOf("electric");

        Assert.Equivalent(
            new Dictionary<string, double> { ["electric"] = 0.5, ["flying"] = 0.5, ["ground"] = 2, ["steel"] = 0.5 },
            pikachu,
            strict: true);
    }

    [Fact]
    public void Fire_attack_against_a_grass_pokemon_is_super_effective()
    {
        Assert.Equal(2, TypeEffectiveness.AgainstAll("fire", [FixtureData.Type("grass")]));
    }

    [Fact]
    public void Single_type_against_itself_uses_the_type_table()
    {
        Assert.Equal(0.5, TypeEffectiveness.Against("fire", FixtureData.Type("fire")));
        Assert.Equal(1, TypeEffectiveness.Against("normal", FixtureData.Type("fire")));
    }

    private static Dictionary<string, double> WeaknessesOf(params string[] types) =>
        TypeEffectiveness.Weaknesses(types.Select(FixtureData.Type).ToList())
            .ToDictionary(m => m.Type, m => m.Multiplier);
}
