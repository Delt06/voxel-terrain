using UnityEngine;

namespace UnityUtilities
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Deconstruct a color component-wise.
        /// </summary>
        /// <param name="color">Deconstructed color</param>
        /// <param name="r">color.r</param>
        /// <param name="g">color.g</param>
        /// <param name="b">color.b</param>
        /// <param name="a">color.a</param>
        public static void Deconstruct(this Color color, 
            out float r, out float g, out float b, out float a)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }
        
        /// <summary>
        /// Deconstruct a color component-wise.
        /// </summary>
        /// <param name="color">Deconstructed color</param>
        /// <param name="r">color.r</param>
        /// <param name="g">color.g</param>
        /// <param name="b">color.b</param>
        /// <param name="a">color.a</param>
        public static void Deconstruct(this Color32 color, 
            out byte r, out byte g, out byte b, out byte a)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }

        /// <summary>
        /// Create a copy of the color with some components being different. 
        /// </summary>
        /// <param name="color">Original color</param>
        /// <param name="r">New r (null if should remain the same)</param>
        /// <param name="g">New g (null if should remain the same)</param>
        /// <param name="b">New b (null if should remain the same)</param>
        /// <param name="a">New a (null if should remain the same)</param>
        /// <returns>A modified copy of the color.</returns>
        public static Color With(this Color color,
            float? r = null, float? g = null, float? b = null,
            float? a = null)
        {
            return new Color
            (
                r.GetValueOrDefault(color.r),
                g.GetValueOrDefault(color.g),
                b.GetValueOrDefault(color.b),
                a.GetValueOrDefault(color.a)
            );
        }
        
        /// <summary>
        /// Create a copy of the color with some components being different. 
        /// </summary>
        /// <param name="color">Original color</param>
        /// <param name="r">New r (null if should remain the same)</param>
        /// <param name="g">New g (null if should remain the same)</param>
        /// <param name="b">New b (null if should remain the same)</param>
        /// <param name="a">New a (null if should remain the same)</param>
        /// <returns>A modified copy of the color.</returns>
        public static Color32 With(this Color32 color,
            byte? r = null, byte? g = null, byte? b = null,
            byte? a = null)
        {
            return new Color32
            (
                r.GetValueOrDefault(color.r),
                g.GetValueOrDefault(color.g),
                b.GetValueOrDefault(color.b),
                a.GetValueOrDefault(color.a)
            );
        }
    }
}