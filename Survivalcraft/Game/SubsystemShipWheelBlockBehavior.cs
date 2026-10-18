using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemShipWheelBlockBehavior : SubsystemBlockBehavior
{
	private SubsystemFloatingBlockBehavior m_subsystemFloatingBlockBehavior;

	private SubsystemAudio m_subsystemAudio;

	private static Random m_random = new Random();

	public override int[] HandledBlocks => new int[1] { 435 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemFloatingBlockBehavior = Project.FindSubsystem<SubsystemFloatingBlockBehavior>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
	}

	public override bool OnInteract(MovingBlocksRaycastResult raycastResult, Point3 offset, ComponentMiner componentMiner)
	{
		if (raycastResult.MovingBlockSet is SubsystemMovingBlocks.MovingBlockSet attacker)
		{
			int value = 0;
			bool flag = false;
			for (int i = 0; i < attacker.Blocks.Count; i++)
			{
				if (attacker.Blocks[i].Offset == offset)
				{
					value = attacker.Blocks[i].Value;
					flag = true;
					break;
				}
			}
			if (flag)
			{
				Matrix m = Matrix.Invert(Matrix.CreateFromQuaternion(attacker.Rotation) * Matrix.CreateTranslation(attacker.Position));
				Vector3 position = Vector3.Transform(raycastResult.Ray.Position, m);
				Vector3 v = Vector3.TransformNormal(raycastResult.Ray.Direction, m);
				Ray3 ray = new Ray3(position, Vector3.Normalize(v));
				Vector3 vector = new Vector3((float)offset.X + 0.5f, (float)offset.Y + 0.5f, (float)offset.Z + 0.5f);
				int data = Terrain.ExtractData(value);
				int mountFace = ShipWheelBlock.GetMountFace(data);
				float? num = ray.Intersection(new BoundingBox(new Vector3(offset), new Vector3(offset + new Point3(1, 1, 1))));
				if (!num.HasValue)
				{
					return false;
				}
				Vector3 v2 = ray.Position + ray.Direction * num.Value - vector;
				Vector3 v3 = CellFace.FaceToVector3(mountFace);
				Vector3 v4 = ((!(MathUtils.Abs(v3.Y) > 0.9f)) ? Vector3.Cross(Vector3.UnitY, v3) : Vector3.UnitX);
				float num2 = Vector3.Dot(v2, v4);
				ShipWheelBlock.WheelMode mode = ShipWheelBlock.GetMode(data);
				ShipWheelBlock.WheelMode wheelMode = mode;
				bool flag2 = num2 > 0f;
				bool flag3 = num2 < 0f;
				if (flag2)
				{
					switch (mode)
					{
					case ShipWheelBlock.WheelMode.Left:
						wheelMode = ShipWheelBlock.WheelMode.Rest;
						break;
					case ShipWheelBlock.WheelMode.Rest:
						wheelMode = ShipWheelBlock.WheelMode.Right;
						break;
					}
				}
				else if (flag3)
				{
					switch (mode)
					{
					case ShipWheelBlock.WheelMode.Right:
						wheelMode = ShipWheelBlock.WheelMode.Rest;
						break;
					case ShipWheelBlock.WheelMode.Rest:
						wheelMode = ShipWheelBlock.WheelMode.Left;
						break;
					}
				}
				if (MathUtils.Abs(num2) < 0.1f)
				{
					wheelMode = ShipWheelBlock.WheelMode.Rest;
				}
				if (wheelMode == mode)
				{
					return true;
				}
				string text = "";
				switch (wheelMode)
				{
				case ShipWheelBlock.WheelMode.Left:
					text = "Turning Port (Left)";
					break;
				case ShipWheelBlock.WheelMode.Right:
					text = "Turning Starboard (Right)";
					break;
				case ShipWheelBlock.WheelMode.Rest:
					text = "Wheel at Rest";
					break;
				}
				componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.White, blinking: true, playNotificationSound: true);
				int data2 = ShipWheelBlock.SetMode(data, wheelMode);
				int newValue = Terrain.ReplaceData(value, data2);
				m_subsystemFloatingBlockBehavior.ModifyMovingBlock(attacker, offset, newValue);
				m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, m_random.Float(-0.1f, 0.1f), raycastResult.HitPoint(), 2f, autoDelay: true);
				componentMiner.Poke(forceRestart: false);
				return true;
			}
		}
		return false;
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		componentMiner.Poke(forceRestart: false);
		return true;
	}
}
