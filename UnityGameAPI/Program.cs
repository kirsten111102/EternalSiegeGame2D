using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Local learning setup: replace YOUR_USER and YOUR_PASSWORD before testing.
// Keep real credentials out of source control.
string connectionString =
    "Server=localhost;Port=3306;Database=eternal_siege;" +
    "User ID=root;Password=120901;Connection Timeout=5;";

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// GET: the database view selects each player's best completed run.
app.MapGet("/api/leaderboard", async () =>
{
    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = """
            SELECT `rank`, `name`, level, score
            FROM v_leaderboard
            ORDER BY `rank`
            LIMIT 10;
            """;

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var players = new List<object>();

        while (await reader.ReadAsync())
        {
            players.Add(new
            {
                rank = checked((int)reader.GetInt64(0)),
                name = reader.GetString(1),
                level = reader.GetInt32(2),
                score = reader.GetInt32(3)
            });
        }

        // Keep the existing local variable 'players'; Unity expects 'attempts'.
        return Results.Ok(new { attempts = players });
    }
    catch (MySqlException exception)
    {
        app.Logger.LogError(exception, "Unable to load leaderboard.");
        return Results.Problem("Unable to load leaderboard.", statusCode: 500);
    }
});

// Local learning prototype: PlayerId is supplied by the client.
// Before public deployment, derive it from an authenticated user instead.
app.MapPost("/api/scores", async (ScoreEntry score) =>
{
    if (score.PlayerId <= 0 || score.RunId == Guid.Empty ||
        score.Level < 1 || score.Level > 99 || score.Score < 0)
    {
        return Results.BadRequest(new { error = "Invalid score data." });
    }

    try
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO game_results
                (player_id, run_id, level, score, achieved_at)
            VALUES
                (@playerId, @runId, @level, @score, UTC_TIMESTAMP());
            """;

        // TIMESTAMP conversion uses the session time zone.
        await using (var command = new MySqlCommand(
            "SET time_zone = '+00:00';", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new MySqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@playerId", score.PlayerId);
            command.Parameters.AddWithValue("@runId", score.RunId.ToString("D"));
            command.Parameters.AddWithValue("@level", score.Level);
            command.Parameters.AddWithValue("@score", score.Score);

            await command.ExecuteNonQueryAsync();
        }

        return Results.Json(
            new { message = "Score saved" },
            statusCode: 201
        );
    }
    catch (MySqlException exception) when (exception.Number == 1062)
    {
        return Results.Conflict(new { error = "This run already has a result." });
    }
    catch (MySqlException exception) when (exception.Number == 1452)
    {
        return Results.NotFound(new { error = "Player does not exist." });
    }
    catch (MySqlException exception)
    {
        app.Logger.LogError(exception, "Unable to save score.");
        return Results.Problem("Unable to save score.", statusCode: 500);
    }
});

app.Run("http://localhost:9999");

// Keep the existing backend model name. AchievedAt is assigned by the server.
public record ScoreEntry(long PlayerId, Guid RunId, int Level, int Score);
