using System.Globalization;
using System.Numerics;

namespace LinkUi;

/// <summary>
/// sRGB, OKLab and OKLCH conversions (Björn Ottosson's matrices), WCAG contrast and OKLab distance.
/// Colours are <see cref="Vector4"/> sRGB in [0, 1] with alpha in W; the math runs in double.
/// </summary>
internal static class Oklch
{
    /// <summary>Parses <c>#rrggbb</c> or <c>#rrggbbaa</c>; each component is <c>byte / 255f</c>, alpha 1 when absent.</summary>
    public static Vector4 Hex(string hex)
    {
        var s = hex.StartsWith('#') ? hex.AsSpan(1) : hex.AsSpan();
        if (s.Length != 6 && s.Length != 8)
            throw new ArgumentException($"Not a #rrggbb or #rrggbbaa colour: {hex}", nameof(hex));
        static float Part(ReadOnlySpan<char> s, int at) => byte.Parse(s.Slice(at, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
        return new Vector4(Part(s, 0), Part(s, 2), Part(s, 4), s.Length == 8 ? Part(s, 6) : 1f);
    }

    /// <summary>OKLab (L, a, b) of an sRGB colour; alpha is ignored.</summary>
    public static Vector3 ToOklab(Vector4 srgb)
    {
        var (l, a, b) = Lab(srgb);
        return new Vector3((float)l, (float)a, (float)b);
    }

    /// <summary>OKLCH of an sRGB colour: hue in degrees in [0, 360), 0 when the chroma is below 1e-6.</summary>
    public static (float L, float C, float H) ToOklch(Vector4 srgb)
    {
        var (l, a, b) = Lab(srgb);
        var c = Math.Sqrt(a * a + b * b);
        var h = c < 1e-6 ? 0 : Math.Atan2(b, a) * 180 / Math.PI;
        if (h < 0)
            h += 360;
        if (h >= 360)
            h -= 360;
        return ((float)l, (float)c, (float)h);
    }

    /// <summary>
    /// sRGB colour (alpha 1) of an OKLCH value. Lightness is clamped to [0, 1]; a chroma outside the sRGB gamut
    /// is reduced by 16 bisection steps to the largest one found inside.
    /// </summary>
    public static Vector4 FromOklch(float l, float c, float hueDegrees)
    {
        var lightness = Math.Clamp((double)l, 0, 1);
        var chroma = Math.Max(0, (double)c);
        var hue = hueDegrees * Math.PI / 180;
        if (!InGamut(Linear(lightness, chroma, hue)))
        {
            double lo = 0, hi = chroma;
            for (var i = 0; i < 16; i++)
            {
                var mid = (lo + hi) / 2;
                if (InGamut(Linear(lightness, mid, hue)))
                    lo = mid;
                else
                    hi = mid;
            }
            chroma = lo;
        }
        var (r, g, b) = Linear(lightness, chroma, hue);
        return new Vector4((float)Encode(r), (float)Encode(g), (float)Encode(b), 1f);
    }

    /// <summary>WCAG 2.x contrast ratio, from 1 to 21; alpha is ignored.</summary>
    public static float Contrast(Vector4 a, Vector4 b)
    {
        var la = Luminance(a);
        var lb = Luminance(b);
        return (float)((Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05));
    }

    /// <summary>Euclidean distance in OKLab.</summary>
    public static float DeltaE(Vector4 a, Vector4 b)
    {
        var (l1, a1, b1) = Lab(a);
        var (l2, a2, b2) = Lab(b);
        return (float)Math.Sqrt((l1 - l2) * (l1 - l2) + (a1 - a2) * (a1 - a2) + (b1 - b2) * (b1 - b2));
    }

    private static (double L, double A, double B) Lab(Vector4 srgb)
    {
        var r = Decode(srgb.X);
        var g = Decode(srgb.Y);
        var b = Decode(srgb.Z);
        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
        return (0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
                1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
                0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
    }

    /// <summary>Linear sRGB of an OKLCH value (hue in radians), not clipped.</summary>
    private static (double R, double G, double B) Linear(double lightness, double chroma, double hue)
    {
        var a = chroma * Math.Cos(hue);
        var b = chroma * Math.Sin(hue);
        var l = lightness + 0.3963377774 * a + 0.2158037573 * b;
        var m = lightness - 0.1055613458 * a - 0.0638541728 * b;
        var s = lightness - 0.0894841775 * a - 1.2914855480 * b;
        l = l * l * l;
        m = m * m * m;
        s = s * s * s;
        return (4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
                -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
                -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
    }

    private static bool InGamut((double R, double G, double B) c) =>
        c.R is >= -1e-6 and <= 1 + 1e-6 && c.G is >= -1e-6 and <= 1 + 1e-6 && c.B is >= -1e-6 and <= 1 + 1e-6;

    private static double Luminance(Vector4 c) => 0.2126 * Decode(c.X) + 0.7152 * Decode(c.Y) + 0.0722 * Decode(c.Z);

    /// <summary>sRGB transfer function, encoded to linear.</summary>
    private static double Decode(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    /// <summary>Linear to encoded sRGB, clamped to [0, 1] first.</summary>
    private static double Encode(double c)
    {
        c = Math.Clamp(c, 0, 1);
        return c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;
    }
}
