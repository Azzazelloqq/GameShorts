using System;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Level
{
    /// <summary>
    /// Вспомогательный компонент для обработки триггеров зоны опустошения
    /// </summary>
    internal class EmptyingZoneTriggerHandler : MonoBehaviour
    {
        public event Action<Collider2D> OnTriggerEntered;
        public event Action<Collider2D> OnTriggerExited;

        private void OnTriggerEnter2D(Collider2D other)
        {
            OnTriggerEntered?.Invoke(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            OnTriggerExited?.Invoke(other);
        }
    }
}
