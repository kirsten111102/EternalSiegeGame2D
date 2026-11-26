using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    [SerializeField]
    private string apiBaseUrl = "http://localhost:9999/api";

    public event Action<LeaderboardResponse> LeaderboardLoaded;
    public event Action<string> LeaderboardFailed;

    // GET: Load leaderboard
    public void LoadLeaderboard()
    {
        StartCoroutine(GetLeaderboard());
    }

    private IEnumerator GetLeaderboard()
    {
        string url = apiBaseUrl.TrimEnd('/') + "/leaderboard";
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"Leaderboard request failed " +
                $"(HTTP {request.responseCode}): {request.error}"
            );
            LeaderboardFailed?.Invoke(
                "Không thể tải bảng xếp hạng. Vui lòng thử lại."
            );
            yield break;
        }

        string json = request.downloadHandler.text;
        LeaderboardResponse response = null;
        try
        {
            response = JsonUtility.FromJson<LeaderboardResponse>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Cannot parse leaderboard JSON: " + exception.Message
            );
        }
        if (response == null || response.attempts == null)
        {
            Debug.LogError(
                "Invalid leaderboard response. " +
                "Expected a JSON object containing 'attempts'."
            );
            LeaderboardFailed?.Invoke(
                "Dữ liệu bảng xếp hạng không hợp lệ."
            );
            yield break;
        }
        foreach (LeaderboardEntry entry in response.attempts)
        {
            if (entry == null ||
                entry.rank < 1 ||
                string.IsNullOrWhiteSpace(entry.name) ||
                entry.level < 1 ||
                entry.level > 99 ||
                entry.score < 0)
            {
                LeaderboardFailed?.Invoke(
                    "Dữ liệu một hàng trong bảng xếp hạng không hợp lệ."
                );
                yield break;
            }
            Debug.Log(
                $"Rank: {entry.rank} | " +
                $"Name: {entry.name} | " +
                $"Level: {entry.level} | " +
                $"Score: {entry.score}"
            );
        }
        LeaderboardLoaded?.Invoke(response);
    }

    // POST: Submit a new score
    private IEnumerator PostScore(
     long playerId,
     string runId,
     int level,
     int score)
    {
        string url = apiBaseUrl.TrimEnd('/') + "/scores";

        ScoreRequest scoreRequest = new()
        {
            playerId = playerId,
            runId = runId,
            level = level,
            score = score
        };

        string json = JsonUtility.ToJson(scoreRequest);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new(url, "POST");

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = 15;

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"Save score failed (HTTP {request.responseCode}): " +
                $"{request.error}\n{request.downloadHandler.text}"
            );

            yield break;
        }

        Debug.Log(request.downloadHandler.text);
        Debug.Log("Score saved successfully!");
    }

    public void SendScore(
        long playerId,
        string runId,
        int level,
        int score)
    {
        if (playerId <= 0 ||
            !Guid.TryParse(runId, out Guid parsedRunId) ||
            parsedRunId == Guid.Empty ||
            level < 1 ||
            level > 99 ||
            score < 0)
        {
            Debug.LogWarning("Cannot send score: Invalid score data.");
            return;
        }

        StartCoroutine(PostScore(playerId, runId, level, score));
    }
}
