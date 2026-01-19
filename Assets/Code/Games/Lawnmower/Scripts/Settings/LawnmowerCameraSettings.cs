using UnityEngine;

[CreateAssetMenu(fileName = "LawnmowerCameraSettings", menuName = "Lawnmower/Settings/Create Camera Settings")]
internal class LawnmowerCameraSettings : ScriptableObject
{
    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 5f; // Скорость следования за игроком
    [SerializeField] private bool smoothFollow = true; // Плавное следование
    [SerializeField] private float smoothDamping = 0.3f; // Демпфирование для плавности
    
    [Header("Camera Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f); // Смещение относительно игрока
    [SerializeField] private bool dynamicOffset = false; // Динамическое смещение в зависимости от направления движения
    [SerializeField] private float dynamicOffsetStrength = 2f; // Сила динамического смещения
    
    [Header("Boundaries")]
    [SerializeField] private bool useLevelBounds = true; // Ограничивать камеру границами уровня
    [SerializeField] private float boundsPadding = 2f; // Отступ от границ уровня
    
    [Header("Zoom")]
    [SerializeField] private float defaultOrthographicSize = 8f; // Размер камеры по умолчанию
    [SerializeField] private bool adaptiveZoom = false; // Адаптивный зум в зависимости от размера уровня
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 15f;
    
    [Header("Look Ahead")]
    [SerializeField] private bool enableLookAhead = true; // Камера смотрит немного вперед по движению
    [SerializeField] private float lookAheadDistance = 3f; // Расстояние "взгляда вперед"
    [SerializeField] private float lookAheadSpeed = 2f; // Скорость адаптации взгляда вперед
    
    // Properties
    public float FollowSpeed => followSpeed;
    public bool SmoothFollow => smoothFollow;
    public float SmoothDamping => smoothDamping;
    public Vector3 Offset => offset;
    public bool DynamicOffset => dynamicOffset;
    public float DynamicOffsetStrength => dynamicOffsetStrength;
    public bool UseLevelBounds => useLevelBounds;
    public float BoundsPadding => boundsPadding;
    public float DefaultOrthographicSize => defaultOrthographicSize;
    public bool AdaptiveZoom => adaptiveZoom;
    public float MinZoom => minZoom;
    public float MaxZoom => maxZoom;
    public bool EnableLookAhead => enableLookAhead;
    public float LookAheadDistance => lookAheadDistance;
    public float LookAheadSpeed => lookAheadSpeed;
    
    private void OnValidate()
    {
        // Ограничиваем значения в разумных пределах
        followSpeed = Mathf.Max(0.1f, followSpeed);
        smoothDamping = Mathf.Max(0.01f, smoothDamping);
        boundsPadding = Mathf.Max(0f, boundsPadding);
        defaultOrthographicSize = Mathf.Max(1f, defaultOrthographicSize);
        minZoom = Mathf.Max(1f, minZoom);
        maxZoom = Mathf.Max(minZoom, maxZoom);
        lookAheadDistance = Mathf.Max(0f, lookAheadDistance);
        lookAheadSpeed = Mathf.Max(0.1f, lookAheadSpeed);
        dynamicOffsetStrength = Mathf.Max(0f, dynamicOffsetStrength);
    }
}
