using UnityEngine;

namespace GameShorts.CubeRunner.Core
{
    internal static class Extensions
    {
        internal static Vector3 ToVector3(this Vector2Int vector)
        {
            return new Vector3(vector.x, 0, vector.y);
        } 
    }
}