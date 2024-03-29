﻿using HarmonyLib;
using UnityEngine;

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
            private static void AddSonyuInside(HFlag __instance)
            {
                // Finish raw vaginal
                //todo add delays? could wait for animation change
                var heroine = KKAPI.Utilities.HSceneUtils.GetLeadingHeroine(__instance);
                var controller = GetEffectController(heroine);
                controller.OnFinishRawInside(heroine, __instance);
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddSonyuAnalInside))]
            public static void AddSonyuAnalInside(HFlag __instance)
            {
                // Finish raw Anal
                //todo add delays? could wait for animation change
                var heroine = KKAPI.Utilities.HSceneUtils.GetLeadingHeroine(__instance);
                var controller = GetEffectController(heroine);
                controller.OnFinishAnalRawInside(heroine, __instance);
            }

            // Always called during the H-Scene whenever there is a vaginal insertion.
            // HFlag.AddSonyuKokanPlay can't be used, it's called only once and is unreliable.
            // Another difference is that AddSonyuKokanPlay is called as the button is pressed,
            // while SetInsertKokan is called after the peen is fully inside.
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.SetInsertKokan))]
            private static void SetInsertKokan(HFlag __instance)
            {
                // Insert vaginal
                var heroine = KKAPI.Utilities.HSceneUtils.GetLeadingHeroine(__instance);
                GetEffectController(heroine).OnInsert(heroine, __instance);
            }

            // Always call during the H-Scene whenever there is an anal insertion.
            // HFlag.AddSonyuAnalPlay can't be used, it's called only once and is unreliable.
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.SetInsertAnal))]
            public static void SetInsertAnal(HFlag __instance)
            {
                // Insert Anal
                var heroine = KKAPI.Utilities.HSceneUtils.GetLeadingHeroine(__instance);
                GetEffectController(heroine).OnAnalInsert(heroine, __instance);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddKuwaeFinish))]
            private static void AddKuwaeFinish(HFlag __instance)
            {
                // Cum inside mouth
                var heroine = KKAPI.Utilities.HSceneUtils.GetLeadingHeroine(__instance);
                GetEffectController(heroine).OnCumInMouth(heroine, __instance);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.AddKiss))]
            public static void AddKiss(HFlag __instance)
            {
                // Kiss Her
                var heroine = KKAPI.Utilities.HSceneUtils.GetLeadingHeroine(__instance);
                GetEffectController(heroine).OnKissing(heroine, __instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HFlag), nameof(HFlag.FemaleGaugeUp))]
            private static void FemaleGaugeUp(HFlag __instance)
            {
                var heroine = KKAPI.Utilities.HSceneUtils.GetLeadingHeroine(__instance);
                GetEffectController(heroine).OnFemaleGaugeUp(heroine, __instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.InitHeroine))]
            private static void InitHeroine(HSprite __instance)
            {
                var heroine = KKAPI.Utilities.HSceneUtils.GetLeadingHeroine(__instance.flags);
                GetEffectController(heroine).OnHSceneProcStart(heroine, __instance.flags);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ShortCut))]
            private static void OnShortCut()
            {
                if (SkinEffectsPlugin.ClearEffectsKey.Value.IsDown())
                {
                    foreach (var effectsController in Object.FindObjectsOfType<SkinEffectsController>())
                    {
                        effectsController.DroolLevel = 0;
                        effectsController.SalivaLevel = 0;
                        effectsController.CumInNoseLevel = 0;
                        effectsController.TearLevel = 0;
                        effectsController.BloodLevel = 0;
                        effectsController.BukkakeLevel = 0;
                        effectsController.AnalBukkakeLevel = 0;
                        effectsController.SweatLevel = 0;
                        effectsController.PussyJuiceLevel = 0;
                    }
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(TalkScene), nameof(TalkScene.TouchFunc), typeof(string), typeof(Vector3))]
            private static void TouchFuncHook(TalkScene __instance, string _kind)
            {
                GetEffectController(__instance.targetHeroine).OnTalkSceneTouch(__instance.targetHeroine, _kind);
            }
        }
    }
}
