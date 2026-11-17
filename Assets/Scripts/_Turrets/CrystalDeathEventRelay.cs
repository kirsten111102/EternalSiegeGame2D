using UnityEngine;

public class CrystalDeathEventRelay : MonoBehaviour
{
    [Header("Destroyed Animation")]
    [SerializeField] private Animator baseAnimator;

    private static readonly int DestroyedHash =
        Animator.StringToHash("Destroyed");

    // Được gọi bởi Animation Event tại Frame 5
    public void TriggerBaseImpact()
    {
        if (baseAnimator == null)
        {
            Debug.LogError(
                "Base Animator is missing!",
                this
            );
            return;
        }

        baseAnimator.SetTrigger(DestroyedHash);

        Debug.Log("Crystal impacted TurretBase!", this);
    }
}
