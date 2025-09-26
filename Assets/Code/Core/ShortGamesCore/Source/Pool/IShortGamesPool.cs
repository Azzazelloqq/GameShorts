using System;
using System.Collections.Generic;
using Code.Core.ShortGamesCore.Source.GameCore;

namespace Code.Core.ShortGamesCore.Source.Pool
{
public interface IShortGamesPool : IDisposable
{
	public bool TryGetShortGame<T>(out T game) where T : class, IPoolableShortGame;
	void ReleaseShortGame<T>(T game) where T : class, IPoolableShortGame;
	bool TryGetShortGame(Type gameType, out IPoolableShortGame game);
	void ReleaseShortGame(IPoolableShortGame game);
	void WarmUpPool<T>(T game) where T : class, IPoolableShortGame;
	void WarmUpPool(IPoolableShortGame game);
	IEnumerable<Type> GetPooledGameTypes();
	void ClearPoolForType<T>() where T : class, IPoolableShortGame;
	void ClearPoolForType(Type gameType);
}
}