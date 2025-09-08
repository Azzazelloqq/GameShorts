namespace Code.Core.ShotGamesCore
{
public interface IShortGame
{
	public int Id { get; }
	
	public void Start();
	public void Pause();
	public void Resume();
	public void Restart();
}
}