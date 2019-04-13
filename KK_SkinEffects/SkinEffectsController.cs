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

        internal void OnFemaleGaugeUp(SaveData.Heroine heroine, HFlag hFlag)
        {
            if (SkinEffectsMgr.EnableSwt.Value)
            {
                // Increase sweat level every time female gauge reaches 70
                if (hFlag.gaugeFemale >= 70)
                {
                    // Using GetOrgCount to prevent adding a level when you let gauge fall below 70 and resume
                    var orgs = hFlag.GetOrgCount() + 1;
                    if (SweatLevel < orgs)
                        SweatLevel = orgs;
                }
            }
        }

        internal void OnFinishRawInside(SaveData.Heroine heroine, HFlag hFlag)
        {
            if (SkinEffectsMgr.EnableCum.Value)
                BukkakeLevel += 1;
        }

        internal void OnHSceneProcStart(SaveData.Heroine heroine, HFlag hFlag)
        {
            if (SkinEffectsMgr.EnableSwt.Value)
            {
                // Full wetness in shower scene
                if (hFlag.mode == HFlag.EMode.peeping && hFlag.nowAnimationInfo.nameAnimation == "シャワー覗き")
                    SweatLevel = TextureLoader.WetTexturesBodyCount;
            }
        }

        internal void OnInsert(SaveData.Heroine heroine, HFlag hFlag)
        {
            if (SkinEffectsMgr.EnableBld.Value && heroine.isVirgin && BloodLevel == -1)
            {
                // figure out bleed level
                var lvl = TextureLoader.BldTexturesCount - 1;
                if (hFlag.gaugeFemale >= 68)
                    lvl -= 1;
                if (hFlag.GetOrgCount() >= 3)
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
        }

        public void OnCumInMouth(SaveData.Heroine heroine, HFlag hFlag)
        {
            if (SkinEffectsMgr.EnableDrl.Value)
                DroolLevel++;
            if (SkinEffectsMgr.EnableTear.Value)
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
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.BldTextures.Contains(x.Texture));

            if (BloodLevel > 0)
            {
                // Insert bld at lowest position to keep it under cum
                _ksox.AdditionalTextures.Insert(0, new AdditionalTexture(TextureLoader.BldTextures[BloodLevel - 1], TexType.BodyOver, this));
            }

            if (refresh)
                _ksox.UpdateTexture(TexType.BodyOver);
        }

        private void UpdateCumTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.CumTextures.Contains(x.Texture));

            if (BukkakeLevel > 0)
                _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.CumTextures[BukkakeLevel - 1], TexType.BodyOver, this));

            if (refresh)
                _ksox.UpdateTexture(TexType.BodyOver);
        }

        private void UpdateWetTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.WetTexturesBody.Contains(x.Texture) || TextureLoader.WetTexturesFace.Contains(x.Texture));

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

        private void UpdateTearTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.TearTextures.Contains(x.Texture));

            if (TearLevel > 0)
                _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.TearTextures[TearLevel - 1], TexType.FaceOver, this));

            if (refresh)
                _ksox.UpdateTexture(TexType.FaceOver);
        }

        private void UpdateDroolTexture(bool refresh = true)
        {
            _ksox.AdditionalTextures.RemoveAll(x => TextureLoader.DroolTextures.Contains(x.Texture));

            if (DroolLevel > 0)
                _ksox.AdditionalTextures.Add(new AdditionalTexture(TextureLoader.DroolTextures[DroolLevel - 1], TexType.FaceOver, this));

            if (refresh)
                _ksox.UpdateTexture(TexType.FaceOver);
        }
    }
}
