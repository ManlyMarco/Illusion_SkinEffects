using System;
using ActionGame.Chara;
using Harmony;
using System.Collections.Generic;
using Manager;

namespace KK_SkinEffects
{
    internal static partial class Hooks
    {
        /// <summary>
        /// Persisting clothing state in the overworld/talk scenes
        /// </summary>
        private static class PersistClothes
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TalkScene), "TalkEnd")]
            public static void PreTalkSceneEndHook()
            {
                // Save clothing state changes at end of TalkScene, specifically from ClothingStateMenu
                var heroine = Utils.GetCurrentVisibleGirl();
                if (heroine != null)
                    SkinEffectGameController.SavePersistData(heroine, GetEffectController(heroine));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.UnLoad), new Type[0])]
            public static void PostSceneUnloadHook()
            {
                // Called after TalkScene ends
                var heroine = Utils.GetCurrentVisibleGirl();
                if (heroine != null)
                    GetGameController()?.OnSceneUnload(heroine, GetEffectController(heroine));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.RandomChangeOfClothesLowPolyEnd))]
            public static bool RandomChangeOfClothseLowPolyEndPrefix(ChaControl __instance)
            {
                // Prevent the method from running if the clothes were not actually changed by RandomChangeOfClothesLowPoly
                // Avoids overriding our saved clothes state at the end of pretty much all actions, no real effect otherwise
                return __instance.isChangeOfClothesRandom;
            }

            /// <summary>
            /// Handle resetting clothing/fluid state when the girl showers or otherwise fixes her clothes
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(AI), "Result")]
            public static void AfterResult(AI __instance)
            {
                var actions = __instance.GetLastActions().ToArray();
                var n = actions.Length;

                if (n == 0) return;

                // 17 (change mind) seems to happen when redirected by the player while desire is something else
                if (actions[n - 1] == 23) return;

                var replaceClothesActions = new HashSet<int>(new[]
                {
                    0, // Change Clothes
                    1, // Toilet
                    2, // Shower
                    4, // H Solo
                    25, // Embarrassment
                    26, // Lez
                    27, // Lez Partner
                });

                // Multiple change clothes actions can be queued up.
                // Put clothes on when the latest action is not in the set.
                if (n >= 2 && actions[n - 2] != actions[n - 1] && replaceClothesActions.Contains(actions[n - 2]))
                {
                    var npc = __instance.GetNPC();
                    var effectsController = GetEffectController(npc.heroine);

                    if (actions[n - 1] == 2)
                    {
                        // 2 - shower
                        // todo should it force update clothes state?
                        effectsController.ClearCharaState(true);
                        SkinEffectGameController.SavePersistData(npc.heroine, effectsController);
                    }
                    else if (actions[n - 1] != 25)
                    {
                        // Changing Clothes -> Embarrassment
                        effectsController.ClothingState = null;
                        SkinEffectGameController.SavePersistData(npc.heroine, effectsController);
                    }
                }
            }
        }
    }
}
