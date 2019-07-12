using Harmony;
using KKAPI.Studio;

namespace KK_SkinEffects
{
    internal static partial class Hooks
    {
        public static void InstallHooks()
        {
            if (!StudioAPI.InsideStudio)
            {
                var instance = HarmonyInstance.Create(typeof(Hooks).FullName);

                instance.PatchAll(typeof(HSceneTriggers));

                if (SkinEffectsPlugin.EnableClothesPersistance.Value)
                    instance.PatchAll(typeof(PersistClothes));
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
