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
    
    
    // Properties
    public float FollowSpeed => followSpeed;
    public bool SmoothFollow => smoothFollow;
    public float SmoothDamping => smoothDamping;
    public Vector3 Offset => offset;
    
    private void OnValidate()
    {
        // Ограничиваем значения в разумных пределах
        followSpeed = Mathf.Max(0.1f, followSpeed);
        smoothDamping = Mathf.Max(0.01f, smoothDamping);
    }
}
