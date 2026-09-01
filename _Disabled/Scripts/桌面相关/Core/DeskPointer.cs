using UnityEngine;

namespace DesktopPet
{
    /// <summary>桌面鼠标射线：屏幕坐标 → 桌面平面 (Z=0) 世界 XY。</summary>
    public static class DeskPointer
    {
        private const float DeskPlaneZ = 0f;

        private static Camera _cachedCamera;

        public static Vector2 WorldOnDeskPlane()
        {
            Camera cam = _cachedCamera;
            if (cam == null || !cam.isActiveAndEnabled)
                _cachedCamera = cam = Camera.main;
            return WorldOnDeskPlane(cam);
        }

        private static Vector2 WorldOnDeskPlane(Camera cam)
        {
            if (cam == null)
                return Vector2.zero;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            float dz = ray.direction.z;
            if (Mathf.Abs(dz) < 1e-6f)
                return new Vector2(ray.origin.x, ray.origin.y);

            float t = (DeskPlaneZ - ray.origin.z) / dz;
            if (t < 0f)
                return new Vector2(ray.origin.x, ray.origin.y);

            Vector3 worldPos = ray.origin + ray.direction * t;
            return new Vector2(worldPos.x, worldPos.y);
        }
    }
}
