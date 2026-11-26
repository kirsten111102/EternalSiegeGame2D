using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
// Set ConnectionStrings__GameDb in the API process, never in the Unity build.
string connectionString =
    "Server=localhost;" +
    "Port=3306;" +
    "Database=eternal_siege;" +
    "User ID=root;" +
    "Password=120901;" +
    "Connection Timeout=5;";
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 256 * 1024);
var app = builder.Build();
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

// LOCAL LEARNING API: PlayerId is not authentication.
// Before public hosting, authenticate and obtain PlayerId from the user's claims.
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (MySqlException exception)
    {
        app.Logger.LogError(exception, "Database operation failed.");
        if (context.Response.HasStarted) throw;
        context.Response.Clear();
        await Results.Problem("Database unavailable. Please retry.", statusCode: 503)
            .ExecuteAsync(context);
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/leaderboard", async (CancellationToken ct) =>
{
    await using var connection = await OpenDatabase(ct);
    await using var command = new MySqlCommand("""
        SELECT `rank`, `name`, level, score
        FROM v_leaderboard ORDER BY `rank` LIMIT 10;
        """, connection);
    await using var reader = await command.ExecuteReaderAsync(ct);
    var attempts = new List<object>();
    while (await reader.ReadAsync(ct))
        attempts.Add(new {
            rank = checked((int)reader.GetInt64(0)), name = reader.GetString(1),
            level = reader.GetInt32(2), score = reader.GetInt32(3)
        });
    return Results.Ok(new { attempts });
});

// Existing player but no checkpoint: HTTP 200, revision=0, saveData=null.
// Unknown player: HTTP 404. A database/network failure is never "no save".
app.MapGet("/api/players/{playerId:long}/save", async Task<IResult>
    (long playerId, CancellationToken ct) =>
{
    if (playerId <= 0) return Results.BadRequest(new { error = "Invalid playerId." });
    await using var connection = await OpenDatabase(ct);
    await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
    if (!await LockPlayer(connection, transaction, playerId, ct))
        return Results.NotFound(new { error = "Player does not exist." });
    var row = await ReadSave(connection, transaction, playerId, ct);
    await transaction.CommitAsync(ct);
    return Results.Ok(new {
        revision = row?.Revision ?? 0,
        schemaVersion = row?.SchemaVersion ?? 1,
        saveData = row?.Data,
        isCompleted = row?.IsCompleted ?? false
    });
});

app.MapPut("/api/players/{playerId:long}/save", async Task<IResult>
    (long playerId, SaveRequest request, CancellationToken ct) =>
{
    if (playerId <= 0 || request.ExpectedRevision < 0 || request.SchemaVersion != 1 ||
        request.SaveData is null || !request.SaveData.IsValid())
        return Results.BadRequest(new { error = "Invalid checkpoint or unsupported schemaVersion." });

    await using var connection = await OpenDatabase(ct);
    await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
    // All save/result writes lock the same parent row, including the first save.
    if (!await LockPlayer(connection, transaction, playerId, ct))
        return Results.NotFound(new { error = "Player does not exist." });

    var row = await ReadSave(connection, transaction, playerId, ct);
    long currentRevision = row?.Revision ?? 0;
    if (request.ExpectedRevision != currentRevision)
        return Results.Conflict(new { error = "Checkpoint changed. Load it again.", currentRevision });
    if (currentRevision == long.MaxValue)
        return Results.Conflict(new { error = "Revision limit reached." });

    await using (var completed = new MySqlCommand("""
        SELECT 1 FROM game_results WHERE player_id=@playerId AND run_id=@runId LIMIT 1;
        """, connection, transaction))
    {
        completed.Parameters.AddWithValue("@playerId", playerId);
        completed.Parameters.AddWithValue("@runId", request.SaveData.RunId);
        if (await completed.ExecuteScalarAsync(ct) is not null)
            return Results.Conflict(new { error = "This run is already completed." });
    }

    long nextRevision = currentRevision + 1;
    string sql = row is null
        ? """
          INSERT INTO game_saves(player_id, schema_version, revision, save_data, updated_at)
          VALUES(@playerId, @schema, @revision, @data, UTC_TIMESTAMP());
          """
        : """
          UPDATE game_saves SET schema_version=@schema, revision=@revision,
              save_data=@data, updated_at=UTC_TIMESTAMP()
          WHERE player_id=@playerId AND revision=@expected;
          """;
    await using (var command = new MySqlCommand(sql, connection, transaction))
    {
        command.Parameters.AddWithValue("@playerId", playerId);
        command.Parameters.AddWithValue("@schema", request.SchemaVersion);
        command.Parameters.AddWithValue("@revision", nextRevision);
        command.Parameters.AddWithValue("@expected", currentRevision);
        command.Parameters.AddWithValue("@data", JsonSerializer.Serialize(request.SaveData, jsonOptions));
        if (await command.ExecuteNonQueryAsync(ct) != 1)
            throw new InvalidOperationException("Checkpoint write did not affect exactly one row.");
    }
    await transaction.CommitAsync(ct);
    return Results.Ok(new { revision = nextRevision });
});

// Only completed runs go here. Retrying the SAME result succeeds without another row.
app.MapPost("/api/scores", async Task<IResult> (ScoreEntry score, CancellationToken ct) =>
{
    if (score.PlayerId <= 0 || score.RunId == Guid.Empty ||
        score.Level is < 1 or > 99 || score.Score < 0)
        return Results.BadRequest(new { error = "Invalid score data." });

    await using var connection = await OpenDatabase(ct);
    await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
    if (!await LockPlayer(connection, transaction, score.PlayerId, ct))
        return Results.NotFound(new { error = "Player does not exist." });

    await using (var command = new MySqlCommand("""
        SELECT level, score FROM game_results WHERE player_id=@playerId AND run_id=@runId;
        """, connection, transaction))
    {
        command.Parameters.AddWithValue("@playerId", score.PlayerId);
        command.Parameters.AddWithValue("@runId", score.RunId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
            return reader.GetInt32(0) == score.Level && reader.GetInt32(1) == score.Score
                ? Results.Ok(new { message = "Score already saved." })
                : Results.Conflict(new { error = "This run has a different final result." });
    }

    await using (var command = new MySqlCommand("""
        INSERT INTO game_results(player_id, run_id, level, score, achieved_at)
        VALUES(@playerId, @runId, @level, @score, UTC_TIMESTAMP());
        """, connection, transaction))
    {
        command.Parameters.AddWithValue("@playerId", score.PlayerId);
        command.Parameters.AddWithValue("@runId", score.RunId.ToString("D"));
        command.Parameters.AddWithValue("@level", score.Level);
        command.Parameters.AddWithValue("@score", score.Score);
        await command.ExecuteNonQueryAsync(ct);
    }
    // Keep the checkpoint and its monotonic revision. GET marks this run completed.
    // Deleting the row here would reset revision to 0 and allow stale clients to overwrite.
    await transaction.CommitAsync(ct);
    return Results.Json(new { message = "Score saved." }, statusCode: 201);
});

app.Run("http://localhost:9999");

async Task<MySqlConnection> OpenDatabase(CancellationToken ct)
{
    var connection = new MySqlConnection(connectionString);
    try
    {
        await connection.OpenAsync(ct);
        await using var command = new MySqlCommand("SET time_zone = '+00:00';", connection);
        await command.ExecuteNonQueryAsync(ct);
        return connection;
    }
    catch { await connection.DisposeAsync(); throw; }
}

static async Task<bool> LockPlayer(MySqlConnection connection, MySqlTransaction transaction,
    long playerId, CancellationToken ct)
{
    await using var command = new MySqlCommand(
        "SELECT id FROM players WHERE id=@playerId FOR UPDATE;", connection, transaction);
    command.Parameters.AddWithValue("@playerId", playerId);
    return await command.ExecuteScalarAsync(ct) is not null;
}

static async Task<SavedRow?> ReadSave(MySqlConnection connection, MySqlTransaction transaction,
    long playerId, CancellationToken ct)
{
    await using var command = new MySqlCommand("""
        SELECT s.revision, s.schema_version, s.save_data,
            EXISTS(SELECT 1 FROM game_results r WHERE r.player_id=s.player_id
                AND r.run_id=JSON_UNQUOTE(JSON_EXTRACT(s.save_data, '$.runId')))
        FROM game_saves s WHERE s.player_id=@playerId;
        """, connection, transaction);
    command.Parameters.AddWithValue("@playerId", playerId);
    await using var reader = await command.ExecuteReaderAsync(ct);
    if (!await reader.ReadAsync(ct)) return null;
    using var document = JsonDocument.Parse(reader.GetString(2));
    return new SavedRow(checked((long)reader.GetUInt64(0)), reader.GetInt32(1),
        document.RootElement.Clone(), reader.GetBoolean(3));
}

public record ScoreEntry(long PlayerId, Guid RunId, int Level, int Score);
public record SaveRequest(int SchemaVersion, long ExpectedRevision, Checkpoint? SaveData);
public record SavedRow(long Revision, int SchemaVersion, JsonElement Data, bool IsCompleted);

// Property names become camelCase, matching the public Unity fields exactly.
public sealed class Checkpoint
{
    public required string RunId { get; init; }
    public required float PlayerX { get; init; }
    public required float PlayerY { get; init; }
    public required float Health { get; init; }
    public required int DeathCount { get; init; }
    public required float RespawnSecondsRemaining { get; init; }
    public required int Score { get; init; }
    public required int NextWaveNumber { get; init; }
    public required long TotalXPHundredths { get; init; }
    public required int BonusXPPercent { get; init; }
    public required float CoreHealth { get; init; }
    public required TurretCheckpoint[] Turrets { get; init; }

    public bool IsValid()
    {
        // Keep 71 in sync with the current 70-wave campaign.
        if (!Guid.TryParseExact(RunId, "D", out var run) || run == Guid.Empty ||
            !float.IsFinite(PlayerX) || !float.IsFinite(PlayerY) ||
            !Nonnegative(Health) || DeathCount < 0 || !Nonnegative(RespawnSecondsRemaining) ||
            Score < 0 || NextWaveNumber is < 1 or > 71 || TotalXPHundredths < 0 ||
            BonusXPPercent < 0 || !float.IsFinite(CoreHealth) || CoreHealth <= 0 ||
            Turrets is null || Turrets.Length > 128) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var turret in Turrets)
            if (turret is null || string.IsNullOrWhiteSpace(turret.TurretId) ||
                turret.TurretId.Length > 100 || !ids.Add(turret.TurretId) ||
                !Nonnegative(turret.CurrentHealth) || !Nonnegative(turret.DamagePerTick) ||
                !Nonnegative(turret.AttackRange) || !Nonnegative(turret.AttackSpeedPoints)) return false;
        return true;
    }
    private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
}

public sealed class TurretCheckpoint
{
    public required string TurretId { get; init; }
    public required float CurrentHealth { get; init; }
    public required float DamagePerTick { get; init; }
    public required float AttackRange { get; init; }
    public required float AttackSpeedPoints { get; init; }
}
