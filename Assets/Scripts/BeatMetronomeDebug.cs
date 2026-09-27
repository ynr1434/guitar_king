using UnityEngine;
using UnityEngine.UI;

public sealed class BeatMetronomeDebug : MonoBehaviour
{
    [SerializeField] private BeatGrid beatGrid;
    [SerializeField] private Text beatText;
    [SerializeField, Min(0.05f)] private float flashDuration = 0.12f;

    private long lastBeat = long.MinValue;
    private float flashUntil;

    private void Update()
    {
        double beatPosition = beatGrid.CurrentBeatPosition;
        if (beatPosition >= 0.0)
        {
            long beat = (long)System.Math.Floor(beatPosition);
            if (beat != lastBeat)
            {
                lastBeat = beat;
                flashUntil = Time.unscaledTime + flashDuration;
            }
        }

        bool isFlashing = Time.unscaledTime < flashUntil;
        beatText.color = isFlashing
            ? new Color(1f, 0.85f, 0.15f, 1f)
            : new Color(1f, 1f, 1f, 0.18f);
        beatText.transform.localScale = isFlashing ? Vector3.one * 1.25f : Vector3.one;
    }
}
