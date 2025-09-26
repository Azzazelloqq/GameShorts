using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace Logic.UI
{
    internal class LaserChargeUiView : BaseMonoBehaviour
    {
        [SerializeField]
        private Slider _slider;
        
        [SerializeField]
        private Image _fillImage;

        public Slider Slider => _slider;
        public Image FillImage => _fillImage;

    }
}