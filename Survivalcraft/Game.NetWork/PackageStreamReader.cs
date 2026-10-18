using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using TemplatesDatabase;
using XmlUtilities;

namespace Game.NetWork;

public class PackageStreamReader : BinaryReader
{
	public PackageStreamReader(byte[] CompressedData)
		: base(new MemoryStream())
	{
		byte[] array = CommonLib.Decompress(CompressedData);
		BaseStream.Write(array, 0, array.Length);
		BaseStream.Position = 0L;
	}

	public float ReadSmallFloat()
	{
		return HalfFloat.HalfToFloat(ReadBytes(2));
	}

	public float? ReadSingleNullable()
	{
		if (ReadBoolean())
		{
			return ReadSingle();
		}
		return null;
	}

	public int? ReadIntNullable()
	{
		if (ReadBoolean())
		{
			return ReadInt32();
		}
		return null;
	}

	public Vector4 ReadVector4()
	{
		return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
	}

	public Vector4? ReadVector4Nullable()
	{
		if (ReadBoolean())
		{
			return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
		}
		return null;
	}

	public Vector3 ReadVector3()
	{
		return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
	}

	public Vector3 ReadSmallVec3()
	{
		return new Vector3(ReadSmallFloat(), ReadSmallFloat(), ReadSmallFloat());
	}

	public Vector3? ReadVector3Nullable()
	{
		if (ReadBoolean())
		{
			return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
		}
		return null;
	}

	public Vector3? ReadSmallVector3Nullable()
	{
		if (ReadBoolean())
		{
			return new Vector3(ReadSmallFloat(), ReadSmallFloat(), ReadSmallFloat());
		}
		return null;
	}

	public Vector2 ReadVector2()
	{
		return new Vector2(ReadSingle(), ReadSingle());
	}

	public Vector2 ReadSmallVec2()
	{
		return new Vector2(ReadSmallFloat(), ReadSmallFloat());
	}

	public Vector2? ReadVector2Nullable()
	{
		if (ReadBoolean())
		{
			return new Vector2(ReadSingle(), ReadSingle());
		}
		return null;
	}

	public Guid ReadGuid()
	{
		byte count = ReadByte();
		return new Guid(ReadBytes(count));
	}

	public Point3 ReadPoint3(bool useByteY = false)
	{
		return new Point3(ReadInt32(), useByteY ? ReadByte() : ReadInt32(), ReadInt32());
	}

	public Point2 ReadPoint2()
	{
		return new Point2(ReadInt32(), ReadInt32());
	}

	public Quaternion ReadQuaternion()
	{
		return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
	}

	public Quaternion ReadSmallQuaternion()
	{
		return new Quaternion(ReadSmallFloat(), ReadSmallFloat(), ReadSmallFloat(), ReadSmallFloat());
	}

	public Ray3 ReadRay3()
	{
		return new Ray3(ReadVector3(), ReadVector3());
	}

	public Ray3? ReadRay3Nullable()
	{
		if (ReadBoolean())
		{
			return ReadRay3();
		}
		return null;
	}

	public Point3 ReadBlockPoint()
	{
		return new Point3(ReadInt32(), ReadByte(), ReadInt32());
	}

	public CellFace ReadCellFace()
	{
		Point3 path = ReadBlockPoint();
		return new CellFace(path.X, path.Y, path.Z, ReadByte());
	}

	public TerrainRaycastResult ReadTerrainRaycastResult()
	{
		return new TerrainRaycastResult
		{
			Ray = ReadRay3(),
			Value = ReadInt32(),
			CollisionBoxIndex = ReadInt32(),
			Distance = ReadSingle(),
			CellFace = ReadCellFace()
		};
	}

	public TerrainRaycastResult? ReadTerrainRaycastResultNullable()
	{
		if (ReadBoolean())
		{
			return ReadTerrainRaycastResult();
		}
		return null;
	}

	public ValuesDictionary ReadValuesDictionary()
	{
		string data = ReadString();
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		XElement overridesNode = XmlUtils.LoadXmlFromString(data, throwOnError: true);
		valuesDictionary.ApplyOverrides(overridesNode);
		return valuesDictionary;
	}

	public Color ReadColor()
	{
		return new Color(ReadByte(), ReadByte(), ReadByte(), ReadByte());
	}

	public ValuesDictionary ReadPlayerData()
	{
		return ReadValuesDictionary();
	}

	public T ReadEnum<T>() where T : struct
	{
		return (T)Enum.ToObject(typeof(T), (int)ReadByte());
	}

	public XElement ReadXElement()
	{
		return XmlUtils.LoadXmlFromString(ReadString(), throwOnError: true);
	}

	public List<Entity> ReadEntityLoadList()
	{
		XElement entitiesNode = ReadXElement();
		EntityDataList entityDataList = new EntityDataList(GameManager.Project.GameDatabase, entitiesNode, ignoreInvalidEntities: false);
		return ((ProjectNet)GameManager.Project).LoadEntitiesAll(entityDataList);
	}

	public Texture2D ReadTexture2D()
	{
		int num = ReadInt32();
		int num2 = ReadInt32();
		Image image = new Image(num, num2);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				image.SetPixel(i, j, ReadColor());
			}
		}
		return Texture2D.Load(image);
	}

	public Matrix ReadMatrix()
	{
		byte path = ReadByte();
		return Utilities.ArrayToStructure<Matrix>(ReadBytes(path));
	}

	public Matrix? ReadMatrixNullable()
	{
		if (ReadBoolean())
		{
			return ReadMatrix();
		}
		return null;
	}
}
