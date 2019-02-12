using System.ComponentModel;
using BepInEx;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using UniRx;
using UnityEngine;

namespace KK_SkinEffects
{
    [BepInPlugin(GUID, "Additional Skin Effects", Version)]
    internal partial class SkinEffectsMgr : BaseUnityPlugin
    {
        public const string GUID = "Marco.SkinEffects";
        internal const string Version = "1.1";

        internal static readonly Texture2D[] BldTextures = new Texture2D[3];
        internal static readonly Texture2D[] CumTextures = new Texture2D[4];
        internal static readonly Texture2D[] WetTexturesBody = new Texture2D[2];
        internal static readonly Texture2D[] WetTexturesFace = new Texture2D[2];

        [DisplayName("Enable virgin bleeding")]
        [Description("Doesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableBld { get; private set; }

        [DisplayName("Enable bukkake")]
        [Description("Doesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableCum { get; private set; }

        [DisplayName("Enable sweating/wet under shower")]
        [Description("Doesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableSwt { get; private set; }

        private void Start()
        {
            EnableBld = new ConfigWrapper<bool>(nameof(EnableBld), this, true);
            EnableCum = new ConfigWrapper<bool>(nameof(EnableCum), this, true);
            EnableSwt = new ConfigWrapper<bool>(nameof(EnableSwt), this, true);

            Hooks.InstallHook();

            CharacterApi.RegisterExtraBehaviour<SkinEffectsController>(GUID);

            InitializeTextures();

            if (StudioAPI.InsideStudio)
                RegisterStudioControls();
        }

        /// <summary>
        /// Just add textures for additional levels here, everything should scale automatically.
        /// Blood might require tweaking of the severity algorithm to make it work well.
        /// </summary>
        private static void InitializeTextures()
        {
            void InitArray(Texture2D[] arr)
            {
                for (var i = 0; i < arr.Length; i++)
                    arr[i] = new Texture2D(1, 1);
            }

            InitArray(CumTextures);
            CumTextures[0].LoadImage(Overlays.c1);
            CumTextures[1].LoadImage(Overlays.c2);
            CumTextures[2].LoadImage(Overlays.c3);
            CumTextures[3].LoadImage(Overlays.c4);

            InitArray(BldTextures);
            BldTextures[0].LoadImage(Overlays.b1);
            BldTextures[1].LoadImage(Overlays.b2);
            BldTextures[2].LoadImage(Overlays.b3);

            InitArray(WetTexturesFace);
            WetTexturesFace[0].LoadImage(Overlays.SweatFace);
            WetTexturesFace[1].LoadImage(Overlays.WetFace);

            InitArray(WetTexturesBody);
            WetTexturesBody[0].LoadImage(Overlays.SweatBody);
            WetTexturesBody[1].LoadImage(Overlays.WetBody);
        }

        private static void RegisterStudioControls()
        {
            var sweatTgl = new CurrentStateCategoryToggle("Sweat", Mathf.Min(4, WetTexturesFace.Length + 1),
                                            c => RescaleLevel(c.charInfo.GetComponent<SkinEffectsController>().SweatLevel, WetTexturesFace.Length, 3));
            sweatTgl.SelectedIndex.Subscribe(Observer.Create((int x) =>
            {
                var controller = GetSelectedController();
                if (controller != null)
                    controller.SweatLevel = RescaleLevel(x, sweatTgl.ToggleCount - 1, WetTexturesFace.Length);
            }));

            var cumTgl = new CurrentStateCategoryToggle("Bukkake", Mathf.Min(4, CumTextures.Length + 1),
                            c => RescaleLevel(c.charInfo.GetComponent<SkinEffectsController>().BukkakeLevel, CumTextures.Length, 3));
            cumTgl.SelectedIndex.Subscribe(Observer.Create((int x) =>
            {
                var controller = GetSelectedController();
                if (controller != null)
                    controller.BukkakeLevel = RescaleLevel(x, cumTgl.ToggleCount - 1, CumTextures.Length);
            }));

            var bldTgl = new CurrentStateCategoryToggle("Virgin blood", Mathf.Min(4, BldTextures.Length + 1),
                            c => RescaleLevel(c.charInfo.GetComponent<SkinEffectsController>().BloodLevel, BldTextures.Length, 3));
            bldTgl.SelectedIndex.Subscribe(Observer.Create((int x) =>
            {
                var controller = GetSelectedController();
                if (controller != null)
                    controller.BloodLevel = RescaleLevel(x, bldTgl.ToggleCount - 1, BldTextures.Length);
            }));

            StudioAPI.CreateCurrentStateCategory(new CurrentStateCategory("Additional skin effects", new[] { sweatTgl, cumTgl, bldTgl }));
        }

        private static SkinEffectsController GetSelectedController()
        {
            return FindObjectOfType<MPCharCtrl>()?.ociChar?.charInfo?.GetComponent<SkinEffectsController>();
        }

        private static int RescaleLevel(int lvl, int maxInLvl, int maxOutLvl)
        {
            var rescaledLvl = maxInLvl < maxOutLvl ? lvl : Mathf.RoundToInt(lvl * (float)maxOutLvl / maxInLvl);
            return Mathf.Clamp(rescaledLvl, 0, maxOutLvl);
        }
    }
}
