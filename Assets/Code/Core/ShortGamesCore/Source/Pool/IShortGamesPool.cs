using System;
using System.Collections.Generic;
using Code.Core.ShortGamesCore.Source.GameCore;

namespace Code.Core.ShortGamesCore.Source.Pool
{
public interface IShortGamesPool : IDisposable
{
	public bool TryGetShortGame<T>(out T game) where T : class, IShortGamePoolable;
	void ReleaseShortGame<T>(T game) where T : class, IShortGamePoolable;
	bool TryGetShortGame(Type gameType, out IShortGamePoolable game);
	void ReleaseShortGame(IShortGamePoolable game);
	void WarmUpPool<T>(T game) where T : class, IShortGamePoolable;
	void WarmUpPool(IShortGamePoolable game);
	IEnumerable<Type> GetPooledGameTypes();
	void ClearPoolForType<T>() where T : class, IShortGamePoolable;
	void ClearPoolForType(Type gameType);
}
}