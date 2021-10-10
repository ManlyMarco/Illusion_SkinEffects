using System;
using ActionGame.Chara;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using KKAPI.MainGame;
using HarmonyLib;
using SaveData;

namespace KK_SkinEffects
{
    internal static partial class Hooks
    {
        /// <summary>
        /// Persisting clothing state in the overworld/talk scenes
        /// </summary>
        private static class PersistClothes
        {
            public static void InstallHooks(Harmony instance)
            {
                instance.PatchAll(typeof(PersistClothes));
            }

            /// <summary>
            /// Copy state over when transitioning between modes.
            /// Entering/leaving talk / H and other scenes that spawn their own copy of the character all set chaCtrl.
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(SaveData.CharaData), nameof(SaveData.CharaData.chaCtrl), MethodType.Setter)]
            private static void OnChaCtrlChangePre(ChaControl value, SaveData.CharaData __instance)
            {
                if (__instance is Heroine heroine)
                {
                    if (__instance.chaCtrl != null)
                        SkinEffectGameController.SavePersistData(heroine, __instance.chaCtrl.GetComponent<SkinEffectsController>());

                    if (__instance.chaCtrl != value && value != null)
                        // This waits until CharaData.chaCtrl is changed so after the original method runs, therefore we can't do this in postfix
                        SkinEffectGameController.OnSceneChange(heroine, value.GetComponent<SkinEffectsController>());
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.RandomChangeOfClothesLowPolyEnd))]
            private static bool RandomChangeOfClothesLowPolyEndPrefix(ChaControl __instance)
            {
                // Prevent the method from running if the clothes were not actually changed by RandomChangeOfClothesLowPoly
                // Avoids overriding our saved clothes state at the end of pretty much all actions, no real effect otherwise

                var controller = __instance.GetComponent<SkinEffectsController>();

                if (__instance.isChangeOfClothesRandom || !HasSiruState(controller) && !HasClothingState(controller))
                {
                    // Clear clothes state and save it
                    if (controller != null)
                    {
                        controller.SiruState = null;
                        controller.ClothingState = null;
                        controller.AccessoryState = null;

                        var heroine = __instance.GetHeroine();

                        if (heroine != null)
                        {
                            // If leaving a special scene (e.g. lunch), maintain clothes from scene.
                            if (heroine.charaBase is NPC npc && npc.IsExitingScene())
                                return false;
                            else
                                SkinEffectGameController.SavePersistData(heroine, controller);
                        }
                    }

                    return true;
                }

                return false;
            }

            private static bool HasSiruState(SkinEffectsController controller)
            {
                return !(controller == null || controller.SiruState == null || controller.SiruState.All(x => x == 0));
            }

            private static bool HasClothingState(SkinEffectsController controller)
            {
                return !(controller == null || controller.ClothingState == null || controller.ClothingState.All(x => x == 0));
            }

            /// <summary>
            /// Handle resetting clothing/fluid state when the girl showers or otherwise fixes her clothes
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(AI), "Result")]
            private static void AfterResultPrefix(AI __instance, ActionControl.ResultInfo result)
            {
                var actionHistory = __instance.GetLastActions().ToArray();
                var actionCount = actionHistory.Length;

                if (actionCount < 2) return;

                // The result gives the action that is currently being carried out, same as this
                int currentAction = actionHistory[actionCount - 1];
                // This is the action that we just finished, this is the important one to compare against
                int previousAction = actionHistory[actionCount - 2];

                // 17 (change mind) seems to happen when interrupted, 23 is making them follow you, 25 is being embarassed
                // In all cases the original task was not finished and will be attempted again
                if (currentAction == 23 || currentAction == 17 || currentAction == 25) return;

                var replaceClothesActions = new HashSet<int>(new[]
                {
                    0, // Change Clothes
                    1, // Toilet
                    2, // Shower
                    4, // H masturbate
                    25, // Embarrassment
                    26, // Lez
                    27, // Lez Partner
                });

                // Multiple change clothes actions can be queued up.
                // Put clothes on when the latest action is not in the set.
                if (previousAction != currentAction && replaceClothesActions.Contains(previousAction))
                {
                    var npc = __instance.npc;
                    // If leaving a special scene (e.g. lunch), maintain clothes from scene.
                    if (npc.IsExitingScene()) return;
                    var heroine = npc.heroine;
                    var effectsController = GetEffectController(heroine);
                    if (effectsController == null) return;

                    if (previousAction == 2 || previousAction == 1)
                    {
                        // After shower clear everything
                        effectsController.ClearCharaState(true, true);
                        SkinEffectGameController.SavePersistData(heroine, effectsController);
                    }
                    else if (currentAction == 2)
                    {
                        if (previousAction == 0)
                        {
                            // Going to shower now after changing clothes
                            // Make the character naked (set all clothing states to fully off)
                            effectsController.ClothingState = Enumerable.Repeat((byte)3, Enum.GetValues(typeof(ChaFileDefine.ClothesKind)).Length).ToArray();
                            // Non public setter. Needed to prevent the state from being reset in RandomChangeOfClothesLowPolyEnd hook
                            effectsController.ChaControl.isChangeOfClothesRandom = false;
                        }
                    }
                    else
                    {
                        // Otherwise do a partial clear
                        effectsController.ClothingState = null;
                        effectsController.AccessoryState = null;
                        effectsController.SiruState = null;
                        effectsController.TearLevel = 0;
                        effectsController.DroolLevel = 0;
                        SkinEffectGameController.SavePersistData(heroine, effectsController);
                    }
                }
            }

#if KK // todo implement in kks, maybe for stuff done in water and sunbathing? perf hit?
            [HarmonyPostfix]
            [HarmonyPatch(typeof(AI), "Result")]
            private static void AfterResultPostfix(AI __instance, ActionControl.ResultInfo result)
            {
                if (result == null) return;

                // Add sweat if the character is doing running workout. Checks need to be in postfix
                // This only has effect if persistance is on
                if ((result.actionNo == 6 || result.actionNo == 18) && result.point != null && result.point.transform.childCount > 0)
                {
                    var heroine = __instance.npc?.heroine;
                    var c = GetEffectController(heroine);
                    if (c != null) c.OnRunning();
                }
            }
#endif
        }
    }
}
