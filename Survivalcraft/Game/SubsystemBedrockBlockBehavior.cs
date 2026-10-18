using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemBedrockBlockBehavior : SubsystemBlockBehavior, IDrawable
{
	public static int TerritoriySize;

	public PrimitivesRenderer3D primitives = new PrimitivesRenderer3D();

	public static string fName;

	public static Dictionary<Guid, List<Territoriy>> Territories;

	private SubsystemGameInfo m_subsystemGameInfo;

	public override int[] HandledBlocks => new int[1] { 1 };

	public int[] DrawOrders => new int[1] { 2 };

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		if ((Terrain.ExtractData(raycastResult.Value) & 1) == 1)
		{
			if (componentMiner.ComponentPlayer.PlayerData.IsMainPlayer && CommonLib.WorkType != WorkType.Client && componentMiner.ActiveBlockValue != 0)
			{
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(componentMiner.ActiveBlockValue)];
				int num = Terrain.ExtractData(componentMiner.ActiveBlockValue);
				string text = (ClipboardManager.ClipboardString = $"{block.CraftingId}:{num}");
				string text3 = LanguageControl.Get("SubsystemBedrockBlockBehavior", "insufficient_permissions") + text;
				componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text3, Color.White, blinking: false, playNotificationSound: true);
				return true;
			}
			if (CheckIsInTerritoriy(raycastResult.CellFace.X, raycastResult.CellFace.Z, out var territoriy))
			{
				if (territoriy.OwnerGuid == componentMiner.ComponentPlayer.PlayerData.PlayerGUID || componentMiner.ComponentPlayer.PlayerData.ServerManager)
				{
					CanvasWidget canvasWidget = new CanvasWidget
					{
						Size = new Vector2(360f, 380f),
						HorizontalAlignment = WidgetAlignment.Near
					};
					RectangleWidget widget = new RectangleWidget
					{
						FillColor = Color.Black,
						OutlineColor = Color.White,
						OutlineThickness = 2f
					};
					StackPanelWidget stackPanelWidget = new StackPanelWidget
					{
						Margin = new Vector2(10f, 0f),
						HorizontalAlignment = WidgetAlignment.Near,
						VerticalAlignment = WidgetAlignment.Center,
						Direction = LayoutDirection.Vertical
					};
					string text4 = "Unknown";
					PlayerData playerData = componentMiner.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true)?.FindPlayerData((PlayerData p) => p.PlayerGUID == territoriy.OwnerGuid);
					if (playerData != null)
					{
						text4 = playerData.Name;
					}
					LabelWidget widget2 = new LabelWidget
					{
						Text = "Owner: " + text4,
						Color = Color.LightGray,
						Margin = new Vector2(0f, 10f),
						HorizontalAlignment = WidgetAlignment.Near
					};
					CheckboxWidget checkboxWidget = new CheckboxWidget
					{
						Text = LanguageControl.Get(fName, 1),
						Size = new Vector2(36f)
					};
					CheckboxWidget checkboxWidget2 = new CheckboxWidget
					{
						Text = LanguageControl.Get(fName, 2),
						Size = new Vector2(36f)
					};
					CheckboxWidget checkboxWidget3 = new CheckboxWidget
					{
						Text = LanguageControl.Get(fName, 3),
						Size = new Vector2(36f)
					};
					CheckboxWidget checkboxWidget4 = new CheckboxWidget
					{
						Text = LanguageControl.Get(fName, 4),
						Size = new Vector2(36f)
					};
					CheckboxWidget checkboxWidget5 = new CheckboxWidget
					{
						Text = LanguageControl.Get(fName, 5),
						Size = new Vector2(36f)
					};
					CheckboxWidget checkboxWidget6 = new CheckboxWidget
					{
						Text = LanguageControl.Get(fName, 8),
						Size = new Vector2(36f)
					};
					CheckboxWidget checkboxWidget7 = new CheckboxWidget
					{
						Text = LanguageControl.Get(fName, 9),
						Size = new Vector2(36f)
					};
					checkboxWidget.CheckStatusChanged += (bool flag) =>
					{
						territoriy.IsVisible = flag;
						CommonLib.Net.QueuePackage(new TerritoriyPackage(territoriy));
					};
					checkboxWidget2.CheckStatusChanged += (bool flag) =>
					{
						territoriy.AllowDig = flag;
						CommonLib.Net.QueuePackage(new TerritoriyPackage(territoriy));
					};
					checkboxWidget3.CheckStatusChanged += (bool flag) =>
					{
						territoriy.AllowPlace = flag;
						CommonLib.Net.QueuePackage(new TerritoriyPackage(territoriy));
					};
					checkboxWidget4.CheckStatusChanged += (bool flag) =>
					{
						territoriy.AllowBlockBehavior = flag;
						CommonLib.Net.QueuePackage(new TerritoriyPackage(territoriy));
					};
					checkboxWidget5.CheckStatusChanged += (bool flag) =>
					{
						territoriy.ApplyToFirend = flag;
						CommonLib.Net.QueuePackage(new TerritoriyPackage(territoriy));
					};
					checkboxWidget6.CheckStatusChanged += (bool flag) =>
					{
						territoriy.BlockFire = flag;
						CommonLib.Net.QueuePackage(new TerritoriyPackage(territoriy));
					};
					checkboxWidget7.CheckStatusChanged += (bool flag) =>
					{
						territoriy.BlockPvp = flag;
						CommonLib.Net.QueuePackage(new TerritoriyPackage(territoriy));
					};
					checkboxWidget.IsChecked = territoriy.IsVisible;
					checkboxWidget2.IsChecked = territoriy.AllowDig;
					checkboxWidget3.IsChecked = territoriy.AllowPlace;
					checkboxWidget4.IsChecked = territoriy.AllowBlockBehavior;
					checkboxWidget5.IsChecked = territoriy.ApplyToFirend;
					checkboxWidget6.IsChecked = territoriy.BlockFire;
					checkboxWidget7.IsChecked = territoriy.BlockPvp;
					canvasWidget.Children.Add(widget);
					canvasWidget.Children.Add(stackPanelWidget);
					stackPanelWidget.Children.Add(widget2);
					stackPanelWidget.Children.Add(checkboxWidget);
					stackPanelWidget.Children.Add(checkboxWidget2);
					stackPanelWidget.Children.Add(checkboxWidget3);
					stackPanelWidget.Children.Add(checkboxWidget4);
					stackPanelWidget.Children.Add(checkboxWidget5);
					stackPanelWidget.Children.Add(checkboxWidget6);
					stackPanelWidget.Children.Add(checkboxWidget7);
					componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = canvasWidget;
					return true;
				}
				string text5 = LanguageControl.Get("SubsystemBedrockBlockBehavior", "no_block_view_permission");
				componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text5, Color.Yellow, blinking: false, playNotificationSound: true);
			}
			else
			{
				string text6 = LanguageControl.Get("SubsystemBedrockBlockBehavior", "single_block_work");
				componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text6, Color.Yellow, blinking: false, playNotificationSound: true);
			}
		}
		return true;
	}

	public static bool CheckIsInTerritoriy(int x, int z, out Territoriy territoriy)
	{
		foreach (KeyValuePair<Guid, List<Territoriy>> territory in Territories)
		{
			foreach (Territoriy item in territory.Value)
			{
				if (x >= item.Origin.X - TerritoriySize && z >= item.Origin.Y - TerritoriySize && x <= item.Origin.X + TerritoriySize && z <= item.Origin.Y + TerritoriySize)
				{
					territoriy = item;
					return true;
				}
			}
		}
		territoriy = null;
		return false;
	}

	public override void OnBlockPlaced(ComponentMiner miner, int x, int y, int z, ref BlockPlacementData placementData, int itemValue)
	{
		int num = Terrain.ExtractData(itemValue);
		ComponentPlayer componentPlayer = miner.ComponentPlayer;
		if (componentPlayer == null || num != 1)
		{
			return;
		}
		if (!componentPlayer.PlayerData.ServerManager && m_subsystemGameInfo != null)
		{
			int regionPrivateLimit = m_subsystemGameInfo.WorldSettings.RegionPrivateLimit;
			if (regionPrivateLimit == 0)
			{
				string text = LanguageControl.Get("ComponentMiner", "region_null_blocks");
				componentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Red, blinking: false, playNotificationSound: true);
				if (CommonLib.WorkType != WorkType.Client)
				{
					Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(itemValue, 1, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), null, null);
				}
				placementData.Value = 0;
				return;
			}
			if (regionPrivateLimit > 0 && Territories.TryGetValue(componentPlayer.PlayerData.PlayerGUID, out var value) && (value?.Count ?? 0) >= regionPrivateLimit)
			{
				string text2 = string.Format(LanguageControl.Get("ComponentMiner", "region_limite"), regionPrivateLimit);
				componentPlayer.ComponentGui.DisplaySmallMessage(text2, Color.Red, blinking: false, playNotificationSound: true);
				if (CommonLib.WorkType != WorkType.Client)
				{
					Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(itemValue, 1, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), null, null);
				}
				placementData.Value = 0;
				return;
			}
		}
		Territoriy territoriy = new Territoriy
		{
			IsVisible = false,
			AllowBlockBehavior = false,
			AllowDig = false,
			AllowPlace = false,
			ApplyToFirend = false,
			BlockFire = false,
			BlockPvp = false,
			OwnerGuid = componentPlayer.PlayerData.PlayerGUID,
			OwnChunkCoord = new Point3(x, y, z),
			Origin = new Point2(x, z)
		};
		territoriy.CalculateBox(TerritoriySize);
		foreach (KeyValuePair<Guid, List<Territoriy>> territory in Territories)
		{
			foreach (Territoriy item in territory.Value)
			{
				if (territoriy.BoundBox.Intersection(item.BoundBox))
				{
					miner.ComponentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 6), Color.Red, blinking: false, playNotificationSound: true);
					if (CommonLib.WorkType != WorkType.Client)
					{
						Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(itemValue, 1, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), null, null);
					}
					placementData.Value = 0;
					return;
				}
			}
		}
		if (!Territories.TryGetValue(componentPlayer.PlayerData.PlayerGUID, out var value2))
		{
			value2 = new List<Territoriy>();
			Territories[componentPlayer.PlayerData.PlayerGUID] = value2;
		}
		value2.Add(territoriy);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		Territoriy territoriy = null;
		foreach (KeyValuePair<Guid, List<Territoriy>> territory in Territories)
		{
			foreach (Territoriy item in territory.Value)
			{
				if (item.OwnChunkCoord.X == x && item.OwnChunkCoord.Y == y && item.OwnChunkCoord.Z == z)
				{
					territoriy = item;
					break;
				}
			}
			if (territoriy != null)
			{
				break;
			}
		}
		if (territoriy == null)
		{
			return;
		}
		Guid ownerGuid = territoriy.OwnerGuid;
		if (Territories.TryGetValue(ownerGuid, out var value2))
		{
			value2.Remove(territoriy);
			if (value2.Count == 0)
			{
				Territories.Remove(ownerGuid);
			}
		}
	}

	public override void Dispose()
	{
		Territories.Clear();
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		Territories.Clear();
		foreach (ValuesDictionary value2 in valuesDictionary.GetValue<ValuesDictionary>("Territoriy").Values)
		{
			Territoriy territoriy = new Territoriy
			{
				AllowBlockBehavior = value2.GetValue<bool>("AllowBlockBehavior"),
				AllowDig = value2.GetValue<bool>("AllowDig"),
				AllowPlace = value2.GetValue<bool>("AllowPlace"),
				OwnChunkCoord = value2.GetValue<Point3>("OwnChunkCoord"),
				IsVisible = value2.GetValue<bool>("IsVisible"),
				ApplyToFirend = value2.GetValue<bool>("ApplyToFirend"),
				OwnerGuid = value2.GetValue<Guid>("Guid"),
				BlockFire = value2.GetValue<bool>("BlockFire"),
				BlockPvp = value2.GetValue<bool>("BlockPvp")
			};
			territoriy.Origin = new Point2(territoriy.OwnChunkCoord.X, territoriy.OwnChunkCoord.Z);
			territoriy.CalculateBox(TerritoriySize);
			if (!Territories.TryGetValue(territoriy.OwnerGuid, out var value))
			{
				value = new List<Territoriy>();
				Territories[territoriy.OwnerGuid] = value;
			}
			value.Add(territoriy);
		}
		ApplyRegionLimit();
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		int num = 0;
		foreach (KeyValuePair<Guid, List<Territoriy>> territory in Territories)
		{
			foreach (Territoriy item in territory.Value)
			{
				ValuesDictionary valuesDictionary3 = new ValuesDictionary();
				valuesDictionary3.SetValue("Guid", territory.Key);
				valuesDictionary3.SetValue("OwnChunkCoord", item.OwnChunkCoord);
				valuesDictionary3.SetValue("AllowDig", item.AllowDig);
				valuesDictionary3.SetValue("AllowPlace", item.AllowPlace);
				valuesDictionary3.SetValue("AllowBlockBehavior", item.AllowBlockBehavior);
				valuesDictionary3.SetValue("ApplyToFirend", item.ApplyToFirend);
				valuesDictionary3.SetValue("IsVisible", item.IsVisible);
				valuesDictionary3.SetValue("BlockFire", item.BlockFire);
				valuesDictionary3.SetValue("BlockPvp", item.BlockPvp);
				valuesDictionary2.SetValue(num++.ToString(), valuesDictionary3);
			}
		}
		valuesDictionary.SetValue("Territoriy", valuesDictionary2);
	}

	public void Draw(Camera camera, int drawOrder)
	{
		Vector3 viewPosition = camera.ViewPosition;
		foreach (KeyValuePair<Guid, List<Territoriy>> territory in Territories)
		{
			List<Territoriy> value = territory.Value;
			foreach (Territoriy item in value)
			{
				BoundingBox boundBox = item.BoundBox;
				BoundingBox boundingBox = new BoundingBox(new Vector3(boundBox.Min.X - 60f, boundBox.Min.Y, boundBox.Min.Z - 60f), new Vector3(boundBox.Max.X + 60f, boundBox.Max.Y, boundBox.Max.Z + 60f));
				if (!boundingBox.Contains(viewPosition))
				{
					continue;
				}
				if (camera.GameWidget.PlayerData.ServerManager)
				{
					FlatBatch3D flatBatch3D = primitives.FlatBatch();
					float x = (float)item.OwnChunkCoord.X + 0.5f;
					float y = (float)item.OwnChunkCoord.Y + 0.5f;
					float z = (float)item.OwnChunkCoord.Z + 0.5f;
					flatBatch3D.QueueLine(new Vector3(x, y, z), new Vector3(x, 256f, z), Color.Green);
					primitives.Flush(camera.ViewProjectionMatrix);
				}
				if (!item.IsVisible || !boundingBox.Contains(viewPosition))
				{
					continue;
				}
				Guid playerGUID = camera.GameWidget.PlayerData.PlayerGUID;
				bool flag = camera.GameWidget.PlayerData.Friends.Contains(item.OwnerGuid);
				Color color;
				if (item.OwnerGuid == playerGUID)
				{
					color = Color.Green;
				}
				else
				{
					color = ((flag && item.ApplyToFirend) ? Color.Yellow : Color.Red);
				}
				FlatBatch3D flatBatch3D2 = primitives.FlatBatch();
				float x2 = boundBox.Min.X;
				float x3 = boundBox.Max.X;
				float z2 = boundBox.Min.Z;
				float z3 = boundBox.Max.Z;
				float y2 = boundBox.Min.Y;
				float y3 = boundBox.Max.Y;
				flatBatch3D2.QueueLine(new Vector3(x2, y2, z2), new Vector3(x3, y2, z2), color);
				flatBatch3D2.QueueLine(new Vector3(x3, y2, z2), new Vector3(x3, y2, z3), color);
				flatBatch3D2.QueueLine(new Vector3(x3, y2, z3), new Vector3(x2, y2, z3), color);
				flatBatch3D2.QueueLine(new Vector3(x2, y2, z3), new Vector3(x2, y2, z2), color);
				flatBatch3D2.QueueLine(new Vector3(x2, y3, z2), new Vector3(x3, y3, z2), color);
				flatBatch3D2.QueueLine(new Vector3(x3, y3, z2), new Vector3(x3, y3, z3), color);
				flatBatch3D2.QueueLine(new Vector3(x3, y3, z3), new Vector3(x2, y3, z3), color);
				flatBatch3D2.QueueLine(new Vector3(x2, y3, z3), new Vector3(x2, y3, z2), color);
				bool flag2 = true;
				bool flag3 = true;
				bool flag4 = true;
				bool flag5 = true;
				foreach (Territoriy item2 in value)
				{
					if (item2 != item)
					{
						if (Math.Abs(item2.BoundBox.Max.X - boundBox.Min.X) < 1.1f && item2.BoundBox.Min.Z < boundBox.Max.Z && item2.BoundBox.Max.Z > boundBox.Min.Z)
						{
							flag2 = false;
						}
						if (Math.Abs(item2.BoundBox.Min.X - boundBox.Max.X) < 1.1f && item2.BoundBox.Min.Z < boundBox.Max.Z && item2.BoundBox.Max.Z > boundBox.Min.Z)
						{
							flag3 = false;
						}
						if (Math.Abs(item2.BoundBox.Max.Z - boundBox.Min.Z) < 1.1f && item2.BoundBox.Min.X < boundBox.Max.X && item2.BoundBox.Max.X > boundBox.Min.X)
						{
							flag4 = false;
						}
						if (Math.Abs(item2.BoundBox.Min.Z - boundBox.Max.Z) < 1.1f && item2.BoundBox.Min.X < boundBox.Max.X && item2.BoundBox.Max.X > boundBox.Min.X)
						{
							flag5 = false;
						}
					}
				}
				if (flag2)
				{
					flatBatch3D2.QueueLine(new Vector3(x2, y2, z2), new Vector3(x2, y3, z2), color);
					flatBatch3D2.QueueLine(new Vector3(x2, y2, z3), new Vector3(x2, y3, z3), color);
				}
				if (flag3)
				{
					flatBatch3D2.QueueLine(new Vector3(x3, y2, z2), new Vector3(x3, y3, z2), color);
					flatBatch3D2.QueueLine(new Vector3(x3, y2, z3), new Vector3(x3, y3, z3), color);
				}
				if (flag4)
				{
					flatBatch3D2.QueueLine(new Vector3(x2, y2, z2), new Vector3(x2, y3, z2), color);
					flatBatch3D2.QueueLine(new Vector3(x3, y2, z2), new Vector3(x3, y3, z2), color);
				}
				if (flag5)
				{
					flatBatch3D2.QueueLine(new Vector3(x2, y2, z3), new Vector3(x2, y3, z3), color);
					flatBatch3D2.QueueLine(new Vector3(x3, y2, z3), new Vector3(x3, y3, z3), color);
				}
				if (flag4)
				{
					float num = x2 - y3;
					float num2 = x3 - y2;
					for (float num3 = 12f * (float)Math.Floor(num / 12f); num3 <= num2; num3 += 12f)
					{
						Vector3? vector = null;
						Vector3? vector2 = null;
						float num4 = num3 + y2;
						if (num4 >= x2 && num4 <= x3)
						{
							vector = new Vector3(num4, y2, z2);
						}
						float num5 = num3 + y3;
						if (num5 >= x2 && num5 <= x3)
						{
							if (!vector.HasValue)
							{
								vector = new Vector3(num5, y3, z2);
							}
							else
							{
								vector2 = new Vector3(num5, y3, z2);
							}
						}
						float num6 = x2 - num3;
						if (num6 >= y2 && num6 <= y3)
						{
							if (!vector.HasValue)
							{
								vector = new Vector3(x2, num6, z2);
							}
							else if (!vector2.HasValue)
							{
								vector2 = new Vector3(x2, num6, z2);
							}
						}
						float num7 = x3 - num3;
						if (num7 >= y2 && num7 <= y3)
						{
							if (!vector.HasValue)
							{
								vector = new Vector3(x3, num7, z2);
							}
							else if (!vector2.HasValue)
							{
								vector2 = new Vector3(x3, num7, z2);
							}
						}
						if (vector.HasValue && vector2.HasValue)
						{
							flatBatch3D2.QueueLine(vector.Value, vector2.Value, color);
						}
					}
				}
				if (flag5)
				{
					float num8 = x2 - y3;
					float num9 = x3 - y2;
					for (float num10 = 12f * (float)Math.Floor(num8 / 12f); num10 <= num9; num10 += 12f)
					{
						Vector3? vector3 = null;
						Vector3? vector4 = null;
						float num11 = num10 + y2;
						if (num11 >= x2 && num11 <= x3)
						{
							vector3 = new Vector3(num11, y2, z3);
						}
						float num12 = num10 + y3;
						if (num12 >= x2 && num12 <= x3)
						{
							if (!vector3.HasValue)
							{
								vector3 = new Vector3(num12, y3, z3);
							}
							else
							{
								vector4 = new Vector3(num12, y3, z3);
							}
						}
						float num13 = x2 - num10;
						if (num13 >= y2 && num13 <= y3)
						{
							if (!vector3.HasValue)
							{
								vector3 = new Vector3(x2, num13, z3);
							}
							else if (!vector4.HasValue)
							{
								vector4 = new Vector3(x2, num13, z3);
							}
						}
						float num14 = x3 - num10;
						if (num14 >= y2 && num14 <= y3)
						{
							if (!vector3.HasValue)
							{
								vector3 = new Vector3(x3, num14, z3);
							}
							else if (!vector4.HasValue)
							{
								vector4 = new Vector3(x3, num14, z3);
							}
						}
						if (vector3.HasValue && vector4.HasValue)
						{
							flatBatch3D2.QueueLine(vector3.Value, vector4.Value, color);
						}
					}
				}
				if (flag2)
				{
					float num15 = z2 - y3;
					float num16 = z3 - y2;
					for (float num17 = 12f * (float)Math.Floor(num15 / 12f); num17 <= num16; num17 += 12f)
					{
						Vector3? vector5 = null;
						Vector3? vector6 = null;
						float num18 = num17 + y2;
						if (num18 >= z2 && num18 <= z3)
						{
							vector5 = new Vector3(x2, y2, num18);
						}
						float num19 = num17 + y3;
						if (num19 >= z2 && num19 <= z3)
						{
							if (!vector5.HasValue)
							{
								vector5 = new Vector3(x2, y3, num19);
							}
							else
							{
								vector6 = new Vector3(x2, y3, num19);
							}
						}
						float num20 = z2 - num17;
						if (num20 >= y2 && num20 <= y3)
						{
							if (!vector5.HasValue)
							{
								vector5 = new Vector3(x2, num20, z2);
							}
							else if (!vector6.HasValue)
							{
								vector6 = new Vector3(x2, num20, z2);
							}
						}
						float num21 = z3 - num17;
						if (num21 >= y2 && num21 <= y3)
						{
							if (!vector5.HasValue)
							{
								vector5 = new Vector3(x2, num21, z3);
							}
							else if (!vector6.HasValue)
							{
								vector6 = new Vector3(x2, num21, z3);
							}
						}
						if (vector5.HasValue && vector6.HasValue)
						{
							flatBatch3D2.QueueLine(vector5.Value, vector6.Value, color);
						}
					}
				}
				if (flag3)
				{
					float num22 = z2 - y3;
					float num23 = z3 - y2;
					for (float num24 = 12f * (float)Math.Floor(num22 / 12f); num24 <= num23; num24 += 12f)
					{
						Vector3? vector7 = null;
						Vector3? vector8 = null;
						float num25 = num24 + y2;
						if (num25 >= z2 && num25 <= z3)
						{
							vector7 = new Vector3(x3, y2, num25);
						}
						float num26 = num24 + y3;
						if (num26 >= z2 && num26 <= z3)
						{
							if (!vector7.HasValue)
							{
								vector7 = new Vector3(x3, y3, num26);
							}
							else
							{
								vector8 = new Vector3(x3, y3, num26);
							}
						}
						float num27 = z2 - num24;
						if (num27 >= y2 && num27 <= y3)
						{
							if (!vector7.HasValue)
							{
								vector7 = new Vector3(x3, num27, z2);
							}
							else if (!vector8.HasValue)
							{
								vector8 = new Vector3(x3, num27, z2);
							}
						}
						float num28 = z3 - num24;
						if (num28 >= y2 && num28 <= y3)
						{
							if (!vector7.HasValue)
							{
								vector7 = new Vector3(x3, num28, z3);
							}
							else if (!vector8.HasValue)
							{
								vector8 = new Vector3(x3, num28, z3);
							}
						}
						if (vector7.HasValue && vector8.HasValue)
						{
							flatBatch3D2.QueueLine(vector7.Value, vector8.Value, color);
						}
					}
				}
				primitives.Flush(camera.ViewProjectionMatrix);
			}
		}
	}

	static SubsystemBedrockBlockBehavior()
	{
		TerritoriySize = 16;
		Territories = new Dictionary<Guid, List<Territoriy>>();
		fName = "SubsystemBedrockBlockBehavior";
	}

	private void ApplyRegionLimit()
	{
		if (m_subsystemGameInfo == null || m_subsystemGameInfo.WorldSettings == null)
		{
			return;
		}
		int regionPrivateLimit = m_subsystemGameInfo.WorldSettings.RegionPrivateLimit;
		if (regionPrivateLimit < 0)
		{
			return;
		}
		switch (regionPrivateLimit)
		{
		case int.MaxValue:
			return;
		case 0:
			Territories.Clear();
			return;
		}
		List<Guid> list = null;
		foreach (KeyValuePair<Guid, List<Territoriy>> territory in Territories)
		{
			List<Territoriy> value = territory.Value;
			if (value == null)
			{
				continue;
			}
			while (value.Count > regionPrivateLimit)
			{
				value.RemoveAt(value.Count - 1);
			}
			if (value.Count == 0)
			{
				if (list == null)
				{
					list = new List<Guid>();
				}
				list.Add(territory.Key);
			}
		}
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				Territories.Remove(list[i]);
			}
		}
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		if ((Terrain.ExtractData(value) & 1) != 1)
		{
			return;
		}
		bool flag = false;
		foreach (KeyValuePair<Guid, List<Territoriy>> territory in Territories)
		{
			foreach (Territoriy item in territory.Value)
			{
				if (item.OwnChunkCoord.X == x && item.OwnChunkCoord.Y == y && item.OwnChunkCoord.Z == z)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (!flag)
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}
}
