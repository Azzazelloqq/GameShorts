using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.BaseDMDisposable.Scripts
{
    public class BaseDisposable:  IDisposable
    {
        protected bool isDisposed;

        private List<IDisposable> _mainThreadDisposables;
        private List<Object> _unityObjects;
        protected bool _isFinished;

        protected BaseDisposable()
        {
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                if (_mainThreadDisposables != null)
                {
                    List<IDisposable> mainThreadDisposables = _mainThreadDisposables;
                    for (int i = mainThreadDisposables.Count - 1; i >= 0; i--)
                    {
                        mainThreadDisposables[i]?.Dispose();
                    }
                    mainThreadDisposables.Clear();
                }
                try
                {
                    OnDispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception when disposing {GetType().Name}: {e}");
                }
                if (_unityObjects != null)
                    for (int index = _unityObjects.Count - 1; index >= 0; index--)
                    {
                        Object obj = _unityObjects[index];
                        if (obj)
                            Object.Destroy(obj);
                    }
            }
        }

        protected virtual void OnDispose()
        {
        }

        protected TDisposable AddDispose<TDisposable>(TDisposable disposable) where TDisposable : IDisposable
        {
            if (disposable == null)
                return default;
            if (_mainThreadDisposables == null)
                _mainThreadDisposables = new List<IDisposable>(1);
            _mainThreadDisposables.Add(disposable);
            return disposable;
        }

        protected TObject AddComponent<TObject>(TObject obj) where TObject : Object
        {
            if (_unityObjects == null)
                _unityObjects = new List<Object>(1);
            _unityObjects.Add(obj);
            return obj;
        }
    }
}