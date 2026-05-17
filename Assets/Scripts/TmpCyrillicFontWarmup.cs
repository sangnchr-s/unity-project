using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Общий источник TMP-шрифта для UI симулятора.
/// Используем стабильный asset-шрифт (без runtime CreateFontAsset), чтобы избежать MissingReferenceException по atlas textures.
/// </summary>
static class TmpCyrillicFontWarmup
{
    /// <summary>Использовать вместо defaultFontAsset для всего UI симулятора.</summary>
    public static TMP_FontAsset PreferredUiFont { get; private set; }
    static TMP_FontAsset _runtimeOsFontAsset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BeforeSceneLoad()
    {
        EnsurePreferredUiFont();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoad()
    {
        EnsurePreferredUiFont();
        ApplyPreferredFontToAllTmp();
    }

    /// <summary>После создания UI — обновить меши (на случай динамически добавленных объектов).</summary>
    public static void WarmupCyrillicAndRefreshTmp()
    {
        EnsurePreferredUiFont();
        ApplyPreferredFontToAllTmp();
    }

    static void EnsurePreferredUiFont()
    {
        if (IsFontUsable(PreferredUiFont))
            return;

        var primary = TryCreateRuntimeOsFontAsset();
        if (!IsFontUsable(primary))
            primary = TMP_Settings.defaultFontAsset;
        if (!IsFontUsable(primary))
            primary = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (!IsFontUsable(primary))
        {
            Debug.LogError("TmpCyrillicFontWarmup: не найден пригодный TMP-шрифт (default/LiberationSans SDF).");
            return;
        }

        var fallbacks = primary.fallbackFontAssetTable ?? new List<TMP_FontAsset>();
        var sdfFallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
        if (IsFontUsable(sdfFallback) && !fallbacks.Contains(sdfFallback))
            fallbacks.Add(sdfFallback);
        primary.fallbackFontAssetTable = fallbacks;
        primary.isMultiAtlasTexturesEnabled = true;

        PreferredUiFont = primary;
    }

    static TMP_FontAsset TryCreateRuntimeOsFontAsset()
    {
        if (IsFontUsable(_runtimeOsFontAsset))
            return _runtimeOsFontAsset;

        // Список с запасом: на macOS/Windows/Linux обычно присутствует хотя бы один.
        var osFont = Font.CreateDynamicFontFromOSFont(new[]
        {
            "Arial",
            "Arial Unicode MS",
            "Helvetica Neue",
            "Segoe UI",
            "Tahoma",
            "Verdana",
            "Times New Roman",
            "Noto Sans",
            "DejaVu Sans"
        }, 64);

        if (osFont == null)
            return null;

        try
        {
            var runtime = TMP_FontAsset.CreateFontAsset(osFont);
            if (runtime == null)
                return null;
            runtime.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            runtime.name = "Runtime_Cyrillic_UI";
            runtime.isMultiAtlasTexturesEnabled = true;
            runtime.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _runtimeOsFontAsset = runtime;
            return _runtimeOsFontAsset;
        }
        catch
        {
            return null;
        }
    }

    static bool IsFontUsable(TMP_FontAsset font)
    {
        if (font == null)
            return false;
        try
        {
            if (font.atlasTexture == null && (font.atlasTextures == null || font.atlasTextures.Length == 0))
                return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    static void ApplyPreferredFontToAllTmp()
    {
        if (!IsFontUsable(PreferredUiFont))
            return;

#if UNITY_2023_1_OR_NEWER
        var texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var texts = Object.FindObjectsOfType<TextMeshProUGUI>(true);
#endif
        foreach (var t in texts)
        {
            if (t == null)
                continue;
            try
            {
                t.font = PreferredUiFont;
                // Без ForceMeshUpdate: он может падать, если в сцене есть TMP-объект со «сломленным» font asset.
                t.SetAllDirty();
            }
            catch
            {
                // Игнорируем только повреждённый экземпляр текста, не валим весь UI.
            }
        }
    }
}
