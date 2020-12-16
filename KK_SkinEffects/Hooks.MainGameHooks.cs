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

        }
    }
}