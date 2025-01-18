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
using Random = UnityEngine.Random;

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
            get => GetEffectLevel(SkinEffectKind.VirginBloodBody);
            set => SetEffectLevel(SkinEffectKind.VirginBloodBody, value, true);
        }

        [Obsolete]
        public int BukkakeLevel
        {
            get => GetEffectLevel(SkinEffectKind.PussyBukkakeBody);
            set => SetEffectLevel(SkinEffectKind.PussyBukkakeBody, value, true);
        }

        [Obsolete]
        public int AnalBukkakeLevel
        {
            get => GetEffectLevel(SkinEffectKind.AnalBukkakeBody);
            set => SetEffectLevel(SkinEffectKind.AnalBukkakeBody, value, true);
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
            get => GetEffectLevel(SkinEffectKind.SalivaFace);
            set => SetEffectLevel(SkinEffectKind.SalivaFace, value, true);
        }

        [Obsolete]
        public int CumInNoseLevel
        {
            get => GetEffectLevel(SkinEffectKind.CumInNoseFace);
            set => SetEffectLevel(SkinEffectKind.CumInNoseFace, value, true);
        }

        [Obsolete]
        public int ButtLevel
        {
            get => GetEffectLevel(SkinEffectKind.ButtBlushBody);
            set => SetEffectLevel(SkinEffectKind.ButtBlushBody, value, true);
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
        
        protected override void Start()
        {
            _ksox = GetComponent<KoiSkinOverlayController>();
            base.Start();
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

                ConvertVal("BukkakeLevel", SkinEffectKind.PussyBukkakeBody);
                ConvertVal("AnalBukkakeLevel", SkinEffectKind.AnalBukkakeBody);
                ConvertVal("SweatLevel", SkinEffectKind.WetBody, SkinEffectKind.WetFace);
                ConvertVal("BloodLevel", SkinEffectKind.VirginBloodBody);
                ConvertVal("TearLevel", SkinEffectKind.TearFace);
                ConvertVal("DroolLevel", SkinEffectKind.DroolFace);
                ConvertVal("SalivaLevel", SkinEffectKind.SalivaFace);
                ConvertVal("CumInNoseLevel", SkinEffectKind.CumInNoseFace);
                ConvertVal("BlushLevel", SkinEffectKind.BlushBody, SkinEffectKind.BlushFace);
                ConvertVal("PussyJuiceLevel", SkinEffectKind.PussyJuiceBody);

                data.version = 2;
            }
            else if (data.version > 2)
            {
                SkinEffectsPlugin.Logger.LogWarning("Character has SkinEffects data of version higher than supported! Update SkinEffects or you may lose the character's settings or encounter bugs.");
            }
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

        #region Event handlers

        private int _talkSceneTouchCount;
        // Sweat after touching a bunch in talk scene
        internal void OnTalkSceneTouch(SaveData.Heroine heroine, string touchKind)
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
                               () => SweatLevel += Random.Range(1, TextureLoader.GetTextureCount(SkinEffectKind.WetBody))));
        }
        
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
                var maxBloodLevel = TextureLoader.GetTextureCount(SkinEffectKind.VirginBloodBody);
                // figure out bleed level
                var level = maxBloodLevel - 1; // start at 1 less than max (index starts at 1, 0 is off)
                if (hFlag.gaugeFemale >= 60)
                    level -= 1;
                if (hFlag.GetOrgCount() >= 2)
                    level -= 1;

                var attribs = heroine.parameter.attribute;
                if (attribs.bitch) level -= 2;
                if (attribs.undo) level -= 1;
                if (attribs.kireizuki) level += 1;
                if (attribs.majime) level += 2;

                switch (heroine.personality)
                {
                    case 03:
                    case 06:
                    case 08:
                    case 19:
                    case 20:
                    case 26:
                    case 28:
                    case 37:
                        level += 1;
                        break;

                    case 00:
                    case 07:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                    case 33:
                        level -= 1;
                        break;
                }

                if (StretchedHymen)
                    level -= 4;

                if (FragileVag)
                    level += 2;

                var minBloodLevel = SkinEffectsPlugin.EnableBldAlways.Value ? 1 : 0;
                BloodLevel = Mathf.Clamp(level, minBloodLevel, maxBloodLevel);

                TearLevel += BloodLevel == maxBloodLevel ? 2 : 1;

                _bloodLevelNeedsCalc = false;
            }

            DisableDeflowering = true;
        }

        internal void OnAnalInsert(SaveData.Heroine heroine, HFlag hFlag)
        {
            TearLevel++;
        }

        internal void OnCumInMouth(SaveData.Heroine heroine, HFlag hFlag)
        {
            DroolLevel++;
            TearLevel++;

            _mouthFilledWithCumCount += 1;
            if (_mouthFilledWithCumCount >= 3)
                CumInNoseLevel += 1;
        }

        internal void OnKissing(SaveData.Heroine heroine, HFlag hFlag)
        {
            SalivaLevel++;
        }

        #endregion
    }
}
