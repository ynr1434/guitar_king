using UnityEngine;
using UnityEngine.UI;

public sealed class ScoreDisplayUI : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text comboText;
    [SerializeField] private Text multiplierText;
    [SerializeField] private Text accuracyText;

    private void OnEnable()
    {
        if (scoreManager != null)
            scoreManager.ValuesChanged += Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.ValuesChanged -= Refresh;
    }

    private void Refresh()
    {
        if (scoreManager == null)
            return;

        scoreText.text = $"SCORE\n{scoreManager.Score}";
        comboText.text = $"COMBO\n{scoreManager.Combo}";
        multiplierText.text = $"x{scoreManager.Multiplier}";
        accuracyText.text = $"ACCURACY\n{scoreManager.Accuracy:0.0}%";
    }
}
