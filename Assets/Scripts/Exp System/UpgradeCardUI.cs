using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Hover Scale")]
    [SerializeField, Min(1f)] private float hoverScale = 1.08f;
    [SerializeField, Min(0.1f)] private float animationSpeed = 12f;

    [Header("Hover Red Outline")]
    [SerializeField] private Outline hoverOutline;
    [SerializeField] private Color outlineColor = Color.red;
    [SerializeField] private Vector2 outlineDistance = new Vector2(3f, -3f);

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (button == null)
            button = GetComponent<Button>();

        // The Outline should be on the same object as the Button's Image.
        // Use an Outline assigned in the Inspector, or find/create one automatically.
        if (hoverOutline == null && button != null)
        {
            hoverOutline = button.GetComponent<Outline>();
            if (hoverOutline == null)
                hoverOutline = button.gameObject.AddComponent<Outline>();
        }

        if (hoverOutline != null)
        {
            hoverOutline.effectColor = outlineColor;
            hoverOutline.effectDistance = outlineDistance;
            hoverOutline.useGraphicAlpha = false;
            hoverOutline.enabled = false;
        }
    }

    private void OnEnable()
    {
        ResetHover();
    }

    private void Update()
    {
        // This animation must keep working while LevelUpPanelUI pauses the game.
        float t = Mathf.Clamp01(animationSpeed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null || !button.isActiveAndEnabled || !button.interactable)
            return;

        targetScale = originalScale * hoverScale;

        if (hoverOutline != null)
            hoverOutline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Shrink smoothly, but hide the outline immediately.
        targetScale = originalScale;

        if (hoverOutline != null)
            hoverOutline.enabled = false;
    }

    private void OnDisable()
    {
        ResetHover();
    }

    private void ResetHover()
    {
        targetScale = originalScale;
        transform.localScale = originalScale;

        if (hoverOutline != null)
            hoverOutline.enabled = false;
    }

    public void Setup(UpgradeType type, Action<UpgradeType> onSelected)
    {
        ResetHover();

        switch (type)
        {
            case UpgradeType.SharpBullet:
                SetContent("SHARP BULLET", "+0.5 Turret Damage");
                break;
            case UpgradeType.RapidFire:
                SetContent("RAPID FIRE", "+15 Attack Speed (+15%)");
                break;
            case UpgradeType.LongRange:
                SetContent("LONG RANGE", "+0.2 Turret Attack Range");
                break;
            case UpgradeType.TurretRepair:
                SetContent("TURRET REPAIR", "Heal 20% Max HP of living turrets");
                break;
            case UpgradeType.BonusXp:
                SetContent("BONUS XP", "+5% XP gained from enemies");
                break;
        }

        if (button == null)
        {
            Debug.LogError($"{name}: UpgradeCardUI is missing Button reference.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(type));
    }

    private void SetContent(string title, string description)
    {
        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;
    }
}
