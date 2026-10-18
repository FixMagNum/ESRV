using Engine;

namespace Game;

public struct VentilationStatus
{
	public bool IsVentilated;

	public Point3? BreachCoordinate;

	public int Volume;

	public bool IsProcessing;
}
