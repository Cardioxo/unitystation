namespace Health
{
	/// <summary>
	///     Body part damage severity.
	///     There are like seven/eight states in original tho!(see screen_gen sprites)
	/// </summary>

	public enum DamageSeverity
	{
		None = 0,
		Light = 20,
		LightModerate = 40,
		Moderate = 60,
		Bad = 80,
		Critical = 100,
		Max = 101
	}
}
