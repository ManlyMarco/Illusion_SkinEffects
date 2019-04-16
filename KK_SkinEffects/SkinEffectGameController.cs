using System;
using System.Collections;
using System.Collections.Generic;
using ActionGame;
using KKAPI.MainGame;
using Manager;
using UnityEngine;

namespace KK_SkinEffects
{
    /// <summary>
    /// Used for keeping state of chracters in the main game
    /// </summary>
    internal class SkinEffectGameController : GameCustomFunctionController
    {
        private static readonly Dictionary<SaveData.Heroine, IDictionary<string, object>> _persistentData = new Dictionary<SaveData.Heroine, IDictionary<string, object>>();

        protected override void OnPeriodChange(Cycle.Type period)
        {
            foreach (var heroine in _persistentData.Keys)
                heroine.chaCtrl.GetComponent<SkinEffectsController>().ClearState(true);

            _persistentData.Clear();
        }

        protected override void OnStartH(HSceneProc proc, bool freeH)
        {
            StopAllCoroutines();
        }

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            if (!SkinEffectsMgr.EnablePersistance.Value) return;

            var isShower = proc.flags.IsShowerPeeping();
            foreach (var heroine in proc.flags.lstHeroine)
            {
                if (isShower)
                {
                    // Clear effects after a shower, save them after other types of h scenes
                    _persistentData.Remove(heroine);
                }
                else
                {
                    var controller = heroine.chaCtrl.GetComponent<SkinEffectsController>();
                    SavePersistData(heroine, controller);
                }

                StartCoroutine(AfterHCo(heroine, heroine.chaCtrl));
            }
        }

        private static IEnumerator AfterHCo(SaveData.Heroine heroine, ChaControl previousControl)
        {
            // Wait until we switch from h scene to map characters
            yield return new WaitUntil(() => heroine.chaCtrl != previousControl && heroine.chaCtrl != null);
            yield return new WaitForEndOfFrame();

            // Make the girl want to take a shower after H. Index 2 is shower
            var actCtrl = Game.Instance?.actScene?.actCtrl;
            actCtrl?.SetDesire(2, heroine, 200);

            // Apply the stored state from h scene
            var controller = heroine.chaCtrl.GetComponent<SkinEffectsController>();
            ApplyPersistData(controller);

            // Slowly remove sweat effect ("cool down")
            while (controller.SweatLevel > 0)
            {
                yield return new WaitForSeconds(60);
                controller.SweatLevel--;
            }
        }

        public static void ApplyPersistData(SkinEffectsController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            IDictionary<string, object> stateDict = null;

            var heroine = controller.ChaControl.GetHeroine();
            if (heroine != null)
                _persistentData.TryGetValue(heroine, out stateDict);

            controller.ApplyState(stateDict);
        }

        private static void SavePersistData(SaveData.Heroine heroine, SkinEffectsController controller)
        {
            if (heroine == null) throw new ArgumentNullException(nameof(heroine));
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            _persistentData.TryGetValue(heroine, out var dict);
            if (dict == null)
                _persistentData[heroine] = dict = new Dictionary<string, object>();

            controller.WriteState(dict);
        }
    }
}
