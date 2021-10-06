using KKAPI.Studio;
using HarmonyLib;

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
                {
                    instance.PatchAll(typeof(PersistClothes));
                    instance.PatchAll(typeof(MainGameHooks));

                    // Patch TalkScene.TalkEnd iterator nested class
                    var iteratorType = AccessTools.FirstInner(typeof(TalkScene), x => x.FullName.Contains("<TalkEnd>c__Iterator"));
                    if (iteratorType == null)
                    {
                        SkinEffectsPlugin.Logger.LogError("Could not find TalkEnd iterator to patch.");
                        return;
                    }
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
