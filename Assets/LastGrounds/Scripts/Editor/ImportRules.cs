using UnityEditor;
using UnityEngine;

namespace LastGrounds.EditorTools
{
    /// <summary>Import settings for pixel art, fonts and audio under Assets/LastGrounds.</summary>
    public static class ImportRules
    {
        public const string Root = "Assets/LastGrounds";
        public const string SpritesRoot = Root + "/Art/Sprites";
        public const string FontsRoot = Root + "/Art/Fonts";
        public const string MusicRoot = Root + "/Audio/Music";
        public const string AudioRoot = Root + "/Audio";

        /// <summary>Point-filtered, uncompressed, 16 px per unit, centred pivot.</summary>
        public static bool ApplySprite(TextureImporter ti)
        {
            bool changed = false;
            if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
            if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!Mathf.Approximately(ti.spritePixelsPerUnit, PlayField.PixelsPerUnit)) { ti.spritePixelsPerUnit = PlayField.PixelsPerUnit; changed = true; }
            if (ti.filterMode != FilterMode.Point) { ti.filterMode = FilterMode.Point; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (ti.mipmapEnabled) { ti.mipmapEnabled = false; changed = true; }
            if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }

            TextureImporterSettings settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            if (settings.spriteAlignment != (int)SpriteAlignment.Center || settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);
                changed = true;
            }
            return changed;
        }

        /// <summary>Aliased rendering keeps the pixel font crisp.</summary>
        public static bool ApplyFont(TrueTypeFontImporter fi)
        {
            if (fi.fontRenderingMode == FontRenderingMode.HintedRaster) return false;
            fi.fontRenderingMode = FontRenderingMode.HintedRaster;
            return true;
        }

        /// <summary>Music streams from disk; short effects are decompressed in memory.</summary>
        public static bool ApplyAudio(AudioImporter ai, bool isMusic)
        {
            AudioImporterSampleSettings s = ai.defaultSampleSettings;
            AudioClipLoadType wanted = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            if (s.loadType == wanted) return false;
            s.loadType = wanted;
            ai.defaultSampleSettings = s;
            return true;
        }
    }

    /// <summary>Applies <see cref="ImportRules"/> to assets added later.</summary>
    public class LastGroundsAssetPostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(ImportRules.SpritesRoot)) ImportRules.ApplySprite((TextureImporter)assetImporter);
        }

        void OnPreprocessAudio()
        {
            if (assetPath.StartsWith(ImportRules.AudioRoot))
                ImportRules.ApplyAudio((AudioImporter)assetImporter, assetPath.StartsWith(ImportRules.MusicRoot));
        }

        void OnPreprocessAsset()
        {
            TrueTypeFontImporter fi = assetImporter as TrueTypeFontImporter;
            if (fi != null && assetPath.StartsWith(ImportRules.FontsRoot)) ImportRules.ApplyFont(fi);
        }
    }
}
