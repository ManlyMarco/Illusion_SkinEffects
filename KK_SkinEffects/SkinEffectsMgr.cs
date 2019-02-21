using System.ComponentModel;
using System.Linq;
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

        internal static Texture2D[] BldTextures;
        internal static Texture2D[] CumTextures;
        internal static Texture2D[] WetTexturesBody;
        internal static Texture2D[] WetTexturesFace;

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
            Texture2D[] MakeArray(byte[][] textures)
            {
                return textures.Select(x =>
                {
                    var texture2D = new Texture2D(1, 1);
                    texture2D.LoadImage(x);
                    return texture2D;
                }).ToArray();
            }

            BldTextures = MakeArray(new[] { Overlays.BloodBody_01, Overlays.BloodBody_02, Overlays.BloodBody_03 });

            CumTextures = MakeArray(new[] { Overlays.BukkakeBody_01, Overlays.BukkakeBody_02, Overlays.BukkakeBody_03 });

            WetTexturesBody = MakeArray(new[] { Overlays.SweatBody, Overlays.WetBody_01, Overlays.WetBody_02 });
            WetTexturesFace = MakeArray(new[] { Overlays.SweatFace, Overlays.WetFace_01, Overlays.WetFace_02 });
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
