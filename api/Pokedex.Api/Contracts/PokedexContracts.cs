namespace Pokedex.Api.Contracts;

// Lo que recibe el front. Tiene la forma que necesita cada pantalla, no la de PokeAPI.
public sealed record TypeDto(string Name, string Label);

public sealed record PokemonCard(
    int Id,
    string Name,
    IReadOnlyList<TypeDto> Types,
    string? ArtworkUrl,
    string SpeciesColor,
    int Total,
    bool IsLegendary,
    bool IsMythical);

public sealed record PokemonPage(IReadOnlyList<PokemonCard> Items, int Total, int Page, int PageSize);

public sealed record Stats(int Hp, int Attack, int Defense, int SpecialAttack, int SpecialDefense, int Speed);

public sealed record WeaknessDto(TypeDto Type, double Multiplier);

public sealed record EvolutionStepDto(int Id, string Name, string ArtworkUrl, string? Trigger);

public sealed record PokemonDetail(
    int Id,
    string Name,
    IReadOnlyList<TypeDto> Types,
    string? ArtworkUrl,
    string SpeciesColor,
    int Total,
    bool IsLegendary,
    bool IsMythical,
    int Generation,
    string? Category,
    string? FlavorText,
    double HeightMeters,
    double WeightKg,
    Stats Stats,
    IReadOnlyList<WeaknessDto> Weaknesses,
    IReadOnlyList<IReadOnlyList<EvolutionStepDto>> Evolution,
    string? CryUrl);

public sealed record PokemonQuery(int Page = 1, int PageSize = 20, string? Type = null, int? Generation = null, string? Q = null);
