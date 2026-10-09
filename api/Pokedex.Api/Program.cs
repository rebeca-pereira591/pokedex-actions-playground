using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Pokedex.Api.Contracts;
using Pokedex.Api.PokeApi;
using Pokedex.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddOptions<PokeApiOptions>().BindConfiguration(PokeApiOptions.Section);

builder.Services.AddHybridCache(options =>
{
    // Los datos de PokeAPI casi no cambian: medio día en memoria alcanza
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromHours(12),
        LocalCacheExpiration = TimeSpan.FromHours(12),
    };
});

builder.Services
    .AddHttpClient<PokeApiClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<PokeApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var options = sp.GetRequiredService<IOptions<PokeApiOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.FixturesPath))
        {
            return new SocketsHttpHandler();
        }

        var root = Path.GetFullPath(options.FixturesPath, sp.GetRequiredService<IHostEnvironment>().ContentRootPath);
        return new FixtureMessageHandler(root);
    });

builder.Services.AddScoped<PokedexService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "ok" }));

api.MapGet("/pokemon", ([AsParameters] PokemonQuery query, PokedexService pokedex, CancellationToken ct) =>
    Handle(() => pokedex.GetPageAsync(query, ct)));

api.MapGet("/pokemon/{idOrName}", (string idOrName, PokedexService pokedex, CancellationToken ct) =>
    Handle(() => pokedex.GetDetailAsync(idOrName, ct)));

api.MapGet("/types", () => Results.Ok(PokedexService.GetTypes()));

await app.RunAsync();

// Traduce los errores esperables a respuestas HTTP: un filtro inválido es 400 y un Pokémon que no
// existe es 404. Cualquier otro error sigue siendo 500.
static async Task<IResult> Handle<T>(Func<Task<T>> action)
{
    try
    {
        return Results.Ok(await action());
    }
    catch (InvalidQueryException e)
    {
        return Results.Problem(e.Message, statusCode: StatusCodes.Status400BadRequest);
    }
    catch (PokeApiNotFoundException)
    {
        return Results.Problem("No existe ese Pokémon.", statusCode: StatusCodes.Status404NotFound);
    }
}

// Expuesto para que los tests de integración puedan usar WebApplicationFactory<Program>. No puede ser
// static (WebApplicationFactory la usa como argumento de tipo), por eso se silencia S1118.
#pragma warning disable S1118
public partial class Program;
#pragma warning restore S1118
