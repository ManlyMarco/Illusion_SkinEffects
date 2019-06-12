using ActionGame.Chara;
using Harmony;
using System.Collections;
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
                if (heroine != null)
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
