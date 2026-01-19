namespace Code.Games.AngryHumans
{
/// <summary>
/// Интерфейс для объектов, у которых можно активировать физику
/// </summary>
internal interface IPhysicsActivatable
{
	/// <summary>
	/// Активирована ли уже физика
	/// </summary>
	bool IsPhysicsActivated { get; }

	/// <summary>
	/// Активировать физику объекта
	/// </summary>
	void ActivatePhysics();
}
}