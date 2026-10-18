using System.Collections.Generic;

namespace Game;

public class DoubleCompare : IComparer<double>
{
	public int Compare(double x, double y)
	{
		double restParams = y - x;
		if (!(restParams > 0.0))
		{
			if (restParams != 0.0)
			{
				return -1;
			}
			return 0;
		}
		return 1;
	}
}
