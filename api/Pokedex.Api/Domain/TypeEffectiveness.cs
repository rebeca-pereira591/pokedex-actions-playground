namespace Pokedex.Api.Domain;

// Lo que dice PokeAPI de un tipo cuando lo atacan.
public sealed record DefendingType(
    string Name,
    IReadOnlyCollection<string> DoubleDamageFrom,
    IReadOnlyCollection<string> HalfDamageFrom,
    IReadOnlyCollection<string> NoDamageFrom);

public sealed record TypeMultiplier(string Type, double Multiplier);

public static class TypeEffectiveness
{
    // Cuánto daño recibe un tipo defensor de un tipo atacante: x2, x½, x0 o x1.
    public static double Against(string attacking, DefendingType defending)
    {
        if (defending.NoDamageFrom.Contains(attacking)) return 0;
        if (defending.DoubleDamageFrom.Contains(attacking)) return 2;
        if (defending.HalfDamageFrom.Contains(attacking)) return 0.5;
        return 1;
    }

    // Para un Pokémon de uno o dos tipos, el multiplicador de cada tipo atacante es el PRODUCTO de
    // los multiplicadores contra cada uno de sus tipos. Por eso aparecen x4 y x¼, y una inmunidad
    // (x0) anula el resto. Se devuelven sólo los que no son x1, en el orden de los tipos.
    // S1244 (no comparar doubles exactos) se silencia: los multiplicadores son productos de 0, ½, 1 y 2,
    // fracciones binarias que un double representa exactas, así que comparar con 1 es exacto.
#pragma warning disable S1244
    public static IReadOnlyList<TypeMultiplier> Weaknesses(IReadOnlyList<DefendingType> defending) =>
        PokemonTypes.All
            .Select(attacking => new TypeMultiplier(
                attacking,
                defending.Aggregate(1.0, (total, type) => total * Against(attacking, type))))
            .Where(m => m.Multiplier != 1)
            .ToList();
#pragma warning restore S1244
}
