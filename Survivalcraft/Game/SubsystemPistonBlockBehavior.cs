using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemPistonBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	public class QueuedAction
	{
		public int StoppedFrame;

		public bool Stop;

		public int? Move;
	}

	public SubsystemTime m_subsystemTime;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemMovingBlocks m_subsystemMovingBlocks;

	public bool m_allowPistonHeadRemove;

	public Dictionary<Point3, QueuedAction> m_actions;

	public List<KeyValuePair<Point3, QueuedAction>> m_tmpActions;

	public DynamicArray<MovingBlock> m_movingBlocks;

	public const string IdString = "Piston";

	public const int PistonMaxMovedBlocks = 8;

	public const int PistonMaxExtension = 8;

	public const int PistonMaxSpeedSetting = 3;

	public SubsystemBodies m_subsystemBodies;

	public DynamicArray<ComponentBody> m_componentBodies = new DynamicArray<ComponentBody>();

	public UpdateOrder UpdateOrder => m_subsystemMovingBlocks.UpdateOrder + 1;

	public override int[] HandledBlocks => new int[0];

	public void AdjustPiston(Point3 position, int length)
	{
		if (!m_actions.TryGetValue(position, out var zero))
		{
			zero = new QueuedAction();
			m_actions[position] = zero;
		}
		zero.Move = length;
	}

	public void Update(float dt)
	{
		if (m_subsystemTime.PeriodicGameTimeEvent(0.125, 0.0))
		{
			ProcessQueuedActions();
		}
		UpdateMovableBlocks();
		PushBodies(dt);
	}

	public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
	{
		int value = inventory.GetSlotValue(slotIndex);
		inventory.GetSlotCount(slotIndex);
		int data = Terrain.ExtractData(value);
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditPistonDialog(data, (int newData) =>
		{
			if (Terrain.ReplaceData(value, newData) != value)
			{
				if (CommonLib.WorkType == WorkType.Client)
				{
					CommonLib.SendEditItemRequest(EditableItemType.Piston, isEditAsItem: true, slotIndex, default, newData);
				}
				else
				{
					CommonLib.PerformEditItem(Project, EditableItemType.Piston, (slotIndex, inventory), newData);
				}
			}
		}));
		return true;
	}

	public override bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer)
	{
		int contents = Terrain.ExtractContents(value);
		int data = Terrain.ExtractData(value);
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditPistonDialog(data, (int newData) =>
		{
			if (newData != data && SubsystemTerrain.Terrain.GetCellContents(x, y, z) == contents)
			{
				if (CommonLib.WorkType == WorkType.Client)
				{
					CommonLib.SendEditItemRequest(EditableItemType.Piston, isEditAsItem: false, 0, new CellFace(x, y, z, 0), newData);
				}
				else
				{
					CommonLib.PerformEditItem(Project, EditableItemType.Piston, new CellFace(x, y, z, 0), newData);
				}
			}
		}));
		return true;
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		int num = Terrain.ExtractContents(value);
		int data = Terrain.ExtractData(value);
		switch (num)
		{
		case 238:
			if (!m_allowPistonHeadRemove)
			{
				int face2 = PistonHeadBlock.GetFace(data);
				Point3 point2 = CellFace.FaceToPoint3(face2);
				int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(x + point2.X, y + point2.Y, z + point2.Z);
				int cellValue3 = m_subsystemTerrain.Terrain.GetCellValue(x - point2.X, y - point2.Y, z - point2.Z);
				int num3 = Terrain.ExtractContents(cellValue2);
				int num4 = Terrain.ExtractContents(cellValue3);
				int data3 = Terrain.ExtractData(cellValue2);
				int data4 = Terrain.ExtractData(cellValue3);
				if (num3 == 238 && PistonHeadBlock.GetFace(data3) == face2)
				{
					m_subsystemTerrain.DestroyCell(0, x + point2.X, y + point2.Y, z + point2.Z, 0, noDrop: false, noParticleSystem: false);
				}
				if (num4 == 237 && PistonBlock.GetFace(data4) == face2)
				{
					m_subsystemTerrain.DestroyCell(0, x - point2.X, y - point2.Y, z - point2.Z, 0, noDrop: false, noParticleSystem: false);
				}
				else if (num4 == 238 && PistonHeadBlock.GetFace(data4) == face2)
				{
					m_subsystemTerrain.DestroyCell(0, x - point2.X, y - point2.Y, z - point2.Z, 0, noDrop: false, noParticleSystem: false);
				}
			}
			break;
		case 237:
		{
			StopPiston(new Point3(x, y, z));
			int face = PistonBlock.GetFace(data);
			Point3 point = CellFace.FaceToPoint3(face);
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
			int num2 = Terrain.ExtractContents(cellValue);
			int data2 = Terrain.ExtractData(cellValue);
			if (num2 == 238 && PistonHeadBlock.GetFace(data2) == face)
			{
				m_subsystemTerrain.DestroyCell(0, x + point.X, y + point.Y, z + point.Z, 0, noDrop: false, noParticleSystem: false);
			}
			break;
		}
		}
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		BoundingBox boundingBox = new BoundingBox(chunk.BoundingBox.Min - new Vector3(16f), chunk.BoundingBox.Max + new Vector3(16f));
		DynamicArray<IMovingBlockSet> dynamicArray = new DynamicArray<IMovingBlockSet>();
		m_subsystemMovingBlocks.FindMovingBlocks(boundingBox, extendToFillCells: false, dynamicArray);
		foreach (IMovingBlockSet item in dynamicArray)
		{
			if (item.Id == "Piston")
			{
				StopPiston((Point3)item.Tag);
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemMovingBlocks.Stopped += MovingBlocksStopped;
		m_subsystemMovingBlocks.CollidedWithTerrain += MovingBlocksCollidedWithTerrain;
	}

	public void ProcessQueuedActions()
	{
		m_tmpActions.Clear();
		m_tmpActions.AddRange(m_actions);
		foreach (KeyValuePair<Point3, QueuedAction> tmpAction in m_tmpActions)
		{
			Point3 key = tmpAction.Key;
			QueuedAction value = tmpAction.Value;
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z)) != 237)
			{
				StopPiston(key);
				value.Move = null;
				value.Stop = false;
			}
			else if (value.Stop)
			{
				StopPiston(key);
				value.Stop = false;
				value.StoppedFrame = Time.FrameIndex;
			}
		}
		foreach (KeyValuePair<Point3, QueuedAction> tmpAction2 in m_tmpActions)
		{
			Point3 key2 = tmpAction2.Key;
			QueuedAction value2 = tmpAction2.Value;
			if (!value2.Move.HasValue || value2.Stop || Time.FrameIndex == value2.StoppedFrame || m_subsystemMovingBlocks.FindMovingBlocks("Piston", key2) != null)
			{
				continue;
			}
			bool flag = true;
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(key2.X + i * 16, key2.Z + j * 16);
					if (chunkAtCell == null || chunkAtCell.State <= TerrainChunkState.InvalidContents4)
					{
						flag = false;
					}
				}
			}
			if (flag && MovePiston(key2, value2.Move.Value))
			{
				value2.Move = null;
			}
		}
		foreach (KeyValuePair<Point3, QueuedAction> tmpAction3 in m_tmpActions)
		{
			Point3 key3 = tmpAction3.Key;
			QueuedAction value3 = tmpAction3.Value;
			if (!value3.Move.HasValue && !value3.Stop)
			{
				m_actions.Remove(key3);
			}
		}
	}

	public void UpdateMovableBlocks()
	{
		foreach (IMovingBlockSet movingBlockSet in m_subsystemMovingBlocks.MovingBlockSets)
		{
			if (!(movingBlockSet.Id == "Piston"))
			{
				continue;
			}
			Point3 point = (Point3)movingBlockSet.Tag;
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
			if (Terrain.ExtractContents(cellValue) != 237)
			{
				continue;
			}
			int data = Terrain.ExtractData(cellValue);
			PistonMode mode = PistonBlock.GetMode(data);
			int face = PistonBlock.GetFace(data);
			Point3 point2 = CellFace.FaceToPoint3(face);
			int num = int.MaxValue;
			foreach (MovingBlock block in movingBlockSet.Blocks)
			{
				num = MathUtils.Min(num, block.Offset.X * point2.X + block.Offset.Y * point2.Y + block.Offset.Z * point2.Z);
			}
			float num2 = movingBlockSet.Position.X * (float)point2.X + movingBlockSet.Position.Y * (float)point2.Y + movingBlockSet.Position.Z * (float)point2.Z;
			float num3 = point.X * point2.X + point.Y * point2.Y + point.Z * point2.Z;
			if (num2 > num3)
			{
				if ((float)num + num2 - num3 > 1f)
				{
					movingBlockSet.SetBlock(point2 * (num - 1), Terrain.MakeBlockValue(238, 0, PistonHeadBlock.SetFace(PistonHeadBlock.SetIsShaft(PistonHeadBlock.SetMode(0, mode), isShaft: true), face)));
				}
			}
			else if (num2 < num3 && (float)num + num2 - num3 <= 0f)
			{
				movingBlockSet.SetBlock(point2 * num, 0);
			}
		}
	}

	public static void GetSpeedAndSmoothness(int pistonSpeed, out float speed, out Vector2 smoothness)
	{
		switch (pistonSpeed)
		{
		case 0:
			speed = 15f;
			smoothness = new Vector2(0.2f, 0.2f);
			break;
		case 1:
			speed = 5f;
			smoothness = new Vector2(0f, 0.5f);
			break;
		case 2:
			speed = 4.5f;
			smoothness = new Vector2(0.6f, 0.6f);
			break;
		case 3:
			speed = 4f;
			smoothness = new Vector2(0.9f, 0.9f);
			break;
		case 4:
			speed = 3.5f;
			smoothness = new Vector2(1.2f, 1.2f);
			break;
		default:
			speed = 5f;
			smoothness = new Vector2(0f, 0.5f);
			break;
		}
	}

	public bool MovePiston(Point3 position, int length)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			return MovePistonNet(position, length);
		}
		return true;
	}

	public bool MovePistonNet(Point3 position, int length)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		int data = Terrain.ExtractData(terrain.GetCellValue(position.X, position.Y, position.Z));
		int face = PistonBlock.GetFace(data);
		PistonMode mode = PistonBlock.GetMode(data);
		int maxExtension = PistonBlock.GetMaxExtension(data);
		int pullCount = PistonBlock.GetPullCount(data);
		int speed = PistonBlock.GetSpeed(data);
		Point3 point = CellFace.FaceToPoint3(face);
		length = MathUtils.Clamp(length, 0, maxExtension + 1);
		int num = 0;
		m_movingBlocks.Clear();
		Point3 offset = point;
		while (m_movingBlocks.Count < 8)
		{
			int cellValue = terrain.GetCellValue(position.X + offset.X, position.Y + offset.Y, position.Z + offset.Z);
			int num2 = Terrain.ExtractContents(cellValue);
			int face2 = PistonHeadBlock.GetFace(Terrain.ExtractData(cellValue));
			if (num2 != 238 || face2 != face)
			{
				break;
			}
			DynamicArray<MovingBlock> movingBlocks = m_movingBlocks;
			MovingBlock item = new MovingBlock
			{
				Offset = offset,
				Value = cellValue
			};
			movingBlocks.Add(item);
			offset += point;
			num++;
		}
		if (length > num)
		{
			DynamicArray<MovingBlock> movingBlocks2 = m_movingBlocks;
			MovingBlock item2 = new MovingBlock
			{
				Offset = Point3.Zero,
				Value = Terrain.MakeBlockValue(238, 0, PistonHeadBlock.SetFace(PistonHeadBlock.SetMode(PistonHeadBlock.SetIsShaft(0, num > 0), mode), face))
			};
			movingBlocks2.Add(item2);
			int num3 = 0;
			while (num3 < 8)
			{
				int cellValue2 = terrain.GetCellValue(position.X + offset.X, position.Y + offset.Y, position.Z + offset.Z);
				if (!IsBlockMovable(cellValue2, face, position.Y + offset.Y, out var isEnd))
				{
					break;
				}
				DynamicArray<MovingBlock> movingBlocks3 = m_movingBlocks;
				item2 = new MovingBlock
				{
					Offset = offset,
					Value = cellValue2
				};
				movingBlocks3.Add(item2);
				num3++;
				offset += point;
				if (isEnd)
				{
					break;
				}
			}
			int x = position.X + offset.X;
			int y = position.Y + offset.Y;
			int z = position.Z + offset.Z;
			int cellValue3 = terrain.GetCellValue(x, y, z);
			int num4 = Terrain.ExtractContents(cellValue3);
			if ((num4 == 18 || num4 == 92) && length > num)
			{
				if (!DisplaceFluidVertically(x, y, z, num4))
				{
					return false;
				}
			}
			else if (IsBlockBlocking(cellValue3))
			{
				return false;
			}
			if (!IsBlockBlocking(terrain.GetCellValue(position.X + offset.X, position.Y + offset.Y, position.Z + offset.Z)))
			{
				GetSpeedAndSmoothness(speed, out var speed2, out var smoothness);
				Point3 p = position + (length - num) * point;
				if (m_subsystemMovingBlocks.AddMovingBlockSet(new Vector3(position) + 0.01f * new Vector3(point), new Vector3(p), speed2, 0f, 0f, smoothness, m_movingBlocks, "Piston", position, testCollision: true) != null)
				{
					if (num4 == 18 || num4 == 92)
					{
						m_subsystemTerrain.ChangeCell(x, y, z, 0);
					}
					m_allowPistonHeadRemove = true;
					try
					{
						foreach (MovingBlock movingBlock in m_movingBlocks)
						{
							if (movingBlock.Offset != Point3.Zero)
							{
								m_subsystemTerrain.ChangeCell(position.X + movingBlock.Offset.X, position.Y + movingBlock.Offset.Y, position.Z + movingBlock.Offset.Z, 0);
							}
						}
					}
					finally
					{
						m_allowPistonHeadRemove = false;
					}
					m_subsystemTerrain.ChangeCell(position.X, position.Y, position.Z, Terrain.MakeBlockValue(237, 0, PistonBlock.SetIsExtended(data, isExtended: true)));
					m_subsystemAudio.PlaySound("Audio/Piston", 1f, 0f, new Vector3(position), 2f, autoDelay: true);
				}
			}
			return false;
		}
		if (length < num)
		{
			if (mode != PistonMode.Pushing)
			{
				int num5 = 0;
				for (int i = 0; i < pullCount + 1; i++)
				{
					int cellValue4 = terrain.GetCellValue(position.X + offset.X, position.Y + offset.Y, position.Z + offset.Z);
					if (!IsBlockMovable(cellValue4, face, position.Y + offset.Y, out var isEnd2))
					{
						break;
					}
					DynamicArray<MovingBlock> movingBlocks4 = m_movingBlocks;
					MovingBlock item3 = new MovingBlock
					{
						Offset = offset,
						Value = cellValue4
					};
					movingBlocks4.Add(item3);
					offset += point;
					num5++;
					if (isEnd2)
					{
						break;
					}
				}
				if (mode == PistonMode.StrictPulling && num5 < pullCount + 1)
				{
					return false;
				}
			}
			int x2 = position.X + offset.X;
			int y2 = position.Y + offset.Y;
			int z2 = position.Z + offset.Z;
			int num6 = Terrain.ExtractContents(terrain.GetCellValue(x2, y2, z2));
			GetSpeedAndSmoothness(speed, out var speed3, out var smoothness2);
			float num7 = ((length == 0) ? 0.01f : 0f);
			Vector3 targetPosition = new Vector3(position) + (length - num) * new Vector3(point) + num7 * new Vector3(point);
			if (m_subsystemMovingBlocks.AddMovingBlockSet(new Vector3(position), targetPosition, speed3, 0f, 0f, smoothness2, m_movingBlocks, "Piston", position, testCollision: true) != null)
			{
				if (num6 == 18 || num6 == 92)
				{
					m_subsystemTerrain.ChangeCell(x2, y2, z2, 0);
				}
				m_allowPistonHeadRemove = true;
				try
				{
					foreach (MovingBlock movingBlock2 in m_movingBlocks)
					{
						m_subsystemTerrain.ChangeCell(position.X + movingBlock2.Offset.X, position.Y + movingBlock2.Offset.Y, position.Z + movingBlock2.Offset.Z, 0);
					}
				}
				finally
				{
					m_allowPistonHeadRemove = false;
				}
				m_subsystemAudio.PlaySound("Audio/Piston", 1f, 0f, new Vector3(position), 2f, autoDelay: true);
			}
			return false;
		}
		return true;
	}

	public void StopPiston(Point3 position)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			StopPistonNet(position);
		}
	}

	public void StopPistonNet(Point3 position)
	{
		IMovingBlockSet movingBlockSet = m_subsystemMovingBlocks.FindMovingBlocks("Piston", position);
		if (movingBlockSet == null)
		{
			return;
		}
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(position.X, position.Y, position.Z);
		int num = Terrain.ExtractContents(cellValue);
		int data = Terrain.ExtractData(cellValue);
		bool flag = num == 237;
		bool isExtended = false;
		m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet);
		foreach (MovingBlock block in movingBlockSet.Blocks)
		{
			int x = Terrain.ToCell(MathUtils.Round(movingBlockSet.Position.X)) + block.Offset.X;
			int y = Terrain.ToCell(MathUtils.Round(movingBlockSet.Position.Y)) + block.Offset.Y;
			int z = Terrain.ToCell(MathUtils.Round(movingBlockSet.Position.Z)) + block.Offset.Z;
			if (new Point3(x, y, z) == position)
			{
				continue;
			}
			int num2 = Terrain.ExtractContents(block.Value);
			if (flag || num2 != 238)
			{
				m_subsystemTerrain.ChangeCell(x, y, z, block.Value);
				if (num2 == 238)
				{
					isExtended = true;
				}
			}
		}
		if (flag)
		{
			m_subsystemTerrain.ChangeCell(position.X, position.Y, position.Z, Terrain.MakeBlockValue(237, 0, PistonBlock.SetIsExtended(data, isExtended)));
		}
	}

	public void MovingBlocksCollidedWithTerrain(IMovingBlockSet movingBlockSet, Point3 p)
	{
		if (!(movingBlockSet.Id == "Piston"))
		{
			return;
		}
		Point3 data = (Point3)movingBlockSet.Tag;
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(data.X, data.Y, data.Z);
		if (Terrain.ExtractContents(cellValue) != 237)
		{
			return;
		}
		Point3 point = CellFace.FaceToPoint3(PistonBlock.GetFace(Terrain.ExtractData(cellValue)));
		int num = p.X * point.X + p.Y * point.Y + p.Z * point.Z;
		int num2 = data.X * point.X + data.Y * point.Y + data.Z * point.Z;
		if (num <= num2)
		{
			return;
		}
		int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z);
		int num3 = Terrain.ExtractContents(cellValue2);
		if (num3 == 18 || num3 == 92)
		{
			bool flag = true;
			if (point.Y > 0 && FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValue2)) > 0.01f && Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(p.X, p.Y + 1, p.Z)) == num3)
			{
				flag = false;
			}
			if (flag && !DisplaceFluidVertically(p.X, p.Y, p.Z, num3))
			{
				movingBlockSet.Stop();
				return;
			}
		}
		else if (IsBlockBlocking(cellValue2))
		{
			movingBlockSet.Stop();
			return;
		}
		SubsystemTerrain.DestroyCell(0, p.X, p.Y, p.Z, 0, noDrop: false, noParticleSystem: false);
	}

	public void MovingBlocksStopped(IMovingBlockSet movingBlockSet)
	{
		if (!(movingBlockSet.Id == "Piston") || !(movingBlockSet.Tag is Point3))
		{
			return;
		}
		Point3 key = (Point3)movingBlockSet.Tag;
		if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z)) == 237)
		{
			if (!m_actions.TryGetValue(key, out var value))
			{
				value = new QueuedAction();
				m_actions.Add(key, value);
			}
			value.Stop = true;
		}
	}

	public static bool IsBlockMovable(int value, int pistonFace, int y, out bool isEnd)
	{
		isEnd = false;
		int num = Terrain.ExtractContents(value);
		int num2 = Terrain.ExtractData(value);
		if (num == 18 || num == 92)
		{
			return true;
		}
		if (num != 64 && num != 65 && num != 303 && num != 304 && num != 45 && num != 27)
		{
			switch (num)
			{
			case 216:
				break;
			case 1:
				if (num2 != 1)
				{
					return y > 1;
				}
				return false;
			case 126:
				return false;
			case 127:
				return false;
			case 131:
			case 132:
				return false;
			case 133:
			case 134:
			case 135:
			case 136:
			case 137:
			case 138:
			case 139:
			case 140:
			case 141:
			case 142:
			case 143:
			case 144:
			case 145:
			case 146:
			case 147:
			case 148:
			case 149:
			case 150:
			case 151:
			case 152:
			case 153:
			case 154:
			case 155:
			case 156:
			case 157:
			case 158:
			case 159:
			case 160:
			case 161:
			case 162:
			case 163:
			case 164:
			case 165:
			case 166:
			case 167:
			case 168:
			case 169:
			case 170:
			case 171:
			case 172:
			case 173:
			case 174:
			case 175:
			case 176:
			case 177:
			case 178:
			case 179:
			case 180:
			case 181:
			case 182:
			case 183:
			case 184:
			case 185:
			case 186:
			case 187:
			case 188:
			case 189:
			case 190:
			case 191:
			case 192:
			case 193:
			case 194:
			case 195:
			case 196:
			case 197:
			case 198:
			case 199:
			case 200:
			case 201:
			case 202:
			case 203:
			case 204:
			case 205:
			case 206:
			case 207:
			case 208:
			case 209:
			case 210:
			case 211:
			case 212:
			case 213:
			case 214:
			case 215:
			case 217:
			case 218:
			case 219:
			case 220:
			case 221:
			case 222:
			case 223:
			case 224:
			case 225:
			case 226:
			case 227:
				if (num == 216)
				{
					goto case 27;
				}
				if (num == 227)
				{
					return true;
				}
				goto default;
			case 237:
				return !PistonBlock.GetIsExtended(num2);
			case 238:
				return false;
			case 244:
				return false;
			case 27:
			case 28:
			case 29:
			case 30:
			case 31:
			case 32:
			case 33:
			case 34:
			case 35:
			case 36:
			case 37:
			case 38:
			case 39:
			case 40:
			case 41:
			case 42:
			case 43:
			case 44:
			case 45:
			case 46:
			case 47:
			case 48:
			case 49:
			case 50:
			case 51:
			case 52:
			case 53:
			case 54:
			case 55:
			case 56:
			case 57:
			case 58:
			case 59:
			case 60:
			case 61:
			case 62:
			case 63:
			case 64:
			case 65:
				return false;
			default:
			{
				Block block = BlocksManager.Blocks[num];
				if (block is MountedElectricElementBlock)
				{
					isEnd = true;
					return ((MountedElectricElementBlock)block).GetFace(value) == pistonFace;
				}
				if (block is DoorBlock || block is TrapdoorBlock)
				{
					return false;
				}
				if (block is LadderBlock)
				{
					isEnd = true;
					return pistonFace == LadderBlock.GetFace(num2);
				}
				if (block is AttachedSignBlock)
				{
					isEnd = true;
					return pistonFace == AttachedSignBlock.GetFace(num2);
				}
				if (!block.IsNonDuplicable)
				{
					return block.IsCollidable;
				}
				return false;
			}
			}
		}
		return true;
	}

	public static bool IsBlockBlocking(int value)
	{
		int num = Terrain.ExtractContents(value);
		return BlocksManager.Blocks[num].IsCollidable;
	}

	public SubsystemPistonBlockBehavior()
	{
		m_actions = new Dictionary<Point3, QueuedAction>();
		m_tmpActions = new List<KeyValuePair<Point3, QueuedAction>>();
		m_movingBlocks = new DynamicArray<MovingBlock>();
	}

	public void PushBodies(float dt)
	{
		foreach (IMovingBlockSet num in m_subsystemMovingBlocks.MovingBlockSets)
		{
			if (!(num.Id == "Piston") || !(num.CurrentVelocity.LengthSquared() > 0.0001f))
			{
				continue;
			}
			m_componentBodies.Clear();
			m_subsystemBodies.FindBodiesAroundPoint(new Vector2(num.Position.X, num.Position.Z), 4f, m_componentBodies);
			if (m_componentBodies.Count == 0)
			{
				continue;
			}
			for (int j = 0; j < m_componentBodies.Count; j++)
			{
				ComponentBody componentBody = m_componentBodies.Array[j];
				if (componentBody.ParentBody != null)
				{
					continue;
				}
				BoundingBox boundingBox = componentBody.BoundingBox;
				boundingBox.Min -= new Vector3(0.02f);
				boundingBox.Max += new Vector3(0.02f);
				bool flag = false;
				for (int k = 0; k < num.Blocks.Count; k++)
				{
					MovingBlock movingBlock = num.Blocks[k];
					Vector3 vector = num.Position + new Vector3(movingBlock.Offset);
					BoundingBox box = new BoundingBox(vector, vector + Vector3.One);
					if (boundingBox.Intersection(box))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					continue;
				}
				Vector3 vector2 = num.CurrentVelocity * dt;
				componentBody.Position += vector2;
				if (Math.Abs(num.CurrentVelocity.Y) > 0.1f)
				{
					if (num.CurrentVelocity.Y > 0f)
					{
						componentBody.m_velocity.Y = MathUtils.Max(componentBody.m_velocity.Y, num.CurrentVelocity.Y);
						componentBody.StandingOnVelocity = num.CurrentVelocity;
						componentBody.StandingOnValue = 237;
					}
					else
					{
						componentBody.m_velocity.Y = MathUtils.Min(componentBody.m_velocity.Y, num.CurrentVelocity.Y);
					}
				}
			}
		}
	}

	private bool DisplaceFluidVertically(int x, int y, int z, int fluidBlockIndex)
	{
		Terrain typeInfo = m_subsystemTerrain.Terrain;
		int typeInfo2 = 0;
		int i;
		for (i = y; i < 256 && Terrain.ExtractContents(typeInfo.GetCellValue(x, i, z)) == fluidBlockIndex; i++)
		{
			typeInfo2++;
		}
		if (typeInfo2 > 4)
		{
			return false;
		}
		if (i >= 256)
		{
			return false;
		}
		int block = Terrain.ExtractContents(typeInfo.GetCellValue(x, i, z));
		if (block != 0)
		{
			if (BlocksManager.Blocks[block].IsFluidBlocker)
			{
				return false;
			}
			m_subsystemTerrain.DestroyCell(0, x, i, z, 0, noDrop: true, noParticleSystem: false);
		}
		for (int num = i - 1; num >= y; num--)
		{
			int cellValue = typeInfo.GetCellValue(x, num, z);
			m_subsystemTerrain.ChangeCell(x, num + 1, z, cellValue);
		}
		Vector3 position = new Vector3((float)x + 0.5f, (float)i + 0.5f, (float)z + 0.5f);
		switch (fluidBlockIndex)
		{
		case 18:
			m_subsystemAudio.PlayRandomSound("Audio/Splashes", 1f, 0f, position, 4f, autoDelay: true);
			break;
		case 92:
			m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, position, 4f, autoDelay: true);
			break;
		}
		return true;
	}
}
