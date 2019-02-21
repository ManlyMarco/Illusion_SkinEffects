using Harmony;

namespace KK_SkinEffects
{
    internal partial class SkinEffectsMgr
    {
        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuInside))]
            public static void AddSonyuInside(HFlag __instance)
            {
                // Finish raw vaginal
                //todo add delays? could wait for animation change
                var heroine = GetLeadHeroine(__instance);
                var controller = GetEffectController(heroine);
                controller.OnFinishRawInside(heroine, __instance);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuKokanPlay))]
            public static void AddSonyuKokanPlay(HFlag __instance)
            {
                // Insert vaginal
                var heroine = GetLeadHeroine(__instance);
                GetEffectController(heroine).OnInsert(heroine, __instance);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddKuwaeFinish))]
            public static void AddKuwaeFinish(HFlag __instance)
            {
                // Cum inside mouth
                var heroine = GetLeadHeroine(__instance);
                GetEffectController(heroine).OnCumInMouth(heroine, __instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.FemaleGaugeUp))]
            public static void FemaleGaugeUp(HFlag __instance)
            {
                var heroine = GetLeadHeroine(__instance);
                GetEffectController(heroine).OnFemaleGaugeUp(heroine, __instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.InitHeroine))]
            public static void InitHeroine(HSprite __instance)
            {
                var heroine = GetLeadHeroine(__instance.flags);
                GetEffectController(heroine).OnHSceneProcStart(heroine, __instance.flags);
            }

            public static void InstallHook()
            {
                HarmonyInstance.Create(typeof(Hooks).FullName).PatchAll(typeof(Hooks));
            }

            //private void ChangeAnimator(HSceneProc.AnimationListInfo _nextAinmInfo, bool _isForceCameraReset = false)

            private static SkinEffectsController GetEffectController(SaveData.Heroine heroine)
            {
                return heroine.chaCtrl.GetComponent<SkinEffectsController>();
            }

            private static SaveData.Heroine GetLeadHeroine(HFlag __instance)
            {
                return __instance.lstHeroine[GetLeadHeroineId(__instance)];
            }

            private static int GetLeadHeroineId(HFlag __instance)
            {
                return __instance.mode >= HFlag.EMode.houshi3P ? __instance.nowAnimationInfo.id % 2 : 0;
            }
        }
    }
}
