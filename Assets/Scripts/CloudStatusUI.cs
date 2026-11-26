using TMPro;
using UnityEngine;

// Optional: attach to an always-active object and assign a TMP text label.
public class CloudStatusUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    private void Update()
    {
        if (statusText != null) statusText.text = CloudSaveClient.Instance.Status;
    }
    public void RetryPendingScore() => CloudSaveClient.Instance.RetryPendingScore();
}
