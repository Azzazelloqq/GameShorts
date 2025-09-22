using System.Collections;
using UnityEngine;

namespace Code.Games.Lawnmower.Scripts
{
    public class GrassDotsController : MonoBehaviour
    {
        [SerializeField] private string shrinkProp = "_Shrink";
        [SerializeField] private string quadPxProp = "_QuadPx";
        [SerializeField] private Vector2 quadSizePx = new Vector2(10, 10); // подгоняешь под свой рендер
        [SerializeField] private float collapseDuration = 1.0f;
        [SerializeField] private float seed = 1f; // можно варьировать, если хочешь разные узоры

        private SpriteRenderer sr;
        private MaterialPropertyBlock mpb;
        private bool collapsed = false;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            mpb = new MaterialPropertyBlock();
            sr.GetPropertyBlock(mpb);
            mpb.SetVector(quadPxProp, new Vector4(quadSizePx.x, quadSizePx.y, 0, 0));
            mpb.SetFloat("_Seed", seed);
            mpb.SetFloat(shrinkProp, 0f);
            sr.SetPropertyBlock(mpb);
        }

        void OnMouseDown()
        {
            // для 2D подходит при наличии Collider2D
            StopAllCoroutines();
            StartCoroutine(AnimateShrink(collapsed ? 1f : 0f, collapsed ? 0f : 1f, collapseDuration));
            collapsed = !collapsed;
        }

        IEnumerator AnimateShrink(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float v = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, k));
                sr.GetPropertyBlock(mpb);
                mpb.SetFloat(shrinkProp, v);
                sr.SetPropertyBlock(mpb);
                yield return null;
            }
        }
    }
}