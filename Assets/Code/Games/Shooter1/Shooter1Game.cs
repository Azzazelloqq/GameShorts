using UnityEngine;
using Code.Core.ShortGamesCore.Source.GameCore;
using System.Collections;
using System.Collections.Generic;

namespace Shooter1
{
    /// <summary>
    /// Простая игра-стрелялка: стреляем по летающему прямоугольнику
    /// </summary>
    public class Shooter1Game : MonoBehaviour, IShortGame
    {
        [Header("Game Settings")]
        [SerializeField] private float gameDuration = 30f;
        [SerializeField] private float targetSpeed = 3f;
        [SerializeField] private float bulletSpeed = 8f;
        [SerializeField] private float targetShrinkAmount = 0.2f;

        [Header("Prefab References")]
        [SerializeField] private GameObject targetPrefab;
        [SerializeField] private GameObject shooterPrefab;
        [SerializeField] private GameObject bulletPrefab;

        // Game objects
        private GameObject target;
        private GameObject shooter;
        private List<GameObject> bullets = new List<GameObject>();
        private GameUI gameUI;

        // Game state
        private bool isGameActive = false;
        private float gameTimer = 0f;
        private int targetDirection = 1;
        private Camera gameCamera;

        // Game boundaries
        private float leftBoundary = -8f;
        private float rightBoundary = 8f;

        public int Id { get; private set; } = System.Guid.NewGuid().GetHashCode();

        private void Awake()
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                gameCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        public void Dispose()
        {
            StopAllCoroutines();
            ClearGameObjects();
        }

        public void StartGame()
        {
            if (isGameActive) return;

            Debug.Log("Shooter1Game: Starting game");
            isGameActive = true;
            gameTimer = 0f;
            
            CreateGameObjects();
            StartCoroutine(GameLoop());
        }

        public void PauseGame()
        {
            Debug.Log("Shooter1Game: Pausing game");
            isGameActive = false;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Debug.Log("Shooter1Game: Resuming game");
            isGameActive = true;
            Time.timeScale = 1f;
        }

        public void RestartGame()
        {
            Debug.Log("Shooter1Game: Restarting game");
            StopAllCoroutines();
            ClearGameObjects();
            gameTimer = 0f;
            targetSpeed = 3f; // Сбрасываем скорость
            StartGame();
        }

        public void StopGame()
        {
            Debug.Log("Shooter1Game: Stopping game");
            isGameActive = false;
            StopAllCoroutines();
            ClearGameObjects();
            Time.timeScale = 1f; // Восстанавливаем нормальную скорость времени
        }

        private void CreateGameObjects()
        {
            // Создаем UI
            var uiObject = new GameObject("GameUI");
            uiObject.transform.SetParent(transform);
            gameUI = uiObject.AddComponent<GameUI>();

            // Создаем летающий прямоугольник (цель)
            if (targetPrefab == null)
            {
                target = CreateTarget();
            }
            else
            {
                target = Instantiate(targetPrefab, transform);
            }
            target.transform.position = new Vector3(0f, 2f, 0f);

            // Создаем стреляющий квадрат
            if (shooterPrefab == null)
            {
                shooter = CreateShooter();
            }
            else
            {
                shooter = Instantiate(shooterPrefab, transform);
            }
            shooter.transform.position = new Vector3(0f, -4f, 0f);
        }

        private GameObject CreateTarget()
        {
            var targetObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObj.name = "Target";
            targetObj.transform.SetParent(transform);
            targetObj.transform.localScale = new Vector3(2f, 0.5f, 1f);
            
            var renderer = targetObj.GetComponent<Renderer>();
            renderer.material.color = Color.blue;
            
            var collider = targetObj.GetComponent<Collider>();
            collider.isTrigger = true;
            
            return targetObj;
        }

        private GameObject CreateShooter()
        {
            var shooterObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shooterObj.name = "Shooter";
            shooterObj.transform.SetParent(transform);
            shooterObj.transform.localScale = new Vector3(1f, 1f, 1f);
            
            var renderer = shooterObj.GetComponent<Renderer>();
            renderer.material.color = Color.green;
            
            return shooterObj;
        }

        private GameObject CreateBullet(Vector3 position)
        {
            GameObject bulletObj;
            
            if (bulletPrefab == null)
            {
                bulletObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulletObj.name = "Bullet";
                bulletObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                
                var renderer = bulletObj.GetComponent<Renderer>();
                renderer.material.color = Color.red;
                
                var collider = bulletObj.GetComponent<Collider>();
                collider.isTrigger = true;
            }
            else
            {
                bulletObj = Instantiate(bulletPrefab);
            }
            
            bulletObj.transform.SetParent(transform);
            bulletObj.transform.position = position;
            
            // Добавляем компонент для движения пули
            var bulletScript = bulletObj.AddComponent<Bullet>();
            bulletScript.Initialize(bulletSpeed, this);
            
            return bulletObj;
        }

        private IEnumerator GameLoop()
        {
            while (isGameActive && gameTimer < gameDuration)
            {
                gameTimer += Time.deltaTime;
                
                // Обновляем UI
                if (gameUI != null)
                {
                    gameUI.UpdateTimer(gameDuration - gameTimer);
                }
                
                // Движение цели
                MoveTarget();
                
                // Проверка ввода
                HandleInput();
                
                // Обновление пуль
                UpdateBullets();
                
                yield return null;
            }
            
            // Игра закончилась
            if (gameTimer >= gameDuration)
            {
                Debug.Log("Shooter1Game: Game completed, restarting...");
                yield return new WaitForSeconds(1f);
                RestartGame();
            }
        }

        private void MoveTarget()
        {
            if (target == null) return;

            var currentPos = target.transform.position;
            currentPos.x += targetDirection * targetSpeed * Time.deltaTime;
            
            // Проверка границ и разворот
            if (currentPos.x >= rightBoundary || currentPos.x <= leftBoundary)
            {
                targetDirection *= -1;
                currentPos.x = Mathf.Clamp(currentPos.x, leftBoundary, rightBoundary);
            }
            
            target.transform.position = currentPos;
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
        }

        private void Shoot()
        {
            if (shooter == null) return;

            var bulletPosition = shooter.transform.position + Vector3.up * 0.5f;
            var bullet = CreateBullet(bulletPosition);
            bullets.Add(bullet);
        }

        private void UpdateBullets()
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                if (bullets[i] == null)
                {
                    bullets.RemoveAt(i);
                }
                else if (bullets[i].transform.position.y > 10f)
                {
                    // Пуля вылетела за границы экрана
                    DestroyBullet(bullets[i]);
                    bullets.RemoveAt(i);
                }
            }
        }

        public void OnBulletHitTarget(GameObject bullet)
        {
            if (target == null) return;

            Debug.Log("Shooter1Game: Target hit!");
            
            // Обновляем счет
            if (gameUI != null)
            {
                gameUI.AddScore(1);
            }
            
            // Уменьшаем размер цели
            var scale = target.transform.localScale;
            scale.x = Mathf.Max(0.2f, scale.x - targetShrinkAmount);
            target.transform.localScale = scale;
            
            // Увеличиваем скорость
            targetSpeed += 0.5f;
            
            // Удаляем пулю
            if (bullets.Contains(bullet))
            {
                bullets.Remove(bullet);
            }
            DestroyBullet(bullet);
        }

        private void DestroyBullet(GameObject bullet)
        {
            if (bullet != null)
            {
                Destroy(bullet);
            }
        }

        private void ClearGameObjects()
        {
            // Очищаем пули
            foreach (var bullet in bullets)
            {
                if (bullet != null)
                    Destroy(bullet);
            }
            bullets.Clear();

            // Очищаем игровые объекты
            if (target != null)
            {
                Destroy(target);
                target = null;
            }

            if (shooter != null)
            {
                Destroy(shooter);
                shooter = null;
            }

            // Сбрасываем UI
            if (gameUI != null)
            {
                gameUI.ResetScore();
            }
        }
    }
}
