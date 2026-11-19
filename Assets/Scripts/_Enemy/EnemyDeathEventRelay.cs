using UnityEngine;

public class EnemyDeathEventRelay : MonoBehaviour
{
    [Header("Destroyed Animation")]
    [SerializeField] private Animator baseAnimator;

    private static readonly int DieHash =
        Animator.StringToHash("Die");

    public void TriggerEnemyDeath()
    {
        if (baseAnimator == null)
        {
            Debug.LogError(
                "Base Animator is missing!",
                this
            );
            return;
        }

        baseAnimator.SetTrigger(DieHash);

        Debug.Log("Crystal impacted TurretBase!", this);
    }
}
