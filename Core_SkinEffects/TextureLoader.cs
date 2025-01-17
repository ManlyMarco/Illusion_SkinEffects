using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using KKAPI.Utilities;
using UnityEngine;

namespace KK_SkinEffects
{
    /// <summary>
    /// Just add textures for additional levels as resources with sequential numbers at the end, everything should scale automatically.
    /// Blood might require tweaking of the severity algorithm to make it work well in-game.
    /// </summary>
    internal static class TextureLoader
    {
        private static readonly Assembly _resourceAssembly;
        private static readonly string[][] _resources;
        private static readonly Texture2D[][] _textures;
        /// <summary>
        /// bool is true if it affects face, false if body
        /// </summary>
        internal static readonly Dictionary<Texture2D, bool> LoadedTextures;

        static TextureLoader()
        {
            LoadedTextures = new Dictionary<Texture2D, bool>();

            _resourceAssembly = Assembly.GetExecutingAssembly();
            var resourceNames = _resourceAssembly.GetManifestResourceNames().OrderBy(x => x).ToList();

            var effectCount = SkinEffectKindUtils.ValidSkinEffectKinds.Length;
            _resources = new string[effectCount][];
            _textures = new Texture2D[effectCount][];

            for (int i = 0; i < effectCount; i++)
            {
                var effectKind = SkinEffectKindUtils.ValidSkinEffectKinds[i];
                var name = Enum.GetName(typeof(SkinEffectKind), effectKind) ?? throw new Exception("Invalid enum value? " + effectKind);
                _resources[i] = resourceNames.Where(x => x.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            }
        }

        public static int GetTextureCount(SkinEffectKind kind)
        {
            if (kind < 0)
                return 0;

            return _resources[(int)kind].Length;
        }

        private static byte[] ReadResource(string resourceName)
        {
            using (var stream = _resourceAssembly.GetManifestResourceStream(resourceName))
                return (stream ?? throw new InvalidOperationException($"The resource {resourceName} was not found")).ReadAllBytes();
        }

        private static Texture2D[] GetTextures(string[] resourceNames)
        {
            return resourceNames.Select(name => ReadResource(name).LoadTexture(TextureFormat.DXT5)).ToArray();
        }

        public static Texture2D[] GetTextures(SkinEffectKind kind)
        {
            if (_textures[(int)kind] == null)
            {
                var newTextures = GetTextures(_resources[(int)kind]);
                _textures[(int)kind] = newTextures;

                var affectsFace = kind.AffectsFace();
                foreach (var newTexture in newTextures)
                    LoadedTextures.Add(newTexture, affectsFace);
            }

            return _textures[(int)kind];
        }

        public static Texture2D GetTexture(SkinEffectKind kind, int level)
        {
            var textures = GetTextures(kind);
            if (level < 0 || level >= textures.Length)
                return null;
            return textures[level];
        }

        public static void PreloadAllTextures()
        {
            if (!_textures.Contains(null))
                return;

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < _textures.Length; i++)
                GetTextures((SkinEffectKind)i);

            SkinEffectsPlugin.Logger.LogDebug($"{nameof(PreloadAllTextures)} finished in {sw.ElapsedMilliseconds}ms");
        }

        public static void PreloadMainGameTextures()
        {
            if (_textures[(int)SkinEffectKind.WetBody] != null &&
                _textures[(int)SkinEffectKind.WetFace] != null)
                return;

            var sw = Stopwatch.StartNew();

            GetTextures(SkinEffectKind.WetBody);
            GetTextures(SkinEffectKind.WetFace);

            SkinEffectsPlugin.Logger.LogDebug($"{nameof(PreloadMainGameTextures)} finished in {sw.ElapsedMilliseconds}ms");
        }
    }
}