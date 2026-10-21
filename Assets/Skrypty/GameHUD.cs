using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    public PlayerStats playerStats;

    public TMP_Text timeText;
    public TMP_Text coinsText;
    public TMP_Text killsText;
    public TMP_Text levelText;

    private float elapsedTime;
    public float magicDamageMultiplier = 1f;

    void Start()
    {
        if (playerStats == null)
            playerStats = Object.FindAnyObjectByType<PlayerStats>();
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (timeText != null)
            timeText.text = $"{minutes:00}:{seconds:00}";

        if (playerStats == null)
            return;

        if (coinsText != null)
            coinsText.text = playerStats.coins.ToString();

        if (killsText != null)
            killsText.text = playerStats.kills.ToString();

        if (levelText != null)
            levelText.text = $"LVL {playerStats.currentLevel}";
    }
}