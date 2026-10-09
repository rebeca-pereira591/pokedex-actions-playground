using System.Globalization;
using System.Text.RegularExpressions;

namespace Pokedex.Api.Domain;

public sealed record LocalizedText(string Language, string Text);

public static partial class Localization
{
    public const string Spanish = "es";

    // Elige el texto en español. Si hay varios (uno por juego), el último es el del juego más nuevo.
    // Si no hay ninguno, devuelve null: la app muestra "Sin descripción en español".
    public static string? PickSpanish(IEnumerable<LocalizedText> entries)
    {
        var text = entries.LastOrDefault(e => e.Language == Spanish)?.Text;
        return text is null ? null : Whitespace().Replace(text.Replace("­", string.Empty), " ").Trim();
    }

    // "mr-mime" -> "Mr Mime", "gengar" -> "Gengar". Para cuando PokeAPI no trae el nombre traducido.
    public static string TitleFromSlug(string slug) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(slug.Replace('-', ' '));

    // Los textos de PokeAPI vienen cortados como en la pantalla del juego: con \n y \f en el medio.
    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
