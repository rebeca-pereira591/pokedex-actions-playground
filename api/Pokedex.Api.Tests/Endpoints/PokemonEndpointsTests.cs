using System.Net;
using System.Net.Http.Json;
using Pokedex.Api.Contracts;
using Pokedex.Api.Tests.Fixtures;

namespace Pokedex.Api.Tests.Endpoints;

public class PokemonEndpointsTests(PokedexApiFactory factory) : IClassFixture<PokedexApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task List_is_ordered_by_number_and_paginated()
    {
        var page = await Get<PokemonPage>("/api/pokemon?page=1&pageSize=5");

        Assert.NotNull(page);
        Assert.Equal(37, page.Total);
        Assert.Equal([1, 2, 3, 4, 5], page.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Card_has_what_the_list_screen_shows()
    {
        var page = await Get<PokemonPage>("/api/pokemon?q=gengar");

        var gengar = Assert.Single(page!.Items);
        Assert.Equal("Gengar", gengar.Name);
        Assert.Equal(["Fantasma", "Veneno"], gengar.Types.Select(t => t.Label));
        Assert.Equal("purple", gengar.SpeciesColor);
        Assert.Equal(500, gengar.Total);
        Assert.EndsWith("/94.png", gengar.ArtworkUrl);
    }

    [Theory]
    [InlineData("?type=ghost", new[] { 92, 93, 94, 302 })]
    [InlineData("?generation=4", new[] { 440, 443, 444, 445, 446, 447, 448, 470, 471 })]
    [InlineData("?q=%230094", new[] { 94 })]
    [InlineData("?type=fairy&generation=6", new[] { 700 })]
    public async Task Filters_combine(string query, int[] expectedIds)
    {
        var page = await Get<PokemonPage>("/api/pokemon" + query + (query.Contains('?') ? "&" : "?") + "pageSize=60");

        Assert.Equal(expectedIds, page!.Items.Select(p => p.Id));
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=61")]
    [InlineData("?type=shadow")]
    [InlineData("?generation=10")]
    public async Task Invalid_filters_are_rejected(string query)
    {
        var response = await _client.GetAsync("/api/pokemon" + query, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Detail_has_everything_the_detail_screen_shows()
    {
        var gengar = await Get<PokemonDetail>("/api/pokemon/94");

        Assert.NotNull(gengar);
        Assert.Equal("Pokémon Sombra", gengar.Category);
        Assert.StartsWith("Dicen que sale de la oscuridad", gengar.FlavorText);
        Assert.Equal(1.5, gengar.HeightMeters);
        Assert.Equal(40.5, gengar.WeightKg);
        Assert.Equal(new Stats(60, 65, 60, 130, 75, 110), gengar.Stats);
        Assert.Equal(1, gengar.Generation);
        Assert.EndsWith("/latest/94.ogg", gengar.CryUrl);
        Assert.Contains(gengar.Weaknesses, w => w.Type.Name == "normal" && w.Multiplier == 0);
        Assert.Contains(gengar.Weaknesses, w => w.Type.Name == "bug" && w.Multiplier == 0.25);
        Assert.Equal(["Gastly", "Haunter", "Gengar"], gengar.Evolution.Select(stage => Assert.Single(stage).Name));
        Assert.Equal("Intercambio", gengar.Evolution[2][0].Trigger);
    }

    [Fact]
    public async Task Detail_accepts_the_name_too()
    {
        var byName = await Get<PokemonDetail>("/api/pokemon/Gengar");

        Assert.Equal(94, byName!.Id);
    }

    [Fact]
    public async Task Legendary_and_mythical_flags_come_from_the_species()
    {
        var mewtwo = await Get<PokemonDetail>("/api/pokemon/150");
        var mew = await Get<PokemonDetail>("/api/pokemon/151");

        Assert.True(mewtwo!.IsLegendary);
        Assert.False(mewtwo.IsMythical);
        Assert.True(mew!.IsMythical);
        Assert.Single(Assert.Single(mew.Evolution));
    }

    [Theory]
    [InlineData("/api/pokemon/99999")]
    [InlineData("/api/pokemon/missingno")]
    public async Task Unknown_pokemon_is_404(string url)
    {
        var response = await _client.GetAsync(url, _ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Types_are_the_18_with_spanish_labels()
    {
        var types = await Get<List<TypeDto>>("/api/types");

        Assert.Equal(18, types!.Count);
        Assert.Equal(new TypeDto("ghost", "Fantasma"), types.Single(t => t.Name == "ghost"));
    }

    private Task<T?> Get<T>(string url) => _client.GetFromJsonAsync<T>(url, _ct);
}
