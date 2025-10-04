using DG.Tweening;
using UnityEngine;

namespace GameShorts.FlyHumans.View
{
    /// <summary>
    /// View компонент для камеры - только визуализация, без логики
    /// </summary>
    public class CameraView : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Transform _cameraTransform;
        
        [Header("Camera Settings")]
        [SerializeField] private Vector3 _cameraFollowOffset = new Vector3(0f, 3f, -5f);
        [SerializeField] private Vector3 _cameraTargetRotation = new Vector3(15f, 0f, 0f);
        [SerializeField] private float _cameraZoomOutDuration = 0.5f;
        [SerializeField] private float _cameraRotationDuration = 0.7f;
        [SerializeField] private float _cameraZoomOutDistance = 3f;
        [SerializeField] private Ease _cameraZoomEase = Ease.OutQuad;
        [SerializeField] private Ease _cameraRotationEase = Ease.InOutQuad;
        
        // Properties (только данные)
        public Transform CameraTransform => _cameraTransform;
        public Vector3 CameraFollowOffset => _cameraFollowOffset;
        public Vector3 CameraTargetRotation => _cameraTargetRotation;
        public float CameraZoomOutDuration => _cameraZoomOutDuration;
        public float CameraRotationDuration => _cameraRotationDuration;
        public float CameraZoomOutDistance => _cameraZoomOutDistance;
        public Ease CameraZoomEase => _cameraZoomEase;
        public Ease CameraRotationEase => _cameraRotationEase;
    }
}

