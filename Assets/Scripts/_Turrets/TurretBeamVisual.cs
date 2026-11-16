
using UnityEngine;

[RequireComponent(typeof(TurretCombat))]
[RequireComponent(typeof(TurretHealth))]
public class TurretBeamVisual : MonoBehaviour
{
    [Header("Beam References")]
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private LineRenderer beamGlow;
    [SerializeField] private LineRenderer beamCore;

    [Header("Beam Impact")]
    [SerializeField] private ParticleSystem beamImpactVFX;

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
            beamCore == null ||
            beamImpactVFX == null)
        {
            Debug.LogError(
                $"{name}: Missing Beam Visual references!",
                this
            );

            enabled = false;
            return;
        }

        SetupBeam(beamGlow);
        SetupBeam(beamCore);

        // Không phát hạt khi bắt đầu game
        beamImpactVFX.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
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
            StopAttackVisual();
            return;
        }

        Vector3 startPosition = beamOrigin.position;

        Vector3 endPosition =
            target.transform.position +
            new Vector3(
                targetOffset.x,
                targetOffset.y,
                0f
            );

        endPosition.z = startPosition.z;

        // Cập nhật Beam
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

        beamGlow.enabled = true;
        beamCore.enabled = true;

        // Di chuyển ImpactVFX đến đầu tia
        beamImpactVFX.transform.position = endPosition;

        // Bắt đầu phát hạt nếu chưa phát
        if (!beamImpactVFX.isEmitting)
        {
            beamImpactVFX.Play();
        }
    }

    private void UpdateBeam(
        LineRenderer beam,
        Vector3 start,
        Vector3 end)
    {
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);
    }

    private void StopAttackVisual()
    {
        beamGlow.enabled = false;
        beamCore.enabled = false;

        if (beamImpactVFX.isEmitting ||
            beamImpactVFX.isPlaying)
        {
            beamImpactVFX.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    private void OnDisable()
    {
        if (beamGlow != null)
            beamGlow.enabled = false;

        if (beamCore != null)
            beamCore.enabled = false;

        if (beamImpactVFX != null)
        {
            beamImpactVFX.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }
}
