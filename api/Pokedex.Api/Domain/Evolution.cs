using Pokedex.Api.PokeApi;

namespace Pokedex.Api.Domain;

public sealed record EvolutionStep(int SpeciesId, string Name, string? Trigger);

public static class Evolution
{
    private static readonly Dictionary<string, string> Items = new()
    {
        ["thunder-stone"] = "Piedra Trueno",
        ["water-stone"] = "Piedra Agua",
        ["fire-stone"] = "Piedra Fuego",
        ["leaf-stone"] = "Piedra Hoja",
        ["ice-stone"] = "Piedra Hielo",
        ["moon-stone"] = "Piedra Lunar",
        ["sun-stone"] = "Piedra Solar",
        ["shiny-stone"] = "Piedra Día",
        ["dusk-stone"] = "Piedra Noche",
        ["dawn-stone"] = "Piedra Alba",
        ["oval-stone"] = "Piedra Oval",
    };

    // PokeAPI da la evolución como un árbol. Para la ficha se aplana en etapas por profundidad:
    // Gastly -> Haunter -> Gengar son 3 etapas de un Pokémon cada una; Eevee es una etapa con Eevee
    // y otra con sus 8 evoluciones (las ramas).
    public static IReadOnlyList<IReadOnlyList<EvolutionStep>> Stages(ChainLink root)
    {
        var stages = new List<IReadOnlyList<EvolutionStep>>();
        List<ChainLink> level = [root];
        while (level.Count > 0)
        {
            stages.Add(level
                .Select(link => new EvolutionStep(
                    link.Species.Id,
                    Localization.TitleFromSlug(link.Species.Name),
                    Describe(link.EvolutionDetails)))
                .ToList());
            level = level.SelectMany(link => link.EvolvesTo).ToList();
        }

        return stages;
    }

    // Una evolución puede tener varias formas válidas según el juego (Leafeon tiene 6). Se usa la que
    // PokeAPI marca como predeterminada y, si ninguna lo está, la última, que es la del juego más nuevo.
    public static string? Describe(IReadOnlyList<EvolutionDetail> details)
    {
        if (details.Count == 0) return null;

        var detail = details.LastOrDefault(d => d.IsDefault) ?? details[^1];
        var text = detail.Trigger.Name switch
        {
            "trade" => detail.HeldItem is null ? "Intercambio" : $"Intercambio con {ItemName(detail.HeldItem.Name)}",
            "use-item" when detail.Item is not null => ItemName(detail.Item.Name),
            "level-up" => LevelUp(detail),
            _ => "Especial",
        };

        return text + TimeOfDay(detail.TimeOfDay);
    }

    private static string LevelUp(EvolutionDetail detail)
    {
        if (detail.MinLevel is { } level) return $"Nv. {level}";

        var conditions = new List<string>();
        if (detail.MinHappiness is not null || detail.MinAffection is not null) conditions.Add("Amistad");
        if (detail.KnownMoveType is not null) conditions.Add($"mov. tipo {PokemonTypes.Label(detail.KnownMoveType.Name)}");
        if (detail.HeldItem is not null) conditions.Add($"con {ItemName(detail.HeldItem.Name)}");
        if (detail.Location is not null) conditions.Add("en un lugar especial");

        if (conditions.Count == 0) return "Subir de nivel";
        var text = string.Join(" y ", conditions);
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static string TimeOfDay(string? timeOfDay) => timeOfDay switch
    {
        "day" => ", de día",
        "night" => ", de noche",
        _ => string.Empty,
    };

    private static string ItemName(string slug) =>
        Items.TryGetValue(slug, out var name) ? name : Localization.TitleFromSlug(slug);
}
