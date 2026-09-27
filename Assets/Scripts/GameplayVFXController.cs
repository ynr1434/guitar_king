using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(10000)]
public sealed class GameplayVFXController : MonoBehaviour
{
    private sealed class BurstSlot
    {
        public GameObject GameObject;
        public ParticleSystem Particles;
        public double ReturnAt;
    }

    private const int MaximumBurstObjects = 18;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private readonly List<BurstSlot> burstPool = new();
    private readonly bool[] sustainActive = new bool[5];
    private readonly double[] punchStart = new double[5];
    private readonly double[] punchDuration = new double[5];
    private readonly float[] punchAmount = new float[5];
    private readonly double[] pressGlowStart = new double[5];
    private readonly double[] pressGlowEnd = new double[5];

    [Header("Fret Press VFX")]
    [SerializeField, Range(0.05f, 0.35f)] private float pressPunchAmount = 0.16f;
    [SerializeField, Range(0.05f, 0.35f)] private float pressEffectDuration = 0.18f;
    [SerializeField, Range(1, 20)] private int pressSparkCount = 9;

    private RhythmInputController input;
    private HitDetector hitDetector;
    private ScoreManager scoreManager;
    private SustainController sustainController;
    private Transform[] hitPoints = new Transform[5];
    private Renderer[] laneRenderers = new Renderer[5];
    private Renderer hitLineRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Color hitLineBaseColor = Color.white;
    private double effectTime;
    private int lastMultiplier = 1;

    private Color hitLineFlashColor;
    private float hitLineFlashIntensity;
    private double hitLineFlashStart;
    private double hitLineFlashDuration;
    private double hitLineFlashEnd;

    private GameObject canvasObject;
    private RectTransform popupRect;
    private Text popupText;
    private bool popupActive;
    private double popupStartTime;
    private float popupDuration;
    private Vector2 popupBasePosition;

    private Camera gameplayCamera;
    private Transform shakeLayer;
    private Vector3 shakeBasePosition;
    private double shakeStartTime;
    private float shakeDuration;
    private float shakeAmplitude;
    private bool pausedLastFrame;
    private Material burstMaterial;

    private void Start()
    {
        ResolveReferences();
        CacheSceneVisuals();
        BuildPopupUI();

        if (input != null)
            input.FretPressed += HandleFretPressed;
        if (hitDetector != null)
        {
            hitDetector.NoteJudged += HandleNoteJudged;
            hitDetector.WrongStrum += HandleWrongStrum;
        }
        if (scoreManager != null)
        {
            lastMultiplier = scoreManager.Multiplier;
            scoreManager.ValuesChanged += HandleScoreChanged;
        }
        if (sustainController != null)
            sustainController.SustainStateChanged += HandleSustainChanged;
    }

    private void OnDestroy()
    {
        if (burstMaterial != null) Destroy(burstMaterial);
        if (input != null)
            input.FretPressed -= HandleFretPressed;
        if (hitDetector != null)
        {
            hitDetector.NoteJudged -= HandleNoteJudged;
            hitDetector.WrongStrum -= HandleWrongStrum;
        }
        if (scoreManager != null)
            scoreManager.ValuesChanged -= HandleScoreChanged;
        if (sustainController != null)
            sustainController.SustainStateChanged -= HandleSustainChanged;
    }

    private void Update()
    {
        if (ResultsController.GameplayFinished || FailController.SongFailed)
            System.Array.Clear(sustainActive, 0, sustainActive.Length);

        bool paused = PauseController.IsPaused;
        if (paused != pausedLastFrame)
        {
            SetParticlesPaused(paused);
            pausedLastFrame = paused;
        }

        if (!paused)
            effectTime += Time.unscaledDeltaTime;

        UpdateBurstPool();
        UpdatePopup();

#if UNITY_EDITOR
        HandleDebugKeys();
#endif
    }

    private void LateUpdate()
    {
        if (PauseController.IsPaused)
            return;

        UpdateLaneVisuals();
        UpdateHitLineVisual();
        UpdateCameraShake();
    }

    private void ResolveReferences()
    {
        input = FindFirstObjectByType<RhythmInputController>();
        hitDetector = FindFirstObjectByType<HitDetector>();
        scoreManager = FindFirstObjectByType<ScoreManager>();
        sustainController = FindFirstObjectByType<SustainController>();
    }

    private void CacheSceneVisuals()
    {
        string[] hitPointNames =
        {
            "GreenHitPoint", "RedHitPoint", "YellowHitPoint", "BlueHitPoint", "OrangeHitPoint"
        };
        for (int i = 0; i < hitPointNames.Length; i++)
        {
            GameObject hitPoint = GameObject.Find(hitPointNames[i]);
            if (hitPoint != null)
                hitPoints[i] = hitPoint.transform;
            laneRenderers[i] = input != null ? input.GetLaneRenderer((RhythmLane)i) : null;
        }

        GameObject hitLineObject = GameObject.Find("HitLineBar");
        if (hitLineObject != null)
        {
            hitLineRenderer = hitLineObject.GetComponent<Renderer>();
            if (hitLineRenderer != null && hitLineRenderer.sharedMaterial != null &&
                hitLineRenderer.sharedMaterial.HasProperty(BaseColorId))
                hitLineBaseColor = hitLineRenderer.sharedMaterial.GetColor(BaseColorId);
        }

        gameplayCamera = Camera.main;
        if (gameplayCamera != null)
            CreateCameraShakeLayer();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void CreateCameraShakeLayer()
    {
        Transform cameraTransform = gameplayCamera.transform;
        Transform originalParent = cameraTransform.parent;
        GameObject layer = new("GameplayCameraShakeOffset");
        shakeLayer = layer.transform;
        shakeLayer.SetParent(originalParent, true);
        shakeLayer.position = cameraTransform.position;
        shakeLayer.rotation = cameraTransform.rotation;
        shakeLayer.localScale = Vector3.one;
        cameraTransform.SetParent(shakeLayer, true);
        shakeBasePosition = shakeLayer.localPosition;
    }

    private void BuildPopupUI()
    {
        canvasObject = new GameObject("GameplayVFXCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject popup = new("MultiplierMilestone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        popup.transform.SetParent(canvasObject.transform, false);
        popupRect = popup.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.86f, 0.18f);
        popupRect.anchorMax = new Vector2(0.98f, 0.27f);
        popupRect.offsetMin = Vector2.zero;
        popupRect.offsetMax = Vector2.zero;
        popupBasePosition = popupRect.anchoredPosition;
        popupText = popup.GetComponent<Text>();
        popupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        popupText.fontSize = 54;
        popupText.fontStyle = FontStyle.Bold;
        popupText.alignment = TextAnchor.MiddleCenter;
        popupText.color = new Color(1f, 0.84f, 0.22f, 0f);
        popupText.raycastTarget = false;
        popup.SetActive(false);
    }

    private void HandleFretPressed(RhythmLane lane, Transform target, Color laneColor)
    {
        if (!CanPlayVfx())
            return;

        int index = (int)lane;
        if (index < 0 || index >= 5)
            return;

        pressGlowStart[index] = effectTime;
        pressGlowEnd[index] = effectTime + pressEffectDuration;
        StartPunch(index, pressPunchAmount, pressEffectDuration);

        // Every fret press gets immediate feedback, even before a strum. The
        // small bright core and wider soft halo make the target feel responsive
        // without competing with the larger successful-hit burst.
        Color sparkColor = Color.Lerp(laneColor, Color.white, 0.24f);
        SpawnBurst(lane, sparkColor, 0.15f, 1.65f, 0.22f, pressSparkCount);
        SpawnBurst(lane, laneColor, 0.32f, 0.72f, 0.16f, 2);
        FlashHitLine(laneColor, 0.16f, 0.78f);
    }

    private void HandleNoteJudged(RhythmLane[] lanes, HitResult result)
    {
        if (!CanPlayVfx() || lanes == null || lanes.Length == 0)
            return;

        if (result == HitResult.Miss)
        {
            for (int i = 0; i < lanes.Length; i++)
            {
                int laneIndex = (int)lanes[i];
                if (laneIndex >= 0 && laneIndex < 5)
                    StartPunch(laneIndex, -0.08f, 0.14f);
            }
            FlashHitLine(new Color(1f, 0.12f, 0.12f), 0.16f, 0.78f);
            return;
        }

        float size;
        float brightness;
        float punch;
        float duration;
        switch (result)
        {
            case HitResult.Perfect:
                size = 0.28f;
                brightness = 1.8f;
                punch = 0.22f;
                duration = 0.31f;
                break;
            case HitResult.Great:
                size = 0.24f;
                brightness = 1.45f;
                punch = 0.19f;
                duration = 0.28f;
                break;
            default:
                size = 0.20f;
                brightness = 1.15f;
                punch = 0.16f;
                duration = 0.25f;
                break;
        }

        Color flashColor = Color.black;
        for (int i = 0; i < lanes.Length; i++)
        {
            RhythmLane lane = lanes[i];
            int index = (int)lane;
            Color color = input != null ? input.GetLaneColor(lane) : Color.white;
            flashColor += color;
            if (index >= 0 && index < 5)
            {
                // A judged hit must always restart the target punch, even if
                // the smaller raw fret-press animation is still running.
                StartPunch(index, punch, 0.18f, true);
            }
            SpawnBurst(lane, color, size * (lanes.Length > 1 ? 1.12f : 1f), brightness,
                duration, lanes.Length > 1 ? 14 : 10);
        }

        flashColor /= lanes.Length;
        FlashHitLine(flashColor, lanes.Length > 1 ? 0.19f : 0.14f,
            lanes.Length > 1 ? 1.25f : 0.9f);

        float hitShakeDuration = result switch
        {
            HitResult.Perfect => 0.13f,
            HitResult.Great => 0.11f,
            _ => 0.09f
        };
        float hitShakeStrength = result switch
        {
            HitResult.Perfect => 0.030f,
            HitResult.Great => 0.024f,
            _ => 0.018f
        };
        if (lanes.Length > 1)
            hitShakeStrength *= 1.25f;
        StartCameraShake(hitShakeDuration, hitShakeStrength);
    }

    private void HandleWrongStrum(RhythmLane[] heldLanes)
    {
        if (!CanPlayVfx())
            return;
        FlashHitLine(new Color(1f, 0.1f, 0.1f), 0.12f, 0.55f);
        if (heldLanes == null)
            return;
        foreach (RhythmLane lane in heldLanes)
        {
            int index = (int)lane;
            if (index >= 0 && index < 5)
                StartPunch(index, -0.045f, 0.10f);
        }
    }

    private void HandleScoreChanged()
    {
        if (scoreManager == null)
            return;

        int multiplier = scoreManager.Multiplier;
        if (multiplier > lastMultiplier && multiplier >= 2 && CanPlayVfx())
        {
            ShowMultiplierPopup(multiplier);
            if (multiplier >= 3)
                StartCameraShake(0.11f, 0.028f);
        }
        lastMultiplier = multiplier;
    }

    private void HandleSustainChanged(RhythmLane lane, bool isActive)
    {
        if (PauseController.IsPaused || ResultsController.GameplayFinished || FailController.SongFailed)
            return;

        int index = (int)lane;
        if (index < 0 || index >= sustainActive.Length)
            return;
        sustainActive[index] = isActive;
        if (isActive)
            FlashHitLine(input != null ? input.GetLaneColor(lane) : Color.white, 0.18f, 0.55f);
    }

    private bool CanPlayVfx()
    {
        return !PauseController.IsPaused && !ResultsController.GameplayFinished &&
               !FailController.SongFailed && !ChartRecorder.IsRecordingActive;
    }

    private void StartPunch(int laneIndex, float amount, float duration, bool forceRestart = false)
    {
        if (laneIndex < 0 || laneIndex >= 5)
            return;
        if (forceRestart || Mathf.Abs(amount) >= Mathf.Abs(punchAmount[laneIndex]) ||
            effectTime >= punchStart[laneIndex] + punchDuration[laneIndex])
        {
            punchStart[laneIndex] = effectTime;
            punchDuration[laneIndex] = duration;
            punchAmount[laneIndex] = amount;
        }
    }

    private void UpdateLaneVisuals()
    {
        for (int i = 0; i < 5; i++)
        {
            RhythmLane lane = (RhythmLane)i;
            Transform target = input != null ? input.GetLaneTarget(lane) : null;
            Renderer laneRenderer = laneRenderers[i];
            if (target == null)
                continue;

            float scalePulse = 0f;
            if (punchDuration[i] > 0f)
            {
                float progress = Mathf.Clamp01((float)((effectTime - punchStart[i]) / punchDuration[i]));
                if (progress < 1f)
                    scalePulse = punchAmount[i] * Mathf.Sin(progress * Mathf.PI);
            }
            target.localScale *= 1f + scalePulse;

            if (laneRenderer == null || input == null)
                continue;

            float glow = input.IsLaneHeld(lane) ? input.PressedBrightness : 0f;
            Color color = input.GetLaneColor(lane);
            if (effectTime < pressGlowEnd[i])
            {
                float glowDuration = Mathf.Max(0.01f, pressEffectDuration);
                float progress = Mathf.Clamp01((float)((effectTime - pressGlowStart[i]) / glowDuration));
                float attack = Mathf.Clamp01(progress / 0.18f);
                float release = 1f - Mathf.Clamp01((progress - 0.18f) / 0.82f);
                glow = Mathf.Max(glow, 4.2f * attack * release);
            }
            if (sustainActive[i])
                glow += 0.7f + 0.18f * Mathf.Sin((float)effectTime * 7f);

            laneRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, color * glow);
            laneRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void FlashHitLine(Color color, float duration, float intensity)
    {
        if (duration <= 0f)
            return;
        hitLineFlashColor = color;
        hitLineFlashIntensity = intensity;
        hitLineFlashStart = effectTime;
        hitLineFlashDuration = duration;
        hitLineFlashEnd = effectTime + duration;
    }

    private void UpdateHitLineVisual()
    {
        if (hitLineRenderer == null)
            return;

        Color tint = Color.black;
        float intensity = 0f;
        if (effectTime < hitLineFlashEnd && hitLineFlashDuration > 0f)
        {
            float progress = Mathf.Clamp01((float)((effectTime - hitLineFlashStart) / hitLineFlashDuration));
            intensity = hitLineFlashIntensity * (1f - progress);
            tint = hitLineFlashColor;
        }

        Color sustainColor = Color.black;
        int sustainCount = 0;
        for (int i = 0; i < sustainActive.Length; i++)
        {
            if (!sustainActive[i])
                continue;
            sustainColor += input != null ? input.GetLaneColor((RhythmLane)i) : Color.white;
            sustainCount++;
        }
        if (sustainCount > 0)
        {
            sustainColor /= sustainCount;
            float pulse = 0.42f + 0.14f * Mathf.Sin((float)effectTime * 6f);
            if (pulse > intensity)
            {
                tint = sustainColor;
                intensity = pulse;
            }
        }

        hitLineRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, Color.Lerp(hitLineBaseColor, tint, Mathf.Clamp01(intensity * 0.65f)));
        propertyBlock.SetColor(EmissionColorId, tint * intensity);
        hitLineRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SpawnBurst(RhythmLane lane, Color color, float size, float brightness,
        float lifetime, int particleCount)
    {
        if (!CanPlayVfx())
            return;

        float vfxScale = SettingsManager.Instance.VfxScale;
        size *= vfxScale; brightness *= vfxScale; particleCount = Mathf.Max(1, Mathf.RoundToInt(particleCount * vfxScale));
        BurstSlot slot = GetBurstSlot();
        if (slot == null)
            return;

        slot.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        slot.GameObject.transform.position = hitPoints[(int)lane] != null
            ? hitPoints[(int)lane].position
            : transform.position;
        slot.GameObject.transform.rotation = Quaternion.identity;
        slot.GameObject.transform.localScale = Vector3.one;

        ParticleSystem.MainModule main = slot.Particles.main;
        main.duration = lifetime;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = 1.25f + size;
        main.startSize = size;
        main.startColor = color * brightness;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = slot.Particles.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });
        ParticleSystem.ShapeModule shape = slot.Particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.035f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = slot.Particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new();
        gradient.SetKeys(
            new[] { new GradientColorKey(color * brightness, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = slot.Particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(1f, 0.05f)));

        slot.GameObject.SetActive(true);
        slot.Particles.Play(true);
        slot.ReturnAt = effectTime + lifetime + 0.08f;
    }

    private BurstSlot GetBurstSlot()
    {
        foreach (BurstSlot slot in burstPool)
            if (!slot.GameObject.activeSelf)
                return slot;

        if (burstPool.Count >= MaximumBurstObjects)
        {
            BurstSlot oldest = burstPool[0];
            for (int i = 1; i < burstPool.Count; i++)
                if (burstPool[i].ReturnAt < oldest.ReturnAt)
                    oldest = burstPool[i];
            return oldest;
        }

        GameObject burstObject = new("PooledHitBurst", typeof(ParticleSystem));
        burstObject.transform.SetParent(transform, false);
        ParticleSystem particles = burstObject.GetComponent<ParticleSystem>();
        if (burstMaterial == null)
            burstMaterial = new Material(Resources.Load<Shader>("ConcertGlow"));
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = burstMaterial;
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.maxParticles = 24;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        BurstSlot created = new() { GameObject = burstObject, Particles = particles };
        burstPool.Add(created);
        burstObject.SetActive(false);
        return created;
    }

    private void UpdateBurstPool()
    {
        foreach (BurstSlot slot in burstPool)
        {
            if (slot.GameObject.activeSelf && effectTime >= slot.ReturnAt)
            {
                slot.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                slot.GameObject.SetActive(false);
            }
        }
    }

    private void SetParticlesPaused(bool pause)
    {
        foreach (BurstSlot slot in burstPool)
        {
            if (!slot.GameObject.activeSelf)
                continue;
            if (pause)
                slot.Particles.Pause(true);
            else
                slot.Particles.Play(true);
        }
    }

    private void ShowMultiplierPopup(int multiplier)
    {
        if (popupText == null || !CanPlayVfx())
            return;
        popupText.text = $"x{multiplier}";
        float vfxScale = SettingsManager.Instance.VfxScale;
        popupText.color = new Color(1f, 0.84f, 0.22f, Mathf.Clamp01(vfxScale));
        popupRect.localScale = Vector3.one * 0.55f;
        popupRect.anchoredPosition = popupBasePosition;
        popupActive = true;
        popupStartTime = effectTime;
        popupDuration = 0.68f * Mathf.Lerp(.8f,1.18f,(vfxScale-.62f)/.73f);
        popupText.gameObject.SetActive(true);
    }

    private void UpdatePopup()
    {
        if (!popupActive || popupText == null)
            return;

        float progress = Mathf.Clamp01((float)((effectTime - popupStartTime) / popupDuration));
        float punch = progress < 0.22f
            ? Mathf.Lerp(0.55f, 1.2f, progress / 0.22f)
            : Mathf.Lerp(1.2f, 0.92f, (progress - 0.22f) / 0.78f);
        popupRect.localScale = Vector3.one * punch;
        popupRect.anchoredPosition = popupBasePosition + Vector2.up * (46f * progress);
        Color color = popupText.color;
        color.a = 1f - progress;
        popupText.color = color;
        if (progress >= 1f)
        {
            popupActive = false;
            popupText.gameObject.SetActive(false);
        }
    }

    private void StartCameraShake(float duration, float amplitude)
    {
        if (shakeLayer == null || !CanPlayVfx() || !SettingsManager.Instance.ScreenShake)
            return;
        amplitude *= SettingsManager.Instance.VfxScale;
        shakeStartTime = effectTime;
        shakeDuration = duration;
        shakeAmplitude = amplitude;
    }

    private void UpdateCameraShake()
    {
        if (shakeLayer == null)
            return;
        if (!SettingsManager.Instance.ScreenShake) { shakeDuration=0; shakeLayer.localPosition=shakeBasePosition; return; }
        float progress = shakeDuration <= 0f
            ? 1f
            : Mathf.Clamp01((float)((effectTime - shakeStartTime) / shakeDuration));
        if (progress >= 1f)
        {
            shakeLayer.localPosition = shakeBasePosition;
            return;
        }

        // Use the shake's own progress so every judged hit produces a clearly
        // visible, repeatable camera kick regardless of the global timer phase.
        float fade = 1f - progress;
        float phase = progress * Mathf.PI * 5f;
        float x = Mathf.Sin(phase) * shakeAmplitude * fade;
        float y = Mathf.Sin(phase * 1.35f) * shakeAmplitude * fade * 0.68f;
        shakeLayer.localPosition = shakeBasePosition + new Vector3(x, y, 0f);
    }

    private void HandleDebugKeys()
    {
        Keyboard keyboard = Keyboard.current;
        if (!CanPlayVfx() || keyboard == null)
            return;
        if (keyboard.f6Key.wasPressedThisFrame)
            HandleNoteJudged(new[] { RhythmLane.Green }, HitResult.Perfect);
        if (keyboard.f7Key.wasPressedThisFrame)
            HandleNoteJudged(new[] { RhythmLane.Green, RhythmLane.Yellow }, HitResult.Perfect);
        if (keyboard.f8Key.wasPressedThisFrame)
            ShowMultiplierPopup(3);
    }
}
