using Harmony;
using Object = UnityEngine.Object;

namespace KK_SkinEffects
{
    internal static partial class Hooks
    {
        public static void InstallHook()
        {
            var instance = HarmonyInstance.Create(typeof(Hooks).FullName);
            instance.PatchAll(typeof(HSceneTriggers));
            instance.PatchAll(typeof(PersistClothes));
        }

        private static SkinEffectGameController GetGameController()
        {
            return Object.FindObjectOfType<SkinEffectGameController>();
        }

        private static SkinEffectsController GetEffectController(SaveData.Heroine heroine)
        {
            return heroine.chaCtrl.GetComponent<SkinEffectsController>();
        }
    }
}
