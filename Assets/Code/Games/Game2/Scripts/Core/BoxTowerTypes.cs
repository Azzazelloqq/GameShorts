using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
    public enum Axis { X, Z }

    public struct BlockData
    {
        public Vector3 center;
        public Vector3 size;
        public Axis axis;

        public BlockData(Vector3 center, Vector3 size, Axis axis)
        {
            this.center = center;
            this.size = size;
            this.axis = axis;
        }
    }

    public struct PlaceResult
    {
        public bool success;
        public BlockData placedBlock;
        public Vector3 chunkCenter;
        public Vector3 chunkSize;
        public bool hasChunk;

        public PlaceResult(bool success, BlockData placedBlock = default, Vector3 chunkCenter = default, Vector3 chunkSize = default, bool hasChunk = false)
        {
            this.success = success;
            this.placedBlock = placedBlock;
            this.chunkCenter = chunkCenter;
            this.chunkSize = chunkSize;
            this.hasChunk = hasChunk;
        }
    }

    public enum GameState
    {
        Ready,
        Running,
        GameOver
    }
}
