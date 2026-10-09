using System.Globalization;
using Pokedex.Api.Contracts;
using Pokedex.Api.Domain;
using Pokedex.Api.PokeApi;

namespace Pokedex.Api.Services;

public sealed class InvalidQueryException(string message) : Exception(message);

// Junta varias respuestas de PokeAPI en lo que pide cada pantalla.
public sealed class PokedexService(PokeApiClient pokeApi)
{
    public const int MaxPageSize = 60;
    public const int Generations = 9;

    public static IReadOnlyList<TypeDto> GetTypes() => PokemonTypes.All.Select(ToTypeDto).ToList();

    public async Task<PokemonPage> GetPageAsync(PokemonQuery query, CancellationToken ct)
    {
        Validate(query);

        var index = await pokeApi.GetSpeciesIndexAsync(ct);
        IEnumerable<NamedResource> species = index.Results;

        if (query.Type is { } type)
        {
            var ofType = (await pokeApi.GetTypeAsync(type, ct)).Pokemon.Select(p => p.Pokemon.Id).ToHashSet();
            species = species.Where(s => ofType.Contains(s.Id));
        }

        if (query.Generation is { } generation)
        {
            var ofGeneration = (await pokeApi.GetGenerationAsync(generation, ct)).PokemonSpecies.Select(s => s.Id).ToHashSet();
            species = species.Where(s => ofGeneration.Contains(s.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            species = species.Where(Matches(query.Q));
        }

        var matching = species.OrderBy(s => s.Id).ToList();
        var pageIds = matching.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(s => s.Id);
        var cards = await Task.WhenAll(pageIds.Select(id => GetCardAsync(id, ct)));

        return new PokemonPage(cards, matching.Count, query.Page, query.PageSize);
    }

    public async Task<PokemonDetail> GetDetailAsync(string idOrName, CancellationToken ct)
    {
        var id = await ResolveIdAsync(idOrName, ct);
        var pokemonTask = pokeApi.GetPokemonAsync(id, ct);
        var speciesTask = pokeApi.GetSpeciesAsync(id, ct);
        var pokemon = await pokemonTask;
        var species = await speciesTask;

        var chainTask = pokeApi.GetEvolutionChainAsync(species.EvolutionChain.Id, ct);
        var defending = await Task.WhenAll(OrderedTypes(pokemon).Select(t => DefendingTypeAsync(t, ct)));
        var chain = await chainTask;

        var card = ToCard(pokemon, species);
        return new PokemonDetail(
            card.Id,
            card.Name,
            card.Types,
            card.ArtworkUrl,
            card.SpeciesColor,
            card.Total,
            card.IsLegendary,
            card.IsMythical,
            species.Generation.Id,
            species.Genera.LastOrDefault(g => g.Language.Name == Localization.Spanish)?.Genus,
            Localization.PickSpanish(species.FlavorTextEntries.Select(e => new LocalizedText(e.Language.Name, e.FlavorText))),
            Measurements.DecimetersToMeters(pokemon.Height),
            Measurements.HectogramsToKilograms(pokemon.Weight),
            ToStats(pokemon),
            TypeEffectiveness.Weaknesses(defending)
                .Select(w => new WeaknessDto(ToTypeDto(w.Type), w.Multiplier))
                .ToList(),
            Evolution.Stages(chain.Chain)
                .Select(stage => (IReadOnlyList<EvolutionStepDto>)stage
                    .Select(s => new EvolutionStepDto(s.SpeciesId, s.Name, Sprites.OfficialArtwork(s.SpeciesId), s.Trigger))
                    .ToList())
                .ToList(),
            pokemon.Cries?.Latest ?? pokemon.Cries?.Legacy);
    }

    private static PokemonCard ToCard(ApiPokemon pokemon, ApiSpecies species) => new(
        pokemon.Id,
        species.Names.FirstOrDefault(n => n.Language.Name == Localization.Spanish)?.Name ?? Localization.TitleFromSlug(species.Name),
        OrderedTypes(pokemon).Select(ToTypeDto).ToList(),
        pokemon.Sprites.Other.OfficialArtwork.FrontDefault,
        species.Color.Name,
        pokemon.Stats.Sum(s => s.BaseStat),
        species.IsLegendary,
        species.IsMythical);

    private static IEnumerable<string> OrderedTypes(ApiPokemon pokemon) =>
        pokemon.Types.OrderBy(t => t.Slot).Select(t => t.Type.Name);

    private static TypeDto ToTypeDto(string name) => new(name, PokemonTypes.Label(name));

    private static Stats ToStats(ApiPokemon pokemon)
    {
        int Stat(string name) => pokemon.Stats.FirstOrDefault(s => s.Stat.Name == name)?.BaseStat ?? 0;
        return new Stats(Stat("hp"), Stat("attack"), Stat("defense"), Stat("special-attack"), Stat("special-defense"), Stat("speed"));
    }

    // "gen" encuentra Gengar; "94", "#94" y "#0094" encuentran el 94.
    private static Func<NamedResource, bool> Matches(string q)
    {
        var term = q.Trim().TrimStart('#').ToLowerInvariant();
        var isNumber = int.TryParse(term, NumberStyles.None, CultureInfo.InvariantCulture, out var number);
        return s => isNumber ? s.Id == number : s.Name.Contains(term, StringComparison.Ordinal);
    }

    private static void Validate(PokemonQuery query)
    {
        if (query.Page < 1) throw new InvalidQueryException("page tiene que ser 1 o más.");
        if (query.PageSize is < 1 or > MaxPageSize) throw new InvalidQueryException($"pageSize tiene que estar entre 1 y {MaxPageSize}.");
        if (query.Type is { } type && !PokemonTypes.IsKnown(type)) throw new InvalidQueryException($"'{type}' no es un tipo.");
        if (query.Generation is < 1 or > Generations) throw new InvalidQueryException($"generation tiene que estar entre 1 y {Generations}.");
    }

    private async Task<PokemonCard> GetCardAsync(int id, CancellationToken ct)
    {
        var pokemonTask = pokeApi.GetPokemonAsync(id, ct);
        var speciesTask = pokeApi.GetSpeciesAsync(id, ct);
        return ToCard(await pokemonTask, await speciesTask);
    }

    private async Task<int> ResolveIdAsync(string idOrName, CancellationToken ct)
    {
        if (int.TryParse(idOrName, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            return id;
        }

        var index = await pokeApi.GetSpeciesIndexAsync(ct);
        var match = index.Results.FirstOrDefault(s => string.Equals(s.Name, idOrName, StringComparison.OrdinalIgnoreCase));
        return match?.Id ?? throw new PokeApiNotFoundException($"pokemon-species/{idOrName}");
    }

    private async Task<DefendingType> DefendingTypeAsync(string name, CancellationToken ct)
    {
        var relations = (await pokeApi.GetTypeAsync(name, ct)).DamageRelations;
        return new DefendingType(
            name,
            relations.DoubleDamageFrom.Select(t => t.Name).ToHashSet(),
            relations.HalfDamageFrom.Select(t => t.Name).ToHashSet(),
            relations.NoDamageFrom.Select(t => t.Name).ToHashSet());
    }
}
