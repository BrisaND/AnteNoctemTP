//TPFinal - Juan Cruz Villarreo

using UnityEngine;

public class PowerUpUIController : MonoBehaviour
{
    [Header("Ticks")]
    public GameObject bootsCheck;
    public GameObject socksCheck;
    public GameObject glovesCheck;

    private PlayerPowerUps powerUps;
    private bool foundPlayer = false;

    void Update()
    {
        if (!foundPlayer)
        {
            TryFindPlayer();
        }

        if (powerUps == null) return;

        if (bootsCheck != null) bootsCheck.SetActive(powerUps.hasBoots);
        if (socksCheck != null) socksCheck.SetActive(powerUps.hasSocks);
        if (glovesCheck != null) glovesCheck.SetActive(powerUps.hasGloves);
    }

    void TryFindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        powerUps = player.GetComponent<PlayerPowerUps>();
        foundPlayer = true;
    }
}