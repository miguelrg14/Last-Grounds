using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Geometry of the play field. The original GameMaker room was 112x176 px,
    /// with enemies spawning in 7 lanes 16 px wide. World units are 16 px.
    /// </summary>
    public static class PlayField
    {
        public const float PixelsPerUnit = 16f;
        public const int RoomWidth = 112;
        public const int RoomHeight = 176;
        public const int LaneCount = 7;

        /// <summary>World Y where enemies appear (just above the top edge).</summary>
        public const float SpawnY = 6f;

        /// <summary>Anything beyond this |Y| is off screen and can be removed.</summary>
        public const float OutOfBoundsY = 7f;

        public static float HalfWidth { get { return RoomWidth / PixelsPerUnit / 2f; } }
        public static float HalfHeight { get { return RoomHeight / PixelsPerUnit / 2f; } }

        /// <summary>World X of the centre of a lane (0 = leftmost).</summary>
        public static float LaneX(int lane)
        {
            return (8f + 16f * lane - RoomWidth / 2f) / PixelsPerUnit;
        }

        public static int NearestLane(float worldX)
        {
            int lane = Mathf.RoundToInt(worldX * PixelsPerUnit / 16f + (LaneCount - 1) / 2f);
            return Mathf.Clamp(lane, 0, LaneCount - 1);
        }

        /// <summary>Converts GameMaker room coordinates (px, origin top-left, Y down) to world space.</summary>
        public static Vector2 FromRoom(float x, float y)
        {
            return new Vector2((x - RoomWidth / 2f) / PixelsPerUnit, (RoomHeight / 2f - y) / PixelsPerUnit);
        }
    }
}
