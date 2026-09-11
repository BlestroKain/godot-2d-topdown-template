using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Server.Configuration;
using NuevoMMO.Server.Database;

internal static class AppearancePersistenceVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        var root = Path.Combine(Path.GetTempPath(), "nuevommo-appearance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var paths = SqliteDatabasePaths.FromConfiguration(new SqliteDatabaseFiles
            {
                Auth = Path.Combine(root, "auth.db"),
                Players = Path.Combine(root, "players.db"),
                Game = Path.Combine(root, "game.db"),
                Logs = Path.Combine(root, "logs.db")
            });
            SqliteMigrator.ApplyAsync(paths).GetAwaiter().GetResult();

            var repository = new SqliteCharacterRepository(paths.Players);
            var expected = CanonicalCharacterAppearance.Default;
            var account = new AccountId(Guid.NewGuid());
            var map = DefinitionId.New();
            var tradition = DefinitionId.New();
            var created = repository.CreateAsync(
                account,
                "Appearance" + Guid.NewGuid().ToString("N")[..8],
                map,
                new Vector2Data(32, 32),
                tradition,
                expected).GetAwaiter().GetResult();
            var loaded = repository.GetAsync(created.Id).GetAwaiter().GetResult()
                ?? throw new InvalidOperationException("FAIL: Appearance: SQLite no devolvió el personaje creado.");

            Check(loaded.Appearance == expected, "Appearance: SQLite conserva el record canónico");
            Check(loaded.Appearance.ToStorageString() == expected.ToStorageString(),
                "Appearance: SQLite conserva el formato persistido completo");
            Check(CharacterAppearance.FromStorageString(expected.ToStorageString()) == expected,
                "Appearance: serialización v1 hace round-trip");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
