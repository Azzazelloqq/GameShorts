
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.GameSwiper
{
    public class GameSwiperView : BaseMonoBehaviour
    {
        [SerializeField] private Button _nextGameButton;
        [SerializeField] private Button _previousGameButton;
        [SerializeField] private GameObject _loadingIndicator;
        public struct Ctx
        {
            public ReactiveTrigger OnNextGameRequested;
            public ReactiveTrigger OnPreviousGameRequested;
            public CompositeDisposable Disposables;
        }

        private Ctx _ctx;
        
        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            _nextGameButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _ctx.OnNextGameRequested.Notify();
                })
                .AddTo(_ctx.Disposables);
            
            _previousGameButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _ctx.OnPreviousGameRequested.Notify();
                })
                .AddTo(_ctx.Disposables);
        }
        
        public void SetLoadingState(bool isLoading)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(isLoading);
            }
            
            // Отключаем кнопки во время загрузки
            if (_nextGameButton != null)
            {
                _nextGameButton.interactable = !isLoading;
            }
            
            if (_previousGameButton != null)
            {
                _previousGameButton.interactable = !isLoading;
            }
        }
    }
}