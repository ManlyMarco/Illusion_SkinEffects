using System;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
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
                value = Math.Min(value, SkinEffectsMgr.BldTextures.Length);
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
                value = Math.Min(value, SkinEffectsMgr.CumTextures.Length);
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
                value = Math.Min(value, SkinEffectsMgr.WetTexturesFace.Length);
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
                value = Math.Min(value, SkinEffectsMgr.TearTextures.Length);
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
                value = Math.Min(value, SkinEffectsMgr.DroolTextures.Length);
                if (_droolLevel != value)
                {
                    _droolLevel = value;
                    UpdateDroolTexture();
                }
            }
        }

        internal void OnFemaleGaugeUp(SaveData.Heroine heroine, HFlag hFlag)
        {
            // Increase sweat level every time female gauge reaches 70
            if (!SkinEffectsMgr.EnableSwt.Value) return;
            if (hFlag.gaugeFemale >= 70)
            {
                var orgs = Math.Min(hFlag.GetOrgCount() + 1, SkinEffectsMgr.WetTexturesBody.Length);
                if (SweatLevel < orgs)
                    SweatLevel = orgs;
            }
        }

        internal void OnFinishRawInside(SaveData.Heroine heroine, HFlag hFlag)
        {
            if (!SkinEffectsMgr.EnableCum.Value) return;
            if (BukkakeLevel >= SkinEffectsMgr.CumTextures.Length - 1) return;
            BukkakeLevel += 1;
        }

        internal void OnHSceneProcStart(SaveData.Heroine heroine, HFlag hFlag)
        {
            // Full wetness in shower scene
            if (!SkinEffectsMgr.EnableSwt.Value) return;
            if (hFlag.mode == HFlag.EMode.peeping && hFlag.nowAnimationInfo.nameAnimation == "シャワー覗き")
                SweatLevel = SkinEffectsMgr.WetTexturesBody.Length;
        }

        internal void OnInsert(SaveData.Heroine heroine, HFlag hFlag)
        {
            if (!SkinEffectsMgr.EnableBld.Value) return;

            if (heroine.isVirgin && BloodLevel == -1)
            {
                // figure out bleed level
                var lvl = SkinEffectsMgr.BldTextures.Length - 1;
                if (hFlag.gaugeFemale >= 68)
                    lvl -= 1;
                if (hFlag.GetOrgCount() >= 1)
                    lvl -= 1;

                var attribs = heroine.parameter.attribute;
                if (attribs.bitch) lvl -= 2;
                if (attribs.undo) lvl -= 1;
                if (attribs.kireizuki) lvl += 1;
                if (attribs.majime) lvl += 2;

                var minLvl = SkinEffectsMgr.EnableBldAlways.Value ? 1 : 0;

                BloodLevel = Mathf.Clamp(lvl, minLvl, SkinEffectsMgr.BldTextures.Length);

                if (BloodLevel == SkinEffectsMgr.BldTextures.Length)
                    TearLevel += 2;
                else
                    TearLevel += 1;
            }
        }

        public void OnCumInMouth(SaveData.Heroine heroine, HFlag hFlag)
        {
            DroolLevel++;
            TearLevel++;
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            if (currentGameMode == GameMode.Studio)
            {
                var data = new PluginData();

                data.data[nameof(BukkakeLevel)] = BukkakeLevel;
                data.data[nameof(SweatLevel)] = SweatLevel;
                data.data[nameof(BloodLevel)] = BloodLevel;
                data.data[nameof(TearLevel)] = TearLevel;
                data.data[nameof(DroolLevel)] = DroolLevel;

                SetExtendedData(data);
            }
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            var update = _ksox.AdditionalTextures.RemoveAll(x => ReferenceEquals(x.Tag, this)) > 0;

            _bukkakeLevel = 0;
            _bloodLevel = -1;
            _sweatLevel = 0;
            _tearLevel = 0;
            _droolLevel = 0;

            if (currentGameMode == GameMode.Studio)
            {
                var data = GetExtendedData();

                if (data != null)
                {
                    if (data.data.TryGetValue(nameof(BukkakeLevel), out var obj)) _bukkakeLevel = (int)obj;
                    if (data.data.TryGetValue(nameof(SweatLevel), out var obj2)) _sweatLevel = (int)obj2;
                    if (data.data.TryGetValue(nameof(BloodLevel), out var obj3)) _bloodLevel = (int)obj3;
                    if (data.data.TryGetValue(nameof(TearLevel), out var obj4)) _tearLevel = (int)obj4;
                    if (data.data.TryGetValue(nameof(DroolLevel), out var obj5)) _droolLevel = (int)obj5;

                    UpdateWetTexture(false);
                    UpdateBldTexture(false);
                    UpdateCumTexture(false);
                    UpdateDroolTexture(false);
                    UpdateTearTexture(false);

                    update = true;
                }
            }

            if (update)
            {
                _ksox.UpdateTexture(TexType.BodyOver);
                _ksox.UpdateTexture(TexType.FaceOver);
            }
        }

        protected override void Start()
        {
            _ksox = GetComponent<KoiSkinOverlayController>();
            base.Start();
        }

        private void UpdateBldTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => SkinEffectsMgr.BldTextures.Contains(x.Texture));

            if (BloodLevel > 0)
            {
                // Insert bld at lowest position to keep it under cum
                _ksox.AdditionalTextures.Insert(0, new AdditionalTexture(SkinEffectsMgr.BldTextures[BloodLevel - 1], TexType.BodyOver, this));
            }

            if (refresh)
                _ksox.UpdateTexture(TexType.BodyOver);
        }

        private void UpdateCumTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => SkinEffectsMgr.CumTextures.Contains(x.Texture));

            if (BukkakeLevel > 0)
                _ksox.AdditionalTextures.Add(new AdditionalTexture(SkinEffectsMgr.CumTextures[BukkakeLevel - 1], TexType.BodyOver, this));

            if (refresh)
                _ksox.UpdateTexture(TexType.BodyOver);
        }

        private void UpdateWetTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => SkinEffectsMgr.WetTexturesBody.Contains(x.Texture) || SkinEffectsMgr.WetTexturesFace.Contains(x.Texture));

            if (SweatLevel > 0)
            {
                _ksox.AdditionalTextures.Add(new AdditionalTexture(SkinEffectsMgr.WetTexturesBody[SweatLevel - 1], TexType.BodyOver, this));
                _ksox.AdditionalTextures.Add(new AdditionalTexture(SkinEffectsMgr.WetTexturesFace[SweatLevel - 1], TexType.FaceOver, this));
            }

            if (refresh)
            {
                _ksox.UpdateTexture(TexType.BodyOver);
                _ksox.UpdateTexture(TexType.FaceOver);
            }
        }

        private void UpdateTearTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => SkinEffectsMgr.TearTextures.Contains(x.Texture));

            if (TearLevel > 0)
                _ksox.AdditionalTextures.Add(new AdditionalTexture(SkinEffectsMgr.TearTextures[TearLevel - 1], TexType.FaceOver, this));

            if (refresh)
                _ksox.UpdateTexture(TexType.FaceOver);
        }

        private void UpdateDroolTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => SkinEffectsMgr.DroolTextures.Contains(x.Texture));

            if (DroolLevel > 0)
                _ksox.AdditionalTextures.Add(new AdditionalTexture(SkinEffectsMgr.DroolTextures[DroolLevel - 1], TexType.FaceOver, this));

            if (refresh)
                _ksox.UpdateTexture(TexType.FaceOver);
        }
    }
}
