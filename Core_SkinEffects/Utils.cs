using ActionGame.Chara;
using System.Collections.Generic;

namespace KK_SkinEffects
{
    internal static class Utils
    {
        /// <summary>
        /// Gets a queue with last ten actions the AI has taken
        /// </summary>
        public static Queue<int> GetLastActions(this AI ai)
        {
            var dicTarget = ai.actScene.actCtrl.dicTarget;
            // ActionControl.DesireInfo
            var di = dicTarget[ai.npc.heroine];
            return di.queueAction;
        }

        /// <summary>
        /// Returns whether the NPC is exiting a scene
        /// </summary>
        public static bool IsExitingScene(this NPC npc)
        {
            // A guess that seems to work. 
            return npc.isActive;
        }
    }
}
