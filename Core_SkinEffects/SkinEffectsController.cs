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

#pragma warning disable CS0612 // Type or member is obsolete

namespace KK_SkinEffects
{
    public class SkinEffectsController : CharaCustomFunctionController
    {
        private int _mouthFilledWithCumCount;
        private int[] _effectLevels = new int[SkinEffectKindUtils.ValidSkinEffectKinds.Length];
        private byte[] _clothingState;
        private bool[] _accessoryState;
        private byte[] _siruState;
        private KoiSkinOverlayController _ksox;
        private bool _studioInitialLoad = true;
        private bool _bloodLevelNeedsCalc = true;

        /// <summary>
        /// Array of all effect levels, index is the SkinEffectKind enum value
        /// Value represents: 0 = disabled, 1 = texture index 0
        /// This is a copy of the internal array, modifying it will not affect the controller.
        /// </summary>
        public int[] GetEffectLevels() => _effectLevels.ToArray();

        /// <summary>
        /// 0 = disabled, 1 = texture index 0
        /// </summary>
        public int GetEffectLevel(SkinEffectKind kind)
        {
            // todo throw instead?
            if (kind < 0 || (int)kind >= SkinEffectKindUtils.ValidSkinEffectKinds.Length) return -1;
            return _effectLevels[(int)kind];
        }

        /// <summary>
        /// 0 = disabled, 1 = texture index 0
        /// </summary>
        public bool SetEffectLevel(SkinEffectKind kind, int level, bool updateEffects)
        {
            // todo throw instead?
            if (kind < 0 || (int)kind >= SkinEffectKindUtils.ValidSkinEffectKinds.Length) return false;

            level = Mathf.Clamp(level, 0, TextureLoader.GetTextureCount(kind));

            if (_effectLevels[(int)kind] != level)
            {
                _effectLevels[(int)kind] = level;

                if (updateEffects)
                    ApplyEffects(true);

                return true;
            }

            return false;
        }


        #region Obsolete props

        [Obsolete]
        public int BloodLevel
        {
            get => GetEffectLevel(SkinEffectKind.BloodBody);
            set => SetEffectLevel(SkinEffectKind.BloodBody, value, true);
        }

        [Obsolete]
        public int BukkakeLevel
        {
            get => GetEffectLevel(SkinEffectKind.BukkakeBody);
            set => SetEffectLevel(SkinEffectKind.BukkakeBody, value, true);
        }

        [Obsolete]
        public int AnalBukkakeLevel
        {
            get => GetEffectLevel(SkinEffectKind.AnalBukkake);
            set => SetEffectLevel(SkinEffectKind.AnalBukkake, value, true);
        }

        [Obsolete]
        public int SweatLevel
        {
            get => GetEffectLevel(SkinEffectKind.WetBody);
            set
            {
                if (SetEffectLevel(SkinEffectKind.WetBody, value, false) | // yes this has to be a single | not ||
                    SetEffectLevel(SkinEffectKind.WetFace, value, false))
                    ApplyEffects(true);
            }
        }

        [Obsolete]
        public int TearLevel
        {
            get => GetEffectLevel(SkinEffectKind.TearFace);
            set => SetEffectLevel(SkinEffectKind.TearFace, value, true);
        }

        [Obsolete]
        public int DroolLevel
        {
            get => GetEffectLevel(SkinEffectKind.DroolFace);
            set => SetEffectLevel(SkinEffectKind.DroolFace, value, true);
        }

        [Obsolete]
        public int SalivaLevel
        {
            get => GetEffectLevel(SkinEffectKind.Saliva);
            set => SetEffectLevel(SkinEffectKind.Saliva, value, true);
        }

        [Obsolete]
        public int CumInNoseLevel
        {
            get => GetEffectLevel(SkinEffectKind.CumInNose);
            set => SetEffectLevel(SkinEffectKind.CumInNose, value, true);
        }

        [Obsolete]
        public int ButtLevel
        {
            get => GetEffectLevel(SkinEffectKind.ButtBody);
            set => SetEffectLevel(SkinEffectKind.ButtBody, value, true);
        }

        [Obsolete]
        public int BlushLevel
        {
            get => GetEffectLevel(SkinEffectKind.BlushBody);
            set
            {
                if (SetEffectLevel(SkinEffectKind.BlushBody, value, false) | // yes this has to be a single | not ||
                    SetEffectLevel(SkinEffectKind.BlushFace, value, false))
                    ApplyEffects(true);
            }
        }

        [Obsolete]
        public int PussyJuiceLevel
        {
            get => GetEffectLevel(SkinEffectKind.PussyJuiceBody);
            set => SetEffectLevel(SkinEffectKind.PussyJuiceBody, value, true);
        }

        #endregion

        public byte[] ClothingState
        {
            get => _clothingState;
            set
            {
                if (_clothingState != value || _clothingState == null)
                {
                    _clothingState = value;

                    ApplyClothingState(true);
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
                    ApplyAccessoryState();
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
                    ApplySiruState();
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
                SweatLevel = TextureLoader.GetTextureCount(SkinEffectKind.WetBody);
        }

        internal void OnInsert(SaveData.Heroine heroine, HFlag hFlag)
        {
            if (++_insertCount == 5 && FragileVag)
                BloodLevel++;

            if (DisableDeflowering) return;

            if (_bloodLevelNeedsCalc && (heroine.isVirgin || HymenRegen))
            {
                var bldTexturesCount = TextureLoader.GetTextureCount(SkinEffectKind.BloodBody);
                // figure out bleed level
                var lvl = bldTexturesCount - 1; // start at 1 less than max (index starts at 1, 0 is off)
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

                BloodLevel = Mathf.Clamp(lvl, minLvl, bldTexturesCount);

                if (SkinEffectsPlugin.EnableTear.Value)
                {
                    if (BloodLevel == bldTexturesCount)
                        TearLevel += 2;
                    else
                        TearLevel += 1;
                }

                _bloodLevelNeedsCalc = false;
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
            var data = new PluginData { version = 2 };

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

            if (_ksox == null)
                _ksox = GetComponent<KoiSkinOverlayController>();

            var data = GetExtendedData() ?? new PluginData { version = 2 };
            AutoConvertExtData(data);

            if (!MakerAPI.InsideAndLoaded || MakerAPI.GetCharacterLoadFlags().Parameters)
            {
                data.data.TryGetValue(nameof(HymenRegen), out var val1);
                HymenRegen = Equals(val1, true);
                data.data.TryGetValue(nameof(StretchedHymen), out var val2);
                StretchedHymen = Equals(val2, true);
                data.data.TryGetValue(nameof(FragileVag), out var val3);
                FragileVag = Equals(val3, true);
            }

            switch (currentGameMode)
            {
                case GameMode.Studio:
                    var dataDict = data.data;

                    // Hold the state across character replacements in studio
                    if (!_studioInitialLoad || maintainState)
                        WriteCharaState(dataDict);
                    _studioInitialLoad = false;

                    // Get the state set in the character state menu
                    ApplyCharaState(dataDict, true);
                    break;

                case GameMode.MainGame:
                    // Get the state persisted in the currently loaded game
                    SkinEffectGameController.ApplyPersistData(this);
                    break;

                case GameMode.Unknown:
                case GameMode.Maker:
                default:
                    ClearCharaState(true);
                    break;
            }
        }

        public bool ClearCharaState(bool refreshEffects, bool forceClothesStateUpdate = false)
        {
            //Console.WriteLine($"ClearCharaState for {ChaControl.name} {refreshEffects} {forceClothesStateUpdate} - {new StackTrace(1)}");

            var hadEffects = _effectLevels.Any(x => x > 0);
            _effectLevels = new int[SkinEffectKindUtils.ValidSkinEffectKinds.Length];
            _bloodLevelNeedsCalc = true;

            if (_siruState != null || forceClothesStateUpdate)
            {
                _siruState = null;
                ApplySiruState();
            }
            if (_clothingState != null || forceClothesStateUpdate)
            {
                _clothingState = null;
                ApplyClothingState(true);
            }
            if (_accessoryState != null || forceClothesStateUpdate)
            {
                _accessoryState = null;
                ApplyAccessoryState();
            }

            if (refreshEffects && hadEffects)
                ApplyEffects(true);

            return hadEffects;
        }

        private static void WriteEffectLevelsToData(IDictionary<string, object> dataDict, int[] effectLevels)
        {
            if (dataDict == null) throw new ArgumentNullException(nameof(dataDict));
            if (effectLevels == null) throw new ArgumentNullException(nameof(effectLevels));
            if (effectLevels.Length != SkinEffectKindUtils.ValidSkinEffectKinds.Length)
                throw new ArgumentException($"effectLevels length does not match the number of valid skin effect kinds - {effectLevels.Length} to {SkinEffectKindUtils.ValidSkinEffectKinds.Length}");

            for (int id = 0; id < effectLevels.Length; id++)
                dataDict[id.ToDataKey()] = effectLevels[id];
        }
        private static int[] ReadEffectLevelsFromData(IDictionary<string, object> dataDict)
        {
            if (dataDict == null) throw new ArgumentNullException(nameof(dataDict));

            var result = new int[SkinEffectKindUtils.ValidSkinEffectKinds.Length];
            for (var index = 0; index < SkinEffectKindUtils.ValidSkinEffectKinds.Length; index++)
            {
                var id = (int)SkinEffectKindUtils.ValidSkinEffectKinds[index];
                if (dataDict.TryGetValue(id.ToDataKey(), out var obj))
                    result[id] = obj is int val ? val : 0;
            }

            return result;
        }
        private static void AutoConvertExtData(PluginData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (data.version < 2)
            {
                var dataDict = data.data;
                void ConvertVal(string oldKey, SkinEffectKind newKind, SkinEffectKind newKind2 = SkinEffectKind.Unknown)
                {
                    dataDict.TryGetValue(oldKey, out var oldVal);
                    dataDict.Remove(oldKey);
                    dataDict[newKind.ToDataKey()] = oldVal;
                    if (newKind2 != SkinEffectKind.Unknown)
                        dataDict[newKind2.ToDataKey()] = oldVal;
                }

                ConvertVal("BukkakeLevel", SkinEffectKind.BukkakeBody);
                ConvertVal("AnalBukkakeLevel", SkinEffectKind.AnalBukkake);
                ConvertVal("SweatLevel", SkinEffectKind.WetBody, SkinEffectKind.WetFace);
                ConvertVal("BloodLevel", SkinEffectKind.BloodBody);
                ConvertVal("TearLevel", SkinEffectKind.TearFace);
                ConvertVal("DroolLevel", SkinEffectKind.DroolFace);
                ConvertVal("SalivaLevel", SkinEffectKind.Saliva);
                ConvertVal("CumInNoseLevel", SkinEffectKind.CumInNose);
                ConvertVal("BlushLevel", SkinEffectKind.BlushBody, SkinEffectKind.BlushFace);
                ConvertVal("PussyJuiceLevel", SkinEffectKind.PussyJuiceBody);

                data.version = 2;
            }
            else if (data.version > 2)
            {
                SkinEffectsPlugin.Logger.LogWarning("Character has SkinEffects data of version higher than supported! Update SkinEffects or you may lose the character's settings or encounter bugs.");
            }
        }

        public void ApplyCharaState(IDictionary<string, object> dataDict, bool onlyCustomEffects = false)
        {
            var needsUpdate = ClearCharaState(false, false);
            if (dataDict != null && dataDict.Count > 0)
            {
                var newLevels = ReadEffectLevelsFromData(dataDict);
                if (newLevels.Any(x => x > 0)) needsUpdate = true;
                _effectLevels = newLevels;

                if (!onlyCustomEffects && !StudioAPI.InsideStudio)
                {
                    // The casts are necessary when deserializing with messagepack because it can produce object[] arrays
                    if (dataDict.TryGetValue(nameof(ClothingState), out var obj7)) _clothingState = ((IEnumerable)obj7).Cast<byte>().ToArray();
                    if (dataDict.TryGetValue(nameof(AccessoryState), out var obj8)) _accessoryState = ((IEnumerable)obj8).Cast<bool>().ToArray();
                    if (dataDict.TryGetValue(nameof(SiruState), out var obj9)) _siruState = ((IEnumerable)obj9).Cast<byte>().ToArray();
                    ApplyClothingState();
                    ApplyAccessoryState();
                    ApplySiruState();
                }
            }

            if (needsUpdate)
                ApplyEffects(true);
        }

        public void WriteCharaState(IDictionary<string, object> dataDict, bool onlyCustomEffects = false)
        {
            WriteEffectLevelsToData(dataDict, _effectLevels);

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

        [Obsolete]
        public void UpdateAllTextures()
        {
            UpdateOverlayTextures(true, true);
        }

        private void UpdateOverlayTextures(bool body, bool face)
        {
            if (!body && !face) return;

            void DoUpdate()
            {
                if (body)
                    _ksox.UpdateTexture(TexType.BodyOver);
                if (face)
                    _ksox.UpdateTexture(TexType.FaceOver);
            }

            // Needed in rare cases to prevent body texture from becoming black during H scene, not needed outside of it
            if (SceneApi.GetLoadSceneName() == "H")
                StartCoroutine(new object[] { new WaitForEndOfFrame(), CoroutineUtils.CreateCoroutine(DoUpdate) }.GetEnumerator());
            else
                DoUpdate();
        }

        private void ApplyEffects(bool updateTextures)
        {
            var body = false;
            var face = false;

            _ksox.AdditionalTextures.RemoveAll(x =>
            {
                if (ReferenceEquals(x.Tag, this))
                {
                    if (TextureLoader.LoadedTextures.TryGetValue(x.Texture, out var affectsFace))
                    {
                        if (affectsFace) face = true;
                        else body = true;
                        return true;
                    }
                }
                return false;
            });

            for (int i = 0; i < _effectLevels.Length; i++)
            {
                var lvl = _effectLevels[i];
                if (lvl > 0 && SkinEffectsPlugin.IsEffectEnabled((SkinEffectKind)i))
                {
                    var skinEffectKind = (SkinEffectKind)i;
                    var isFace = skinEffectKind.AffectsFace();

                    if (isFace) face = true;
                    else body = true;

                    _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.GetTexture(skinEffectKind, lvl - 1), isFace ? TexType.FaceOver : TexType.BodyOver, this, 100 + i));
                }
            }

            if (updateTextures)
                UpdateOverlayTextures(body, face);
        }

        private void ApplyClothingState(bool forceClothesStateUpdate = false)
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

        private void ApplyAccessoryState()
        {
            if (StudioAPI.InsideStudio) return;

            if (_accessoryState != null)
                ChaFileControl.status.showAccessory = _accessoryState;
        }

        private void ApplySiruState()
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
                        SweatLevel++;
                }
            }
        }

        internal void OnRunning()
        {
            //Console.WriteLine("running " + gameObject.FullPath());
            // todo not working reliably?
            StartCoroutine(CoroutineUtils.CreateCoroutine(
                new WaitForSeconds(25),
                () => SweatLevel += UnityEngine.Random.Range(1, TextureLoader.GetTextureCount(SkinEffectKind.WetBody))));
        }
    }
}
