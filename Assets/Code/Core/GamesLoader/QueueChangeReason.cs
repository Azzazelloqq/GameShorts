namespace Code.Core.GamesLoader
{
	/// <summary>
	/// Provides more diagnostics when the queue updates its state,
	/// allowing observers to understand what triggered the change.
	/// </summary>
	public enum QueueChangeReason
	{
		Initialized,
		Moved,
		Inserted,
		Removed,
		Replaced,
		Reset,
		Cleared
	}
}
