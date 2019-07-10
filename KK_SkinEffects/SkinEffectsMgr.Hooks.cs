using System;
using ActionGame;
using ActionGame.Chara;
using Harmony;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Manager;

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

            #region KK_Persist Hooks

            [HarmonyPrefix]
            [HarmonyPatch(typeof(TalkScene), "TalkEnd")]
            public static void PreTalkSceneEndHook()
            {
                // Save clothing state changes at end of TalkScene, specifically from ClothingStateMenu
                var heroine = GetCurrentVisibleGirl();
                if (heroine != null)
                    SkinEffectGameController.SavePersistData(heroine, GetEffectController(heroine));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Scene), "UnLoad")]
            public static void PostSceneUnloadHook()
            {
                // Called after TalkScene ends
                var heroine = GetCurrentVisibleGirl();
                if (heroine != null)
                    GetGameController()?.OnSceneUnload(heroine, GetEffectController(heroine));
            }

            /*[HarmonyTranspiler]
            [HarmonyPatch(typeof(ChaControl), "RandomChangeOfClothesLowPolyEnd")]
            public static IEnumerable<CodeInstruction> RandomChangeTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                // IL_0000: ldarg.0
                // IL_0001: ldc.i4.0
                // IL_0002: call instance void ChaControl::SetClothesStateAll(uint8)
                // IL_0007: ldarg.0
                // IL_0008: ldc.i4.0
                // IL_0009: call instance void ChaControl::set_isChangeOfClothesRandom(bool)
                // IL_000E: ret

                var target = AccessTools.Method(typeof(ChaControl), nameof(ChaControl.SetClothesStateAll));
                if (target == null) throw new ArgumentNullException(nameof(target));
                foreach (var instruction in instructions)
                {
                    if (instruction.operand == target)
                    {
                        //todo why? necessary?
                        // Kill call to SetClothesStateAll
                        // Pop both of its arguments to be as compatible as possible
                        instruction.operand = null;
                        instruction.opcode = OpCodes.Pop;
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Pop);
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }*/

            // Gets a list of the last ten actions the AI has taken
            private static int[] GetLastActions(AI ai, NPC npc = null)
            {
                if (npc == null) npc = GetNPC(ai);

                var scene = Traverse.Create(ai).Property("actScene").GetValue<ActionScene>();

                // Dictionary<SaveData.Heroine, ActionControl.DesireInfo>
                var dicTarget = Traverse.Create(scene.actCtrl).Field("dicTarget").GetValue<IDictionary>();

                // ActionControl.DesireInfo
                var di = dicTarget[npc.heroine];
                var lastActions = Traverse.Create(di).Field("_queueAction").GetValue<Queue<int>>();

                // _queueAction is limited to ten elements
                return lastActions.ToArray();
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(AI), "Result")]
            public static void AfterResult(ActionControl.ResultInfo result, AI __instance)
            {
                var npc = GetNPC(__instance);

                var actions = GetLastActions(__instance, npc);
                var n = actions.Length;

                if (n == 0) return;

                // 17 (change mind) seems to happen when redirected by the player while desire is something else
                if (actions[n - 1] == 23) return;

                var replaceClothesActions = new HashSet<int>(new int[]
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
                    var effectsController = GetEffectController(npc.heroine);
                    // Changing Clothes -> Embarrassment
                    if (actions[n - 1] != 25)
                    {
                        effectsController.ClothingState = null;
                        SkinEffectGameController.SavePersistData(npc.heroine, effectsController);
                    }
                    //shower
                    if (actions[n - 1] == 2)
                    {
                        effectsController.ClearCharaState(true);
                        SkinEffectGameController.SavePersistData(npc.heroine, effectsController);
                    }
                }
            }

            #endregion

            public static void InstallHook()
            {
                HarmonyInstance.Create(typeof(Hooks).FullName).PatchAll(typeof(Hooks));
            }

            private static SkinEffectGameController GetGameController()
            {
                return FindObjectOfType<SkinEffectGameController>();
            }

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
                return __instance.mode == HFlag.EMode.houshi3P || __instance.mode == HFlag.EMode.sonyu3P ? __instance.nowAnimationInfo.id % 2 : 0;
            }

            private static SaveData.Heroine GetCurrentVisibleGirl()
            {
                var result = FindObjectOfType<TalkScene>()?.targetHeroine;
                if (result != null)
                    return result;

                var nowScene = Game.Instance?.actScene?.AdvScene?.nowScene;
                if (nowScene != null)
                {
                    var traverse = Traverse.Create(nowScene).Field("m_TargetHeroine");
                    if (traverse.FieldExists())
                    {
                        var girl = traverse.GetValue<SaveData.Heroine>();
                        if (girl != null) return girl;
                    }
                }
                return null;
            }

            private static NPC GetNPC(AI ai)
            {
                return Traverse.Create(ai).Property("npc").GetValue<NPC>();
            }
        }
    }
}
