using UnityEngine;

namespace GameShorts.FlyHumans
{
    /// <summary>
    /// Включает целевой игровой объект через заданное количество секунд
    /// </summary>
    public class DelayedObjectActivator : MonoBehaviour
    {
        [Header("Настройки активации")]
        [SerializeField] 
        [Tooltip("Объект, который будет активирован")]
        private GameObject targetObject;

        [SerializeField] 
        [Tooltip("Задержка в секундах перед активацией")]
        private float delayInSeconds = 3f;

        [SerializeField]
        [Tooltip("Активировать при старте игры")]
        private bool activateOnStart = true;

        private void Start()
        {
            if (activateOnStart)
            {
                ActivateAfterDelay();
            }
        }

        /// <summary>
        /// Запускает отложенную активацию объекта
        /// </summary>
        public void ActivateAfterDelay()
        {
            if (targetObject != null)
            {
                targetObject.SetActive(false);
                Invoke(nameof(ActivateObject), delayInSeconds);
            }
            else
            {
                Debug.LogWarning("Target object не назначен!", this);
            }
        }

        private void ActivateObject()
        {
            if (targetObject != null)
            {
                targetObject.SetActive(true);
                Debug.Log($"Объект {targetObject.name} активирован после задержки {delayInSeconds} сек.", targetObject);
            }
        }

        private void OnDisable()
        {
            // Отменяем вызов, если компонент отключается
            CancelInvoke(nameof(ActivateObject));
        }
    }
}

