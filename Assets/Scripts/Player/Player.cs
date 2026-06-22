using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    public Volume volume;
    [SerializeField] float fadeInTime;
    public Color normalColor,redColor;
    public 

    Vignette vignette;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    IEnumerator CourtineEffect()
    {
        var timer = 0f;
        yield return new WaitForSeconds(5);
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeInTime;
            yield return null;
        }
    }

}
