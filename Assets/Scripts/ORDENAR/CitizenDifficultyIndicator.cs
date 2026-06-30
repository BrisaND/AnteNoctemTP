using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class CitizenDifficultyIndicator : MonoBehaviour
{
    [Header("Colores por dificultad")]
    public Color easyColor = Color.green;
    public Color mediumColor = Color.yellow;
    public Color hardColor = Color.red;

    private CitizenAI citizen;
    private SpriteRenderer sr;
    private Camera mainCam;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;
        citizen = GetComponentInParent<CitizenAI>();
    }

    void Start()
    {
        StartCoroutine(ApplyColorNextFrame());
    }

    IEnumerator ApplyColorNextFrame()
    {
        yield return null;
        yield return null;
        UpdateColor();
    }

    void LateUpdate()
    {
        if (mainCam != null)
        {
            transform.LookAt(transform.position + mainCam.transform.forward, Vector3.up);
        }
    }

    public void UpdateColor()
    {
        if (citizen == null || sr == null) return;

        switch (citizen.difficulty)
        {
            case RobberySystem.DifficultyLevel.Easy:
                sr.color = easyColor;
                break;
            case RobberySystem.DifficultyLevel.Medium:
                sr.color = mediumColor;
                break;
            case RobberySystem.DifficultyLevel.Hard:
                sr.color = hardColor;
                break;
        }
    }
}