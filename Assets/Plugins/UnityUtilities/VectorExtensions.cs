using UnityEngine;

namespace UnityUtilities
{
    public static class VectorExtensions
    {
        /// <summary>
        /// Deconstruct a vector component-wise.
        /// </summary>
        /// <param name="vector">Deconstructed vector</param>
        /// <param name="x">vector.x</param>
        /// <param name="y">vector.y</param>
        /// <param name="z">vector.z</param>
        public static void Deconstruct(this Vector3 vector, out float x, out float y, out float z)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }
    
        /// <summary>
        /// Deconstruct a vector component-wise.
        /// </summary>
        /// <param name="vector">Deconstructed vector</param>
        /// <param name="x">vector.x</param>
        /// <param name="y">vector.y</param>
        public static void Deconstruct(this Vector2 vector, out float x, out float y)
        {
            x = vector.x;
            y = vector.y;
        }

        /// <summary>
        /// Create a copy of the vector with some components being different. 
        /// </summary>
        /// <param name="vector">Original vector</param>
        /// <param name="x">New x (null if should remain the same)</param>
        /// <param name="y">New y (null if should remain the same)</param>
        /// <param name="z">New z (null if should remain the same)</param>
        /// <returns>A modified copy of the vector.</returns>
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }
    
        /// <summary>
        /// Create a copy of the vector with some components being different. 
        /// </summary>
        /// <param name="vector">Original vector</param>
        /// <param name="x">New x (null if should remain the same)</param>
        /// <param name="y">New y (null if should remain the same)</param>
        /// <returns>A modified copy of the vector.</returns>
        public static Vector2 With(this Vector2 vector, float? x = null, float? y = null)
        {
            return new Vector2(x ?? vector.x, y ?? vector.y);
        } 
    }
}