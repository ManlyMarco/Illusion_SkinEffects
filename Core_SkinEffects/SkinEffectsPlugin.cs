using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Studio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_SkinEffects
{
    [BepInPlugin(GUID, "Additional Skin Effects", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(KoiSkinOverlayX.KoiSkinOverlayMgr.GUID, KoiSkinOverlayX.KoiSkinOverlayMgr.Version)]
    internal class SkinEffectsPlugin : BaseUnityPlugin
    {
        public const string GUID = "Marco.SkinEffects";
        public const string Version = "2.1.4";

        internal static new ManualLogSource Logger { get; private set; }

        public static ConfigEntry<bool> EnableBldAlways { get; private set; }

        public static ConfigEntry<bool> EnablePersistence { get; private set; }
        public static ConfigEntry<bool> EnableClothesPersistence { get; private set; }
        public static ConfigEntry<bool> EnableSwtActions { get; private set; }

        public static ConfigEntry<KeyboardShortcut> ClearEffectsKey { get; private set; }

        private static readonly Dictionary<SkinEffectKind, ConfigEntry<bool>> _effectEnabledSettings = new Dictionary<SkinEffectKind, ConfigEntry<bool>>(13);
        public static bool IsEffectEnabled(SkinEffectKind kind) => StudioAPI.InsideStudio || !_effectEnabledSettings.TryGetValue(kind, out var setting) || setting.Value;

        private void Start()
        {
            Logger = base.Logger;

            if (!StudioAPI.InsideStudio)
            {
                //foreach (var kind in SkinEffectKindUtils.ValidSkinEffectKinds)
                //    _effectEnabledSettings[kind] = Config.Bind("Effects", $"Enable {kind}", true, $"Allow the '{kind.GetDisplayName()}' effect to be used in Story and FreeH modes. Does not affect studio.");

                _effectEnabledSettings[SkinEffectKind.VirginBloodBody] = Config.Bind("Effects", "Enable virgin bleeding", true, "When penetrated for the first time, virgins have a chance to bleed. The extent varies based on their status.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");

                var bukkake = Config.Bind("Effects", "Enable bukkake", true, "Triggers when cumming inside.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");
                _effectEnabledSettings[SkinEffectKind.PussyBukkakeBody] = bukkake;
                _effectEnabledSettings[SkinEffectKind.AnalBukkakeBody] = bukkake;

                var wet = Config.Bind("Effects", "Enable sweating/wet under shower", true, "When excited girls sweat. Also triggers under shower because it makes things wet.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");
                _effectEnabledSettings[SkinEffectKind.WetBody] = wet;
                _effectEnabledSettings[SkinEffectKind.WetFace] = wet;

                _effectEnabledSettings[SkinEffectKind.TearFace] = Config.Bind("Effects", "Enable tears", true, "Triggers on multiple occasions, e.g. in case of a virgin or after BJ.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");

                var drool = Config.Bind("Effects", "Enable drool", true, "Triggers when cumming inside the mouth.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");
                _effectEnabledSettings[SkinEffectKind.DroolFace] = drool;
                _effectEnabledSettings[SkinEffectKind.SalivaFace] = drool;
                _effectEnabledSettings[SkinEffectKind.CumInNoseFace] = drool;

                _effectEnabledSettings[SkinEffectKind.ButtBlushBody] = Config.Bind("Effects", "Enable butt blush", true, "Triggers when massaging roughly.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");

                var blush = Config.Bind("Effects", "Enable face and body blush", true, "Triggers after a few orgasms and when extremely excited.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");
                _effectEnabledSettings[SkinEffectKind.BlushBody] = blush;
                _effectEnabledSettings[SkinEffectKind.BlushFace] = blush;

                _effectEnabledSettings[SkinEffectKind.PussyJuiceBody] = Config.Bind("Effects", "Enable pussy juice", true, "Triggers when girl's H-Gauge increases over 70%.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");

                EnablePersistence = Config.Bind("Persistence", "Persist skin effects in school", true, "Characters keep the skin effects after H in story mode (only the modded effects).\n\nEffects get cleared after period change or taking a shower.");
                EnableClothesPersistence = Config.Bind("Persistence", "Persist clothes state in school", true, "Characters keep the state of their clothes after H and talk scenes (for example if you undress them with ClothingStateMenu they will stay undressed after ending the conversation). Cum on clothes is maintained as well.\n\nEffects get cleared after period change or changing clothes/taking a shower.\n\nChanges take effect after game restart.");
                EnableSwtActions = Config.Bind("Persistence", "Some AI actions cause sweating", true, "Some actions like excercising apply the sweat effect to the character. Persistance needs to be fully on for this to work.");

                EnableBldAlways = Config.Bind("Effects", "Virgins always bleed", false, "By default some girls might not bleed on their first time depending on some parameters. This setting makes sure some blood to always be there.\n\nDoesn't affect studio. May need to reload the current scene to take effects.");

                ClearEffectsKey = Config.Bind("Effects", "Clear all effects in H scene", new KeyboardShortcut(KeyCode.Alpha0), "Clears all effects from all characters in the current H scene. Only works in H scenes.");

                SceneManager.sceneLoaded += (scene, mode) =>
                {
                    // Preload effects for H scene in case they didn't get loaded yet to prevent freeze on first effect appearing
                    if (scene.name == "H" || scene.name == "VRHScene")
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
