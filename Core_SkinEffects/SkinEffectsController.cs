using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using KoiSkinOverlayX;
using UnityEngine;
using KKAPI.Utilities;

namespace KK_SkinEffects
{
    public class SkinEffectsController : CharaCustomFunctionController
    {
        private int _bloodLevel;
        private int _bukkakeLevel;
        private int _analbukkakeLevel;
        private int _sweatLevel;
        private int _tearLevel;
        private int _droolLevel;
        private int _salivaLevel;
        private int _mouthFilledWithCumCount;
        private int _cumInNoseLevel;
        private int _buttLevel;
        private int _blushLevel;
        private int _pussyJuiceLevel;
        private byte[] _clothingState;
        private bool[] _accessoryState;
        private byte[] _siruState;
        private KoiSkinOverlayController _ksox;
        private bool _studioInitialLoad = true;

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
        public int AnalBukkakeLevel
        {
            get => _analbukkakeLevel;
            set
            {
                value = Math.Min(value, TextureLoader.AnalCumTexturesCount);
                if (_analbukkakeLevel != value)
                {
                    _analbukkakeLevel = value;
                    UpdateAnalCumTexture();
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
        public int SalivaLevel
        {
            get => _salivaLevel;
            set
            {
                value = Math.Min(value, TextureLoader.SalivaTexturesCount);
                if (_salivaLevel != value)
                {
                    _salivaLevel = value;
                    UpdateSalivaTexture();
                }
            }
        }

        public int CumInNoseLevel
        {
            get => _cumInNoseLevel;
            set
            {
                value = Math.Min(value, TextureLoader.CumInNoseTexturesCount);
                if (_cumInNoseLevel != value)
                {
                    _cumInNoseLevel = value;
                    UpdateCumInNoseTexture();
                }
            }
        }

        public int ButtLevel
        {
            get => _buttLevel;
            set
            {
                value = Math.Min(value, TextureLoader.ButtTexturesCount);
                if (_buttLevel != value)
                {
                    _buttLevel = value;
                    UpdateButtTexture();
                }
            }
        }

        public int BlushLevel
        {
            get => _blushLevel;
            set
            {
                value = Math.Min(value, TextureLoader.BlushTexturesFaceCount);
                if (_blushLevel != value)
                {
                    _blushLevel = value;
                    UpdateBlushTexture();
                }
            }
        }

        public int PussyJuiceLevel
        {
            get => _pussyJuiceLevel;
            set
            {
                value = Math.Min(value, TextureLoader.PussyJuiceTexturesCount);
                if (_pussyJuiceLevel != value)
                {
                    _pussyJuiceLevel = value;
                    UpdatePussyJuiceTexture();
                }
            }
        }

        public byte[] ClothingState
        {
            get => _clothingState;
            set
            {
                if (_clothingState != value || _clothingState == null)
                {
                    _clothingState = value;

                    UpdateClothingState(true);
                }
            }
        }

        public bool[] AccessoryState
        {
            get => _accessoryState;
            set
            {
                if (_accessoryState != value || _accessoryState == null)
                {
                    _accessoryState = value;
                    UpdateAccessoryState();
                }
            }
        }

        public byte[] SiruState
        {
            get => _siruState;
            set
            {
                if (_siruState != value || _siruState == null)
                {
                    _siruState = value;
                    UpdateSiruState();
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
        private bool _stopChecking;

        internal void OnFemaleGaugeUp(SaveData.Heroine heroine, HFlag hFlag)
        {
            var gaugeFemale = hFlag.gaugeFemale;

            if (hFlag.mode == HFlag.EMode.masturbation)
            {
                // Worakound to org count not increasing when the girl climaxes in masturbation/peeping scenes
                if (_stopChecking)
                {
                    if (gaugeFemale >= 0 && gaugeFemale < 5)
                    {
                        //Reset it when meter resets, only if not already set back to false.
                        _stopChecking = false;
                    }
                }
                else
                {
                    if (gaugeFemale >= 70 && gaugeFemale < 71)
                    {
                        // Also make this check between 70-71, as it seems to be a decimal value between these two numbers. Don't want to check consistently.
                        SweatLevel += 1;
                        PussyJuiceLevel += 1;
                        if (SweatLevel >= 2)
                            BlushLevel++;

                        _stopChecking = true;
                    }
                }
            }
            else
            {
                if (gaugeFemale >= 70)
                {
                    // Using GetOrgCount to prevent adding a level when you let gauge fall below 70 and resume
                    var orgCount = hFlag.GetOrgCount();
                    var orgsPlusOne = orgCount + 1;
                    if (SweatLevel < orgsPlusOne)
                        SweatLevel = orgsPlusOne;
                    if (PussyJuiceLevel < orgsPlusOne)
                        PussyJuiceLevel = orgsPlusOne;
                    if (BlushLevel < orgCount / 2)
                        BlushLevel = orgCount / 2;
                }
            }

            // When going too rough and has FragileVag, add bld effect
            if (FragileVag)
            {
                if (_fragileVagTriggeredLvl == 0)
                {
                    var orgCount = hFlag.GetOrgCount();
                    if (orgCount == 0 && IsRoughPiston(hFlag))
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
        internal void OnFinishAnalRawInside(SaveData.Heroine heroine, HFlag hFlag)
        {
            AnalBukkakeLevel += 1;
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

                var minLvl = SkinEffectsPlugin.EnableBldAlways.Value ? 1 : 0;

                BloodLevel = Mathf.Clamp(lvl, minLvl, TextureLoader.BldTexturesCount);

                if (SkinEffectsPlugin.EnableTear.Value)
                {
                    if (BloodLevel == TextureLoader.BldTexturesCount)
                        TearLevel += 2;
                    else
                        TearLevel += 1;
                }
            }

            DisableDeflowering = true;
        }

        public void OnAnalInsert(SaveData.Heroine heroine, HFlag hFlag)
        {
            TearLevel++;
        }

        public void OnCumInMouth(SaveData.Heroine heroine, HFlag hFlag)
        {
            DroolLevel++;
            TearLevel++;

            _mouthFilledWithCumCount += 1;
            if (_mouthFilledWithCumCount >= 3)
                CumInNoseLevel += 1;
        }

        public void OnKissing(SaveData.Heroine heroine, HFlag hFlag)
        {
            SalivaLevel++;
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            var data = new PluginData();

            data.data[nameof(HymenRegen)] = HymenRegen;
            data.data[nameof(StretchedHymen)] = StretchedHymen;
            data.data[nameof(FragileVag)] = FragileVag;

            if (currentGameMode == GameMode.Studio)
                WriteCharaState(data.data, true);

            SetExtendedData(data);
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (currentGameMode != GameMode.Studio && maintainState) return;

            _insertCount = 0;
            _fragileVagTriggeredLvl = 0;
            DisableDeflowering = false;
            _talkSceneTouchCount = 0;

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
                    var dataDict = data?.data;

                    if (!_studioInitialLoad)
                    {
                        // persist current state when replacing character in studio
                        dataDict = dataDict ?? new Dictionary<string, object>();
                        dataDict[nameof(BukkakeLevel)] = _bukkakeLevel;
                        dataDict[nameof(AnalBukkakeLevel)] = _analbukkakeLevel;
                        dataDict[nameof(SweatLevel)] = _sweatLevel;
                        dataDict[nameof(BloodLevel)] = _bloodLevel;
                        dataDict[nameof(TearLevel)] = _tearLevel;
                        dataDict[nameof(DroolLevel)] = _droolLevel;
                        dataDict[nameof(SalivaLevel)] = _salivaLevel;
                        dataDict[nameof(CumInNoseLevel)] = _cumInNoseLevel;
                        dataDict[nameof(BlushLevel)] = _blushLevel;
                        dataDict[nameof(PussyJuiceLevel)] = _pussyJuiceLevel;
                    }

                    _studioInitialLoad = false;
                    // Get the state set in the character state menu
                    ApplyCharaState(dataDict, true);
                    break;

                case GameMode.MainGame:
                    // Get the state persisted in the currently loaded game
                    SkinEffectGameController.ApplyPersistData(this);
                    break;

                default:
                    ClearCharaState(true);
                    break;
            }
        }

        public bool ClearCharaState(bool refreshTextures = false, bool forceClothesStateUpdate = false)
        {
            var needsUpdate = _ksox.AdditionalTextures.RemoveAll(x => ReferenceEquals(x.Tag, this)) > 0;

            _bukkakeLevel = 0;
            _analbukkakeLevel = 0;
            _bloodLevel = -1;
            _sweatLevel = 0;
            _tearLevel = 0;
            _droolLevel = 0;
            _salivaLevel = 0;
            _cumInNoseLevel = 0;
            _blushLevel = 0;
            _pussyJuiceLevel = 0;

            if (_siruState != null || forceClothesStateUpdate)
            {
                _siruState = null;
                UpdateSiruState();
            }
            if (_clothingState != null || forceClothesStateUpdate)
            {
                _clothingState = null;
                UpdateClothingState(true);
            }
            if (_accessoryState != null || forceClothesStateUpdate)
            {
                _accessoryState = null;
                UpdateAccessoryState();
            }

            if (refreshTextures && needsUpdate)
                UpdateAllTextures();

            return needsUpdate;
        }

        public void ApplyCharaState(IDictionary<string, object> dataDict, bool onlyCustomEffects = false)
        {
            var needsUpdate = ClearCharaState();
            if (dataDict != null && dataDict.Count > 0)
            {
                int GetValue(string propName)
                {
                    if (dataDict.TryGetValue(propName, out var obj))
                    {
                        var result = (int)obj;
                        if (result > 0) needsUpdate = true;
                        return result;
                    }
                    return 0;
                }

                _bukkakeLevel = GetValue(nameof(BukkakeLevel));
                _analbukkakeLevel = GetValue(nameof(AnalBukkakeLevel));
                _sweatLevel = GetValue(nameof(SweatLevel));
                _bloodLevel = GetValue(nameof(BloodLevel));
                _tearLevel = GetValue(nameof(TearLevel));
                _droolLevel = GetValue(nameof(DroolLevel));
                _buttLevel = GetValue(nameof(ButtLevel));
                _salivaLevel = GetValue(nameof(SalivaLevel));
                _cumInNoseLevel = GetValue(nameof(CumInNoseLevel));
                _blushLevel = GetValue(nameof(BlushLevel));
                _pussyJuiceLevel = GetValue(nameof(PussyJuiceLevel));

                UpdateWetTexture(false);
                UpdateBldTexture(false);
                UpdateCumTexture(false);
                UpdateAnalCumTexture(false);
                UpdateDroolTexture(false);
                UpdateSalivaTexture(false);
                UpdateCumInNoseTexture(false);
                UpdateTearTexture(false);
                UpdateButtTexture(false);
                UpdateBlushTexture(false);
                UpdatePussyJuiceTexture(false);

                if (!onlyCustomEffects && !StudioAPI.InsideStudio)
                {
                    // The casts are necessary when deserializing with messagepack because it can produce object[] arrays
                    if (dataDict.TryGetValue(nameof(ClothingState), out var obj7)) _clothingState = ((IEnumerable)obj7).Cast<byte>().ToArray();
                    if (dataDict.TryGetValue(nameof(AccessoryState), out var obj8)) _accessoryState = ((IEnumerable)obj8).Cast<bool>().ToArray();
                    if (dataDict.TryGetValue(nameof(SiruState), out var obj9)) _siruState = ((IEnumerable)obj9).Cast<byte>().ToArray();
                    UpdateClothingState();
                    UpdateAccessoryState();
                    UpdateSiruState();
                }
            }

            if (needsUpdate)
                UpdateAllTextures();
        }

        public void WriteCharaState(IDictionary<string, object> dataDict, bool onlyCustomEffects = false)
        {
            dataDict[nameof(BukkakeLevel)] = BukkakeLevel;
            dataDict[nameof(AnalBukkakeLevel)] = AnalBukkakeLevel;
            dataDict[nameof(SweatLevel)] = SweatLevel;
            dataDict[nameof(BloodLevel)] = BloodLevel;
            dataDict[nameof(TearLevel)] = TearLevel;
            dataDict[nameof(DroolLevel)] = DroolLevel;
            dataDict[nameof(SalivaLevel)] = SalivaLevel;
            dataDict[nameof(CumInNoseLevel)] = CumInNoseLevel;
            dataDict[nameof(ButtLevel)] = ButtLevel;
            dataDict[nameof(BlushLevel)] = BlushLevel;
            dataDict[nameof(PussyJuiceLevel)] = PussyJuiceLevel;

            if (!onlyCustomEffects && !StudioAPI.InsideStudio)
            {
                dataDict[nameof(ClothingState)] = (byte[])ChaFileControl.status.clothesState.Clone();
                dataDict[nameof(AccessoryState)] = (bool[])ChaFileControl.status.showAccessory.Clone();
                dataDict[nameof(SiruState)] = (byte[])ChaFileControl.status.siruLv.Clone();
            }
        }

        protected override void Start()
        {
            _ksox = GetComponent<KoiSkinOverlayController>();
            base.Start();
        }

        public void UpdateAllTextures()
        {
            UpdateTextures(true, true);
        }

        private void UpdateTextures(bool body, bool face)
        {
            void Update()
            {
                if (body)
                    _ksox.UpdateTexture(TexType.BodyOver);
                if (face)
                    _ksox.UpdateTexture(TexType.FaceOver);
            }

            // Needed in rare cases to prevent body texture from becoming black during H scene, not needed outside of it
            if (SceneApi.GetLoadSceneName() == "H")
                StartCoroutine(new object[] { new WaitForEndOfFrame(), CoroutineUtils.CreateCoroutine(Update) }.GetEnumerator());
            else
                Update();
        }

        private void UpdateBldTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.BldTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableBld.Value)
            {
                if (BloodLevel > 0)
                {
                    // Keep it under the cum tex
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.BldTextures[BloodLevel - 1], TexType.BodyOver, this, 101));
                }

                if (refresh)
                    UpdateTextures(true, false);
            }
        }

        private void UpdateCumTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.CumTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableCum.Value)
            {
                if (BukkakeLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.CumTextures[BukkakeLevel - 1], TexType.BodyOver, this, 102));

                if (refresh)
                    UpdateTextures(true, false);
            }
        }

        private void UpdateAnalCumTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.AnalCumTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableCum.Value)
            {
                if (AnalBukkakeLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.AnalCumTextures[AnalBukkakeLevel - 1], TexType.BodyOver, this, 102));

                if (refresh)
                    UpdateTextures(true, false);
            }
        }

        private void UpdateWetTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.WetTexturesBody.Contains(x.Texture) || TextureLoader.WetTexturesFace.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableSwt.Value)
            {
                if (SweatLevel > 0)
                {
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.WetTexturesBody[SweatLevel - 1], TexType.BodyOver, this, 100));
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.WetTexturesFace[SweatLevel - 1], TexType.FaceOver, this, 100));
                }

                if (refresh)
                    UpdateAllTextures();
            }
        }

        private void UpdateTearTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.TearTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableTear.Value)
            {
                if (TearLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.TearTextures[TearLevel - 1], TexType.FaceOver, this, 102));

                if (refresh)
                    UpdateTextures(false, true);
            }
        }

        private void UpdateDroolTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.DroolTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableDrl.Value)
            {
                if (DroolLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.DroolTextures[DroolLevel - 1], TexType.FaceOver, this, 101));

                if (refresh)
                    UpdateTextures(false, true);
            }
        }

        private void UpdateSalivaTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.SalivaTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableDrl.Value)
            {
                if (SalivaLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.SalivaTextures[SalivaLevel - 1], TexType.FaceOver, this, 101));

                if (refresh)
                    UpdateTextures(false, true);
            }
        }

        private void UpdateCumInNoseTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.CumInNoseTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableDrl.Value)
            {
                if (CumInNoseLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.CumInNoseTextures[CumInNoseLevel - 1], TexType.FaceOver, this, 101));

                if (refresh)
                    UpdateTextures(false, true);
            }
        }

        private void UpdateButtTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.ButtTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableButt.Value)
            {
                if (ButtLevel > 0)
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.ButtTextures[ButtLevel - 1], TexType.BodyOver, this, 99));

                if (refresh)
                    UpdateTextures(true, false);
            }
        }

        private void UpdateBlushTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.BlushTexturesFace.Contains(x.Texture));
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.BlushTexturesBody.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableBlush.Value)
            {
                if (BlushLevel > 0)
                {
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.BlushTexturesBody[BlushLevel - 1], TexType.BodyOver, this, 98));
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.BlushTexturesFace[BlushLevel - 1], TexType.FaceOver, this, 98));
                }

                if (refresh)
                    UpdateTextures(true, true);
            }
        }

        private void UpdatePussyJuiceTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.PussyJuiceTextures.Contains(x.Texture));

            if (StudioAPI.InsideStudio || SkinEffectsPlugin.EnableJuice.Value)
            {
                if (PussyJuiceLevel > 0)
                {
                    // Keep it under the cum tex but over the body blush
                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.PussyJuiceTextures[PussyJuiceLevel - 1], TexType.BodyOver, this, 99));
                }

                if (refresh)
                    UpdateTextures(true, false);
            }
        }

        private void UpdateClothingState(bool forceClothesStateUpdate = false)
        {
            if (StudioAPI.InsideStudio) return;

            if (_clothingState != null)
            {
                ChaControl.fileStatus.clothesState = _clothingState;
                ChaControl.UpdateClothesStateAll();
            }
            else if (forceClothesStateUpdate)
                ChaControl.SetClothesStateAll(0);
        }

        private void UpdateAccessoryState()
        {
            if (StudioAPI.InsideStudio) return;

            if (_accessoryState != null)
                ChaFileControl.status.showAccessory = _accessoryState;
        }

        private void UpdateSiruState()
        {
            if (StudioAPI.InsideStudio) return;

            foreach (ChaFileDefine.SiruParts s in Enum.GetValues(typeof(ChaFileDefine.SiruParts)))
            {
                if (_siruState != null)
                    ChaControl.SetSiruFlags(s, _siruState[(int)s]);
                else
                    ChaControl.SetSiruFlags(s, 0);
            }
            try
            {

                var ccType = typeof(ChaControl);

                // Store previous high poly value and if it's off then force it on so the semen can appear
                var hiPolyProperty = ccType.GetProperty(nameof(ChaControl.hiPoly), AccessTools.all);
                if (hiPolyProperty == null) throw new ArgumentNullException(nameof(hiPolyProperty));
                var hiPoly = (bool)hiPolyProperty.GetValue(ChaControl, null);
                if (!hiPoly)
                    hiPolyProperty.SetValue(ChaControl, true, null);

                // Trigger Semen update
                var updateSiruMethod = ccType.GetMethod("UpdateSiru", AccessTools.all);
                if (updateSiruMethod == null) throw new ArgumentNullException(nameof(updateSiruMethod));
                updateSiruMethod.Invoke(ChaControl, new object[] { true });

                // Restore previous high poly value
                if (!hiPoly)
                    hiPolyProperty.SetValue(ChaControl, false, null);
            }
            catch (Exception e)
            {
                SkinEffectsPlugin.Logger.LogError(e);
            }
        }

        private int _talkSceneTouchCount;
        // Sweat after touching a bunch in talk scene
        public void OnTalkSceneTouch(SaveData.Heroine heroine, string touchKind)
        {
            if (touchKind == "MuneL" || touchKind == "MuneR")
            {
                if (heroine.lewdness > 80)
                {
                    _talkSceneTouchCount++;
                    if (_talkSceneTouchCount >= 4 && _talkSceneTouchCount % 4 == 0) // Trigger every 4 touch events
                    {
                        if (SweatLevel < TextureLoader.WetTexturesFaceCount - 1) // Limit to not allow reaching max level
                            SweatLevel++;
                    }
                }
            }
        }

        public void OnRunning()
        {
            //Console.WriteLine("running " + gameObject.FullPath());
            // todo not working reliably?
            StartCoroutine(CoroutineUtils.CreateCoroutine(
                new WaitForSeconds(25),
                () => SweatLevel += UnityEngine.Random.Range(1, TextureLoader.WetTexturesFaceCount)));
        }
    }
}
