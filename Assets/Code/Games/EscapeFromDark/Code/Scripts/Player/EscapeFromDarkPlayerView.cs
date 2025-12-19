using Disposable;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Player
{
    internal class EscapeFromDarkPlayerView : MonoBehaviourDisposable
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform characterTransform;
        [SerializeField] private Animator animator;
        
        [Header("Audio")]
        [SerializeField] private AudioSource movementAudioSource;
        [SerializeField] private AudioClip footstepSound;
        
        // Movement state
        private Vector2 _lastDirection;
        private bool _isMoving;

        internal struct Ctx
        {
            // Контекст для инициализации, если потребуется
        }

        public void SetCtx(Ctx ctx)
        {
            // Инициализация компонентов
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            if (characterTransform == null)
                characterTransform = transform;
                
            if (animator == null)
                animator = GetComponent<Animator>();
                
            if (movementAudioSource == null)
                movementAudioSource = GetComponent<AudioSource>();
            
            Debug.Log("EscapeFromDarkPlayerView: Context set");
        }

        public void UpdatePosition(Vector3 newPosition)
        {
            transform.position = newPosition;
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
            // Поворачиваем персонажа
            if (characterTransform != null)
            {
                characterTransform.rotation = Quaternion.Euler(0, 0, rotationAngle);
            }
        }

        private void UpdateVisuals()
        {
            // Обновляем анимацию
            if (animator != null)
            {
                animator.SetBool("IsMoving", _isMoving);
                animator.SetFloat("MoveSpeed", _lastDirection.magnitude);
            }
            
            // Флип спрайта по направлению движения
            if (_isMoving && spriteRenderer != null && _lastDirection.x != 0)
            {
                spriteRenderer.flipX = _lastDirection.x < 0;
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

        public void PlayFootstepSound()
        {
            if (movementAudioSource != null && footstepSound != null)
            {
                movementAudioSource.PlayOneShot(footstepSound);
            }
        }

        public void PlaySound(AudioClip clip)
        {
            if (movementAudioSource != null && clip != null)
            {
                movementAudioSource.PlayOneShot(clip);
            }
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

    }
}
