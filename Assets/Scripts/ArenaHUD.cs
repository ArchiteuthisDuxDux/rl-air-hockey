using TMPro;
using UnityEngine;

public class ArenaHUD : MonoBehaviour
{
    [SerializeField] private AirHockeyArenaManager arena;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;

    private void Update()
    {
        scoreText.text = $"{arena.RedScore} : {arena.BlueScore}";
        timerText.text = $"{arena.CurrentRoundTime:F1}";
    }
}