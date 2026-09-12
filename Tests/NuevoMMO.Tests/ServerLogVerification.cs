using System.Runtime.CompilerServices;
using NuevoMMO.Server.Telemetry;

internal static class ServerLogVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        var line = ServerLog.Format(
            "info",
            "auth",
            "login",
            ("account", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            ("sessionToken", "super-secret-token"),
            ("username", "Uno\nDos"));

        Expect(line.Contains("\"level\":\"info\"", StringComparison.Ordinal), "incluye nivel estructurado");
        Expect(line.Contains("\"area\":\"auth\"", StringComparison.Ordinal), "incluye área estructurada");
        Expect(line.Contains("\"action\":\"login\"", StringComparison.Ordinal), "incluye acción estructurada");
        Expect(line.Contains("[REDACTED]", StringComparison.Ordinal), "redacta token por nombre de campo");
        Expect(!line.Contains("super-secret-token", StringComparison.Ordinal), "no filtra secreto");
        Expect(line.Contains("Uno Dos", StringComparison.Ordinal), "normaliza saltos de línea del usuario");
        Expect(!line.Contains("Uno\\nDos", StringComparison.Ordinal), "evita inyección de línea en JSON");
    }

    private static void Expect(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("ServerLogVerification FAIL: " + name);
    }
}
