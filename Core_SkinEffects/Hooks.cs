using KKAPI.Studio;
using HarmonyLib;
using KKAPI.Utilities;

namespace KK_SkinEffects
{
    internal static partial class Hooks
    {
        public static void InstallHooks()
        {
            if (!StudioAPI.InsideStudio)
            {
                var instance = Harmony.CreateAndPatchAll(typeof(HSceneTriggers));

                if (SkinEffectsPlugin.EnableClothesPersistence.Value)
                    PersistClothes.InstallHooks(instance);
            }
        }

        private static SkinEffectGameController GetGameController()
        {
            return UnityEngine.Object.FindObjectOfType<SkinEffectGameController>();
        }

        private static SkinEffectsController GetEffectController(SaveData.Heroine heroine)
        {
            return heroine?.chaCtrl != null ? heroine.chaCtrl.GetComponent<SkinEffectsController>() : null;
        }
    }
}
