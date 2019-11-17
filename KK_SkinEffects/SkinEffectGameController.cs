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
        private static readonly Dictionary<SaveData.Heroine, IDictionary<string, object>> _persistentCharaState = new Dictionary<SaveData.Heroine, IDictionary<string, object>>();
        private static readonly HashSet<SaveData.Heroine> _disableDeflowering = new HashSet<SaveData.Heroine>();

        protected override void OnPeriodChange(Cycle.Type period)
        {
            ClearCharaState();
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            ClearCharaState();
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
            if (freeH || !SkinEffectsPlugin.EnablePersistence.Value) return;

            var isShower = proc.flags.IsShowerPeeping();
            foreach (var heroine in proc.flags.lstHeroine)
            {
                if (isShower)
                {
                    // Clear effects after a shower, save them after other types of h scenes
                    _persistentCharaState.Remove(heroine);
                }
                else
                {
                    var controller = heroine.chaCtrl.GetComponent<SkinEffectsController>();
                    SavePersistData(heroine, controller);

                    if (controller.DisableDeflowering)
                        _disableDeflowering.Add(heroine);
                }

                StartCoroutine(RefreshOnSceneChangeCo(heroine, true));
            }
        }

        /// <summary>
        /// Needed to apply new state to the copy of current character used outside of current scene.
        /// Must be called before the current scene exits. Can happen for Talk > Roaming, Talk > H, H > Roaming
        /// </summary>
        private static IEnumerator RefreshOnSceneChangeCo(SaveData.Heroine heroine, bool afterH)
        {
            // Store reference to the character copy used in current scene
            var previousControl = heroine.chaCtrl;
            // Wait until we switch from temporary character copy to the character used in the next scene
            yield return new WaitUntil(() => heroine.chaCtrl != previousControl && heroine.chaCtrl != null);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Apply the stored state from h scene
            var controller = heroine.chaCtrl.GetComponent<SkinEffectsController>();
            ApplyPersistData(controller);

            if (afterH)
            {
                if (Game.Instance != null && Game.Instance.actScene != null)
                {
                    // Make the girl want to take a shower after H. Index 2 is shower
                    var actCtrl = Game.Instance.actScene.actCtrl;
                    actCtrl?.SetDesire(2, heroine, 200);
                }

                // Slowly remove sweat effects as she "cools down"
                while (controller.SweatLevel > 0 || controller.TearLevel > 0)
                {
                    yield return new WaitForSeconds(60);

                    if (Scene.Instance.IsNowLoadingFade) break;

                    if (controller.SweatLevel > 0) controller.SweatLevel--;
                    if (controller.TearLevel > 0) controller.TearLevel--;
                    if (controller.DroolLevel > 0) controller.DroolLevel--;
                }
            }
        }

        private static void ClearCharaState()
        {
            foreach (var heroine in _persistentCharaState.Keys)
            {
                var chaCtrl = heroine.chaCtrl;
                if (chaCtrl != null)
                    chaCtrl.GetComponent<SkinEffectsController>().ClearCharaState(true);
            }

            _persistentCharaState.Clear();
        }

        public static void ApplyPersistData(SkinEffectsController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            IDictionary<string, object> stateDict = null;

            var heroine = controller.ChaControl.GetHeroine();
            if (heroine != null)
                _persistentCharaState.TryGetValue(heroine, out stateDict);

            controller.ApplyCharaState(stateDict);
        }

        internal void OnSceneUnload(SaveData.Heroine heroine, SkinEffectsController controller)
        {
            StartCoroutine(RefreshOnSceneChangeCo(heroine, false));
        }

        public static void SavePersistData(SaveData.Heroine heroine, SkinEffectsController controller)
        {
            if (heroine == null) throw new ArgumentNullException(nameof(heroine));
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            _persistentCharaState.TryGetValue(heroine, out var dict);
            if (dict == null)
                _persistentCharaState[heroine] = dict = new Dictionary<string, object>();

            controller.WriteCharaState(dict);
        }
    }
}
