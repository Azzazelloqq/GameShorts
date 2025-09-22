using UnityEngine;

namespace Code.Games.Lawnmower.Scripts
{
    public class GrassDotsSizeSync : MonoBehaviour
    {
        [SerializeField] private string quadPxProp = "_QuadPx";
        [SerializeField] private Camera cam; // можно не задавать — возьмём main
        private SpriteRenderer sr;
        private MaterialPropertyBlock mpb;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (!cam) cam = Camera.main;
            mpb = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            if (!cam) return;

            // Меряем экранный размер рендера в пикселях
            var b = sr.bounds;
            Vector3 c = b.center;
            float wWorld = b.size.x;
            float hWorld = b.size.y;

            Vector3 pC = cam.WorldToScreenPoint(c);
            Vector3 pW = cam.WorldToScreenPoint(c + new Vector3(wWorld, 0f, 0f));
            Vector3 pH = cam.WorldToScreenPoint(c + new Vector3(0f, hWorld, 0f));

            float wPx = Mathf.Abs(pW.x - pC.x);
            float hPx = Mathf.Abs(pH.y - pC.y);

            // ширина/высота всего спрайта (умножаем на 2, т.к. мерили от центра до края)
            wPx *= 2f;
            hPx *= 2f;

            sr.GetPropertyBlock(mpb);
            mpb.SetVector(quadPxProp, new Vector4(Mathf.Max(1f, wPx), Mathf.Max(1f, hPx), 0, 0));
            sr.SetPropertyBlock(mpb);
        }
    }
}