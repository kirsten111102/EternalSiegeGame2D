using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Created automatically. Do not attach this to scene objects.
public class CloudSaveClient : MonoBehaviour
{
    private static CloudSaveClient instance;
    public static CloudSaveClient Instance
    {
        get
        {
            if (instance == null)
                new GameObject("CloudSaveClient").AddComponent<CloudSaveClient>();
            return instance;
        }
    }

    public bool Busy { get; private set; }
    public bool IsReady { get; private set; }
    public bool NeedsReload { get; private set; }
    public bool RunEnded { get; private set; }
    public string RunId { get; private set; }
    public long Revision { get; private set; }
    public string Status { get; private set; } = "Chưa kết nối server.";
    public bool CanChangeScene => !Busy && pendingScore == null;

    private ScoreRequest pendingScore;
    private bool postingScore;
    private string ScoreKey => "eternal_siege.pending_score.v1." +
        ServerConfig.ApiBaseUrl + "." + ServerConfig.PlayerId;
    private string SaveUrl => ServerConfig.ApiBaseUrl.TrimEnd('/') +
        "/players/" + ServerConfig.PlayerId + "/save";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        if (PlayerPrefs.HasKey(ScoreKey))
        {
            pendingScore = Parse<ScoreRequest>(PlayerPrefs.GetString(ScoreKey));
            if (pendingScore != null) RetryPendingScore();
            else Report("Không đọc được điểm chờ gửi trong PlayerPrefs.");
        }
    }

    public void BeginNewRun(Action<bool> onReady)
    {
        if (!CanRead()) { onReady(false); return; }
        StartCoroutine(ReadSlot(false, data => onReady(IsReady)));
    }

    public void LoadGame(Action<SaveData> onLoaded)
    {
        if (!CanRead()) { onLoaded(null); return; }
        StartCoroutine(ReadSlot(true, onLoaded));
    }

    private bool CanRead()
    {
        if (ServerConfig.PlayerId <= 0)
        {
            Report("Điền PlayerId trong ServerConfig.cs bằng ID có thật trong MySQL.");
            return false;
        }
        if (!CanChangeScene)
        {
            Report("Đang gửi dữ liệu hoặc còn điểm chưa gửi. Hãy chờ / Retry Pending Score.");
            return false;
        }
        return true;
    }

    private IEnumerator ReadSlot(bool load, Action<SaveData> onLoaded)
    {
        Busy = true;
        // New-run failures must never inherit the previous run's ready state.
        if (!load) IsReady = false;
        Report(load ? "Đang tải checkpoint..." : "Đang chuẩn bị trận mới...");
        using UnityWebRequest request = UnityWebRequest.Get(SaveUrl);
        request.timeout = 15;
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Busy = false;
            ReportError(request);
            onLoaded(null);
            yield break;
        }
        CloudSaveResponse response = Parse<CloudSaveResponse>(request.downloadHandler.text);
        if (response == null || response.revision < 0 || response.schemaVersion != 1)
        {
            Busy = false;
            Report("Checkpoint có phiên bản không được hỗ trợ hoặc dữ liệu không hợp lệ.");
            onLoaded(null);
            yield break;
        }
        if (load && (response.saveData == null || response.isCompleted ||
            response.saveData.turrets == null || response.saveData.coreHealth <= 0f ||
            response.saveData.nextWaveNumber < 1 ||
            !Guid.TryParseExact(response.saveData.runId, "D", out Guid parsed) || parsed == Guid.Empty))
        {
            Busy = false;
            Report(response.isCompleted ? "Trận này đã kết thúc. Hãy chọn New Game." :
                "Chưa có checkpoint hợp lệ trên server.");
            onLoaded(null);
            yield break;
        }
        Revision = response.revision;
        RunId = load ? response.saveData.runId : Guid.NewGuid().ToString("D");
        IsReady = true;
        NeedsReload = false;
        RunEnded = false;
        Busy = false;
        Report(load ? "Đã tải checkpoint từ server." : "Trận mới đã sẵn sàng.");
        onLoaded(load ? response.saveData : null);
    }

    public void SaveGame(SaveData data)
    {
        if (!IsReady || RunEnded || data == null || data.runId != RunId)
        {
            Report("Không thể lưu: trận chưa sẵn sàng hoặc đã kết thúc.");
            return;
        }
        if (NeedsReload)
        {
            Report("Lần lưu trước chưa xác nhận / xung đột. Load lại server trước khi lưu tiếp.");
            return;
        }
        if (Busy || pendingScore != null)
        {
            Report("Đang gửi dữ liệu. Hãy chờ yêu cầu hiện tại hoàn tất rồi lưu lại.");
            return;
        }
        // Serialize immediately: this is a snapshot, not a reference to mutable gameplay state.
        var body = new CloudSaveRequest { expectedRevision = Revision, saveData = data };
        StartCoroutine(WriteSave(JsonUtility.ToJson(body)));
    }

    private IEnumerator WriteSave(string json)
    {
        Busy = true;
        long expected = Revision;
        Report("Đang lưu checkpoint...");
        using UnityWebRequest request = JsonRequest(SaveUrl, "PUT", json);
        yield return request.SendWebRequest();
        var acknowledgement = request.result == UnityWebRequest.Result.Success
            ? Parse<SaveAcknowledgement>(request.downloadHandler.text) : null;
        if (acknowledgement != null && acknowledgement.revision == expected + 1)
        {
            Revision = acknowledgement.revision;
            Report("Đã lưu trên server. Revision " + Revision);
        }
        else
        {
            // A timeout can happen AFTER the database commits. Do not guess the revision,
            // silently refresh it and overwrite, or present this operation as successful.
            NeedsReload = true;
            ReportError(request);
            Report("Chưa xác nhận được checkpoint. Cần Load lại trước lần lưu tiếp theo.");
        }
        Busy = false;
    }

    public void SubmitResult(int level, int score)
    {
        if (!IsReady || string.IsNullOrEmpty(RunId) || level < 1 || level > 99 || score < 0)
        {
            Report("Không thể gửi kết quả: dữ liệu trận không hợp lệ.");
            return;
        }
        if (RunEnded) { RetryPendingScore(); return; }
        RunEnded = true;
        pendingScore = new ScoreRequest
        {
            playerId = ServerConfig.PlayerId,
            runId = RunId,
            level = level,
            score = score
        };
        // Small local outbox: retry the identical final result after a timeout or restart.
        // PlayerPrefs is storage for retry, not authentication or anti-cheat protection.
        PlayerPrefs.SetString(ScoreKey, JsonUtility.ToJson(pendingScore));
        PlayerPrefs.Save();
        RetryPendingScore();
    }

    public void RetryPendingScore()
    {
        if (pendingScore != null && !postingScore)
            StartCoroutine(PostPendingScore());
    }

    private IEnumerator PostPendingScore()
    {
        postingScore = true;
        // Finish any in-flight checkpoint before closing the run on the server.
        while (Busy) yield return null;
        Busy = true;
        Report("Đang gửi điểm cuối trận...");
        using UnityWebRequest request = JsonRequest(
            ServerConfig.ApiBaseUrl.TrimEnd('/') + "/scores", "POST", JsonUtility.ToJson(pendingScore));
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            pendingScore = null;
            PlayerPrefs.DeleteKey(ScoreKey);
            PlayerPrefs.Save();
            Report("Đã lưu điểm cuối trận trên server.");
        }
        else
        {
            ReportError(request);
            Report("Điểm vẫn đang chờ gửi. Nhấn Retry Pending Score sau khi sửa lỗi kết nối.");
        }
        Busy = false;
        postingScore = false;
    }

    private static UnityWebRequest JsonRequest(string url, string method, string json)
    {
        var request = new UnityWebRequest(url, method);
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 15;
        return request;
    }

    private T Parse<T>(string json) where T : class
    {
        try { return JsonUtility.FromJson<T>(json); }
        catch (Exception exception) { Report("JSON không hợp lệ: " + exception.Message); return null; }
    }

    private void ReportError(UnityWebRequest request)
    {
        Report("HTTP " + request.responseCode + ": " + request.error + " " + request.downloadHandler.text);
    }

    private void Report(string message)
    {
        Status = message;
        Debug.Log("[Cloud] " + message);
    }
}
