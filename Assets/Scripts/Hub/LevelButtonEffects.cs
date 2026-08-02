//TPFinal - Brisa Desouches

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configuración de Escala")]
    [Tooltip("Arrastrá acá el objeto hijo que contiene la imagen visual para evitar el parpadeo.")]
    public RectTransform visualContainer;
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
    private Vector3 originalScale;

    [Header("Configuración de Color (Opcional)")]
    public Image targetImage;
    public Color hoverColor = Color.white;
    private Color originalColor;

    private bool isUnlocked = false;

    void Awake()
    {
        // Si te olvidás de asignar el contenedor visual, usa el objeto principal por defecto
        if (visualContainer == null)
            visualContainer = GetComponent<RectTransform>();

        originalScale = visualContainer.localScale;
        
        if (targetImage != null)
        {
            originalColor = targetImage.color;
        }
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isUnlocked) return;

        if (visualContainer != null)
            visualContainer.localScale = hoverScale;

        if (targetImage != null)
        {
            targetImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetVisuals();
    }

    void OnDisable()
    {
        ResetVisuals();
    }

    void ResetVisuals()
    {
        if (visualContainer != null)
            visualContainer.localScale = originalScale;

        if (targetImage != null)
        {
            targetImage.color = originalColor;
        }
    }
}