using UnityEngine;

public class GameplayMusic : MonoBehaviour
{
    [SerializeField] private AudioSource introSource;
    [SerializeField] private AudioSource loopSource;

    private void Start()
    {
        PlayMusic();
    }

    private void PlayMusic()
    {
        double startTime = AudioSettings.dspTime + 0.2;

        double introDuration =
            (double)introSource.clip.samples /
            introSource.clip.frequency;

        introSource.PlayScheduled(startTime);

        loopSource.PlayScheduled(startTime + introDuration);
    }

    #region Music Control
    public void MuteMusic()
    {
        introSource.mute = true;
        loopSource.mute = true;
    }
    public void UnmuteMusic()
    {
        introSource.mute = false;
        loopSource.mute = false;
    }
    #endregion
}