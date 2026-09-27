using UnityEngine;
using UnityEngine.UI;

public sealed class SongPackageDisplayUI : MonoBehaviour
{
    [SerializeField] private SongPackageLoader loader;
    [SerializeField] private Text songInfoText;

    private void OnEnable()
    {
        if (loader != null)
            loader.StateChanged += Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (loader != null)
            loader.StateChanged -= Refresh;
    }

    private void Refresh()
    {
        if (loader == null || songInfoText == null)
            return;

        string status = loader.State.ToString().ToUpperInvariant();
        songInfoText.text = $"{loader.SongTitle}\n{loader.Artist}\nBPM {loader.BPM:0.##}\n{loader.Difficulty.ToString().ToUpperInvariant()}\n{status}";
        songInfoText.color = loader.State switch
        {
            SongPackageState.Ready => new Color(0.45f, 1f, 0.55f),
            SongPackageState.Error => new Color(1f, 0.25f, 0.25f),
            _ => Color.white
        };
    }
}
