using System;
using ActionGame.Chara;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using KKAPI.MainGame;
using Manager;
using ADV;
using HarmonyLib;

namespace KK_SkinEffects
{
    internal static partial class Hooks
    {
        /// <summary>
        /// Persisting clothing state in the overworld/talk scenes
        /// </summary>
        private static class PersistClothes
        {
            public static void PreTalkSceneIteratorEndHook(object __instance)
            {
                // __instance is of the compiler_generated type TalkScene+<TalkEnd>c__Iterator5
                // $PC is the number of times yield return has been called
                // We want this to run just before the third yield return in TalkScene.TalkEnd, just before fading out
                int? counter = Traverse.Create(__instance)?.Field("$PC")?.GetValue<int>();
                if (counter == 2)
                {
                    var heroine = Utils.GetCurrentVisibleGirl();
                    var controller = GetEffectController(heroine);
                    if (controller != null)
                        SkinEffectGameController.SavePersistData(heroine, controller);
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(TextScenario), nameof(ADV.Commands.EventCG.Release))]
            public static void PreTextScenarioReleaseHook()
            {
                var heroine = Utils.GetCurrentVisibleGirl();
                var controller = GetEffectController(heroine);
                if (controller != null)
                    SkinEffectGameController.SavePersistData(heroine, controller);
            }

            [HarmonyPostfix]
#if KK
            [HarmonyPatch(typeof(Scene), nameof(Scene.UnLoad), new Type[0])]
#elif KKS
            [HarmonyPatch(typeof(Scene), nameof(Scene.Unload))]
#endif
            public static void PostSceneUnloadHook()
            {
                // Update the character used outside of current talk scene. Called after any TalkScene ends, including 
                // when entering H mode. This will copy clothes state into a H scene, and out to the main map.
                var heroine = Utils.GetCurrentVisibleGirl();
                var controller = GetEffectController(heroine);
                if (controller != null)
                    GetGameController()?.OnSceneUnload(heroine, controller);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.RandomChangeOfClothesLowPolyEnd))]
            public static bool RandomChangeOfClothesLowPolyEndPrefix(ChaControl __instance)
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
            public static void AfterResult(AI __instance, ActionControl.ResultInfo result)
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
                    var npc = __instance.GetNPC();

                    // If leaving a special scene (e.g. lunch), maintain clothes from scene.
                    if (npc.IsExitingScene()) return;
                    var effectsController = GetEffectController(npc.heroine);
                    if (effectsController == null) return;

                    if (previousAction == 2)
                    {
                        // After shower clear everything
                        effectsController.ClearCharaState(true, true);
                        SkinEffectGameController.SavePersistData(npc.heroine, effectsController);
                    }
                    else if (currentAction == 2)
                    {
                        if (previousAction == 0)
                        {
                            // Going to shower now after changing clothes
                            // Make the character naked (set all clothing states to fully off)
                            effectsController.ClothingState = Enumerable.Repeat((byte)3, Enum.GetValues(typeof(ChaFileDefine.ClothesKind)).Length).ToArray();
                            // Non public setter. Needed to prevent the state from being reset in RandomChangeOfClothesLowPolyEnd hook
                            Traverse.Create(effectsController.ChaControl).Property(nameof(effectsController.ChaControl.isChangeOfClothesRandom)).SetValue(false);
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
                        SkinEffectGameController.SavePersistData(npc.heroine, effectsController);
                    }
                }
            }
        }
    }
}
