using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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
        public const string Version = "1.6.3";

        internal static new ManualLogSource Logger { get; private set; }

        public static ConfigEntry<bool> EnableBld { get; private set; }
        public static ConfigEntry<bool> EnableBldAlways { get; private set; }
        public static ConfigEntry<bool> EnableCum { get; private set; }
        public static ConfigEntry<bool> EnableSwt { get; private set; }
        public static ConfigEntry<bool> EnableTear { get; private set; }
        public static ConfigEntry<bool> EnableDrl { get; private set; }
        public static ConfigEntry<bool> EnablePersistence { get; private set; }
        public static ConfigEntry<bool> EnableClothesPersistence { get; private set; }

        private void Start()
        {
            Logger = base.Logger;

            if (!StudioAPI.InsideStudio)
            {
                EnableBld = Config.AddSetting("Effects", "Enable virgin bleeding", true,
                    "When penetrated for the first time, virgins have a chance to bleed. The extent varies based on their status.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");
                EnableBldAlways = Config.AddSetting("Effects", "Virgins always bleed", false,
                    "By default some girls might not bleed on their first time depending on some parameters. This setting makes sure some blood to always be there.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");
                EnableCum = Config.AddSetting("Effects", "Enable bukkake", true,
                    "Triggers when cumming inside.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");
                EnableSwt = Config.AddSetting("Effects", "Enable sweating/wet under shower", true,
                    "When excited girls sweat. Also triggers under shower because it makes things wet.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");
                EnableTear = Config.AddSetting("Effects", "Enable tears", true,
                    "Triggers on multiple occasions, e.g. in case of a virgin or after BJ.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");
                EnableDrl = Config.AddSetting("Effects", "Enable drool", true,
                    "Triggers when cumming inside the mouth.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");

                EnablePersistence = Config.AddSetting("Persistence", "Persist skin effects in school", true,
                    "Characters keep the skin effects after H in story mode (only the modded effects).\n\nEffects get cleared after period change or taking a shower.");
                EnableClothesPersistence = Config.AddSetting("Persistence", "Persist clothes state in school", true,
                    "Characters keep the state of their clothes after H and talk scenes (for example if you undress them with ClothingStateMenu they will stay undressed after ending the conversation). Cum on clothes is maintained as well.\n\nEffects get cleared after period change or changing clothes/taking a shower.\n\nChanges take effect after game restart.");

                SceneManager.sceneLoaded += (scene, mode) =>
                {
                    // Preload effects for H scene in case they didn't get loaded yet to prevent freeze on first effect appearing
                    if (scene.name == "H")
                        TextureLoader.PreloadAllTextures();
                };
            }

            Hooks.InstallHooks();

            CharacterApi.RegisterExtraBehaviour<SkinEffectsController>(GUID);
            GameAPI.RegisterExtraBehaviour<SkinEffectGameController>(GUID);

            SkinEffectsGui.Init(this);
        }
    }
}
