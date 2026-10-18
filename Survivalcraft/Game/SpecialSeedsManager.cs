using System.Linq;

namespace Game;

public static class SpecialSeedsManager
{
	public struct SeedModifiers
	{
		public float RoughnessScale;

		public float CaveThickness;

		public float HeightAdd;

		public bool ForceCheater;
	}

	public static SeedModifiers GetModifiers(string seed)
	{
		SeedModifiers result = new SeedModifiers
		{
			RoughnessScale = 1f,
			CaveThickness = 1f,
			HeightAdd = 0f,
			ForceCheater = false
		};
		if (string.IsNullOrEmpty(seed))
		{
			return result;
		}
		string text = seed.ToLower().Trim();
		if (text == "cheater")
		{
			result.RoughnessScale = 50f;
		}
		else if (new string[4] { "cave", "caves", "bigcave", "bigcaves" }.Contains(text))
		{
			result.HeightAdd = 200f;
		}
		return result;
	}
}
