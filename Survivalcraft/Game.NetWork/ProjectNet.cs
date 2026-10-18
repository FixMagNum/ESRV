using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Engine.Serialization;
using Game.NetWork.Packages;
using GameEntitySystem;
using LiteNetLib.Utils;
using TemplatesDatabase;

namespace Game.NetWork;

public class ProjectNet : Project
{
	private Dictionary<string, Subsystem> dictionary = new Dictionary<string, Subsystem>();

	public static SubsystemPlayers subsystemPlayers;

	public static SubsystemTerrain subsystemTerrain;

	public static SubsystemPlantBlockBehavior subsystemPlantBlockBehavior;

	public static SubsystemGrassBlockBehavior subsystemGrassBlockBehavior;

	public static SubsystemPickables subsystemPickables;

	public static SubsystemTimeOfDay subsystemTimeOfDay;

	public static SubsystemBlockEntities subsystemBlockEntities;

	public static SubsystemSky subsystemSky;

	public static SubsystemAudio subsystemAudio;

	public static SubsystemCreatureSpawn subsystemCreatureSpawn;

	public static SubsystemSpawn subsystemSpawn;

	public static SubsystemGameInfo subsystemGameInfo;

	public static SubsystemBodies subsystemBodies;

	public static SubsystemTime subsystemTime;

	public static SubsystemElectricity subsystemElectricity;

	public static SubsystemBlockBehaviors subsystemBlockBehaviors;

	public static SubsystemParticles subsystemParticles;

	public static SubsystemAnimatedTextures subsystemAnimatedTextures;

	public static SubsystemExplosions subsystemExplosions;

	public static SubsystemProjectiles subsystemProjectiles;

	public static SubsystemInventories inventories;

	public static ProjectNet project;

	public bool SendToClientMode;

	public const string Server_Version = "x23.01.31";

	public Dictionary<int, Entity> EntityMaps = new Dictionary<int, Entity>();

	public ProjectNet(GameDatabase gameDatabase, ProjectData projectData)
	{
		try
		{
			dictionary.Clear();
			m_gameDatabase = gameDatabase;
			m_projectTemplate = projectData.ValuesDictionary.DatabaseObject;
			foreach (ValuesDictionary item in from x in projectData.ValuesDictionary.Values
				select x as ValuesDictionary into x
				where x != null && x.DatabaseObject != null && x.DatabaseObject.Type == gameDatabase.MemberSubsystemTemplateType
				select x)
			{
				bool value = item.GetValue<bool>("IsOptional");
				string value2 = item.GetValue<string>("Class");
				Type type = TypeCache.FindType(value2, skipSystemAssemblies: false, !value);
				if (type != null)
				{
					object obj;
					try
					{
						obj = Activator.CreateInstance(type);
					}
					catch (TargetInvocationException ex)
					{
						throw ex.InnerException;
					}
					if (!(obj is Subsystem subsystem))
					{
						throw new InvalidOperationException("Type \"" + value2 + "\" cannot be used as a subsystem because it does not inherit from Subsystem class.");
					}
					subsystem.Initialize(this, item);
					dictionary.Add(item.DatabaseObject.Name, subsystem);
					m_subsystems.Add(subsystem);
				}
			}
			subsystemPlayers = FindSubsystem<SubsystemPlayers>();
			subsystemTerrain = FindSubsystem<SubsystemTerrain>();
			subsystemPlantBlockBehavior = FindSubsystem<SubsystemPlantBlockBehavior>();
			subsystemGrassBlockBehavior = FindSubsystem<SubsystemGrassBlockBehavior>();
			subsystemPickables = FindSubsystem<SubsystemPickables>();
			subsystemTimeOfDay = FindSubsystem<SubsystemTimeOfDay>();
			subsystemSky = FindSubsystem<SubsystemSky>();
			subsystemAudio = FindSubsystem<SubsystemAudio>();
			subsystemCreatureSpawn = FindSubsystem<SubsystemCreatureSpawn>();
			subsystemSpawn = FindSubsystem<SubsystemSpawn>();
			subsystemGameInfo = FindSubsystem<SubsystemGameInfo>();
			subsystemBodies = FindSubsystem<SubsystemBodies>();
			subsystemTime = FindSubsystem<SubsystemTime>();
			subsystemBlockEntities = FindSubsystem<SubsystemBlockEntities>();
			subsystemElectricity = FindSubsystem<SubsystemElectricity>();
			subsystemBlockBehaviors = FindSubsystem<SubsystemBlockBehaviors>();
			subsystemParticles = FindSubsystem<SubsystemParticles>();
			subsystemExplosions = FindSubsystem<SubsystemExplosions>();
			subsystemAnimatedTextures = FindSubsystem<SubsystemAnimatedTextures>();
			subsystemProjectiles = FindSubsystem<SubsystemProjectiles>();
			inventories = FindSubsystem<SubsystemInventories>();
			PackageManager.Initialize();
			if (CommonLib.WorkType == WorkType.Client)
			{
				BeforeEntityAdded += (object obj3, EntityAddRemoveEventArgs arg) =>
				{
					EntityMaps[arg.Entity.EntityId] = arg.Entity;
				};
				EntityRemoved += (object obj3, EntityAddRemoveEventArgs arg) =>
				{
					EntityMaps.Remove(arg.Entity.EntityId);
				};
				CommonLib.Net.OnRecieve += OnClientRecievePackage;
				CommonLib.Net.OnClientStateChanged += (IClient c) =>
				{
					if (c.State == ClientState.NotConnected)
					{
						PlayerData playerData = subsystemPlayers.PlayersData.FirstOrDefault((PlayerData pd) => pd.PlayerGUID == c.GUID);
						if (playerData != null && !playerData.IsMainPlayer)
						{
							subsystemPlayers.MakePlayerOffline(playerData);
						}
					}
				};
			}
			else
			{
				BeforeEntityAdded += (object obj3, EntityAddRemoveEventArgs arg) =>
				{
					if (arg.Entity.EntityId == 0)
					{
						GenerateEntityId(arg.Entity);
					}
					else if (EntityMaps.ContainsKey(arg.Entity.EntityId))
					{
						GenerateEntityId(arg.Entity);
					}
					else
					{
						EntityMaps.Add(arg.Entity.EntityId, arg.Entity);
					}
				};
				EntityRemoved += (object obj3, EntityAddRemoveEventArgs arg) =>
				{
					EntityMaps.Remove(arg.Entity.EntityId);
				};
				if (CommonLib.WorkType == WorkType.Server)
				{
					EntityAdded += (object obj3, EntityAddRemoveEventArgs arg) =>
					{
						CommonLib.Net.QueuePackage(new EntityPackage(arg.Entity));
					};
					EntityRemoved += (object obj3, EntityAddRemoveEventArgs arg) =>
					{
						CommonLib.Net.QueuePackage(new EntityPackage(arg.Entity.EntityId));
					};
					CommonLib.Net.OnRecieve += OnServerRecievePackage;
					CommonLib.Net.OnClientStateChanged += GrantClient;
					CommonLib.Net.OnClientStateChanged += OnClientStateChanged;
				}
			}
			Load(dictionary, projectData);
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.Send(CommonLib.Net.MakePackage(PackageFlag.ClientState, (NetDataWriter w) =>
				{
					w.Put((byte)3);
				}));
			}
		}
		catch
		{
			try
			{
				Dispose();
			}
			catch (Exception message)
			{
				Log.Error(message);
			}
			finally
			{
				if (CommonLib.WorkType == WorkType.Server)
				{
					CommonLib.Net.Stop();
				}
			}
			throw;
		}
		project = this;
	}

	public override void AddEntity(Entity entity)
	{
		base.AddEntity(entity);
	}

	public override void RemoveEntity(Entity entity, bool disposeEntity)
	{
		base.RemoveEntity(entity, disposeEntity);
	}

	public override void GenerateEntityId(Entity entity)
	{
		for (ushort num = 1; num < ushort.MaxValue; num++)
		{
			if (!EntityMaps.ContainsKey(num))
			{
				entity.EntityId = num;
				EntityMaps.Add(num, entity);
				break;
			}
		}
	}

	public override bool FindEntityById(ushort Id, Action<Entity> action = null)
	{
		if (EntityMaps.TryGetValue(Id, out var value))
		{
			action?.Invoke(value);
			return true;
		}
		return false;
	}

	public void OnClientStateChanged(IClient c)
	{
		if (CommonLib.WorkType == WorkType.Server)
		{
			CommonLib.Net.SendToAllExcept(CommonLib.Net.MakePackage(PackageFlag.ClientState, (NetDataWriter w) =>
			{
				w.Put(c.ID);
				w.Put((byte)c.State);
			}), c);
		}
		switch (c.State)
		{
		case ClientState.NotConnected:
			subsystemPlayers.MakePlayerOffline(c.PlayerData);
			break;
		case ClientState.Connected:
		{
			string projectXml = CommonLib.GetNowProject(this);
			Task.Run(() =>
			{
				byte[] array = null;
				if (!string.IsNullOrEmpty(subsystemGameInfo.WorldSettings.BlocksTextureName))
				{
					using Stream stream = Storage.OpenFile(BlocksTexturesManager.GetFileName(subsystemGameInfo.WorldSettings.BlocksTextureName), OpenFileMode.Read);
					array = ModsManager.StreamToBytes(stream);
				}
				byte[] projectCompressed = CommonLib.Compress(Encoding.UTF8.GetBytes(projectXml));
				byte[] textureCompressed = null;
				if (array != null)
				{
					textureCompressed = CommonLib.Compress(array);
				}
				CommonLib.Net.SendPackage(PackageFlag.NetProject, (NetDataWriter w) =>
				{
					w.PutBytesWithLength(projectCompressed);
					subsystemPlayers.m_offlinePlayers.Find((PlayerData p) => p.PlayerGUID == c.GUID);
					w.Put(textureCompressed != null);
					if (textureCompressed != null)
					{
						w.PutBytesWithLength(textureCompressed, 0, textureCompressed.Length);
					}
				}, c);
			});
			break;
		}
		case ClientState.ProjectLoaded:
		{
			Entity entity = subsystemPlayers.MakePlayerOnline(c.GUID);
			List<Entity> list = new List<Entity>();
			foreach (Entity key in m_entities.Keys)
			{
				if (key != entity)
				{
					list.Add(key);
					if (list.Count >= 50)
					{
						CommonLib.Net.QueuePackage(new EntityPackage(list)
						{
							To = c
						});
						list = new List<Entity>();
					}
				}
			}
			if (list.Count > 0)
			{
				CommonLib.Net.QueuePackage(new EntityPackage(list)
				{
					To = c
				});
			}
			break;
		}
		case ClientState.Joining:
			break;
		}
	}

	public void GrantClient(IClient client)
	{
		if (client.State == ClientState.Playing)
		{
			if (subsystemPlayers.BlackPlayerGuidList.Contains(client.GUID.ToString()))
			{
				CommonLib.Net.RemoveClientImmediate(client, "You are banned from joining this server");
			}
			else if (!client.Granted && !string.IsNullOrEmpty(subsystemGameInfo.WorldSettings.Password))
			{
				CommonLib.Net.RemoveClientImmediate(client, "Unauthenticated client");
			}
			else if (string.IsNullOrEmpty(subsystemGameInfo.WorldSettings.Password))
			{
				client.OnGranteStateChange(state: true);
			}
		}
	}

	public FurnitureInventoryPanel FindFurnitureInventorPanel(IClient client)
	{
		PlayerData baseColor = subsystemPlayers.PlayersData.Find((PlayerData x) => x.Client == client);
		if (baseColor != null && baseColor.ComponentPlayer != null)
		{
			return new CreativeInventoryWidget(baseColor.ComponentPlayer.Entity).furnitureInventoryPanel;
		}
		return null;
	}

	private void DecodePackage(Package p)
	{
		PackageManager.DecodePackages(new PackageStreamReader(p.Reader.GetBytesWithLength()), p.FromClient);
	}

	private void OnServerRecievePackage(Package p)
	{
		switch (p.Flag)
		{
		case PackageFlag.ClientKnowPlayer:
		{
			Guid num = p.Reader.GetGuid();
			p.FromClient.UpdateKnowsPlayer(num);
			break;
		}
		case PackageFlag.ChangeEditableRequest:
			CommonLib.RecieveEditItemRequest(p, p.Guid, this);
			break;
		case PackageFlag.ChangeSignBlockRequest:
		{
			EditSignDialog.ReadSignData(p.Reader, out var pos, out var data2);
			FindSubsystem<SubsystemSignBlockBehavior>().SetSignData(pos, data2.Lines, data2.Colors, data2.Url);
			Package p2 = CommonLib.Net.MakePackage(PackageFlag.ChangeSignBlock, (NetDataWriter w) =>
			{
				EditSignDialog.WriteSignData(w, pos, data2.Lines, data2.Colors, data2.Url);
			});
			CommonLib.Net.SendToAllExcept(p2, p.FromClientID);
			break;
		}
		case PackageFlag.ChangeDispenserRequest:
		{
			Point3 point = p.Reader.GetPoint3();
			byte b = p.Reader.GetByte();
			int cellValue = subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
			if (Terrain.ExtractContents(cellValue) == 216)
			{
				int data = Terrain.ExtractData(cellValue);
				data = DispenserBlock.SetMode(data, ((b & 1) != 0) ? DispenserBlock.Mode.Shoot : DispenserBlock.Mode.Dispense);
				data = DispenserBlock.SetAcceptsDrops(data, (b & 2) > 0);
				subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, Terrain.ReplaceData(cellValue, data));
			}
			break;
		}
		case PackageFlag.WrenchEntityUpdate:
			HandleWrenchUpdatePackage(p, isServer: true);
			break;
		case PackageFlag.AddFriendReuqest:
		{
			bool JoinOrQuit = p.Reader.GetBool();
			byte from3 = p.Reader.GetByte();
			byte to3 = p.Reader.GetByte();
			PlayerData playerData3 = subsystemPlayers.m_playersData.Find((PlayerData x) => x.IsMainPlayer);
			if (playerData3 != null && playerData3.ClientId.HasValue && to3 == playerData3.ClientId.Value)
			{
				PlayerData fromp2 = subsystemPlayers.m_playersData.Find((PlayerData x) => x.ClientId.HasValue && x.ClientId.Value == from3);
				PlayerData top2 = subsystemPlayers.m_playersData.Find((PlayerData x) => x.ClientId.HasValue && x.ClientId.Value == to3);
				if (fromp2 == null || top2 == null || top2.BlackList.Contains(fromp2.PlayerGUID))
				{
					break;
				}
				if (!JoinOrQuit)
				{
					CheckboxWidget checkbox = new CheckboxWidget
					{
						Text = "Do not receive this message again",
						HorizontalAlignment = WidgetAlignment.Center,
						Margin = new Vector2(0f, 40f)
					};
					AlertDialog alertDialog = new AlertDialog("Prompt", "<" + fromp2.Name + ">wants to invite you to join the team. Do you agree?", "Yes", "No", () =>
					{
						DialogsManager.HideAllDialogs();
						if (checkbox.IsChecked)
						{
							top2.BlackList.Add(fromp2.PlayerGUID);
						}
						else
						{
							fromp2.AddFriend(top2.PlayerGUID);
							top2.AddFriend(fromp2.PlayerGUID);
							CommonLib.Net.SendPackage(PackageFlag.AddFriendReply, (NetDataWriter w) =>
							{
								w.Put(from3);
								w.Put(to3);
								w.Put(value: true);
								w.Put(JoinOrQuit);
							});
						}
					}, () =>
					{
						DialogsManager.HideAllDialogs();
						if (checkbox.IsChecked)
						{
							top2.BlackList.Add(fromp2.PlayerGUID);
						}
						CommonLib.Net.SendPackage(PackageFlag.AddFriendReply, (NetDataWriter w) =>
						{
							w.Put(from3);
							w.Put(to3);
							w.Put(value: false);
							w.Put(JoinOrQuit);
						});
					});
					alertDialog.SetContent(checkbox);
					DialogsManager.ShowDialog(null, alertDialog);
				}
				else
				{
					fromp2.RemoveFriend(top2.PlayerGUID);
					top2.RemoveFriend(fromp2.PlayerGUID);
					CommonLib.Net.SendPackage(PackageFlag.AddFriendReply, (NetDataWriter w) =>
					{
						w.Put(from3);
						w.Put(to3);
						w.Put(value: true);
						w.Put(JoinOrQuit);
					});
				}
			}
			else
			{
				CommonLib.Net.SendPackage(PackageFlag.AddFriendReuqest, (NetDataWriter w) =>
				{
					w.Put(JoinOrQuit);
					w.Put(from3);
					w.Put(to3);
				});
			}
			break;
		}
		case PackageFlag.Package:
			DecodePackage(p);
			break;
		case PackageFlag.AddFriendReply:
		{
			byte from2 = p.Reader.GetByte();
			byte to2 = p.Reader.GetByte();
			bool result = p.Reader.GetBool();
			bool JoinOrResult = p.Reader.GetBool();
			PlayerData playerData = subsystemPlayers.m_playersData.Find((PlayerData x) => x.ClientId.HasValue && x.ClientId.Value == from2);
			PlayerData playerData2 = subsystemPlayers.m_playersData.Find((PlayerData x) => x.ClientId.HasValue && x.ClientId.Value == to2);
			if (playerData != null && playerData2 != null)
			{
				if (!JoinOrResult & result)
				{
					playerData.AddFriend(playerData2.PlayerGUID);
					playerData2.AddFriend(playerData.PlayerGUID);
				}
				else
				{
					playerData.RemoveFriend(playerData2.PlayerGUID);
					playerData2.RemoveFriend(playerData.PlayerGUID);
				}
				CommonLib.Net.SendPackage(PackageFlag.AddFriendReply, (NetDataWriter w) =>
				{
					w.Put(from2);
					w.Put(to2);
					w.Put(result);
					w.Put(JoinOrResult);
				});
			}
			break;
		}
		}
		PackageManager.HandlePackages(this, CommonLib.Net, isServer: true);
	}

	private void OnClientRecievePackage(Package p)
	{
		switch (p.Flag)
		{
		case PackageFlag.ChangeEditableItemData:
			CommonLib.RecieveChangeEditableItemData(p, this);
			break;
		case PackageFlag.ExplodeCells:
			subsystemExplosions.RecieveExplodeCells(p);
			break;
		case PackageFlag.ChangeSignBlock:
		{
			EditSignDialog.ReadSignData(p.Reader, out var boneAbsoluteTransform, out var boneAbsoluteTransform2);
			FindSubsystem<SubsystemSignBlockBehavior>().SetSignData(boneAbsoluteTransform, boneAbsoluteTransform2.Lines, boneAbsoluteTransform2.Colors, boneAbsoluteTransform2.Url);
			break;
		}
		case PackageFlag.WrenchEntityUpdate:
			HandleWrenchUpdatePackage(p, isServer: false);
			break;
		case PackageFlag.AddFriendReply:
		{
			byte from2 = p.Reader.GetByte();
			byte to2 = p.Reader.GetByte();
			bool flag = p.Reader.GetBool();
			bool flag2 = p.Reader.GetBool();
			PlayerData playerData2 = subsystemPlayers.m_playersData.Find((PlayerData x) => x.ClientId.HasValue && x.ClientId.Value == from2);
			PlayerData playerData3 = subsystemPlayers.m_playersData.Find((PlayerData x) => x.ClientId.HasValue && x.ClientId.Value == to2);
			if (playerData2 != null && playerData3 != null)
			{
				if (!flag2 & flag)
				{
					playerData2.AddFriend(playerData3.PlayerGUID);
					playerData3.AddFriend(playerData2.PlayerGUID);
				}
				else
				{
					playerData2.RemoveFriend(playerData3.PlayerGUID);
					playerData3.RemoveFriend(playerData2.PlayerGUID);
				}
			}
			break;
		}
		case PackageFlag.Package:
			SubsystemInventories.flushSyncItems();
			DecodePackage(p);
			break;
		case PackageFlag.AddFriendReuqest:
		{
			bool JoinOrQuit2 = p.Reader.GetBool();
			byte from = p.Reader.GetByte();
			byte to = p.Reader.GetByte();
			PlayerData playerData = subsystemPlayers.m_playersData.Find((PlayerData x) => x.IsMainPlayer);
			if (playerData == null || !playerData.ClientId.HasValue || to != playerData.ClientId.Value)
			{
				break;
			}
			PlayerData fromp = subsystemPlayers.m_playersData.Find((PlayerData x) => x.ClientId.HasValue && x.ClientId.Value == from);
			PlayerData top = subsystemPlayers.m_playersData.Find((PlayerData x) => x.ClientId.HasValue && x.ClientId.Value == to);
			if (top == null || fromp == null || top.BlackList.Contains(fromp.PlayerGUID))
			{
				break;
			}
			if (!JoinOrQuit2)
			{
				CheckboxWidget checkbox = new CheckboxWidget
				{
					Size = new Vector2(40f),
					Text = "Block player",
					HorizontalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(0f, 40f)
				};
				AlertDialog alertDialog = new AlertDialog("Prompt", fromp.Name + " wants to invite you to join the team. Do you agree?", "Yes", "No", () =>
				{
					DialogsManager.HideAllDialogs();
					if (checkbox.IsChecked)
					{
						top.BlackList.Add(fromp.PlayerGUID);
					}
					else
					{
						CommonLib.Net.SendPackage(PackageFlag.AddFriendReply, (NetDataWriter w) =>
						{
							w.Put(from);
							w.Put(to);
							w.Put(value: true);
							w.Put(JoinOrQuit2);
						});
					}
				}, () =>
				{
					if (checkbox.IsChecked)
					{
						top.BlackList.Add(fromp.PlayerGUID);
					}
					DialogsManager.HideAllDialogs();
					CommonLib.Net.SendPackage(PackageFlag.AddFriendReply, (NetDataWriter w) =>
					{
						w.Put(from);
						w.Put(to);
						w.Put(value: false);
						w.Put(JoinOrQuit2);
					});
				});
				alertDialog.SetContent(checkbox);
				DialogsManager.ShowDialog(null, alertDialog);
			}
			else
			{
				fromp.Friends.Remove(top.PlayerGUID);
				top.Friends.Remove(fromp.PlayerGUID);
				CommonLib.Net.SendPackage(PackageFlag.AddFriendReply, (NetDataWriter w) =>
				{
					w.Put(from);
					w.Put(to);
					w.Put(value: true);
					w.Put(JoinOrQuit2);
				});
			}
			break;
		}
		}
		PackageManager.HandlePackages(this, CommonLib.Net, isServer: false);
	}

	public void PlayerAction(byte cid, Action<ComponentPlayer> action)
	{
		Guid? level = CommonLib.Net.GetClientByID(cid)?.GUID;
		if (level.HasValue)
		{
			PlayerAction(level, action);
		}
	}

	public void PlayerAction(Guid? guid, Action<ComponentPlayer> action)
	{
		ComponentPlayer componentPlayer = subsystemPlayers.ComponentPlayers.FirstOrDefault((ComponentPlayer pll) =>
		{
			Guid playerGUID = pll.PlayerData.PlayerGUID;
			Guid? guid2 = guid;
			return playerGUID == guid2;
		});
		if (componentPlayer != null)
		{
			action(componentPlayer);
		}
	}

	public override List<Entity> LoadEntities(EntityDataList entityDataList)
	{
		List<Entity> level = new List<Entity>(entityDataList.EntitiesData.Count);
		Dictionary<int, Entity> num = new Dictionary<int, Entity>();
		IdToEntityMap idToEntityMap = new IdToEntityMap(num);
		List<EntityData> list = new List<EntityData>();
		Guid guid;
		foreach (EntityData entitiesDatum in entityDataList.EntitiesData)
		{
			try
			{
				Entity entity = new Entity(this, entitiesDatum.ValuesDictionary);
				bool shouldAdd = CommonLib.WorkType == WorkType.Local || entity.FindComponent<ComponentPlayer>() == null;
				if (!shouldAdd)
				{
					guid = entitiesDatum.ValuesDictionary.GetValue<ValuesDictionary>("Player").GetValue<Guid>("PlayerGuid");
					shouldAdd = !subsystemPlayers.PlayersData.All((PlayerData pd) => pd.PlayerGUID != guid);
				}
				if (shouldAdd)
				{
					level.Add(entity);
					if (entitiesDatum.Id != 0)
					{
						entity.EntityId = (ushort)entitiesDatum.Id;
						num.Add(entitiesDatum.Id, entity);
						list.Add(entitiesDatum);
					}
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Error creating entity from template \"" + entitiesDatum.ValuesDictionary.DatabaseObject.Name + "\".", innerException);
			}
		}
		int num2 = 0;
		foreach (EntityData item in list)
		{
			level[num2].publicLoadEntity(item.ValuesDictionary, idToEntityMap);
			num2++;
		}
		return level;
	}

	public override EntityDataList SaveEntities(IEnumerable<Entity> entities)
	{
		if (SendToClientMode)
		{
			return new EntityDataList
			{
				EntitiesData = new List<EntityData>()
			};
		}
		Dictionary<Entity, bool> dictionary = Project.DetermineNotOwnedEntities(entities);
		Dictionary<Entity, int> dictionary2 = new Dictionary<Entity, int>();
		EntityToIdMap entityToIdMap = new EntityToIdMap(dictionary2);
		foreach (Entity key in dictionary.Keys)
		{
			dictionary2.Add(key, key.EntityId);
		}
		EntityDataList entityDataList = new EntityDataList();
		entityDataList.EntitiesData = new List<EntityData>(dictionary.Keys.Count);
		foreach (Entity key2 in dictionary.Keys)
		{
			EntityData entityData = new EntityData();
			entityData.Id = entityToIdMap.FindId(key2);
			entityData.ValuesDictionary = new ValuesDictionary();
			entityData.ValuesDictionary.DatabaseObject = key2.ValuesDictionary.DatabaseObject;
			key2.InternalSaveEntity(entityData.ValuesDictionary, entityToIdMap);
			entityDataList.EntitiesData.Add(entityData);
		}
		return entityDataList;
	}

	public List<Entity> LoadEntitiesAll(EntityDataList entityDataList)
	{
		List<Entity> list = new List<Entity>(entityDataList.EntitiesData.Count);
		Dictionary<int, Entity> dictionary = new Dictionary<int, Entity>();
		IdToEntityMap idToEntityMap = new IdToEntityMap(dictionary);
		List<EntityData> list2 = new List<EntityData>();
		foreach (EntityData entitiesDatum in entityDataList.EntitiesData)
		{
			try
			{
				Entity entity = new Entity(this, entitiesDatum.ValuesDictionary);
				list.Add(entity);
				entity.EntityId = (ushort)entitiesDatum.Id;
				dictionary.Add(entitiesDatum.Id, entity);
				list2.Add(entitiesDatum);
			}
			catch (Exception innerException)
			{
				throw new Exception("Error creating entity from template \"" + entitiesDatum.ValuesDictionary.DatabaseObject.Name + "\".", innerException);
			}
		}
		int num = 0;
		foreach (EntityData item in list2)
		{
			list[num].publicLoadEntity(item.ValuesDictionary, idToEntityMap);
			num++;
		}
		return list;
	}

	public EntityDataList SaveEntitiesAll(IEnumerable<Entity> entities)
	{
		Dictionary<Entity, int> vector = new Dictionary<Entity, int>();
		EntityToIdMap flag = new EntityToIdMap(vector);
		foreach (Entity particle in entities)
		{
			vector.Add(particle, particle.EntityId);
		}
		EntityDataList num = new EntityDataList();
		num.EntitiesData = new List<EntityData>(dictionary.Keys.Count);
		foreach (Entity maxLength in entities)
		{
			EntityData vector2 = new EntityData();
			vector2.Id = flag.FindId(maxLength);
			vector2.ValuesDictionary = new ValuesDictionary();
			vector2.ValuesDictionary.DatabaseObject = maxLength.ValuesDictionary.DatabaseObject;
			maxLength.InternalSaveEntity(vector2.ValuesDictionary, flag);
			num.EntitiesData.Add(vector2);
		}
		return num;
	}

	public void Load(Dictionary<string, Subsystem> dictionary, ProjectData projectData)
	{
		Dictionary<Subsystem, bool> viewDirection = new Dictionary<Subsystem, bool>();
		foreach (Subsystem i in dictionary.Values)
		{
			LoadSubsystem(i, dictionary, viewDirection, 0);
		}
		if (projectData.EntityDataList != null && CommonLib.WorkType != WorkType.Client)
		{
			List<Entity> particle = LoadEntities(projectData.EntityDataList);
			AddEntities(particle);
		}
	}

	private void HandleWrenchUpdatePackage(Package p, bool isServer)
	{
		try
		{
			int entityId = p.Reader.GetInt();
			string componentName = p.Reader.GetString();
			string memberName = p.Reader.GetString();
			byte typeCode = p.Reader.GetByte();
			object value = null;
			switch (typeCode)
			{
			case 0:
				value = p.Reader.GetInt();
				break;
			case 1:
				value = p.Reader.GetFloat();
				break;
			case 2:
				value = p.Reader.GetBool();
				break;
			case 3:
				value = p.Reader.GetString();
				break;
			case 4:
				value = new Vector3(p.Reader.GetFloat(), p.Reader.GetFloat(), p.Reader.GetFloat());
				break;
			}
			Entity block = null;
			if (EntityMaps.ContainsKey(entityId))
			{
				block = EntityMaps[entityId];
			}
			if (block != null)
			{
				Component component = null;
				foreach (Component component2 in block.Components)
				{
					if (component2.GetType().Name == componentName)
					{
						component = component2;
						break;
					}
				}
				if (component != null)
				{
					PropertyInfo runtimeProperty = component.GetType().GetRuntimeProperty(memberName);
					FieldInfo runtimeField = component.GetType().GetRuntimeField(memberName);
					try
					{
						if (runtimeProperty != null && runtimeProperty.CanWrite)
						{
							runtimeProperty.SetValue(component, value, null);
						}
						else if (runtimeField != null)
						{
							runtimeField.SetValue(component, value);
						}
					}
					catch (Exception ex)
					{
						Log.Warning("Wrench: Failed to set value " + memberName + " on " + componentName + ". " + ex.Message);
					}
				}
				else
				{
					Log.Warning($"Wrench: Component {componentName} not found on Entity {entityId}");
				}
			}
			if (!isServer)
			{
				return;
			}
			CommonLib.Net.SendToAllExcept(CommonLib.Net.MakePackage(PackageFlag.WrenchEntityUpdate, (NetDataWriter w) =>
			{
				w.Put(entityId);
				w.Put(componentName);
				w.Put(memberName);
				w.Put(typeCode);
				switch (typeCode)
				{
				case 0:
					w.Put((int)value);
					break;
				case 1:
					w.Put((float)value);
					break;
				case 2:
					w.Put((bool)value);
					break;
				case 3:
					w.Put((string)value);
					break;
				case 4:
				{
					Vector3 vector = (Vector3)value;
					w.Put(vector.X);
					w.Put(vector.Y);
					w.Put(vector.Z);
					break;
				}
				}
			}), p.FromClient, sendToNotReady: true);
		}
		catch (Exception ex2)
		{
			Log.Error("Error handling wrench update: " + ex2.Message);
		}
	}

	private Type IsTypeSupported(string name)
	{
		return TypeCache.FindType(name, skipSystemAssemblies: false, throwIfNotFound: false);
	}
}
