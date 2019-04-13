using System.Linq;
using UnityEngine;

namespace KK_SkinEffects
{
    internal static class TextureLoader
    {
        /// <summary>
        /// Just add textures for additional levels here, everything should scale automatically.
        /// Blood might require tweaking of the severity algorithm to make it work well.
        /// </summary>
        private static byte[][] GetBldResources() => new[] { Overlays.BloodBody_01, Overlays.BloodBody_02, Overlays.BloodBody_03 };
        private static byte[][] GetCumResources() => new[] { Overlays.BukkakeBody_01, Overlays.BukkakeBody_02, Overlays.BukkakeBody_03 };
        private static byte[][] GetWetBodyResources() => new[] { Overlays.SweatBody, Overlays.WetBody_01, Overlays.WetBody_02 };
        private static byte[][] GetWetFaceResources() => new[] { Overlays.SweatFace, Overlays.WetFace_01, Overlays.WetFace_02 };
        private static byte[][] GetTearResources() => new[] { Overlays.TearFace_01, Overlays.TearFace_02, Overlays.TearFace_03 };
        private static byte[][] GetDroolResources() => new[] { Overlays.Drool_Face };

        static TextureLoader()
        {
            BldTexturesCount = GetBldResources().Length;
            CumTexturesCount = GetCumResources().Length;
            WetTexturesBodyCount = GetWetBodyResources().Length;
            WetTexturesFaceCount = GetWetFaceResources().Length;
            DroolTexturesCount = GetDroolResources().Length;
            TearTexturesCount = GetTearResources().Length;

            Overlays.ResourceManager.ReleaseAllResources();
        }

        public static int BldTexturesCount { get; }
        public static int CumTexturesCount { get; }
        public static int WetTexturesBodyCount { get; }
        public static int WetTexturesFaceCount { get; }
        public static int DroolTexturesCount { get; }
        public static int TearTexturesCount { get; }

        private static Texture2D[] _bldTextures;
        private static Texture2D[] _cumTextures;
        private static Texture2D[] _wetTexturesBody;
        private static Texture2D[] _wetTexturesFace;
        private static Texture2D[] _droolTextures;
        private static Texture2D[] _tearTextures;

        public static Texture2D[] BldTextures
        {
            get
            {
                if (_bldTextures == null)
                    InitializeTextures();

                return _bldTextures;
            }
        }

        public static Texture2D[] CumTextures
        {
            get
            {
                if (_cumTextures == null)
                    InitializeTextures();
                return _cumTextures;
            }
        }

        public static Texture2D[] WetTexturesBody
        {
            get
            {
                if (_wetTexturesBody == null)
                    InitializeTextures();
                return _wetTexturesBody;
            }
        }

        public static Texture2D[] WetTexturesFace
        {
            get
            {
                if (_wetTexturesFace == null)
                    InitializeTextures();
                return _wetTexturesFace;
            }
        }

        public static Texture2D[] DroolTextures
        {
            get
            {
                if (_droolTextures == null)
                    InitializeTextures();
                return _droolTextures;
            }
        }

        public static Texture2D[] TearTextures
        {
            get
            {
                if (_tearTextures == null)
                    InitializeTextures();
                return _tearTextures;
            }
        }

        public static void InitializeTextures()
        {
            if(_bldTextures != null) return;

            Texture2D[] MakeArray(byte[][] textures)
            {
                return textures.Select(x =>
                {
                    var texture2D = new Texture2D(1, 1, TextureFormat.DXT5, false);
                    texture2D.LoadImage(x);
                    return texture2D;
                }).ToArray();
            }

            _bldTextures = MakeArray(GetBldResources());

            _cumTextures = MakeArray(GetCumResources());

            _wetTexturesBody = MakeArray(GetWetBodyResources());
            _wetTexturesFace = MakeArray(GetWetFaceResources());

            _tearTextures = MakeArray(GetTearResources());

            _droolTextures = MakeArray(GetDroolResources());

            Overlays.ResourceManager.ReleaseAllResources();
        }
    }
}