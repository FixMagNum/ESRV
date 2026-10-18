using Engine;

namespace Game.NetWork.Packages;

public class MovingBlockChangePackage : IPackage
{
	public int MovingBlockSetID;

	public Point3 Offset;

	public int NewValue;

	public byte ID => 38;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.Playing;

	public MovingBlockChangePackage()
	{
	}

	public MovingBlockChangePackage(int setId, Point3 offset, int newValue)
	{
		MovingBlockSetID = setId;
		Offset = offset;
		NewValue = newValue;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(MovingBlockSetID);
		writer.Write(Offset);
		writer.Write(NewValue);
	}

	public void ReadData(PackageStreamReader reader)
	{
		MovingBlockSetID = reader.ReadInt32();
		Offset = reader.ReadPoint3();
		NewValue = reader.ReadInt32();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		SubsystemFloatingBlockBehavior subsystemFloatingBlockBehavior = projectNet.FindSubsystem<SubsystemFloatingBlockBehavior>();
		SubsystemMovingBlocks subsystemMovingBlocks = projectNet.FindSubsystem<SubsystemMovingBlocks>();
		if (subsystemFloatingBlockBehavior == null || subsystemMovingBlocks == null)
		{
			return;
		}
		SubsystemMovingBlocks.MovingBlockSet movingBlockSet = null;
		foreach (IMovingBlockSet movingBlockSet3 in subsystemMovingBlocks.MovingBlockSets)
		{
			if (movingBlockSet3 is SubsystemMovingBlocks.MovingBlockSet movingBlockSet2 && movingBlockSet2.CollisionGroupId == MovingBlockSetID)
			{
				movingBlockSet = movingBlockSet2;
				break;
			}
		}
		if (movingBlockSet != null)
		{
			subsystemFloatingBlockBehavior.UpdateMovingBlockValue(movingBlockSet, Offset, NewValue);
			if (isServer)
			{
				MovingBlockChangePackage package = new MovingBlockChangePackage(MovingBlockSetID, Offset, NewValue);
				CommonLib.Net.QueuePackage(package);
			}
		}
	}
}
