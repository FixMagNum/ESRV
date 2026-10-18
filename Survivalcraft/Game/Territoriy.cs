using System;
using Engine;

namespace Game;

public class Territoriy
{
	public Point3 OwnChunkCoord;

	public Point2 Origin;

	public Guid OwnerGuid;

	public bool AllowPlace;

	public bool AllowDig;

	public bool AllowBlockBehavior;

	public bool IsVisible;

	public bool ApplyToFirend;

	public BoundingBox BoundBox;

	public BoundingBox ViewBoundBox;

	public bool BlockFire;

	public bool BlockPvp;

	public void CalculateBox(int territorySizeInBlocks)
	{
		BoundBox = new BoundingBox(new Vector3(Origin.X - territorySizeInBlocks, OwnChunkCoord.Y, Origin.Y - territorySizeInBlocks), new Vector3(Origin.X + territorySizeInBlocks, 256f, Origin.Y + territorySizeInBlocks));
		ViewBoundBox = new BoundingBox(new Vector3(Origin.X - territorySizeInBlocks, OwnChunkCoord.Y, Origin.Y - territorySizeInBlocks), new Vector3(Origin.X + territorySizeInBlocks, 256f, Origin.Y + territorySizeInBlocks));
	}
}
