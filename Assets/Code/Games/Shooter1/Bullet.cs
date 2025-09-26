using UnityEngine;

namespace Shooter1
{
    /// <summary>
    /// Компонент пули для игры Shooter1
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        private float speed;
        private Shooter1Game gameController;

        public void Initialize(float bulletSpeed, Shooter1Game controller)
        {
            speed = bulletSpeed;
            gameController = controller;
        }

        private void Update()
        {
            // Движение пули вверх
            transform.position += Vector3.up * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Проверяем попадание по цели
            if (other.name == "Target")
            {
                gameController?.OnBulletHitTarget(gameObject);
            }
        }
    }
}


