using System;
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
        internal const string Version = "1.2";

        internal static Texture2D[] BldTextures;
        internal static Texture2D[] CumTextures;
        internal static Texture2D[] WetTexturesBody;
        internal static Texture2D[] WetTexturesFace;
        internal static Texture2D[] DroolTextures;
        internal static Texture2D[] TearTextures;

        [DisplayName("Enable virgin bleeding")]
        [Description("Doesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableBld { get; private set; }

        [DisplayName("Enable bukkake")]
        [Description("Doesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableCum { get; private set; }

        [DisplayName("Enable sweating/wet under shower")]
        [Description("Doesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableSwt { get; private set; }

        [DisplayName("All virgins bleed regardless of parameters")]
        [Description("Doesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableBldAll { get; private set; }

        private void Start()
        {
            EnableBld = new ConfigWrapper<bool>(nameof(EnableBld), this, true);            
            EnableCum = new ConfigWrapper<bool>(nameof(EnableCum), this, true);
            EnableSwt = new ConfigWrapper<bool>(nameof(EnableSwt), this, true);
            EnableBldAll = new ConfigWrapper<bool>(nameof(EnableBldAll), this, true);

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

            TearTextures = MakeArray(new[] { Overlays.TearFace_01, Overlays.TearFace_02, Overlays.TearFace_03 });

            DroolTextures = MakeArray(new[] { Overlays.Drool_Face });
        }

        private static void RegisterStudioControls()
        {
            CurrentStateCategoryToggle CreateToggle(string name, Texture2D[] textures, Action<SkinEffectsController, int> set, Func<SkinEffectsController, int> get)
            {
                var tgl = new CurrentStateCategoryToggle(name,
                    Mathf.Min(4, textures.Length + 1),
                    c => RescaleLevel(get(c.charInfo.GetComponent<SkinEffectsController>()), textures.Length, 3));

                tgl.SelectedIndex.Subscribe(Observer.Create((int x) =>
                {
                    var controller = GetSelectedController();
                    if (controller != null)
                        set(controller, RescaleLevel(x, tgl.ToggleCount - 1, textures.Length));
                }));

                return tgl;
            }

            var sweatTgl = CreateToggle("Sweat", WetTexturesFace, (controller, i) => controller.SweatLevel = i, controller => controller.SweatLevel);
            var tearsTgl = CreateToggle("Tears", TearTextures, (controller, i) => controller.TearLevel = i, controller => controller.TearLevel);
            var droolTgl = CreateToggle("Drool", DroolTextures, (controller, i) => controller.DroolLevel = i, controller => controller.DroolLevel);
            var cumTgl = CreateToggle("Bukkake", CumTextures, (controller, i) => controller.BukkakeLevel = i, controller => controller.BukkakeLevel);
            var bldTgl = CreateToggle("Virgin blood", BldTextures, (controller, i) => controller.BloodLevel = i, controller => controller.BloodLevel);

            StudioAPI.CreateCurrentStateCategory(new CurrentStateCategory("Additional skin effects", new[] { sweatTgl, tearsTgl, droolTgl, cumTgl, bldTgl }));
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
