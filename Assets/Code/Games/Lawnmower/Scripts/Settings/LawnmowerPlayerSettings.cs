using UnityEngine;

[CreateAssetMenu(fileName = "LawnmowerPlayerSettings", menuName = "Lawnmower/Settings/Create Player Settings")]
public class LawnmowerPlayerSettings : ScriptableObject
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float acceleration = 25f; // Быстрое ускорение для человека
    [SerializeField] private float deceleration = 15f; // Быстрое торможение
    [SerializeField] private bool useAcceleration = false; // Для человека лучше мгновенная реакция
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360f; // 1 оборот в секунду - достаточно быстро
    [SerializeField] private bool instantRotation = false; // Плавный поворот по умолчанию
    [SerializeField] private float rotationSmoothness = 8f; // Оптимальная плавность
    
    [Header("Grass Cutting")]
    [SerializeField] private float cuttingRadius = 1f;
    [SerializeField] private float cuttingInterval = 0.1f; // секунды между стрижками
    
    // Properties
    public float MaxSpeed => maxSpeed;
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    public bool UseAcceleration => useAcceleration;
    public float RotationSpeed => rotationSpeed;
    public bool InstantRotation => instantRotation;
    public float CuttingRadius => cuttingRadius;
    public float CuttingInterval => cuttingInterval;
    
    private void OnValidate()
    {
        // Ограничиваем значения в разумных пределах
        maxSpeed = Mathf.Max(0.1f, maxSpeed);
        acceleration = Mathf.Max(0.1f, acceleration);
        deceleration = Mathf.Max(0.1f, deceleration);
        rotationSpeed = Mathf.Max(1f, rotationSpeed);
        rotationSmoothness = Mathf.Max(0.1f, rotationSmoothness);
        cuttingRadius = Mathf.Max(0.1f, cuttingRadius);
        cuttingInterval = Mathf.Max(0.01f, cuttingInterval);
    }
}