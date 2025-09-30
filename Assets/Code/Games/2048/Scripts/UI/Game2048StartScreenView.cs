using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Games._2048.Scripts.UI
{
    internal class Game2048StartScreenView : BaseMonoBehaviour
    {
        internal struct Ctx
        {
            // Empty for now, will be filled when UI is implemented
        }

        [SerializeField] 
        private Button _startButton;

        private Ctx _ctx;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            
            // TODO: Setup UI bindings when UI is implemented
        }
    }
}
