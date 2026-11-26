using UnityEngine;

// Optional player identity component. It does not manage EXP, Gold, or saved data.
public class Player : MonoBehaviour
{
    private PlayerLevelStats levelStats;
    public int Level => levelStats != null ? levelStats.CurrentLevel : 1;
    private void Awake()
    {
        levelStats = GetComponent<PlayerLevelStats>();
    }

        private void Start()
    {
        Debug.Log($"Player Started. Level: {Level}");
    }
}
