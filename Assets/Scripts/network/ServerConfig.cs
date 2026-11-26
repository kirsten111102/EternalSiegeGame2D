// LOCAL TEST ONLY. Use the existing Player ID from SELECT id, display_name FROM players.
// This is not a login system. Never store MySQL credentials in a Unity script.
public static class ServerConfig
{
    public const string ApiBaseUrl = "http://localhost:9999/api";
    public static readonly long PlayerId = 10;
}
