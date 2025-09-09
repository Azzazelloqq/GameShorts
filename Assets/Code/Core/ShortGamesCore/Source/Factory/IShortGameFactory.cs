using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Source.Factory
{
public interface IShortGameFactory : IDisposable
{
	/// <summary>
	/// Creates a new game instance
	/// </summary>
	ValueTask<T> CreateShortGameAsync<T>(CancellationToken token) where T : Component, IShortGame;
	
	/// <summary>
	/// Creates a new game instance by type
	/// </summary>
	ValueTask<IShortGame> CreateShortGameAsync(Type gameType, CancellationToken token);
	
	/// <summary>
	/// Preloads game resources for fast instance creation
	/// </summary>
	ValueTask PreloadGameResourcesAsync<T>(CancellationToken token) where T : Component, IShortGame;
	
	/// <summary>
	/// Preloads game resources by type
	/// </summary>
	ValueTask PreloadGameResourcesAsync(Type gameType, CancellationToken token);
	
	/// <summary>
	/// Unloads preloaded game resources
	/// </summary>
	void UnloadGameResources<T>() where T : Component, IShortGame;
	
	/// <summary>
	/// Unloads preloaded game resources by type
	/// </summary>
	void UnloadGameResources(Type gameType);
}
}