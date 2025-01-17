using System;
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

        /// <summary>
        /// Gets an attribute on an enum field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="enumVal">The enum value</param>
        /// <returns>The attribute of type T that exists on the enum value</returns>
        /// <example><![CDATA[string desc = myEnumVariable.GetAttributeOfType<DescriptionAttribute>().Description;]]></example>
        public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
            return attributes.Length > 0 ? (T)attributes[0] : null;
        }
    }
}
