using System;
using System.Linq;
using System.Reflection;
using KKAPI.Utilities;
using UnityEngine;

namespace KK_SkinEffects
{
    internal static class TextureLoader
    {
        private static readonly Assembly _resourceAssembly;

        private static Texture2D[] GetTextures(string[] resourceNames)
        {
            byte[] ReadResource(string resourceName)
            {
                using (var stream = _resourceAssembly.GetManifestResourceStream(resourceName))
                    return (stream ?? throw new InvalidOperationException($"The resource {resourceName} was not found")).ReadAllBytes();
            }

            return resourceNames.Select(ReadResource).Select(bytes => bytes.LoadTexture(TextureFormat.DXT5)).ToArray();
        }

        // Just add textures for additional levels here, everything should scale automatically.
        // Blood might require tweaking of the severity algorithm to make it work well.
        private static readonly string[] _bldResources;
        private static readonly string[] _cumResources;
        private static readonly string[] _wetBodyResources;
        private static readonly string[] _wetFaceResources;
        private static readonly string[] _tearResources;
        private static readonly string[] _droolResources;
        private static readonly string[] _buttResources;

        static TextureLoader()
        {
            _resourceAssembly = Assembly.GetExecutingAssembly();
            var resourceNames = _resourceAssembly.GetManifestResourceNames().OrderBy(x => x).ToList();

            _bldResources = resourceNames.Where(x => x.IndexOf("BloodBody", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            _cumResources = resourceNames.Where(x => x.IndexOf("BukkakeBody", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            _wetBodyResources = resourceNames.Where(x => x.IndexOf("WetBody", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            _wetFaceResources = resourceNames.Where(x => x.IndexOf("WetFace", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            _tearResources = resourceNames.Where(x => x.IndexOf("TearFace", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            _droolResources = resourceNames.Where(x => x.IndexOf("DroolFace", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            _buttResources = resourceNames.Where(x => x.IndexOf("ButtBody", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        }

        public static int BldTexturesCount => _bldResources.Length;
        public static int CumTexturesCount => _cumResources.Length;
        public static int WetTexturesBodyCount => _wetBodyResources.Length;
        public static int WetTexturesFaceCount => _wetFaceResources.Length;
        public static int DroolTexturesCount => _droolResources.Length;
        public static int TearTexturesCount => _tearResources.Length;
        public static int ButtTexturesCount => _tearResources.Length;

        private static Texture2D[] _bldTextures;
        private static Texture2D[] _cumTextures;
        private static Texture2D[] _wetTexturesBody;
        private static Texture2D[] _wetTexturesFace;
        private static Texture2D[] _droolTextures;
        private static Texture2D[] _tearTextures;
        private static Texture2D[] _buttTextures;

        public static Texture2D[] BldTextures
        {
            get
            {
                if (_bldTextures == null)
                    _bldTextures = GetTextures(_bldResources);

                return _bldTextures;
            }
        }

        public static Texture2D[] CumTextures
        {
            get
            {
                if (_cumTextures == null)
                    _cumTextures = GetTextures(_cumResources);

                return _cumTextures;
            }
        }

        public static Texture2D[] WetTexturesBody
        {
            get
            {
                if (_wetTexturesBody == null)
                    _wetTexturesBody = GetTextures(_wetBodyResources);

                return _wetTexturesBody;
            }
        }

        public static Texture2D[] WetTexturesFace
        {
            get
            {
                if (_wetTexturesFace == null)
                    _wetTexturesFace = GetTextures(_wetFaceResources);

                return _wetTexturesFace;
            }
        }

        public static Texture2D[] DroolTextures
        {
            get
            {
                if (_droolTextures == null)
                    _droolTextures = GetTextures(_droolResources);

                return _droolTextures;
            }
        }

        public static Texture2D[] TearTextures
        {
            get
            {
                if (_tearTextures == null)
                    _tearTextures = GetTextures(_tearResources);

                return _tearTextures;
            }
        }

        public static Texture2D[] ButtTextures
        {
            get
            {
                if (_buttTextures == null)
                    _buttTextures = GetTextures(_buttResources);

                return _buttTextures;
            }
        }

        public static void PreloadAllTextures()
        {
            // Preload the textures
            var _ = TearTextures;
            _ = DroolTextures;
            _ = WetTexturesBody;
            _ = WetTexturesFace;
            _ = CumTextures;
            _ = BldTextures;
        }
    }
}