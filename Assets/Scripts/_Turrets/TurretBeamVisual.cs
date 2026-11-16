using UnityEngine;

[RequireComponent(typeof(TurretCombat))]
[RequireComponent(typeof(TurretHealth))]
public class TurretBeamVisual : MonoBehaviour
{
    [Header("Beam References")]
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private LineRenderer beamGlow;
    [SerializeField] private LineRenderer beamCore;

    [Header("Target Position")]
    [SerializeField] private Vector2 targetOffset;

    private TurretCombat turretCombat;
    private TurretHealth turretHealth;

    private void Awake()
    {
        turretCombat = GetComponent<TurretCombat>();
        turretHealth = GetComponent<TurretHealth>();

        if (beamOrigin == null ||
            beamGlow == null ||
            beamCore == null)
        {
            Debug.LogError(
                $"{name}: Missing Beam references!",
                this
            );

            enabled = false;
            return;
        }

        SetupBeam(beamGlow);
        SetupBeam(beamCore);
    }

    private void SetupBeam(LineRenderer beam)
    {
        beam.positionCount = 2;
        beam.useWorldSpace = true;
        beam.enabled = false;
    }

    private void LateUpdate()
    {
        EnemyHealth target = turretCombat.CurrentTarget;

        bool canAttack =
            !turretHealth.IsDead &&
            target != null &&
            !target.IsDead;

        if (!canAttack)
        {
            SetBeamVisible(false);
            return;
        }

        // Vị trí xuất phát từ Crystal
        Vector3 startPosition = beamOrigin.position;

        // Vị trí Zombie đang được Lock-on
        Vector3 endPosition =
            target.transform.position +
            new Vector3(
                targetOffset.x,
                targetOffset.y,
                0f
            );

        // Giữ Beam cùng mặt phẳng hiển thị
        endPosition.z = startPosition.z;

        // Cập nhật hai lớp tia
        UpdateBeam(
            beamGlow,
            startPosition,
            endPosition
        );

        UpdateBeam(
            beamCore,
            startPosition,
            endPosition
        );

        SetBeamVisible(true);
    }

    private void UpdateBeam(
        LineRenderer beam,
        Vector3 start,
        Vector3 end)
    {
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);
    }

    private void SetBeamVisible(bool visible)
    {
        beamGlow.enabled = visible;
        beamCore.enabled = visible;
    }

    private void OnDisable()
    {
        if (beamGlow != null)
            beamGlow.enabled = false;

        if (beamCore != null)
            beamCore.enabled = false;
    }

    
}
