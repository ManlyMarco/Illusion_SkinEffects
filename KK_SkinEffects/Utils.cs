using ActionGame.Chara;
using Harmony;
using System.Collections;
using System.Collections.Generic;
using Manager;
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
            var result = Object.FindObjectOfType<TalkScene>()?.targetHeroine;
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
    }
}
