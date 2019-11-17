using HarmonyLib;

namespace KK_SkinEffects
{
    internal static partial class Hooks
    {
        /// <summary>
        /// H scene effect triggers
        /// </summary>
        private static class HSceneTriggers
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuInside))]
            public static void AddSonyuInside(HFlag __instance)
            {
                // Finish raw vaginal
                //todo add delays? could wait for animation change
                var heroine = __instance.GetLeadHeroine();
                var controller = GetEffectController(heroine);
                controller.OnFinishRawInside(heroine, __instance);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuKokanPlay))]
            public static void AddSonyuKokanPlay(HFlag __instance)
            {
                // Insert vaginal
                var heroine = __instance.GetLeadHeroine();
                GetEffectController(heroine).OnInsert(heroine, __instance);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddKuwaeFinish))]
            public static void AddKuwaeFinish(HFlag __instance)
            {
                // Cum inside mouth
                var heroine = __instance.GetLeadHeroine();
                GetEffectController(heroine).OnCumInMouth(heroine, __instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.FemaleGaugeUp))]
            public static void FemaleGaugeUp(HFlag __instance)
            {
                var heroine = __instance.GetLeadHeroine();
                GetEffectController(heroine).OnFemaleGaugeUp(heroine, __instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.InitHeroine))]
            public static void InitHeroine(HSprite __instance)
            {
                var heroine = __instance.flags.GetLeadHeroine();
                GetEffectController(heroine).OnHSceneProcStart(heroine, __instance.flags);
            }
        }
    }
}
