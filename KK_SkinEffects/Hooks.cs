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
                {
                    instance.PatchAll(typeof(PersistClothes));

                    // Patch TalkScene.TalkEnd's iterator
                    var iteratorType = typeof(TalkScene).GetNestedType("<TalkEnd>c__Iterator5", AccessTools.all);
                    var iteratorMethod = AccessTools.Method(iteratorType, "MoveNext");
                    var prefix = new HarmonyMethod(typeof(PersistClothes), nameof(PersistClothes.PreTalkSceneIteratorEndHook));

                    instance.Patch(iteratorMethod, prefix, null, null);
                }


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
