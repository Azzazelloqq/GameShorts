using UnityEngine;
using Code.Core.BaseDMDisposable.Scripts;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Level
{
    /// <summary>
    /// View компонент для зоны опустошения контейнера
    /// </summary>
    internal class EmptyingZoneView : BaseMonoBehaviour
    {
        internal struct Ctx
        {
            public string zoneName;
            public Color normalColor;
            public Color activeColor;
        }

        [Header("Zone Settings")]
        [SerializeField] private string zoneName = "Emptying Zone";
        [SerializeField] private Collider2D zoneCollider;
        
        [Header("Visual")]
        [SerializeField] private SpriteRenderer zoneRenderer;
        [SerializeField] private Color normalColor = Color.yellow;
        [SerializeField] private Color activeColor = Color.green;

        private Ctx _ctx;
        private bool _isPlayerInside = false;

        // Properties
        public string ZoneName => _ctx.zoneName;
        public bool IsPlayerInside => _isPlayerInside;
        public Collider2D ZoneCollider => zoneCollider;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            
            // Настраиваем коллайдер как триггер
            if (zoneCollider == null)
                zoneCollider = GetComponent<Collider2D>();
                
            if (zoneCollider != null)
                zoneCollider.isTrigger = true;
                
            // Устанавливаем начальный цвет
            if (zoneRenderer != null)
                zoneRenderer.color = _ctx.normalColor;
        }

        public void SetPlayerInside(bool isInside)
        {
            _isPlayerInside = isInside;
            
            // Меняем визуал
            if (zoneRenderer != null)
            {
                zoneRenderer.color = isInside ? _ctx.activeColor : _ctx.normalColor;
            }
        }

        public bool IsPositionInZone(Vector3 worldPosition)
        {
            if (zoneCollider == null) return false;
            return zoneCollider.bounds.Contains(worldPosition);
        }

        public Vector3 GetZoneCenter()
        {
            if (zoneCollider != null)
                return zoneCollider.bounds.center;
            return transform.position;
        }

        public Vector3 GetZoneSize()
        {
            if (zoneCollider != null)
                return zoneCollider.bounds.size;
            return Vector3.one;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Отрисовываем границы зоны
            Color gizmoColor = _isPlayerInside ? _ctx.activeColor : _ctx.normalColor;
            if (gizmoColor == default) gizmoColor = _isPlayerInside ? activeColor : normalColor;
            
            Gizmos.color = gizmoColor;
            
            if (zoneCollider != null)
            {
                Gizmos.DrawWireCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
        }
        
        private void OnDrawGizmos()
        {
            // Отрисовываем полупрозрачную область
            Color gizmoColor = _ctx.normalColor != default ? _ctx.normalColor : normalColor;
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            
            if (zoneCollider != null)
            {
                Gizmos.DrawCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
            }
            else
            {
                Gizmos.DrawCube(transform.position, transform.localScale);
            }
        }
#endif
    }
}
