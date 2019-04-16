using System;
using System.ComponentModel;
using BepInEx;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_SkinEffects
{
    [BepInPlugin(GUID, "Additional Skin Effects", Version)]
    internal partial class SkinEffectsMgr : BaseUnityPlugin
    {
        public const string GUID = "Marco.SkinEffects";
        internal const string Version = "1.4";

        [DisplayName("!Enable virgin bleeding")]
        [Description("When penetrated for the first time, virgins have a chance to bleed. The extent varies based on their status." +
                     "\n\nDoesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableBld { get; private set; }

        [DisplayName("!Virgins always bleed")]
        [Description("By default some girls might not bleed on their first time depending on some parameters. " +
                     "This setting makes sure some blood to always be there." +
                     "\n\nDoesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableBldAlways { get; private set; }

        [DisplayName("Enable bukkake")]
        [Description("Triggers when cumming inside." +
                     "\n\nDoesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableCum { get; private set; }

        [DisplayName("Enable sweating/wet under shower")]
        [Description("When excited girls sweat, same deal under the shower." +
                     "\n\nDoesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableSwt { get; private set; }

        [DisplayName("Enable tears")]
        [Description("Doesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableTear { get; private set; }

        [DisplayName("Enable drool")]
        [Description("Doesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableDrl { get; private set; }

        [DisplayName("After H effects persist in school")]
        [Description("Characters keep the skin effects after H in story mode. Effects get cleared after period change.")]
        public static ConfigWrapper<bool> EnablePersistance { get; private set; }

        private void Start()
        {
            EnableBld = new ConfigWrapper<bool>(nameof(EnableBld), this, true);
            EnableBldAlways = new ConfigWrapper<bool>(nameof(EnableBldAlways), this, false);
            EnableCum = new ConfigWrapper<bool>(nameof(EnableCum), this, true);
            EnableSwt = new ConfigWrapper<bool>(nameof(EnableSwt), this, true);
            EnableTear = new ConfigWrapper<bool>(nameof(EnableTear), this, true);
            EnableDrl = new ConfigWrapper<bool>(nameof(EnableDrl), this, true);
            EnablePersistance = new ConfigWrapper<bool>(nameof(EnablePersistance), this, true);

            Hooks.InstallHook();

            CharacterApi.RegisterExtraBehaviour<SkinEffectsController>(GUID);
            GameAPI.RegisterExtraBehaviour<SkinEffectGameController>(GUID);

            if (StudioAPI.InsideStudio)
            {
                RegisterStudioControls();
            }
            else
            {
                SceneManager.sceneLoaded += (arg0, mode) =>
                {
                    // Preload effects for H scene in case they didn't get loaded yet to prevent freeze on first effect appearing
                    if (arg0.name == "H")
                        TextureLoader.InitializeTextures();
                };
            }
        }

        private static void RegisterStudioControls()
        {
            CurrentStateCategoryToggle CreateToggle(string name, int textureCount, Action<SkinEffectsController, int> set, Func<SkinEffectsController, int> get)
            {
                var tgl = new CurrentStateCategoryToggle(name,
                    Mathf.Min(4, textureCount + 1),
                    c => RescaleLevel(get(c.charInfo.GetComponent<SkinEffectsController>()), textureCount, 3));

                tgl.SelectedIndex.Subscribe(Observer.Create((int x) =>
                {
                    var controller = GetSelectedController();
                    if (controller != null)
                        set(controller, RescaleLevel(x, tgl.ToggleCount - 1, textureCount));
                }));

                return tgl;
            }

            var sweatTgl = CreateToggle("Sweat", TextureLoader.WetTexturesFaceCount, (controller, i) => controller.SweatLevel = i, controller => controller.SweatLevel);
            var tearsTgl = CreateToggle("Tears", TextureLoader.TearTexturesCount, (controller, i) => controller.TearLevel = i, controller => controller.TearLevel);
            var droolTgl = CreateToggle("Drool", TextureLoader.DroolTexturesCount, (controller, i) => controller.DroolLevel = i, controller => controller.DroolLevel);
            var cumTgl = CreateToggle("Bukkake", TextureLoader.CumTexturesCount, (controller, i) => controller.BukkakeLevel = i, controller => controller.BukkakeLevel);
            var bldTgl = CreateToggle("Virgin blood", TextureLoader.BldTexturesCount, (controller, i) => controller.BloodLevel = i, controller => controller.BloodLevel);

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
