using System;
using ActionGame;
using ActionGame.Chara;
using HarmonyLib;
using UnityEngine;

namespace KK_SkinEffects
{
    internal static partial class Hooks
    {
        private static class MainGameHooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TalkScene), "TouchFunc", typeof(string), typeof(Vector3))]
            private static void TouchFuncHook(TalkScene __instance, string _kind)
            {
                GetEffectController(__instance.targetHeroine).OnTalkSceneTouch(__instance.targetHeroine, _kind);
            }

#if KK // todo implement in kks, maybe for stuff done in water and sunbathing? perf hit?
            [HarmonyPostfix]
            [HarmonyPatch(typeof(AI), "Result")]
            public static void AfterResult(AI __instance, ActionControl.ResultInfo result)
            {
                if (result == null) return;

                // Add sweat if the character is doing running workout. Checks need to be in postfix
                if ((result.actionNo == 6 || result.actionNo == 18) && result.point != null && result.point.transform.childCount > 0)
                {
                    var heroine = __instance.GetNPC()?.heroine;
                    var c = GetEffectController(heroine);
                    if (c != null) c.OnRunning();
                }
            }
#endif
        }
    }
}