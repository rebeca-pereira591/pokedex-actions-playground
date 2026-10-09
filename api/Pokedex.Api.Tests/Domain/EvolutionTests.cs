using Pokedex.Api.Domain;
using Pokedex.Api.Tests.Fixtures;

namespace Pokedex.Api.Tests.Domain;

public class EvolutionTests
{
    // Ids de las cadenas grabadas en fixtures/pokeapi/evolution-chain
    private const int Bulbasaur = 1;
    private const int Pichu = 10;
    private const int Gastly = 40;
    private const int Happiny = 51;
    private const int Eevee = 67;
    private const int Riolu = 232;
    private const int Mew = 78;

    [Fact]
    public void Linear_chain_has_one_pokemon_per_stage()
    {
        var stages = Evolution.Stages(FixtureData.Chain(Gastly));

        Assert.Equal(["Gastly", "Haunter", "Gengar"], stages.Select(s => Assert.Single(s).Name));
    }

    [Fact]
    public void Branching_chain_keeps_all_branches_in_one_stage()
    {
        var stages = Evolution.Stages(FixtureData.Chain(Eevee));

        Assert.Equal(2, stages.Count);
        Assert.Equal(8, stages[1].Count);
        Assert.Contains(stages[1], s => s.Name == "Sylveon");
    }

    [Fact]
    public void Pokemon_without_evolution_is_a_single_stage()
    {
        var stages = Evolution.Stages(FixtureData.Chain(Mew));

        var only = Assert.Single(Assert.Single(stages));
        Assert.Equal(151, only.SpeciesId);
        Assert.Null(only.Trigger);
    }

    [Theory]
    [InlineData(Bulbasaur, "Ivysaur", "Nv. 16")]
    [InlineData(Gastly, "Gengar", "Intercambio")]
    [InlineData(Pichu, "Pikachu", "Amistad")]
    [InlineData(Pichu, "Raichu", "Piedra Trueno")]
    [InlineData(Riolu, "Lucario", "Amistad, de día")]
    [InlineData(Happiny, "Chansey", "Con Piedra Oval, de día")]
    [InlineData(Eevee, "Umbreon", "Amistad, de noche")]
    [InlineData(Eevee, "Sylveon", "Amistad y mov. tipo Hada")]
    public void Describes_how_each_pokemon_evolves(int chain, string pokemon, string expected)
    {
        var step = Evolution.Stages(FixtureData.Chain(chain)).SelectMany(s => s).Single(s => s.Name == pokemon);

        Assert.Equal(expected, step.Trigger);
    }

    [Fact]
    public void With_several_valid_methods_uses_the_default_one()
    {
        // Leafeon tiene 6 formas en PokeAPI: subir de nivel cerca de una roca en distintos juegos,
        // y usar Piedra Hoja, que es la marcada como predeterminada.
        var leafeon = Evolution.Stages(FixtureData.Chain(Eevee))[1].Single(s => s.Name == "Leafeon");

        Assert.Equal("Piedra Hoja", leafeon.Trigger);
    }
}
