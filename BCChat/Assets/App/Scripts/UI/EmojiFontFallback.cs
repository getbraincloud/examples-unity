using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Adds the platform's color emoji font to TextMeshPro's emoji fallback list.
/// </summary>
public static class EmojiFontFallback
{
    private static readonly string[] FontFamilies =
    {
        "Segoe UI Emoji",
        "Apple Color Emoji",
        "Noto Color Emoji",
        "Noto Emoji"
    };

    private static TMP_FontAsset systemEmojiFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        foreach (string family in FontFamilies)
        {
            // TextMeshPro resolves the platform-specific font file and face
            // internally, then creates a dynamic atlas as glyphs are needed.
            systemEmojiFont = TMP_FontAsset.CreateFontAsset(family, "Regular", 90);
            if (systemEmojiFont == null)
            {
                continue;
            }

            systemEmojiFont.name = $"{family} BCChat Runtime Fallback";
            List<TMP_Asset> fallbacks = TMP_Settings.emojiFallbackTextAssets ?? new List<TMP_Asset>();
            if (!fallbacks.Contains(systemEmojiFont))
            {
                fallbacks.Insert(0, systemEmojiFont);
                TMP_Settings.emojiFallbackTextAssets = fallbacks;
            }
            return;
        }

        Debug.LogWarning("BCChat could not find a system emoji font. Emoji coverage will be limited to bundled fallbacks.");
    }
}
