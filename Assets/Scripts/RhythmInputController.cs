using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RhythmInputController : MonoBehaviour
{
    [System.Serializable]
    private sealed class TargetBinding
    {
        public Transform target;
        public Renderer targetRenderer;
        public Color baseColor = Color.white;

        [System.NonSerialized] public Vector3 normalScale;
        [System.NonSerialized] public MaterialPropertyBlock properties;
    }

    [SerializeField] private TargetBinding[] targets = new TargetBinding[5];
    [SerializeField, Min(1f)] private float pressedScale = 1.2f;
    [SerializeField, Min(1f)] private float pressedBrightness = 2.2f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly Key[] FretKeys =
    {
        Key.Digit1,
        Key.Digit2,
        Key.Digit3,
        Key.Digit4,
        Key.Digit5
    };
    private readonly bool[] heldFrets = new bool[5];

    public event Action Strummed;
    public event Action<RhythmLane, Transform, Color> FretPressed;

    public float PressedBrightness => pressedBrightness;

    public Transform GetLaneTarget(RhythmLane lane)
    {
        int index = (int)lane;
        return index >= 0 && index < targets.Length && targets[index] != null
            ? targets[index].target
            : null;
    }

    public Renderer GetLaneRenderer(RhythmLane lane)
    {
        int index = (int)lane;
        return index >= 0 && index < targets.Length && targets[index] != null
            ? targets[index].targetRenderer
            : null;
    }

    public Color GetLaneColor(RhythmLane lane)
    {
        int index = (int)lane;
        return index >= 0 && index < targets.Length && targets[index] != null
            ? targets[index].baseColor
            : Color.white;
    }

    private void Awake()
    {
        foreach (TargetBinding binding in targets)
        {
            if (binding == null || binding.target == null || binding.targetRenderer == null)
                continue;

            binding.normalScale = binding.target.localScale;
            binding.properties = new MaterialPropertyBlock();
            SetPressed(binding, false);
        }
    }

    private void Update()
    {
        if (PauseController.IsPaused)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                TargetBinding binding = targets[i];
                if (i < heldFrets.Length)
                    heldFrets[i] = false;
                if (binding != null && binding.target != null && binding.targetRenderer != null)
                    SetPressed(binding, false);
            }
            return;
        }

        if (ResultsController.GameplayFinished)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                TargetBinding binding = targets[i];
                if (i < heldFrets.Length)
                    heldFrets[i] = false;
                if (binding != null && binding.target != null && binding.targetRenderer != null)
                    SetPressed(binding, false);
            }
            return;
        }

        Keyboard keyboard = Keyboard.current;

        for (int i = 0; i < targets.Length; i++)
        {
            TargetBinding binding = targets[i];
            if (i >= FretKeys.Length || binding == null || binding.target == null || binding.targetRenderer == null)
                continue;

            bool isPressed = keyboard != null && keyboard[FretKeys[i]].isPressed;
            if (keyboard != null && keyboard[FretKeys[i]].wasPressedThisFrame)
                FretPressed?.Invoke((RhythmLane)i, binding.target, binding.baseColor);
            if (i < heldFrets.Length)
                heldFrets[i] = isPressed;
            SetPressed(binding, isPressed);
        }

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && !ChartRecorder.IsRecordingActive)
            Strummed?.Invoke();
    }

    public bool TryGetSingleHeldLane(out RhythmLane lane)
    {
        int heldIndex = -1;
        for (int i = 0; i < heldFrets.Length; i++)
        {
            if (!heldFrets[i])
                continue;

            if (heldIndex >= 0)
            {
                lane = default;
                return false;
            }

            heldIndex = i;
        }

        lane = heldIndex >= 0 ? (RhythmLane)heldIndex : default;
        return heldIndex >= 0;
    }

    public bool IsLaneHeld(RhythmLane lane)
    {
        int index = (int)lane;
        return index >= 0 && index < heldFrets.Length && heldFrets[index];
    }

    public RhythmLane[] GetHeldLanes()
    {
        List<RhythmLane> result = new();
        for (int i = 0; i < heldFrets.Length; i++)
        {
            if (heldFrets[i])
                result.Add((RhythmLane)i);
        }
        return result.ToArray();
    }

    private void OnDisable()
    {
        foreach (TargetBinding binding in targets)
        {
            if (binding != null && binding.target != null && binding.targetRenderer != null)
                SetPressed(binding, false);
        }
    }

    private void SetPressed(TargetBinding binding, bool isPressed)
    {
        binding.target.localScale = binding.normalScale * (isPressed ? pressedScale : 1f);

        Color displayColor = binding.baseColor * (isPressed ? pressedBrightness : 1f);
        displayColor.a = 1f;
        binding.targetRenderer.GetPropertyBlock(binding.properties);
        binding.properties.SetColor(BaseColorId, displayColor);
        binding.properties.SetColor(EmissionColorId, isPressed ? binding.baseColor * pressedBrightness : Color.black);
        binding.targetRenderer.SetPropertyBlock(binding.properties);
    }
}
