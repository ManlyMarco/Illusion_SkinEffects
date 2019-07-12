using System;
using System.Collections;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using UniRx;
using UnityEngine;

namespace KK_SkinEffects
{
    internal static class SkinEffectsGui
    {
        private static MakerToggle _stretched;
        private static MakerToggle _fragile;
        private static MakerToggle _regen;

        private static SkinEffectsPlugin _skinEffectsPlugin;

        public static void Init(SkinEffectsPlugin skinEffectsPlugin)
        {
            _skinEffectsPlugin = skinEffectsPlugin;

            if (StudioAPI.InsideStudio)
            {
                RegisterStudioControls();
            }
            else
            {
                MakerAPI.RegisterCustomSubCategories += RegisterMakerControls;
                MakerAPI.ChaFileLoaded += (sender, args) => _skinEffectsPlugin.StartCoroutine(ChaFileLoadedCo());
                MakerAPI.MakerExiting += MakerExiting;
            }
        }

        private static void MakerExiting(object sender, EventArgs e)
        {
            _stretched = null;
            _fragile = null;
            _regen = null;
        }

        private static IEnumerator ChaFileLoadedCo()
        {
            yield return null;

            if (MakerAPI.InsideMaker && _stretched != null)
            {
                var ctrl = GetMakerController();

                _stretched.Value = ctrl.StretchedHymen;
                _fragile.Value = ctrl.FragileVag;
                _regen.Value = ctrl.HymenRegen;
            }
        }

        private static void RegisterMakerControls(object sender, RegisterSubCategoriesEvent e)
        {
            // Doesn't apply for male characters
            if (MakerAPI.GetMakerSex() == 0) return;

            var cat = MakerConstants.GetBuiltInCategory("05_ParameterTop", "tglH");

            _stretched = e.AddControl(new MakerToggle(cat, "Stretched hymen", false, _skinEffectsPlugin));
            _stretched.ValueChanged.Subscribe(b => GetMakerController().StretchedHymen = b);
            e.AddControl(new MakerText("Makes it much less likely that she will bleed during the first time.", cat, _skinEffectsPlugin)).TextColor = new Color(0.7f, 0.7f, 0.7f);
            _regen = e.AddControl(new MakerToggle(cat, "Hymen regenerates", false, _skinEffectsPlugin));
            _regen.ValueChanged.Subscribe(b => GetMakerController().HymenRegen = b);
            e.AddControl(new MakerText("The hymen grows back after a good night's sleep (to the state before sex).", cat, _skinEffectsPlugin)).TextColor = new Color(0.7f, 0.7f, 0.7f);
            _fragile = e.AddControl(new MakerToggle(cat, "Fragile vagina", false, _skinEffectsPlugin));
            _fragile.ValueChanged.Subscribe(b => GetMakerController().FragileVag = b);
            e.AddControl(new MakerText("When going at it very roughly has a chance to bleed, be gentle!", cat, _skinEffectsPlugin)).TextColor = new Color(0.7f, 0.7f, 0.7f);
        }

        private static SkinEffectsController GetMakerController()
        {
            return MakerAPI.GetCharacterControl().GetComponent<SkinEffectsController>();
        }

        private static void RegisterStudioControls()
        {
            CurrentStateCategoryToggle CreateToggle(string name, int textureCount, Action<SkinEffectsController, int> set, Func<SkinEffectsController, int> get)
            {
                var tgl = new CurrentStateCategoryToggle(name,
                    Mathf.Min(4, textureCount + 1),
                    c => RescaleStudioLevel(get(c.charInfo.GetComponent<SkinEffectsController>()), textureCount, 3));

                tgl.SelectedIndex.Subscribe(Observer.Create((int x) =>
                {
                    var controller = GetSelectedStudioController();
                    if (controller != null)
                        set(controller, RescaleStudioLevel(x, tgl.ToggleCount - 1, textureCount));
                }));

                return tgl;
            }

            var sweatTgl = CreateToggle("Sweat", TextureLoader.WetTexturesFaceCount, (controller, i) => controller.SweatLevel = i, controller => controller.SweatLevel);
            var tearsTgl = CreateToggle("Tears", TextureLoader.TearTexturesCount, (controller, i) => controller.TearLevel = i, controller => controller.TearLevel);
            var droolTgl = CreateToggle("Drool", TextureLoader.DroolTexturesCount, (controller, i) => controller.DroolLevel = i, controller => controller.DroolLevel);
            var cumTgl = CreateToggle("Bukkake", TextureLoader.CumTexturesCount, (controller, i) => controller.BukkakeLevel = i, controller => controller.BukkakeLevel);
            var bldTgl = CreateToggle("Virgin blood", TextureLoader.BldTexturesCount, (controller, i) => controller.BloodLevel = i, controller => controller.BloodLevel);

            StudioAPI.CreateCurrentStateCategory(new CurrentStateCategory("Additional skin effects", new[] { sweatTgl, tearsTgl, droolTgl, cumTgl, bldTgl }));
        }

        private static SkinEffectsController GetSelectedStudioController()
        {
            return UnityEngine.Object.FindObjectOfType<MPCharCtrl>()?.ociChar?.charInfo?.GetComponent<SkinEffectsController>();
        }

        private static int RescaleStudioLevel(int lvl, int maxInLvl, int maxOutLvl)
        {
            var rescaledLvl = maxInLvl < maxOutLvl ? lvl : Mathf.RoundToInt(lvl * (float)maxOutLvl / maxInLvl);
            return Mathf.Clamp(rescaledLvl, 0, maxOutLvl);
        }
    }
}