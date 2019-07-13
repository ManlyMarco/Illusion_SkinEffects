using System.ComponentModel;
using BepInEx;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Studio;
using UnityEngine.SceneManagement;

namespace KK_SkinEffects
{
    [BepInPlugin(GUID, "Additional Skin Effects", Version)]
    internal class SkinEffectsPlugin : BaseUnityPlugin
    {
        public const string GUID = "Marco.SkinEffects";
        internal const string Version = "1.6.1";

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
        [Description("When excited girls sweat. Also triggers under shower because it makes things wet." +
                     "\n\nDoesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableSwt { get; private set; }

        [DisplayName("Enable tears")]
        [Description("Triggers on multiple occasions, e.g. in case of a virgin or after BJ." +
                     "\n\nDoesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableTear { get; private set; }

        [DisplayName("Enable drool")]
        [Description("Triggers when cumming inside the mouth." +
                     "\n\nDoesn't affect studio. May need to reload the current scene to take effects.")]
        public static ConfigWrapper<bool> EnableDrl { get; private set; }

        [DisplayName("Persist skin effects in school")]
        [Description("Characters keep the skin effects after H in story mode (only the modded effects).\n\n" +
                     "Effects get cleared after period change or taking a shower.")]
        public static ConfigWrapper<bool> EnablePersistance { get; private set; }

        [DisplayName("Persist clothes state in school")]
        [Description("Characters keep the state of their clothes after H and talk scenes (for example if you " +
                     "undress them with ClothingStateMenu they will stay undressed after ending the conversation). " +
                     "Cum on clothes is maintained as well.\n\n" +
                     "Effects get cleared after period change or changing clothes/taking a shower.\n\n" +
                     "Changes take effect after game restart.")]
        public static ConfigWrapper<bool> EnableClothesPersistance { get; private set; }

        private void Start()
        {
            if (!StudioAPI.InsideStudio)
            {
                EnableBld = new ConfigWrapper<bool>(nameof(EnableBld), this, true);
                EnableBldAlways = new ConfigWrapper<bool>(nameof(EnableBldAlways), this, false);
                EnableCum = new ConfigWrapper<bool>(nameof(EnableCum), this, true);
                EnableSwt = new ConfigWrapper<bool>(nameof(EnableSwt), this, true);
                EnableTear = new ConfigWrapper<bool>(nameof(EnableTear), this, true);
                EnableDrl = new ConfigWrapper<bool>(nameof(EnableDrl), this, true);
                EnablePersistance = new ConfigWrapper<bool>(nameof(EnablePersistance), this, true);
                EnableClothesPersistance = new ConfigWrapper<bool>(nameof(EnableClothesPersistance), this, true);

                SceneManager.sceneLoaded += (scene, mode) =>
                {
                    // Preload effects for H scene in case they didn't get loaded yet to prevent freeze on first effect appearing
                    if (scene.name == "H")
                        TextureLoader.InitializeTextures();
                };
            }

            Hooks.InstallHooks();

            CharacterApi.RegisterExtraBehaviour<SkinEffectsController>(GUID);
            GameAPI.RegisterExtraBehaviour<SkinEffectGameController>(GUID);

            SkinEffectsGui.Init(this);
        }
    }
}
