using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SkillCheck : MonoBehaviour
{
    [Header("Configuración UI")]
    public RectTransform needle;
    public Image successZoneImage;

    [Header("Ajustes del Juego")]
    public float rotationSpeed = 150f;
    public float successWindowSize = 0.1f;

    private float _currentAngle;
    private bool _isActive;
    private Citizen _currentCitizen;

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void StartSkillCheck(Citizen citizen)
    {
        _currentCitizen = citizen;
        gameObject.SetActive(true);
        PrepareSkillCheck();
    }

    private void PrepareSkillCheck()
    {
        float startAngle = Random.Range(40f, 300f);
        successZoneImage.fillAmount = successWindowSize;
        successZoneImage.rectTransform.localRotation = Quaternion.Euler(0, 0, -startAngle);

        _currentAngle = 0;
        _isActive = true;
    }

    void Update()
    {
        if (!_isActive) return;

        _currentAngle += rotationSpeed * Time.unscaledDeltaTime;
        if (_currentAngle >= 360f) _currentAngle -= 360f;

        needle.localRotation = Quaternion.Euler(0, 0, -_currentAngle);

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CheckSuccess();
        }
    }

    void CheckSuccess()
    {
        _isActive = false;

        float winRotation = successZoneImage.rectTransform.localEulerAngles.z;
        float correctedTargetCenter = -winRotation + (successWindowSize * 360f / 2f);
        float diff = Mathf.Abs(Mathf.DeltaAngle(_currentAngle, correctedTargetCenter));
        float tolerance = (successWindowSize * 360f) / 2f;

        if (diff <= tolerance)
        {
            Debug.Log("¡ÉXITO!");
            _currentCitizen.OnStealResult(true); 
        }
        else
        {
            Debug.Log("¡FALLO!");
            _currentCitizen.OnStealResult(false);
        }

        gameObject.SetActive(false);
    }
}