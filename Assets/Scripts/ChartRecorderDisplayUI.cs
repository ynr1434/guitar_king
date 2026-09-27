using UnityEngine;
using UnityEngine.UI;

public sealed class ChartRecorderDisplayUI : MonoBehaviour
{
    [SerializeField] private ChartRecorder recorder;
    [SerializeField] private Text recorderText;

    private void OnEnable()
    {
        if (recorder != null)
            recorder.ValuesChanged += Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (recorder != null)
            recorder.ValuesChanged -= Refresh;
    }

    private void Refresh()
    {
        if (recorder == null || recorderText == null)
            return;

        string mode = recorder.Recording ? "REC  •  RECORDING" : "RECORDING OFF";
        string lastTime = recorder.LastRecordedHitTime >= 0.0
            ? recorder.LastRecordedHitTime.ToString("0.000")
            : "---";
        recorderText.text = $"{mode}\nRecorded Notes: {recorder.RecordedNoteCount}\nLast: {lastTime}\n{recorder.LastStatus}";
        recorderText.color = recorder.Recording
            ? new Color(1f, 0.18f, 0.18f)
            : new Color(0.8f, 0.8f, 0.8f);
    }
}
