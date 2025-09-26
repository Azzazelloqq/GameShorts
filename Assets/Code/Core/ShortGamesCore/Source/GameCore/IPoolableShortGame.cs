namespace Code.Core.ShortGamesCore.Source.GameCore
{
public interface IPoolableShortGame : IShortGame
{
	public void OnPooled();
	public void OnUnpooled();
}
}