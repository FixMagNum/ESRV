using System;
using System.Collections.Generic;
using System.IO;
using Engine;
using Engine.Graphics;

namespace Game;

public class ObjModelReader
{
	public struct ObjPosition
	{
		public float x;

		public float y;

		public float z;

		public ObjPosition(string x_, string y_, string z_)
		{
			x = float.Parse(x_);
			y = float.Parse(y_);
			z = float.Parse(z_);
		}

		public ObjPosition(float x_, float y_, float z_)
		{
			x = x_;
			y = y_;
			z = z_;
		}
	}

	public struct ObjVertex
	{
		public ObjPosition position;

		public ObjNormal objNormal;

		public ObjTexCood texCood;
	}

	public struct ObjNormal
	{
		public float x;

		public float y;

		public float z;

		public ObjNormal(string x_, string y_, string z_)
		{
			x = float.Parse(x_);
			y = float.Parse(y_);
			z = float.Parse(z_);
		}

		public ObjNormal(float x_, float y_, float z_)
		{
			x = x_;
			y = y_;
			z = z_;
		}
	}

	public struct ObjTexCood
	{
		public float tx;

		public float ty;

		public ObjTexCood(string tx_, string ty_)
		{
			tx = float.Parse(tx_);
			ty = float.Parse(ty_);
		}

		public ObjTexCood(float tx_, float ty_)
		{
			tx = tx_;
			ty = ty_;
		}
	}

	public class ObjMesh
	{
		public int ElementIndex;

		public DynamicArray<ObjVertex> Vertices = new DynamicArray<ObjVertex>();

		public DynamicArray<ushort> Indices = new DynamicArray<ushort>();

		public string TexturePath = "Textures/NoneTexture";

		public string MeshName;

		public Matrix? MeshMatrix;

		public List<ObjMesh> ChildMeshes = new List<ObjMesh>();

		public ObjMesh(string meshname)
		{
			MeshName = meshname;
		}

		public BoundingBox CalculateBoundingBox()
		{
			List<Vector3> list = new List<Vector3>();
			for (int i = 0; i < Vertices.Count; i++)
			{
				list.Add(new Vector3(Vertices[i].position.x, Vertices[i].position.y, Vertices[i].position.z));
			}
			return new BoundingBox(list);
		}
	}

	public static Dictionary<int, List<int>> FaceMap;

	static ObjModelReader()
	{
		FaceMap = new Dictionary<int, List<int>>();
		FaceMap.Add(4, new List<int> { 0, 2, 1 });
		FaceMap.Add(5, new List<int> { 0, 2, 1 });
		FaceMap.Add(2, new List<int> { 0, 2, 1 });
		FaceMap.Add(3, new List<int> { 0, 2, 1 });
		FaceMap.Add(0, new List<int> { 0, 2, 1 });
		FaceMap.Add(1, new List<int> { 0, 2, 1 });
	}

	public static ObjModel Load(Stream stream)
	{
		Dictionary<string, ObjMesh> dictionary = new Dictionary<string, ObjMesh>();
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		List<ObjPosition> list = new List<ObjPosition>();
		List<ObjTexCood> list2 = new List<ObjTexCood>();
		List<ObjNormal> list3 = new List<ObjNormal>();
		using (stream)
		{
			StreamReader streamReader = new StreamReader(stream);
			ObjMesh objMesh = null;
			string value = null;
			while (!streamReader.EndOfStream)
			{
				string[] array = streamReader.ReadLine().Split(new char[2] { '\t', ' ' }, StringSplitOptions.None);
				switch (array[0])
				{
				case "mtllib":
					dictionary2 = ContentManager.Get<MtllibStruct>(array[1]).TexturePaths;
					break;
				case "o":
				{
					if (dictionary.TryGetValue(array[1], out var value2))
					{
						objMesh = value2;
						break;
					}
					objMesh = new ObjMesh(array[1]);
					dictionary.Add(array[1], objMesh);
					break;
				}
				case "v":
					list.Add(new ObjPosition(array[1], array[2], array[3]));
					break;
				case "vt":
					list2.Add(new ObjTexCood(array[1], array[2]));
					break;
				case "vn":
					list3.Add(new ObjNormal(array[1], array[2], array[3]));
					break;
				case "usemtl":
					if (!dictionary2.TryGetValue(array[1], out value))
					{
					}
					break;
				case "f":
				{
					if (string.IsNullOrEmpty(value))
					{
						value = "Textures/NoneTexture";
					}
					objMesh.TexturePath = value;
					if (array.Length - 1 != 3)
					{
						throw new Exception("模型必须为三角面");
					}
					int num = 0;
					int count = objMesh.Vertices.Count;
					while (++num < array.Length)
					{
						string[] array2 = array[num].Split(new char[1] { '/' }, StringSplitOptions.None);
						if (array2.Length != 3)
						{
							throw new Exception("面参数错误");
						}
						int num2 = int.Parse(array2[0]);
						int num3 = int.Parse(array2[1]);
						int num4 = int.Parse(array2[2]);
						ObjPosition position = list[num2 - 1];
						ObjTexCood texCood = list2[num3 - 1];
						ObjNormal objNormal = list3[num4 - 1];
						int key = CellFace.Vector3ToFace(new Vector3(objNormal.x, objNormal.y, objNormal.z));
						objMesh.Indices.Add((ushort)(count + FaceMap[key][num - 1]));
						objMesh.Vertices.Add(new ObjVertex
						{
							position = position,
							objNormal = objNormal,
							texCood = texCood
						});
					}
					break;
				}
				}
			}
		}
		return ObjMeshesToModel<ObjModel>(dictionary);
	}

	public static void AppendMesh(Model model, ModelBone rootBone, string texturepath, ObjMesh objMesh)
	{
		ModelBone modelBone = model.NewBone(objMesh.MeshName, objMesh.MeshMatrix.HasValue ? objMesh.MeshMatrix.Value : Matrix.Identity, rootBone);
		if (objMesh.Vertices.Count > 0)
		{
			ModelMesh modelMesh = model.NewMesh(objMesh.MeshName, modelBone, objMesh.CalculateBoundingBox());
			VertexBuffer vertexBuffer = new VertexBuffer(new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position), new VertexElement(12, VertexElementFormat.Vector3, VertexElementSemantic.Normal), new VertexElement(24, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate)), objMesh.Vertices.Count);
			MemoryStream memoryStream = new MemoryStream();
			MemoryStream memoryStream2 = new MemoryStream();
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			BinaryWriter binaryWriter2 = new BinaryWriter(memoryStream2);
			for (int i = 0; i < objMesh.Vertices.Count; i++)
			{
				ObjVertex objVertex = objMesh.Vertices[i];
				binaryWriter.Write(objVertex.position.x);
				binaryWriter.Write(objVertex.position.y);
				binaryWriter.Write(objVertex.position.z);
				binaryWriter.Write(objVertex.objNormal.x);
				binaryWriter.Write(objVertex.objNormal.y);
				binaryWriter.Write(objVertex.objNormal.z);
				binaryWriter.Write(objVertex.texCood.tx);
				binaryWriter.Write(objVertex.texCood.ty);
			}
			for (int j = 0; j < objMesh.Indices.Count; j++)
			{
				binaryWriter2.Write(objMesh.Indices[j]);
			}
			byte[] tag = memoryStream.ToArray();
			byte[] tag2 = memoryStream2.ToArray();
			memoryStream.Close();
			memoryStream2.Close();
			vertexBuffer.SetData(objMesh.Vertices.Array, 0, objMesh.Vertices.Count);
			vertexBuffer.Tag = tag;
			IndexBuffer indexBuffer = new IndexBuffer(IndexFormat.SixteenBits, objMesh.Indices.Count);
			indexBuffer.SetData(objMesh.Indices.Array, 0, objMesh.Indices.Count);
			indexBuffer.Tag = tag2;
			modelMesh.NewMeshPart(vertexBuffer, indexBuffer, 0, objMesh.Indices.Count, objMesh.CalculateBoundingBox()).TexturePath = objMesh.TexturePath;
			model.AddMesh(modelMesh);
		}
		foreach (ObjMesh childMesh in objMesh.ChildMeshes)
		{
			AppendMesh(model, modelBone, childMesh.TexturePath, childMesh);
		}
	}

	public static T ObjMeshesToModel<T>(Dictionary<string, ObjMesh> Meshes) where T : class
	{
		Type typeFromHandle = typeof(T);
		if (!typeFromHandle.IsSubclassOf(typeof(Model)))
		{
			throw new Exception("不能将" + typeFromHandle.Name + "转换为Model类型");
		}
		object obj = Activator.CreateInstance(typeFromHandle);
		Model model = obj as Model;
		ModelBone rootBone = model.NewBone("Object", Matrix.Identity, null);
		foreach (KeyValuePair<string, ObjMesh> Mesh in Meshes)
		{
			AppendMesh(model, rootBone, Mesh.Key, Mesh.Value);
		}
		return obj as T;
	}
}
