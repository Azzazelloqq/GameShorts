using UnityEngine;

namespace Code.Games
{
    [ExecuteAlways]
    internal class FitGradientQuadToCamera : MonoBehaviour
    {
        public Camera targetCamera;        // если пусто — возьмём камеру у родителя
        public float planeDistance = 2f;   // должно быть > Near

        void LateUpdate()
        {
            if (targetCamera == null)
                targetCamera = GetComponentInParent<Camera>();
            if (!targetCamera) return;

            // прямо перед камерой
            transform.localPosition = new Vector3(0,0, planeDistance);
            transform.localRotation = Quaternion.identity;

            // ширина/высота фрустума на этой дистанции
            float h = 2f * Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * planeDistance;
            float w = h * targetCamera.aspect;

            // Quad = 1×1, подгоняем под размер
            transform.localScale = new Vector3(w, h, 1f);
        }
    }
}