using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Games
{
    internal class Game2048StartScreenView : BaseMonoBehaviour
    {

        [SerializeField] 
        private Button _startButton;

        public Button StartButton => _startButton;

    }
}
