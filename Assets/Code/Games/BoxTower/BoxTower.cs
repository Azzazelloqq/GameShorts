using System;
using System.Threading;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using Cysharp.Threading.Tasks;
using Disposable;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.ShortGamesCore.Game2
{
public class BoxTower : MonoBehaviourDisposable, IShortGame2D
{
    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private BoxTowerSceneContextView _sceneContext;

    [SerializeField]
    private GraphicRaycaster _graphicRaycaster;

    public bool IsPreloaded { get; private set; }

    private bool _isDisposed;
    private IDisposable _root;
    private RenderTexture _renderTexture;
    private BoxTowerCorePm _core;
    private CancellationTokenSource _cancellationTokenSource;

    public async UniTask PreloadGameAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_renderTexture == null)
        {
            _renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera);
        }

        if (IsPreloaded)
        {
            return;
        }

        // Warm up presenters/models/pools so StartGame becomes instant.
        EnsureCoreCreated();
        await _core.PreloadAsync(cancellationToken);
        IsPreloaded = true;
    }

    public RenderTexture GetRenderTexture()
    {
        return _renderTexture;
    }

    public void StartGame()
    {
        if (_isDisposed)
        {
            return;
        }

        EnsureCoreCreated();
        _core.EnsureInitialized();
        IsPreloaded = true;
        _core.ResetForNewSession();
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }

    public void Enable()
    {
        gameObject.SetActive(true);
    }

    public void ResumeGame()
    {
    }

    public void RestartGame()
    {
        if (_isDisposed)
        {
            return;
        }

        EnsureCoreCreated();
        _core.EnsureInitialized();
        IsPreloaded = true;
        _core.ResetForNewSession();
    }

    public void StopGame()
    {
        Disable();
        DisableInput();
    }

    public void EnableInput()
    {
        _graphicRaycaster.enabled = true;
    }

    public void DisableInput()
    {
        _graphicRaycaster.enabled = false;
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_isDisposed)
        {
            return;
        }

        DisposeCore();
        RenderTextureUtils.ReleaseAndDestroy(ref _renderTexture, _camera);
        IsPreloaded = false;

        _isDisposed = true;
        Destroy(gameObject);
    }

    private void DisposeCore()
    {
        if (_cancellationTokenSource != null)
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            _cancellationTokenSource.Dispose();

            _cancellationTokenSource = null;
        }

        _core?.Dispose();
        _core = null;
    }

    private void EnsureCoreCreated()
    {
        if (_core != null)
        {
            return;
        }

        if (_cancellationTokenSource == null)
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        var rootCtx = new BoxTowerCorePm.Ctx
        {
            sceneContextView = _sceneContext,
            cancellationToken = _cancellationTokenSource.Token,
            restartGame = RestartGame
        };
        _core = BoxTowerCorePmFactory.CreateBoxTowerCorePm(rootCtx);
    }
}
}