using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace KK_SkinEffects
{
    /// <summary>
    /// List of all effect texture kinds. The values and names are important and should not be changed.
    /// Names are used for resource lookup, effect textures should have the same name as the enum value + a number.
    /// Values are used for indexing and should not be changed or some saved data might become misaligned. Values must be sequential starting from 0.
    /// Description is used only for display names and can be changed.
    /// AffectsBody / AffectsFace attributes are used to determine where the effect should be applied, only one can be used.
    /// </summary>
    public enum SkinEffectKind
    {
        Unknown = -1,
        [AffectsBody, Description("Virgin blood")] BloodBody = 0,
        [AffectsBody, Description("Bukkake")] BukkakeBody = 1,
        [AffectsBody, Description("Anal bukkake")] AnalBukkake = 2,
        [AffectsBody, Description("Body sweat")] WetBody = 3,
        [AffectsFace, Description("Face sweat")] WetFace = 4,
        [AffectsFace, Description("Tears")] TearFace = 5,
        [AffectsFace, Description("Drool")] DroolFace = 6,
        [AffectsFace, Description("Saliva")] Saliva = 7,
        [AffectsFace, Description("Cum in nose")] CumInNose = 8,
        [AffectsBody, Description("Butt blush")] ButtBody = 9,
        [AffectsBody, Description("Body blush")] BlushBody = 10,
        [AffectsFace, Description("Face blush")] BlushFace = 11,
        [AffectsBody, Description("Pussy juices")] PussyJuiceBody = 12,
    }

    public sealed class AffectsFaceAttribute : Attribute { }
    public sealed class AffectsBodyAttribute : Attribute { }

    public static class SkinEffectKindUtils
    {
        static SkinEffectKindUtils()
        {
            ValidSkinEffectKinds = Enum.GetValues(typeof(SkinEffectKind)).Cast<SkinEffectKind>().Where(x => x >= 0).OrderBy(x => x).ToArray();
            _affectFaceEffects = ValidSkinEffectKinds.Where(x => x.GetAttributeOfType<AffectsFaceAttribute>() != null).ToArray();
        }

        public static readonly SkinEffectKind[] ValidSkinEffectKinds;

        private static readonly SkinEffectKind[] _affectFaceEffects;

        public static bool AffectsFace(this SkinEffectKind kind) => Array.IndexOf(_affectFaceEffects, kind) >= 0;
        public static bool AffectsBody(this SkinEffectKind kind) => kind != SkinEffectKind.Unknown && !kind.AffectsFace();
        public static string ToDataKey(this SkinEffectKind kind) => ((int)kind).ToDataKey();
        public static string ToDataKey(this int id) => id.ToString("D", CultureInfo.InvariantCulture);
        public static string GetDisplayName(this SkinEffectKind kind) => kind.GetAttributeOfType<DescriptionAttribute>()?.Description ?? kind.ToString();
    }
}
