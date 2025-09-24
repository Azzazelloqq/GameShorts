using Code.Games.Game2.Scripts.Core;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
    internal static class BlockSlicer
    {
        public static PlaceResult TryPlace(BlockData prev, BlockData curr)
        {
            float prevMin = (curr.axis == Axis.X) ? prev.center.x - prev.size.x * 0.5f : prev.center.z - prev.size.z * 0.5f;
            float prevMax = (curr.axis == Axis.X) ? prev.center.x + prev.size.x * 0.5f : prev.center.z + prev.size.z * 0.5f;

            float curMin = (curr.axis == Axis.X) ? curr.center.x - curr.size.x * 0.5f : curr.center.z - curr.size.z * 0.5f;
            float curMax = (curr.axis == Axis.X) ? curr.center.x + curr.size.x * 0.5f : curr.center.z + curr.size.z * 0.5f;

            float overlapMin = Mathf.Max(prevMin, curMin);
            float overlapMax = Mathf.Min(prevMax, curMax);
            float overlap = overlapMax - overlapMin;

            if (overlap <= 0f)
            {
                return new PlaceResult(false);
            }

            float newSizeAlong = overlap;
            float newCenterAlong = (overlapMin + overlapMax) * 0.5f;

            BlockData placed = curr;
            Vector3 chunkCenter = Vector3.zero;
            Vector3 chunkSize = Vector3.zero;
            bool hasChunk = false;

            if (curr.axis == Axis.X)
            {
                placed.center.x = newCenterAlong;
                placed.size.x = newSizeAlong;
                
                // Calculate chunk only if there's significant overhang
                float overhangSize = curr.size.x - placed.size.x;
                if (overhangSize > 0.01f)
                {
                    hasChunk = true;
                    chunkCenter = new Vector3(
                        curr.center.x > placed.center.x 
                            ? placed.center.x + placed.size.x * 0.5f + overhangSize * 0.5f
                            : placed.center.x - placed.size.x * 0.5f - overhangSize * 0.5f,
                        curr.center.y, 
                        curr.center.z);
                    chunkSize = new Vector3(overhangSize, curr.size.y, curr.size.z);
                }
            }
            else
            {
                placed.center.z = newCenterAlong;
                placed.size.z = newSizeAlong;
                
                // Calculate chunk only if there's significant overhang
                float overhangSize = curr.size.z - placed.size.z;
                if (overhangSize > 0.01f)
                {
                    hasChunk = true;
                    chunkCenter = new Vector3(
                        curr.center.x, 
                        curr.center.y,
                        curr.center.z > placed.center.z 
                            ? placed.center.z + placed.size.z * 0.5f + overhangSize * 0.5f
                            : placed.center.z - placed.size.z * 0.5f - overhangSize * 0.5f);
                    chunkSize = new Vector3(curr.size.x, curr.size.y, overhangSize);
                }
            }

            return new PlaceResult(true, placed, chunkCenter, chunkSize, hasChunk);
        }
    }
}
