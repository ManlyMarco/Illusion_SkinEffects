using ActionGame.Chara;
using System.Collections;
using System.Collections.Generic;
using ActionGame;
using ADV;
using HarmonyLib;
using Object = UnityEngine.Object;

namespace KK_SkinEffects
{
    internal static class Utils
    {
        public static SaveData.Heroine GetLeadHeroine(this HFlag hflag)
        {
            return hflag.lstHeroine[GetLeadHeroineId(hflag)];
        }

        public static int GetLeadHeroineId(this HFlag hflag)
        {
            return hflag.mode == HFlag.EMode.houshi3P || hflag.mode == HFlag.EMode.sonyu3P ? hflag.nowAnimationInfo.id % 2 : 0;
        }

        public static SaveData.Heroine GetCurrentVisibleGirl()
        {
            var advScene = GetADVScene();
            if (advScene != null)
            {
                if (advScene.Scenario?.currentHeroine != null)
                    return advScene.Scenario.currentHeroine;
                if (advScene.nowScene is TalkScene s && s.targetHeroine != null)
                    return s.targetHeroine;
            }
            
            return GetActionControl() != null ? Object.FindObjectOfType<TalkScene>()?.targetHeroine : null;
        }

        /// <summary>
        /// Get the NPC object assigned to this AI
        /// </summary>
        public static NPC GetNPC(this AI ai)
        {
            return Traverse.Create(ai).Property("npc").GetValue<NPC>();
        }

        /// <summary>
        /// Gets a queue with last ten actions the AI has taken
        /// </summary>
        public static Queue<int> GetLastActions(this AI ai)
        {
            var scene = Traverse.Create(ai).Property("actScene").GetValue<ActionScene>();

            // Dictionary<SaveData.Heroine, ActionControl.DesireInfo>
            var dicTarget = Traverse.Create(scene.actCtrl).Field("dicTarget").GetValue<IDictionary>();

            var npc = GetNPC(ai);
            // ActionControl.DesireInfo
            var di = dicTarget[npc.heroine];
            return Traverse.Create(di).Field("_queueAction").GetValue<Queue<int>>();
        }

        /// <summary>
        /// Returns whether the NPC is exiting a scene
        /// </summary>
        public static bool IsExitingScene(this NPC npc)
        {
            // A guess that seems to work. 
            return npc.isActive;
        }

        public static ActionControl GetActionControl()
        {
#if KK
            return Manager.Game.IsInstance() ? Manager.Game.Instance.actScene?.actCtrl : null;
#elif KKS
            return ActionControl.initialized ? ActionControl.instance : null;
#endif
        }

        public static ADVScene GetADVScene()
        {
#if KK
            return Manager.Game.IsInstance() ? Manager.Game.Instance.actScene?.advScene : null;
#elif KKS
            return ActionControl.initialized ? ActionControl.instance.actionScene?.AdvScene : null;
#endif
        }
    }
}
