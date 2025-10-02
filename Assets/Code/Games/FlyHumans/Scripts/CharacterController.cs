using System;
using R3;
using R3.Triggers;
using UnityEngine;

namespace GameShorts.AngryFlier
{
    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private RagdollRoot _ragdollRoot;
        [Header("Components")]
        [SerializeField] private Animator animator;
        
        [Header("Movement Settings")]
        [SerializeField] private float forwardSpeed = 5f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float gravity = 9.8f;
        
        private float verticalVelocity;
        private bool wasFlying;
        private bool isActive;
        private float _currentGravity;
        private float _currentSpeed;
        private IDisposable _jumpTrigger;

        private void Start()
        {
            _currentGravity = 0;
            _currentSpeed = 0;
            
            foreach (var obs in animator.GetBehaviours<ObservableStateMachineTrigger>())
            {
                _jumpTrigger = obs.OnStateEnterAsObservable()
                    .Subscribe(_ =>
                    {
                        InitGravity();
                        _jumpTrigger?.Dispose();
                    });
            }
            _ragdollRoot.CollisionEnter = OnCollisionEnterRagdoll;
        }

        public void InitGravity()
        {
            _currentGravity = gravity;
            _currentSpeed = forwardSpeed;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive) return;
            
            HandleInput();
            ApplyGravity();
            UpdatePosition();
            UpdateAnimation();
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
        }
        
        private void Jump()
        {
            verticalVelocity = jumpForce;
        }
        
        private void ApplyGravity()
        {
            verticalVelocity -= _currentGravity * Time.deltaTime;
        }
        
        private void UpdatePosition()
        {
            // Движение вперед
            transform.position += transform.forward * _currentSpeed * Time.deltaTime;
            
            // Движение вверх/вниз
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
        }
        
        private void UpdateAnimation()
        {
            bool isFlying = verticalVelocity > 0f;
            
            // Обновляем параметр Flying только когда состояние изменилось
            if (isFlying != wasFlying)
            {
                animator.SetBool("Flying", isFlying);
                wasFlying = isFlying;
            }
        }
        
        private void OnCollisionEnterRagdoll()
        {
            // Останавливаем персонажа и выключаем анимации
            isActive = false;
            animator.enabled = false;
        }
    }
}

