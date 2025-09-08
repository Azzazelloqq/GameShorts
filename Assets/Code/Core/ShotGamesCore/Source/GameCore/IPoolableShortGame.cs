namespace Code.Core.ShotGamesCore
{
public interface IPoolableShortGame : IShortGame
{
	public void OnPooled();
	public void OnUnpooled();
}
}