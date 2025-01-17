using System;
using System.Linq;
using BepInEx;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
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

            var cat = MakerConstants.Parameter.H;

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
            CurrentStateCategoryToggle CreateToggle(SkinEffectKind kind)
            {
                var textureCount = TextureLoader.GetTextureCount(kind);
                if (textureCount == 0) throw new Exception($"No textures for {kind} ???");

                var tgl = new CurrentStateCategoryToggle(name: kind.GetDisplayName(),
                                                         toggleCount: Mathf.Min(4, textureCount + 1),
                                                         onUpdateSelection: c => RescaleStudioLevel(c.charInfo.GetComponent<SkinEffectsController>().GetEffectLevel(kind), textureCount, 3));

                tgl.Value.Subscribe(Observer.Create((int x) =>
                {
                    foreach (var controller in StudioAPI.GetSelectedControllers<SkinEffectsController>())
                        controller.SetEffectLevel(kind, RescaleStudioLevel(x, tgl.ToggleCount - 1, textureCount), true);
                }));

                return tgl;
            }

            StudioAPI.GetOrCreateCurrentStateCategory("Additional skin effects")
                     .AddControls(SkinEffectKindUtils.ValidSkinEffectKinds.Select(CreateToggle).Cast<CurrentStateCategorySubItemBase>().ToArray());

            if (TimelineCompatibility.IsTimelineAvailable())
            {
                foreach (var skinEffectKind in SkinEffectKindUtils.ValidSkinEffectKinds)
                {
                    TimelineCompatibility.AddCharaFunctionInterpolable<int, SkinEffectsController>(owner: "SkinEffects",
                                                                                                     id: "Effect_" + skinEffectKind.ToDataKey(),
                                                                                                     name: skinEffectKind.GetDisplayName(),
                                                                                                     interpolateBefore: (oci, ctrl, leftValue, rightValue, factor) => ctrl.SetEffectLevel(skinEffectKind, Mathf.RoundToInt(Mathf.Lerp(leftValue, rightValue, factor)), true),
                                                                                                     interpolateAfter: null,
                                                                                                     getValue: (c, controller) => controller.GetEffectLevel(skinEffectKind));
                }
            }
        }

        private static int RescaleStudioLevel(int lvl, int maxInLvl, int maxOutLvl)
        {
            var rescaledLvl = maxInLvl < maxOutLvl ? lvl : Mathf.RoundToInt(lvl * (float)maxOutLvl / maxInLvl);
            return Mathf.Clamp(rescaledLvl, 0, maxOutLvl);
        }
    }
}