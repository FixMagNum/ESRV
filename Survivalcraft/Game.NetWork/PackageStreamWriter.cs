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

public class PackageStreamWriter : BinaryWriter
{
	public ProjectNet Project;

	public bool IsServer;

	public PackageStreamWriter()
		: base(new MemoryStream())
	{
	}

	public byte[] Data()
	{
		BaseStream.Seek(0L, SeekOrigin.Begin);
		return CommonLib.Compress(BaseStream);
	}

	public void WriteSmallFloat(float f)
	{
		Write(HalfFloat.FloatToHalf(f));
	}

	public void WriteNullable(float? v)
	{
		Write(v.HasValue);
		if (v.HasValue)
		{
			Write(v.Value);
		}
	}

	public void WriteNullable(int? v)
	{
		Write(v.HasValue);
		if (v.HasValue)
		{
			Write(v.Value);
		}
	}

	public void Write(Color color)
	{
		Write(color.R);
		Write(color.G);
		Write(color.B);
		Write(color.A);
	}

	public void Write(Vector4? vector)
	{
		Write(vector.HasValue);
		if (vector.HasValue)
		{
			Write(vector.Value);
		}
	}

	public void Write(Vector4 vector)
	{
		Write(vector.X);
		Write(vector.Y);
		Write(vector.Z);
		Write(vector.W);
	}

	public void Write(Vector3 vector)
	{
		Write(vector.X);
		Write(vector.Y);
		Write(vector.Z);
	}

	public void WriteSmallVec3(Vector3 vector)
	{
		WriteSmallFloat(vector.X);
		WriteSmallFloat(vector.Y);
		WriteSmallFloat(vector.Z);
	}

	public void Write(Vector3? vector)
	{
		Write(vector.HasValue);
		if (vector.HasValue)
		{
			Write(vector.Value);
		}
	}

	public void WriteSmallVector3Nuallable(Vector3? vector)
	{
		Write(vector.HasValue);
		if (vector.HasValue)
		{
			WriteSmallVec3(vector.Value);
		}
	}

	public void Write(Vector2 vector)
	{
		Write(vector.X);
		Write(vector.Y);
	}

	public void WriteSmallVec2(Vector2 vector)
	{
		WriteSmallFloat(vector.X);
		WriteSmallFloat(vector.Y);
	}

	public void Write(Vector2? vector)
	{
		Write(vector.HasValue);
		if (vector.HasValue)
		{
			Write(vector.Value);
		}
	}

	public void Write(Guid guid)
	{
		byte[] array = guid.ToByteArray();
		Write((byte)array.Length);
		Write(array);
	}

	public void Write(Point3 point, bool useByteY = false)
	{
		Write(point.X);
		if (useByteY)
		{
			Write((byte)point.Y);
		}
		else
		{
			Write(point.Y);
		}
		Write(point.Z);
	}

	public void Write(Point2 point)
	{
		Write(point.X);
		Write(point.Y);
	}

	public void Write(Quaternion quaternion)
	{
		Write(quaternion.X);
		Write(quaternion.Y);
		Write(quaternion.Z);
		Write(quaternion.W);
	}

	public void WriteSmallQuaternion(Quaternion quaternion)
	{
		WriteSmallFloat(quaternion.X);
		WriteSmallFloat(quaternion.Y);
		WriteSmallFloat(quaternion.Z);
		WriteSmallFloat(quaternion.W);
	}

	public void Write(Ray3 ray)
	{
		Write(ray.Position);
		Write(ray.Direction);
	}

	public void Write(Ray3? ray)
	{
		Write(ray.HasValue);
		if (ray.HasValue)
		{
			Write(ray.Value);
		}
	}

	public void WriteBlockPoint(Point3 point)
	{
		Write(point.X);
		Write((byte)point.Y);
		Write(point.Z);
	}

	public void Write(CellFace cellFace)
	{
		WriteBlockPoint(cellFace.Point);
		Write((byte)cellFace.Face);
	}

	public void Write(TerrainRaycastResult raycastResult)
	{
		Write(raycastResult.Ray);
		Write(raycastResult.Value);
		Write(raycastResult.CollisionBoxIndex);
		Write(raycastResult.Distance);
		Write(raycastResult.CellFace);
	}

	public void Write(TerrainRaycastResult? raycastResult)
	{
		Write(raycastResult.HasValue);
		if (raycastResult.HasValue)
		{
			Write(raycastResult.Value);
		}
	}

	public void Write(ValuesDictionary vd)
	{
		XElement node = new XElement("Values");
		vd.Save(node);
		string value = XmlUtils.SaveXmlToString(node, throwOnError: true);
		Write(value);
	}

	public void Write(PlayerData playerData)
	{
		ValuesDictionary path = new ValuesDictionary();
		playerData.Save(path);
		Write(path);
	}

	public void WriteEnum<T>(T obj) where T : struct
	{
		byte value = Convert.ToByte(obj);
		Write(value);
	}

	public void Write(XElement xElement)
	{
		string value = XmlUtils.SaveXmlToString(xElement, throwOnError: true);
		Write(value);
	}

	public void WriteEntityLoadList(List<Entity> entities)
	{
		XElement xElement = new XElement("Root");
		foreach (EntityData entitiesDatum in ((ProjectNet)GameManager.Project).SaveEntitiesAll(entities).EntitiesData)
		{
			XElement xElement2 = new XElement("Entity");
			entitiesDatum.Save(xElement2);
			xElement.Add(xElement2);
		}
		Write(xElement);
	}

	public void Write(Texture2D texture)
	{
		RenderTarget2D renderTarget2D = new RenderTarget2D(texture.Width, texture.Height, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
		RenderTarget2D renderTarget = Display.RenderTarget;
		Display.RenderTarget = renderTarget2D;
		Display.Clear(Color.Transparent);
		PrimitivesRenderer2D primitivesRenderer2D = new PrimitivesRenderer2D();
		primitivesRenderer2D.TexturedBatch(texture, useAlphaTest: true).QueueQuad(Vector2.Zero, new Vector2(texture.Width, texture.Height), 1f, Vector2.Zero, Vector2.One, Color.White);
		primitivesRenderer2D.Flush();
		Display.RenderTarget = renderTarget;
		Image image = new Image(texture.Width, texture.Height);
		renderTarget2D.GetData(image.Pixels, 0, new Engine.Rectangle(0, 0, texture.Width, texture.Height));
		Write(image.Width);
		Write(image.Height);
		for (int i = 0; i < image.Pixels.Length; i++)
		{
			Write(image.Pixels[i]);
		}
	}

	public void Write(Matrix matrix)
	{
		byte[] path = Utilities.StructureToArray(matrix);
		Write((byte)path.Length);
		Write(path);
	}

	public void Write(Matrix? matrix)
	{
		if (matrix.HasValue)
		{
			Write(value: true);
			Write(matrix.Value);
		}
		else
		{
			Write(value: false);
		}
	}
}
