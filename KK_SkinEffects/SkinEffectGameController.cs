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
        private static readonly Dictionary<SaveData.Heroine, IDictionary<string, object>> _persistentFluidData = new Dictionary<SaveData.Heroine, IDictionary<string, object>>();
        private static readonly HashSet<SaveData.Heroine> _disableDeflowering = new HashSet<SaveData.Heroine>();

        protected override void OnPeriodChange(Cycle.Type period)
        {
            ClearFluidState();
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            ClearFluidState();
            _disableDeflowering.Clear();
        }

        protected override void OnStartH(HSceneProc proc, bool freeH)
        {
            StopAllCoroutines();

            // Prevent the HymenRegen taking effect every time H is done in a day
            foreach (var heroine in proc.flags.lstHeroine)
                heroine.chaCtrl.GetComponent<SkinEffectsController>().DisableDeflowering = _disableDeflowering.Contains(heroine);
        }

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            if (freeH || !SkinEffectsMgr.EnablePersistance.Value) return;

            var isShower = proc.flags.IsShowerPeeping();
            foreach (var heroine in proc.flags.lstHeroine)
            {
                if (isShower)
                {
                    // Clear effects after a shower, save them after other types of h scenes
                    _persistentFluidData.Remove(heroine);
                }
                else
                {
                    var controller = heroine.chaCtrl.GetComponent<SkinEffectsController>();
                    SavePersistData(heroine, controller);

                    if (controller.DisableDeflowering)
                        _disableDeflowering.Add(heroine);
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

        private static void ClearFluidState()
        {
            foreach (var heroine in _persistentFluidData.Keys)
            {
                var chaCtrl = heroine.chaCtrl;
                if (chaCtrl != null)
                    chaCtrl.GetComponent<SkinEffectsController>().ClearFluidState(true);
            }

            _persistentFluidData.Clear();
        }

        public static void ApplyPersistData(SkinEffectsController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            IDictionary<string, object> stateDict = null;

            var heroine = controller.ChaControl.GetHeroine();
            if (heroine != null)
                _persistentFluidData.TryGetValue(heroine, out stateDict);

            controller.ApplyFluidState(stateDict);
        }

        private static void SavePersistData(SaveData.Heroine heroine, SkinEffectsController controller)
        {
            if (heroine == null) throw new ArgumentNullException(nameof(heroine));
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            _persistentFluidData.TryGetValue(heroine, out var dict);
            if (dict == null)
                _persistentFluidData[heroine] = dict = new Dictionary<string, object>();

            controller.WriteFluidState(dict);
        }
    }
}
