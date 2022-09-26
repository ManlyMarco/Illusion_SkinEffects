using System;
using BepInEx;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using UniRx;
using UnityEngine;

namespace KK_SkinEffects
{
    internal static class SkinEffectsGui
    {
        public static void Init(SkinEffectsPlugin skinEffectsPlugin)
        {
            if (StudioAPI.InsideStudio)
                RegisterStudioControls();
            else
                MakerAPI.RegisterCustomSubCategories += (sender, args) => RegisterMakerControls(skinEffectsPlugin, args);
        }

        private static void RegisterMakerControls(BaseUnityPlugin skinEffectsPlugin, RegisterCustomControlsEvent e)
        {
            // Doesn't apply to male characters
            if (MakerAPI.GetMakerSex() == 0) return;

            var cat = MakerConstants.GetBuiltInCategory("05_ParameterTop", "tglH");

            e.AddControl(new MakerToggle(cat, "Stretched hymen", false, skinEffectsPlugin))
                .BindToFunctionController<SkinEffectsController, bool>(controller => controller.StretchedHymen, (controller, value) => controller.StretchedHymen = value);
            e.AddControl(new MakerText("Makes it much less likely that she will bleed during the first time.", cat, skinEffectsPlugin)).TextColor = new Color(0.7f, 0.7f, 0.7f);

            e.AddControl(new MakerToggle(cat, "Hymen regenerates", false, skinEffectsPlugin))
                .BindToFunctionController<SkinEffectsController, bool>(controller => controller.HymenRegen, (controller, value) => controller.HymenRegen = value);
            e.AddControl(new MakerText("The hymen grows back after a good night's sleep (to the state before sex).", cat, skinEffectsPlugin)).TextColor = new Color(0.7f, 0.7f, 0.7f);

            e.AddControl(new MakerToggle(cat, "Fragile vagina", false, skinEffectsPlugin))
                .BindToFunctionController<SkinEffectsController, bool>(controller => controller.FragileVag, (controller, value) => controller.FragileVag = value);
            e.AddControl(new MakerText("When going at it very roughly has a chance to bleed, be gentle!", cat, skinEffectsPlugin)).TextColor = new Color(0.7f, 0.7f, 0.7f);
        }

        private static void RegisterStudioControls()
        {
            CurrentStateCategoryToggle CreateToggle(string name, int textureCount, Action<SkinEffectsController, int> set, Func<SkinEffectsController, int> get)
            {
                var tgl = new CurrentStateCategoryToggle(name,
                    Mathf.Min(4, textureCount + 1),
                    c => RescaleStudioLevel(get(c.charInfo.GetComponent<SkinEffectsController>()), textureCount, 3));

                tgl.Value.Subscribe(Observer.Create((int x) =>
                {
                    foreach (var controller in StudioAPI.GetSelectedControllers<SkinEffectsController>())
                        set(controller, RescaleStudioLevel(x, tgl.ToggleCount - 1, textureCount));
                }));

                return tgl;
            }
            
            var buttTgl = CreateToggle("Butt blush", TextureLoader.BldTexturesCount, (controller, i) => controller.ButtLevel = i, controller => controller.ButtLevel);
            var sweatTgl = CreateToggle("Sweat", TextureLoader.WetTexturesFaceCount, (controller, i) => controller.SweatLevel = i, controller => controller.SweatLevel);
            var tearsTgl = CreateToggle("Tears", TextureLoader.TearTexturesCount, (controller, i) => controller.TearLevel = i, controller => controller.TearLevel);
            var droolTgl = CreateToggle("Drool", TextureLoader.DroolTexturesCount, (controller, i) => controller.DroolLevel = i, controller => controller.DroolLevel);
            var salivaTgl = CreateToggle("Saliva", TextureLoader.SalivaTexturesCount, (controller, i) => controller.SalivaLevel = i, controller => controller.SalivaLevel);
            var cuminnoseTgl = CreateToggle("CumInNose", TextureLoader.CumInNoseTexturesCount, (controller, i) => controller.CumInNoseLevel = i, controller => controller.CumInNoseLevel);
            var cumTgl = CreateToggle("Bukkake", TextureLoader.CumTexturesCount, (controller, i) => controller.BukkakeLevel = i, controller => controller.BukkakeLevel);
            var analcumTgl = CreateToggle("AnalBukkake", TextureLoader.AnalCumTexturesCount, (controller, i) => controller.AnalBukkakeLevel = i, controller => controller.AnalBukkakeLevel);
            var bldTgl = CreateToggle("Virgin blood", TextureLoader.BldTexturesCount, (controller, i) => controller.BloodLevel = i, controller => controller.BloodLevel);
            var pusTgl = CreateToggle("Pussy juice", TextureLoader.PussyJuiceTexturesCount, (controller, i) => controller.PussyJuiceLevel = i, controller => controller.PussyJuiceLevel);

            StudioAPI.GetOrCreateCurrentStateCategory("Additional skin effects").AddControls(buttTgl, sweatTgl, tearsTgl, droolTgl,salivaTgl,cuminnoseTgl, cumTgl,analcumTgl, bldTgl);
        }

        private static int RescaleStudioLevel(int lvl, int maxInLvl, int maxOutLvl)
        {
            var rescaledLvl = maxInLvl < maxOutLvl ? lvl : Mathf.RoundToInt(lvl * (float)maxOutLvl / maxInLvl);
            return Mathf.Clamp(rescaledLvl, 0, maxOutLvl);
        }
    }
}