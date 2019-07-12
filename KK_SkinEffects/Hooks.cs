using Harmony;
using Object = UnityEngine.Object;

namespace KK_SkinEffects
{
    internal static partial class Hooks
    {
        public static void InstallHooks()
        {
            var instance = HarmonyInstance.Create(typeof(Hooks).FullName);

            instance.PatchAll(typeof(HSceneTriggers));

            if (SkinEffectsMgr.EnableClothesPersistance.Value)
                instance.PatchAll(typeof(PersistClothes));
        }

        private static SkinEffectGameController GetGameController()
        {
            return Object.FindObjectOfType<SkinEffectGameController>();
        }

        private static SkinEffectsController GetEffectController(SaveData.Heroine heroine)
        {
            return heroine?.chaCtrl != null ? heroine.chaCtrl.GetComponent<SkinEffectsController>() : null;
        }
    }
}
