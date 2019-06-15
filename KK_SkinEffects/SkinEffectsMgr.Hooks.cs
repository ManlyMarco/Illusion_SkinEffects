using ActionGame;
using ActionGame.Chara;
using BepInEx;
using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

            // KK_Persist Hooks
            private static SaveData.Heroine GetCurrentVisibleGirl()
            {
                var result = FindObjectOfType<TalkScene>()?.targetHeroine;
                if (result != null)
                    return result;

                try
                {
                    var nowScene = Manager.Game.Instance?.actScene?.AdvScene?.nowScene;
                    if (nowScene)
                    {
                        var advSceneTargetHeroineProp = typeof(ADV.ADVScene).GetField("m_TargetHeroine", BindingFlags.Instance | BindingFlags.NonPublic);
                        var girl = advSceneTargetHeroineProp?.GetValue(nowScene) as SaveData.Heroine;
                        if (girl != null) return girl;
                    }
                }
                catch
                {
                }
                return null;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(TalkScene), "TalkEnd")]
            public static void PreTalkSceneEndHook()
            {
                // Save clothing state changes at end of TalkScene, specifically from ClothingStateMenu
                var heroine = GetCurrentVisibleGirl();
                if (heroine != null && Singleton<SkinEffectGameController>.Instance != null)
                    Singleton<SkinEffectGameController>.Instance.OnTalkEnd(heroine, GetEffectController(heroine));
            }
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Manager.Scene), "UnLoad")]
            public static void PostSceneUnloadHook()
            {
                // Called after TalkScene ends
                var heroine = GetCurrentVisibleGirl();
                if (heroine != null)
                    Singleton<SkinEffectGameController>.Instance.OnUnload(heroine, GetEffectController(heroine));
            }
            
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ChaControl), "RandomChangeOfClothesLowPolyEnd")]
            public static IEnumerable<CodeInstruction> RandomChangeTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                /*
                 * IL_0000: ldarg.0
                 * IL_0001: ldc.i4.0
                 * IL_0002: call instance void ChaControl::SetClothesStateAll(uint8)
                 * IL_0007: ldarg.0
                 * IL_0008: ldc.i4.0
                 * IL_0009: call instance void ChaControl::set_isChangeOfClothesRandom(bool)
                 * IL_000E: ret
                 */

                // Kill first call to SetClothesStateAll
                var codes = new List<CodeInstruction>(instructions);
                codes.RemoveRange(0, 3);
                return codes.AsEnumerable();

            }

            public static NPC GetNPC(AI ai)
            {
                PropertyInfo npc_property = typeof(AI).GetProperty("npc", BindingFlags.NonPublic | BindingFlags.Instance);
                return (NPC)npc_property.GetGetMethod(true).Invoke(ai, new object[] { });
            }

            // Gets a list of the last ten actions the AI has taken
            public static int[] GetLastActions(AI ai, NPC npc = null)
            {
                if (npc == null)
                {
                    npc = GetNPC(ai);
                }
                PropertyInfo actScene_property = typeof(AI).GetProperty("actScene", BindingFlags.NonPublic | BindingFlags.Instance);
                ActionScene scene = (ActionScene)actScene_property.GetGetMethod(true).Invoke(ai, new object[] { });

                // Private dictionary with a private type of value causes a huge headache.
                FieldInfo dicTarget_property = typeof(ActionControl).GetField("dicTarget", BindingFlags.NonPublic | BindingFlags.Instance);
                IDictionary dicTarget = (IDictionary) dicTarget_property.GetValue(scene.actCtrl);

                //DesireInfo
                object di = dicTarget[npc.heroine];

                Type DesireInfo_type = typeof(ActionControl).GetNestedType("DesireInfo", BindingFlags.NonPublic);
                FieldInfo queueAction_property = DesireInfo_type.GetField("_queueAction", BindingFlags.Instance | BindingFlags.NonPublic);
                Queue<int> lastActions = (Queue<int>)queueAction_property.GetValue(di);
                
                // _queueAction is limited to ten elements
                return lastActions.ToArray();

            }

            private static HashSet<int> _replaceClothesActions = new HashSet<int>(new int[]
            {
                0x0, // Change Clothes
                0x1, // Toilet
                0x2, // Shower
                0x4, // H Solo
                0x26, // Lez
                0x27, // Lez Partner
            });

            [HarmonyPrefix]
            [HarmonyPatch(typeof(AI), "Result")]
            public static void AfterResult(ActionControl.ResultInfo result, AI __instance)
            {
                NPC npc = GetNPC(__instance);

                int[] actions = GetLastActions(__instance, npc);
                int n = actions.Length;

                // 17 (change mind) seems to happen when redirected by the player while desire is something else
                if (actions[n - 1] == 0x17)
                {
                    return;
                }

                // Multiple change clothes actions can be queued up.
                // Put clothes on when the latest action is not in the set.
                if (n >= 2 && (_replaceClothesActions.Contains(actions[n - 2])) && actions[n - 2] != actions[n - 1])
                {
                    npc.heroine.chaCtrl.SetClothesStateAll(0);
                    if (actions[n - 1] == 2) //shower
                    {
                        npc.heroine.chaCtrl.GetComponent<SkinEffectsController>().ClearCharaState(true);
                    }
                }
            }

            public static void InstallHook()
            {
                HarmonyInstance.Create(typeof(Hooks).FullName).PatchAll(typeof(Hooks));
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
        }
    }
}
