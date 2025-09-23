using UnityEngine;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Player
{
    public class PlayerView : MonoBehaviour
    {
    [Header("Grass Cutting")]
    [SerializeField] private float cuttingRadius = 1f;
    [SerializeField] private Transform cuttingCenter;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    
    [Header("Audio")]
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private AudioClip grassCuttingSound;
    
    // Properties
    public float CuttingRadius => cuttingRadius;
    public Transform CuttingCenter => cuttingCenter != null ? cuttingCenter : transform;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public Animator Animator => animator;
    
    // Movement state
    private Vector2 _lastDirection;
    private bool _isMoving;
        
        private void Awake()
        {
            // Находим компоненты, если не назначены
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (animator == null)
                animator = GetComponent<Animator>();
            
            if (movementAudioSource == null)
                movementAudioSource = GetComponent<AudioSource>();
            
            if (cuttingCenter == null)
                cuttingCenter = transform;
        }
        
        public void UpdatePosition(Vector2 position)
        {
            transform.position = position;
        }
        
        public void UpdateMovementVisuals(Vector2 direction, bool isMoving)
        {
            _isMoving = isMoving;
            if (isMoving)
            {
                _lastDirection = direction;
            }
            
            UpdateVisuals();
            UpdateAudio();
        }
        
        public void UpdateRotation(float rotationAngle)
        {
            // Поворачиваем весь объект
            transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
        }
        
        private void UpdateVisuals()
        {
            // Поворот теперь управляется через UpdateRotation(), 
            // поэтому флип спрайта не нужен
            
            // Обновляем анимацию
            if (animator != null)
            {
                animator.SetBool("IsMoving", _isMoving);
                animator.SetFloat("MoveSpeed", _lastDirection.magnitude);
            }
        }
        
        private void UpdateAudio()
        {
            if (movementAudioSource == null) return;
            
            if (_isMoving && !movementAudioSource.isPlaying)
            {
                movementAudioSource.Play();
            }
            else if (!_isMoving && movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop();
            }
        }
        
        public void PlayGrassCuttingSound()
        {
            if (movementAudioSource != null && grassCuttingSound != null)
            {
                movementAudioSource.PlayOneShot(grassCuttingSound);
            }
        }
        
        public Vector3 GetCuttingPosition()
        {
            return CuttingCenter.position;
        }
        
        public bool IsMoving()
        {
            return _isMoving;
        }
        
        public Vector2 GetLastDirection()
        {
            return _lastDirection;
        }
        
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
        
        public void Stop()
        {
            UpdateMovementVisuals(Vector2.zero, false);
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Отрисовываем радиус стрижки травы
            Gizmos.color = Color.green;
            Vector3 cuttingPos = CuttingCenter.position;
            Gizmos.DrawWireSphere(cuttingPos, cuttingRadius);
            
            // Отрисовываем направление движения
            if (_isMoving)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)_lastDirection.normalized * 2f);
            }
        }
#endif
    }
}
