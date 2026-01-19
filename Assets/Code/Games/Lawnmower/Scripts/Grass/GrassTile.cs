using System.Collections;
using UnityEngine;

internal class GrassTile : MonoBehaviour
{
    [Header("Grass Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Material grassMaterial;
    [SerializeField] private float shrinkDuration = 1f;
    [SerializeField] private float restoreDuration = 2f;
    
    [Header("State")]
    [SerializeField] private bool isGrassVisible = true;
    
    private Coroutine currentAnimation;
    private static readonly int ShrinkProperty = Shader.PropertyToID("_Shrink");
    
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void Start()
    {
        // Получаем материал после того, как GrassGrid установил уникальный материал
        if (grassMaterial == null && spriteRenderer != null)
            grassMaterial = spriteRenderer.material;
            
        // Убеждаемся, что трава изначально видна
        if (grassMaterial != null)
        {
            grassMaterial.SetFloat(ShrinkProperty, 0f);
            Debug.Log($"GrassTile {gameObject.name}: Set initial Shrink = 0");
        }
        else
        {
            Debug.LogWarning($"GrassTile {gameObject.name}: grassMaterial is null!");
        }
    }
    
    /// <summary>
    /// Убирает траву (анимация сжатия)
    /// </summary>
    public void CutGrass()
    {
        if (!isGrassVisible) return;
        
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
            
        currentAnimation = StartCoroutine(ShrinkGrassAnimation());
    }
    
    /// <summary>
    /// Восстанавливает траву (анимация роста)
    /// </summary>
    public void RestoreGrass()
    {
        if (isGrassVisible) return;
        
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
            
        currentAnimation = StartCoroutine(RestoreGrassAnimation());
    }
    
    /// <summary>
    /// Переключает состояние травы
    /// </summary>
    public void ToggleGrass()
    {
        if (isGrassVisible)
            CutGrass();
        else
            RestoreGrass();
    }
    
    private IEnumerator ShrinkGrassAnimation()
    {
        isGrassVisible = false;
        float startTime = Time.time;
        
        while (Time.time - startTime < shrinkDuration)
        {
            float progress = (Time.time - startTime) / shrinkDuration;
            float shrinkValue = Mathf.SmoothStep(0f, 1f, progress);
            
            if (grassMaterial != null)
                grassMaterial.SetFloat(ShrinkProperty, shrinkValue);
                
            yield return null;
        }
        
        if (grassMaterial != null)
            grassMaterial.SetFloat(ShrinkProperty, 1f);
            
        currentAnimation = null;
    }
    
    private IEnumerator RestoreGrassAnimation()
    {
        isGrassVisible = true;
        float startTime = Time.time;
        
        while (Time.time - startTime < restoreDuration)
        {
            float progress = (Time.time - startTime) / restoreDuration;
            float shrinkValue = Mathf.SmoothStep(1f, 0f, progress);
            
            if (grassMaterial != null)
                grassMaterial.SetFloat(ShrinkProperty, shrinkValue);
                
            yield return null;
        }
        
        if (grassMaterial != null)
            grassMaterial.SetFloat(ShrinkProperty, 0f);
            
        currentAnimation = null;
    }
    
    /// <summary>
    /// Получить текущее состояние травы
    /// </summary>
    public bool IsGrassVisible => isGrassVisible;
    
    /// <summary>
    /// Мгновенно установить состояние травы без анимации
    /// </summary>
    public void SetGrassState(bool visible)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        isGrassVisible = visible;
        if (grassMaterial != null)
            grassMaterial.SetFloat(ShrinkProperty, visible ? 0f : 1f);
    }
    
    /// <summary>
    /// Принудительно обновить ссылку на материал и установить начальное состояние
    /// </summary>
    public void RefreshMaterial()
    {
        if (spriteRenderer != null)
            grassMaterial = spriteRenderer.material;
            
        if (grassMaterial != null)
        {
            grassMaterial.SetFloat(ShrinkProperty, isGrassVisible ? 0f : 1f);
            Debug.Log($"GrassTile {gameObject.name}: Material refreshed, Shrink = {(isGrassVisible ? 0f : 1f)}");
        }
    }
}
