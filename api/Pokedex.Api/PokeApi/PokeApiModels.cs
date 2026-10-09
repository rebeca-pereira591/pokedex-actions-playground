using System.Text.Json.Serialization;

namespace Pokedex.Api.PokeApi;

// Forma de las respuestas de PokeAPI, sólo con los campos que usa la app.
// Los nombres en JSON van en snake_case (ver PokeApiJson).
public sealed record NamedResource(string Name, string Url)
{
    public int Id => ResourceUrl.IdFrom(Url);
}

public sealed record ResourceRef(string Url)
{
    public int Id => ResourceUrl.IdFrom(Url);
}

public sealed record NameOnly(string Name);

public sealed record LocalizedName(string Name, NamedResource Language);

public sealed record SpeciesIndex(int Count, IReadOnlyList<NamedResource> Results);

public sealed record ApiPokemon(
    int Id,
    string Name,
    int Height,
    int Weight,
    IReadOnlyList<PokemonTypeSlot> Types,
    IReadOnlyList<PokemonStat> Stats,
    NamedResource Species,
    PokemonSprites Sprites,
    PokemonCries? Cries);

public sealed record PokemonTypeSlot(int Slot, NamedResource Type);

public sealed record PokemonStat(int BaseStat, NameOnly Stat);

public sealed record PokemonSprites(OtherSprites Other);

public sealed record OtherSprites([property: JsonPropertyName("official-artwork")] Artwork OfficialArtwork);

public sealed record Artwork(string? FrontDefault);

public sealed record PokemonCries(string? Latest, string? Legacy);

public sealed record ApiSpecies(
    int Id,
    string Name,
    NamedResource Color,
    NamedResource Generation,
    bool IsLegendary,
    bool IsMythical,
    IReadOnlyList<GenusEntry> Genera,
    IReadOnlyList<LocalizedName> Names,
    IReadOnlyList<FlavorTextEntry> FlavorTextEntries,
    ResourceRef EvolutionChain);

public sealed record GenusEntry(string Genus, NamedResource Language);

public sealed record FlavorTextEntry(string FlavorText, NamedResource Language, NamedResource Version);

public sealed record ApiEvolutionChain(int Id, ChainLink Chain);

public sealed record ChainLink(
    NamedResource Species,
    IReadOnlyList<EvolutionDetail> EvolutionDetails,
    IReadOnlyList<ChainLink> EvolvesTo);

public sealed record EvolutionDetail(
    NamedResource Trigger,
    NamedResource? Item,
    NamedResource? HeldItem,
    NamedResource? KnownMoveType,
    NamedResource? Location,
    int? MinLevel,
    int? MinHappiness,
    int? MinAffection,
    string? TimeOfDay,
    bool IsDefault);

public sealed record ApiType(
    int Id,
    string Name,
    DamageRelations DamageRelations,
    IReadOnlyList<TypePokemon> Pokemon);

public sealed record DamageRelations(
    IReadOnlyList<NameOnly> DoubleDamageFrom,
    IReadOnlyList<NameOnly> HalfDamageFrom,
    IReadOnlyList<NameOnly> NoDamageFrom);

public sealed record TypePokemon(NamedResource Pokemon);

public sealed record ApiGeneration(int Id, string Name, IReadOnlyList<NamedResource> PokemonSpecies);

public static class ResourceUrl
{
    // "https://pokeapi.co/api/v2/pokemon-species/94/" -> 94
    public static int IdFrom(string url)
    {
        var segment = url.TrimEnd('/').Split('/')[^1];
        return int.Parse(segment, System.Globalization.CultureInfo.InvariantCulture);
    }
}
