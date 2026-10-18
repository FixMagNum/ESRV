using Engine;

namespace Game;

public class Pickable : WorldItem
{
	public int Count;

	public Vector3? FlyToPosition;

	public Matrix? StuckMatrix;

	public bool SplashGenerated = true;

	public bool PlayPickupSound;

	public ushort Id;

	public Vector3? LastPosition;

	public byte? GetPickPlayer;

	public Vector3 PostionFix;

	public bool PlaySound;

	public float distance;

	public bool Net_ToRemove;

	public Color DefaultColor;

	public Color Color;

	public double? LavaContactTime;

	public Vector3 Rotation;

	public double LastLookedAtTime;

	public bool IsSpecialPickable { get; set; }

	public bool IsDeletePickable { get; set; }

	public bool IsPersistent { get; set; } = true;

	public bool IsStuckProjectile { get; set; }
}
