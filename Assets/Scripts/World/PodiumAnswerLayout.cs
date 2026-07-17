using UnityEngine;

namespace ArucoQuiz
{
    /// <summary>
    /// Maps answer column index (0=A … 3=D) to camera viewport, matching Playing HUD feed insets.
    /// </summary>
    public static class PodiumAnswerLayout
    {
        public const float DesignWidth = 1920f;
        public const float DesignHeight = 1080f;
        public const float FeedInsetLeft = 195f;
        public const float FeedInsetRight = 195f;
        public const float PodiumBarHeight = 370f;
        public const float ViewportWidthFill = 0.96f;
        public const float CanvasPlaneDistance = 2.05f;
        public const float CubeInsetFromCanvas = 0.45f;

        public static float CubePlaneDistance => CanvasPlaneDistance - CubeInsetFromCanvas;

        public static float ResolveCubeDistance(Camera cam)
        {
            var follow = Object.FindObjectOfType<WorldCanvasFollowCamera>();
            if (follow != null)
                return follow.PlaneDistance - CubeInsetFromCanvas;

            return CubePlaneDistance;
        }

        public static Vector2 ViewportPosition(int columnIndex)
        {
            var innerW = DesignWidth - FeedInsetLeft - FeedInsetRight;
            var centerX = FeedInsetLeft + innerW * (columnIndex + 0.5f) / 4f;
            var centerYFromBottom = PodiumBarHeight * 0.52f;

            // Canvas-local coordinates (center-origin, matching the canvas RectTransform's pivot).
            var canvasLocalX = centerX - DesignWidth * 0.5f;
            var canvasLocalY = centerYFromBottom - DesignHeight * 0.5f;

            var follow = Object.FindObjectOfType<WorldCanvasFollowCamera>();
            if (follow != null && follow.WorldWidth > 0f && follow.WorldHeight > 0f)
            {
                var vxLive = 0.5f + canvasLocalX * follow.AppliedScale / follow.WorldWidth;
                var vyLive = 0.5f + canvasLocalY * follow.AppliedScale / follow.WorldHeight;
                return new Vector2(vxLive, vyLive);
            }

            // Fallback before the canvas has scaled itself at least once (e.g. very first frame).
            var margin = (1f - ViewportWidthFill) * 0.5f;
            var vx = margin + ViewportWidthFill * (centerX / DesignWidth);
            var vy = centerYFromBottom / DesignHeight;
            return new Vector2(vx, vy);
        }
    }
}
