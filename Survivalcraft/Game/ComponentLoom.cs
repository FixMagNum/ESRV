using System;
using System.IO;
using Engine;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentLoom : ComponentInventoryBase, IDrawable, IUpdateable
{
	public ComponentBlockEntity m_blockEntity;

	public SubsystemTerrain m_subsystemTerrain;

	public PrimitivesRenderer3D m_primitivesRenderer;

	public int SavedBrushSize;

	public Color SavedEditColor;

	public float SavedLightRotation;

	public bool SavedNoShadows;

	public bool SavedIsPaintMode;

	public Color[] SavedTextureData;

	public int SavedTextureWidth;

	public int SavedTextureHeight;

	public float SavedCameraZoom;

	public Vector2 SavedCameraPan;

	public bool SavedShowSkin;

	public string SavedSkinName;

	public float SavedCameraRotation = (float)Math.PI;

	public float SavedCameraRotationVertical;

	public int[] DrawOrders => new int[1] { 100 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int GetSlotCapacity(int slotIndex, int value)
	{
		return 1;
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_blockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		SavedBrushSize = valuesDictionary.GetValue("BrushSize", 1);
		SavedEditColor = valuesDictionary.GetValue("EditColor", Color.Red);
		SavedLightRotation = valuesDictionary.GetValue("LightRot", 0f);
		SavedNoShadows = valuesDictionary.GetValue("NoShadows", defaultValue: false);
		SavedIsPaintMode = valuesDictionary.GetValue("IsPaintMode", defaultValue: false);
		SavedCameraZoom = valuesDictionary.GetValue("CamZoom", 1f);
		SavedCameraPan = valuesDictionary.GetValue("CamPan", Vector2.Zero);
		SavedShowSkin = valuesDictionary.GetValue("ShowSkin", defaultValue: false);
		SavedSkinName = valuesDictionary.GetValue<string>("SkinName", null);
		SavedCameraRotation = valuesDictionary.GetValue("CamRot", (float)Math.PI);
		SavedCameraRotationVertical = valuesDictionary.GetValue("CamRotV", 0f);
		string value = valuesDictionary.GetValue<string>("TexData", null);
		if (string.IsNullOrEmpty(value))
		{
			return;
		}
		SavedTextureWidth = valuesDictionary.GetValue("TexW", 0);
		SavedTextureHeight = valuesDictionary.GetValue("TexH", 0);
		try
		{
			byte[] array = Convert.FromBase64String(value);
			SavedTextureData = new Color[SavedTextureWidth * SavedTextureHeight];
			for (int i = 0; i < SavedTextureData.Length; i++)
			{
				SavedTextureData[i] = new Color(array[i * 4], array[i * 4 + 1], array[i * 4 + 2], array[i * 4 + 3]);
			}
		}
		catch (Exception)
		{
			SavedTextureData = null;
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (SlotsCount != 0 && GetSlotCount(0) != 0)
		{
			Point3 coordinates = m_blockEntity.Coordinates;
			float num = m_subsystemTerrain.Terrain.GetCellLight(coordinates.X, coordinates.Y, coordinates.Z);
			Vector3 position = new Vector3((float)coordinates.X + 0.5f, (float)coordinates.Y + 0.85f, (float)coordinates.Z + 0.5f);
			Matrix matrix = Matrix.CreateRotationY((float)LoomBlock.GetRotation(Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z))) * (float)Math.PI / 2f) * Matrix.CreateTranslation(position);
			int slotValue = GetSlotValue(0);
			BlocksManager.Blocks[Terrain.ExtractContents(slotValue)].DrawBlock(environmentData: new DrawBlockEnvironmentData
			{
				SubsystemTerrain = m_subsystemTerrain,
				Light = (int)num,
				InWorldMatrix = matrix
			}, primitivesRenderer: m_primitivesRenderer, value: slotValue, color: Color.White, size: 0.5f, matrix: ref matrix);
			m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
		}
	}

	public void Update(float dt)
	{
	}

	public ComponentLoom()
	{
		SavedCameraZoom = 1f;
		SavedCameraPan = Vector2.Zero;
		SavedBrushSize = 1;
		SavedEditColor = Color.Red;
		m_primitivesRenderer = new PrimitivesRenderer3D();
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		base.Save(valuesDictionary, entityToIdMap);
		valuesDictionary.SetValue("BrushSize", SavedBrushSize);
		valuesDictionary.SetValue("EditColor", SavedEditColor);
		valuesDictionary.SetValue("LightRot", SavedLightRotation);
		valuesDictionary.SetValue("NoShadows", SavedNoShadows);
		valuesDictionary.SetValue("IsPaintMode", SavedIsPaintMode);
		valuesDictionary.SetValue("CamZoom", SavedCameraZoom);
		valuesDictionary.SetValue("CamPan", SavedCameraPan);
		valuesDictionary.SetValue("ShowSkin", SavedShowSkin);
		valuesDictionary.SetValue("CamRot", SavedCameraRotation);
		valuesDictionary.SetValue("CamRotV", SavedCameraRotationVertical);
		if (SavedSkinName != null)
		{
			valuesDictionary.SetValue("SkinName", SavedSkinName);
		}
		if (SavedTextureData != null)
		{
			valuesDictionary.SetValue("TexW", SavedTextureWidth);
			valuesDictionary.SetValue("TexH", SavedTextureHeight);
			byte[] array = new byte[SavedTextureData.Length * 4];
			for (int i = 0; i < SavedTextureData.Length; i++)
			{
				array[i * 4] = SavedTextureData[i].R;
				array[i * 4 + 1] = SavedTextureData[i].G;
				array[i * 4 + 2] = SavedTextureData[i].B;
				array[i * 4 + 3] = SavedTextureData[i].A;
			}
			valuesDictionary.SetValue("TexData", Convert.ToBase64String(array));
		}
	}

	public void SafeguardDesign()
	{
		if (SavedTextureData == null || SavedTextureWidth <= 0 || SavedTextureHeight <= 0)
		{
			return;
		}
		try
		{
			string text = "Unknown";
			int slotValue = GetSlotValue(0);
			if (Terrain.ExtractContents(slotValue) != 0 && BlocksManager.Blocks[Terrain.ExtractContents(slotValue)] is ClothingBlock clothingBlock)
			{
				text = clothingBlock.GetClothingData(Terrain.ExtractData(slotValue)).DisplayName;
				if (text.StartsWith("["))
				{
					text = "Item_" + clothingBlock.GetClothingData(Terrain.ExtractData(slotValue)).Index;
				}
			}
			string text2 = "AutoSave_" + text + "_" + DateTime.Now.Ticks;
			string text3 = "app:/LoomBackups";
			if (!Storage.DirectoryExists(text3))
			{
				Storage.CreateDirectory(text3);
			}
			Image image = new Image(SavedTextureWidth, SavedTextureHeight);
			for (int i = 0; i < SavedTextureData.Length; i++)
			{
				int x = i % SavedTextureWidth;
				int y = i / SavedTextureWidth;
				image.SetPixel(x, y, SavedTextureData[i]);
			}
			string text4 = Storage.CombinePaths(text3, text2 + ".png");
			using (Stream stream = Storage.OpenFile(text4, OpenFileMode.Create))
			{
				Image.Save(image, stream, ImageFileFormat.Png, saveAlpha: true);
			}
			Log.Information("Loom destroyed! Design emergency saved to: " + text4);
		}
		catch (Exception ex)
		{
			Log.Error("Failed to emergency save loom design: " + ex.Message);
		}
	}

	public void AddSlotItem(int slotIndex, int value, int count)
	{
		m_slots[slotIndex].Value = value;
		m_slots[slotIndex].Count = count;
		OnSlotChange(slotIndex);
		Log.Information($"[ComponentLoom] Slot {slotIndex} manually updated to Value: {value}");
	}
}
