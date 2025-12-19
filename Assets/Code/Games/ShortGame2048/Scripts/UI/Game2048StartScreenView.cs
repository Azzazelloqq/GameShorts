using Disposable;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Games
{
    internal class Game2048StartScreenView : MonoBehaviourDisposable
    {

        [SerializeField] 
        private Button _startButton;

        public Button StartButton => _startButton;

    }
}
