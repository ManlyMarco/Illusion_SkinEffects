using System;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using KoiSkinOverlayX;
using UnityEngine;

namespace KK_SkinEffects
{
    public class SkinEffectsController : CharaCustomFunctionController
    {
        private int _bloodLevel;
        private int _bukkakeLevel;
        private int _sweatLevel;
        private int _tearLevel;
        private int _droolLevel;
        private KoiSkinOverlayController _ksox;

        public int BloodLevel
        {
            get => _bloodLevel;
            set
            {
                value = Math.Min(value, TextureLoader.BldTexturesCount);
                if (_bloodLevel != value)
                {
                    _bloodLevel = value;
                    UpdateBldTexture();
                }
            }
        }

        public int BukkakeLevel
        {
            get => _bukkakeLevel;
            set
            {
                value = Math.Min(value, TextureLoader.CumTexturesCount);
                if (_bukkakeLevel != value)
                {
                    _bukkakeLevel = value;
                    UpdateCumTexture();
                }
            }
        }

        public int SweatLevel
        {
            get => _sweatLevel;
            set
            {
                value = Math.Min(value, TextureLoader.WetTexturesFaceCount);
                if (_sweatLevel != value)
                {
                    _sweatLevel = value;
                    UpdateWetTexture();
                }
            }
        }

        public int TearLevel
        {
            get => _tearLevel;
            set
            {
                value = Math.Min(value, TextureLoader.TearTexturesCount);
                if (_tearLevel != value)
                {
                    _tearLevel = value;
                    UpdateTearTexture();
                }
            }
        }

        public int DroolLevel
        {
            get => _droolLevel;
            set
            {
                value = Math.Min(value, TextureLoader.DroolTexturesCount);
                if (_droolLevel != value)
                {
                    _droolLevel = value;
                    UpdateDroolTexture();
                }
            }
        }

        public bool HymenRegen { get; set; }
        public bool StretchedHymen { get; set; }
        public bool FragileVag { get; set; }

        /// <summary>
        /// Prevents the deflowering effect from appearing, not saved to the card
        /// </summary>
        public bool DisableDeflowering { get; set; }

        private int _fragileVagTriggeredLvl;
        private int _insertCount;

        internal void OnFemaleGaugeUp(SaveData.Heroine heroine, HFlag hFlag)
        {
            var orgs = hFlag.GetOrgCount();

            // Increase sweat level every time female gauge reaches 70
            if (hFlag.gaugeFemale >= 70)
            {
                // Using GetOrgCount to prevent adding a level when you let gauge fall below 70 and resume
                if (SweatLevel < orgs + 1)
                    SweatLevel = orgs + 1;
            }

            // When going too rough and has FragileVag, add bld effect
            if (FragileVag)
            {
                if (_fragileVagTriggeredLvl == 0)
                {
                    if (orgs == 0 && IsRoughPiston(hFlag))
                    {
                        BloodLevel = Mathf.Max(1, BloodLevel + 1);
                        _fragileVagTriggeredLvl = 1;
                    }
                }

                if (_fragileVagTriggeredLvl < hFlag.count.sonyuOrg - 2)
                {
                    if (IsRoughPiston(hFlag))
                    {
                        BloodLevel++;
                        _fragileVagTriggeredLvl = hFlag.count.sonyuOrg - 1;
                    }
                }
            }
        }

        private static bool IsRoughPiston(HFlag hFlag)
        {
            return hFlag.gaugeFemale < 55 && hFlag.speed > 2.1f && hFlag.nowAnimStateName == "SLoop";
        }

        internal void OnFinishRawInside(SaveData.Heroine heroine, HFlag hFlag)
        {
            BukkakeLevel += 1;
        }

        internal void OnHSceneProcStart(SaveData.Heroine heroine, HFlag hFlag)
        {
            // Full wetness in shower scene
            if (hFlag.mode == HFlag.EMode.peeping && hFlag.nowAnimationInfo.nameAnimation == "シャワー覗き")
                SweatLevel = TextureLoader.WetTexturesBodyCount;
        }

        internal void OnInsert(SaveData.Heroine heroine, HFlag hFlag)
        {
            if (++_insertCount == 5 && FragileVag)
                BloodLevel++;

            if (DisableDeflowering) return;

            // -1 means it wasn't calculated yet for this scene
            if (BloodLevel == -1 && (heroine.isVirgin || HymenRegen))
            {
                // figure out bleed level
                var lvl = TextureLoader.BldTexturesCount - 1;
                if (hFlag.gaugeFemale >= 60)
                    lvl -= 1;
                if (hFlag.GetOrgCount() >= 2)
                    lvl -= 1;

                var attribs = heroine.parameter.attribute;
                if (attribs.bitch) lvl -= 2;
                if (attribs.undo) lvl -= 1;
                if (attribs.kireizuki) lvl += 1;
                if (attribs.majime) lvl += 2;

                var moreBldPersonalities = new[] { 03, 06, 08, 19, 20, 26, 28, 37 };
                var lessBldPersonalities = new[] { 00, 07, 11, 12, 13, 14, 15, 33 };
                if (moreBldPersonalities.Contains(heroine.personality))
                    lvl += 1;
                else if (lessBldPersonalities.Contains(heroine.personality))
                    lvl -= 1;

                if (StretchedHymen)
                    lvl -= 4;

                if (FragileVag)
                    lvl += 2;

                var minLvl = SkinEffectsMgr.EnableBldAlways.Value ? 1 : 0;

                BloodLevel = Mathf.Clamp(lvl, minLvl, TextureLoader.BldTexturesCount);

                if (SkinEffectsMgr.EnableTear.Value)
                {
                    if (BloodLevel == TextureLoader.BldTexturesCount)
                        TearLevel += 2;
                    else
                        TearLevel += 1;
                }
            }

            DisableDeflowering = true;
        }

        public void OnCumInMouth(SaveData.Heroine heroine, HFlag hFlag)
        {
            DroolLevel++;
            TearLevel++;
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            var data = new PluginData();

            data.data[nameof(HymenRegen)] = HymenRegen;
            data.data[nameof(StretchedHymen)] = StretchedHymen;
            data.data[nameof(FragileVag)] = FragileVag;

            if (currentGameMode == GameMode.Studio)
                WriteFluidState(data.data);

            SetExtendedData(data);
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (maintainState) return;

            _insertCount = 0;
            _fragileVagTriggeredLvl = 0;
            DisableDeflowering = false;

            var data = GetExtendedData();

            if (!MakerAPI.InsideAndLoaded || MakerAPI.GetCharacterLoadFlags().Parameters)
            {
                HymenRegen = false;
                StretchedHymen = false;
                FragileVag = false;

                if (data != null)
                {
                    if (data.data.TryGetValue(nameof(HymenRegen), out var val1)) HymenRegen = (bool)val1;
                    if (data.data.TryGetValue(nameof(StretchedHymen), out var val2)) StretchedHymen = (bool)val2;
                    if (data.data.TryGetValue(nameof(FragileVag), out var val3)) FragileVag = (bool)val3;
                }
            }

            switch (currentGameMode)
            {
                case GameMode.Studio:
                    // Get the state set in the character state menu
                    ApplyFluidState(data?.data);
                    break;

                case GameMode.MainGame:
                    // Get the state persisted in the currently loaded game
                    SkinEffectGameController.ApplyPersistData(this);
                    break;

                default:
                    ClearFluidState(true);
                    break;
            }
        }

        public bool ClearFluidState(bool refreshTextures = false)
        {
            var needsUpdate = _ksox.AdditionalTextures.RemoveAll(x => ReferenceEquals(x.Tag, this)) > 0;

            _bukkakeLevel = 0;
            _bloodLevel = -1;
            _sweatLevel = 0;
            _tearLevel = 0;
            _droolLevel = 0;

            if (refreshTextures)
                UpdateAllTextures();

            return needsUpdate;
        }

        public void ApplyFluidState(IDictionary<string, object> dataDict)
        {
            var needsUpdate = ClearFluidState();
            if (dataDict != null && dataDict.Count > 0)
            {
                if (dataDict.TryGetValue(nameof(BukkakeLevel), out var obj)) _bukkakeLevel = (int)obj;
                if (dataDict.TryGetValue(nameof(SweatLevel), out var obj2)) _sweatLevel = (int)obj2;
                if (dataDict.TryGetValue(nameof(BloodLevel), out var obj3)) _bloodLevel = (int)obj3;
                if (dataDict.TryGetValue(nameof(TearLevel), out var obj4)) _tearLevel = (int)obj4;
                if (dataDict.TryGetValue(nameof(DroolLevel), out var obj5)) _droolLevel = (int)obj5;

                UpdateWetTexture(false);
                UpdateBldTexture(false);
                UpdateCumTexture(false);
                UpdateDroolTexture(false);
                UpdateTearTexture(false);

                needsUpdate = true;
            }

            if (needsUpdate)
                UpdateAllTextures();
        }

        public void WriteFluidState(IDictionary<string, object> dataDict)
        {
            dataDict[nameof(BukkakeLevel)] = BukkakeLevel;
            dataDict[nameof(SweatLevel)] = SweatLevel;
            dataDict[nameof(BloodLevel)] = BloodLevel;
            dataDict[nameof(TearLevel)] = TearLevel;
            dataDict[nameof(DroolLevel)] = DroolLevel;
        }

        protected override void Start()
        {
            _ksox = GetComponent<KoiSkinOverlayController>();
            base.Start();
        }

        public void UpdateAllTextures()
        {
            _ksox.UpdateTexture(TexType.BodyOver);
            _ksox.UpdateTexture(TexType.FaceOver);
        }

        private void UpdateBldTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.BldTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsMgr.EnableBld.Value)
            {
                if (BloodLevel > 0)
                {
                    // Insert bld at lowest position to keep it under cum
                    _ksox.AdditionalTextures.Insert(0, new AdditionalTexture(TextureLoader.BldTextures[BloodLevel - 1], TexType.BodyOver, this));
                }

                if (refresh)
                    _ksox.UpdateTexture(TexType.BodyOver);
            }
        }

        private void UpdateCumTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.CumTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsMgr.EnableCum.Value)
            {
                if (BukkakeLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.CumTextures[BukkakeLevel - 1], TexType.BodyOver, this));

                if (refresh)
                    _ksox.UpdateTexture(TexType.BodyOver);
            }
        }

        private void UpdateWetTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.WetTexturesBody.Contains(x.Texture) || TextureLoader.WetTexturesFace.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsMgr.EnableSwt.Value)
            {
                if (SweatLevel > 0)
                {
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.WetTexturesBody[SweatLevel - 1], TexType.BodyOver, this));
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.WetTexturesFace[SweatLevel - 1], TexType.FaceOver, this));
                }

                if (refresh)
                {
                    _ksox.UpdateTexture(TexType.BodyOver);
                    _ksox.UpdateTexture(TexType.FaceOver);
                }
            }
        }

        private void UpdateTearTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.TearTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsMgr.EnableTear.Value)
            {
                if (TearLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.TearTextures[TearLevel - 1], TexType.FaceOver, this));

                if (refresh)
                    _ksox.UpdateTexture(TexType.FaceOver);
            }
        }

        private void UpdateDroolTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.DroolTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsMgr.EnableDrl.Value)
            {
                if (DroolLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.DroolTextures[DroolLevel - 1], TexType.FaceOver, this));

                if (refresh)
                    _ksox.UpdateTexture(TexType.FaceOver);
            }
        }
    }
}
