using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class RayTracingExperimentController : MonoBehaviour
{
    public enum RenderTechnique
    {
        RecursiveRayTracing,
        RealTimePathTracing
    }

    [Header("Referências")]
    [Tooltip("Arraste o Global Volume da cena para este campo.")]
    [SerializeField] private Volume globalVolume;

    [Header("Configurações do Experimento")]
    [SerializeField] private RenderTechnique activeTechnique = RenderTechnique.RecursiveRayTracing;
    [SerializeField] private KeyCode switchKey = KeyCode.Tab;

    [Header("Parâmetros Comparativos (Manter equivalentes para justiça no teste)")]
    [SerializeField, Range(1, 10)] private int rayBouncesDepth = 4;
    [SerializeField] private float maxRayLength = 50f;
    [SerializeField, Range(1, 512)] private int pathTracingSamples = 128;

    private RecursiveRendering _recursiveRendering;
    private PathTracing _pathTracing;
    private float _deltaTime;

    private void Start()
    {
        if (globalVolume == null || globalVolume.profile == null)
        {
            Debug.LogError("[Experimento] Atribua o Global Volume no Inspector!");
            enabled = false;
            return;
        }

        // Obtém os overrides do Volume Profile no HDRP 14 (Unity 2022.3)
        if (!globalVolume.profile.TryGet(out _recursiveRendering))
        {
            Debug.LogError("[Experimento] Override 'Recursive Rendering' não encontrado no Volume Profile.");
            enabled = false;
            return;
        }

        if (!globalVolume.profile.TryGet(out _pathTracing))
        {
            Debug.LogError("[Experimento] Override 'Path Tracing' não encontrado no Volume Profile.");
            enabled = false;
            return;
        }

        ApplyTechnique(activeTechnique);
    }

    private void Update()
    {
        // Cálculo suavizado de Frame Time (ms) e FPS
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

        // Alterna entre Recursive Ray Tracing e Path Tracing com TAB
        if (Input.GetKeyDown(switchKey))
        {
            activeTechnique = (activeTechnique == RenderTechnique.RecursiveRayTracing)
                ? RenderTechnique.RealTimePathTracing
                : RenderTechnique.RecursiveRayTracing;

            ApplyTechnique(activeTechnique);
        }

        // Teclas 1 a 6 alteram a profundidade de raios (Bounces / Max Depth) em tempo real
        for (int i = 1; i <= 6; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                rayBouncesDepth = i;
                ApplyTechnique(activeTechnique);
            }
        }
    }

    public void ApplyTechnique(RenderTechnique technique)
    {
        bool isPathTracing = (technique == RenderTechnique.RealTimePathTracing);

        // 1. Configura Recursive Rendering (HDRP 14 usa .enable como BoolParameter)
        _recursiveRendering.active = !isPathTracing;
        _recursiveRendering.enable.overrideState = true;
        _recursiveRendering.enable.value = !isPathTracing;

        _recursiveRendering.maxDepth.overrideState = true;
        _recursiveRendering.maxDepth.value = rayBouncesDepth;

        _recursiveRendering.rayLength.overrideState = true;
        _recursiveRendering.rayLength.value = maxRayLength;

        // 2. Configura Path Tracing
        _pathTracing.active = isPathTracing;
        _pathTracing.enable.overrideState = true;
        _pathTracing.enable.value = isPathTracing;

        _pathTracing.maximumSamples.overrideState = true;
        _pathTracing.maximumSamples.value = pathTracingSamples;

        _pathTracing.maximumDepth.overrideState = true;
        _pathTracing.maximumDepth.value = rayBouncesDepth;

        Debug.Log($"[Experimento] Modo: {technique} | Profundidade (Depth): {rayBouncesDepth}");
    }

    private void OnGUI()
    {
        float ms = _deltaTime * 1000.0f;
        float fps = 1.0f / _deltaTime;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.white;

        string info = $"Tecnica Ativa [TAB]: {activeTechnique}\n" +
                      $"Max Depth [Teclas 1-6]: {rayBouncesDepth}\n" +
                      $"Path Tracing Samples: {pathTracingSamples}\n" +
                      $"Frame Time: {ms:F2} ms ({fps:F1} FPS)";

        GUI.Box(new Rect(15, 15, 360, 105), info, style);
    }
}