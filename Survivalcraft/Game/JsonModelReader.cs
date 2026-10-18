using System;
using System.Collections.Generic;
using System.IO;
using Engine;
using SimpleJson;

namespace Game;

public class JsonModelReader
{
	public static Dictionary<string, List<Vector3>> FacesDic;

	public static Dictionary<string, Vector3> NormalDic;

	public static Dictionary<string, List<int>> FacedirecDic;

	public static Dictionary<float, List<int>> TextureRotate;

	static JsonModelReader()
	{
		FacesDic = new Dictionary<string, List<Vector3>>();
		NormalDic = new Dictionary<string, Vector3>();
		FacedirecDic = new Dictionary<string, List<int>>();
		TextureRotate = new Dictionary<float, List<int>>();
		FacesDic.Add("north", new List<Vector3>
		{
			Vector3.UnitX,
			Vector3.Zero,
			Vector3.UnitY,
			new Vector3(1f, 1f, 0f)
		});
		FacesDic.Add("south", new List<Vector3>
		{
			new Vector3(1f, 0f, 1f),
			Vector3.UnitZ,
			new Vector3(0f, 1f, 1f),
			new Vector3(1f, 1f, 1f)
		});
		FacesDic.Add("east", new List<Vector3>
		{
			new Vector3(1f, 0f, 0f),
			new Vector3(1f, 0f, 1f),
			new Vector3(1f, 1f, 1f),
			new Vector3(1f, 1f, 0f)
		});
		FacesDic.Add("west", new List<Vector3>
		{
			Vector3.Zero,
			Vector3.UnitZ,
			new Vector3(0f, 1f, 1f),
			Vector3.UnitY
		});
		FacesDic.Add("up", new List<Vector3>
		{
			Vector3.UnitY,
			new Vector3(0f, 1f, 1f),
			Vector3.One,
			new Vector3(1f, 1f, 0f)
		});
		FacesDic.Add("down", new List<Vector3>
		{
			Vector3.Zero,
			Vector3.UnitZ,
			new Vector3(1f, 0f, 1f),
			new Vector3(1f, 0f, 0f)
		});
		NormalDic.Add("north", new Vector3(0f, 0f, -1f));
		NormalDic.Add("south", new Vector3(0f, 0f, 1f));
		NormalDic.Add("east", new Vector3(1f, 0f, 0f));
		NormalDic.Add("west", new Vector3(-1f, 0f, 0f));
		NormalDic.Add("up", new Vector3(0f, 1f, 0f));
		NormalDic.Add("down", new Vector3(0f, -1f, 0f));
		FacedirecDic.Add("north", new List<int> { 0, 2, 1, 0, 3, 2 });
		FacedirecDic.Add("west", new List<int> { 0, 2, 1, 0, 3, 2 });
		FacedirecDic.Add("up", new List<int> { 0, 2, 1, 0, 3, 2 });
		FacedirecDic.Add("south", new List<int> { 0, 1, 2, 0, 2, 3 });
		FacedirecDic.Add("east", new List<int> { 0, 1, 2, 0, 2, 3 });
		FacedirecDic.Add("down", new List<int> { 0, 1, 2, 0, 2, 3 });
		TextureRotate.Add(0f, new List<int> { 0, 3, 2, 3, 2, 1, 0, 1 });
		TextureRotate.Add(90f, new List<int> { 0, 1, 0, 3, 2, 3, 2, 1 });
		TextureRotate.Add(180f, new List<int> { 2, 1, 0, 1, 0, 3, 2, 3 });
		TextureRotate.Add(270f, new List<int> { 2, 3, 2, 1, 0, 1, 0, 3 });
	}

	public static float ObjConvertFloat(object obj)
	{
		if (obj is double)
		{
			return (float)(double)obj;
		}
		if (obj is long)
		{
			return (long)obj;
		}
		throw new Exception("错误的数据转换，不能将" + obj.GetType().Name + "转换为float");
	}

	public static JsonModel Load(Stream stream)
	{
		Dictionary<string, ObjModelReader.ObjMesh> dictionary = new Dictionary<string, ObjModelReader.ObjMesh>();
		Vector3 firstPersonOffset = Vector3.One;
		Vector3 firstPersonRotation = Vector3.Zero;
		Vector3 firstPersonScale = Vector3.One;
		Vector3 inHandOffset = Vector3.One;
		Vector3 inHandRotation = Vector3.Zero;
		Vector3 inHandScale = Vector3.One;
		string text = string.Empty;
		using (stream)
		{
			object obj = global::SimpleJson.SimpleJson.DeserializeObject(new StreamReader(stream).ReadToEnd());
			if (obj is JsonObject)
			{
				JsonObject jsonObject = obj as JsonObject;
				Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
				if (jsonObject.TryGetValue("display", out var value))
				{
					if ((value as JsonObject).TryGetValue("thirdperson_righthand", out var value2))
					{
						JsonObject obj2 = value2 as JsonObject;
						if (obj2.TryGetValue("rotation", out var value3))
						{
							JsonArray jsonArray = value3 as JsonArray;
							inHandRotation = new Vector3(ObjConvertFloat(jsonArray[0]), ObjConvertFloat(jsonArray[1]), ObjConvertFloat(jsonArray[2]));
						}
						if (obj2.TryGetValue("translation", out var value4))
						{
							JsonArray jsonArray2 = value4 as JsonArray;
							inHandOffset = new Vector3(ObjConvertFloat(jsonArray2[0]), ObjConvertFloat(jsonArray2[1]), ObjConvertFloat(jsonArray2[2]));
						}
						if (obj2.TryGetValue("scale", out var value5))
						{
							JsonArray jsonArray3 = value5 as JsonArray;
							inHandScale = new Vector3(ObjConvertFloat(jsonArray3[0]), ObjConvertFloat(jsonArray3[1]), ObjConvertFloat(jsonArray3[2]));
						}
					}
					if ((value as JsonObject).TryGetValue("firstperson_righthand", out var value6))
					{
						JsonObject obj3 = value6 as JsonObject;
						if (obj3.TryGetValue("rotation", out var value7))
						{
							JsonArray jsonArray4 = value7 as JsonArray;
							firstPersonRotation = new Vector3(ObjConvertFloat(jsonArray4[0]), ObjConvertFloat(jsonArray4[1]), ObjConvertFloat(jsonArray4[2]));
						}
						if (obj3.TryGetValue("translation", out var value8))
						{
							JsonArray jsonArray5 = value8 as JsonArray;
							firstPersonOffset = new Vector3(ObjConvertFloat(jsonArray5[0]), ObjConvertFloat(jsonArray5[1]), ObjConvertFloat(jsonArray5[2]));
						}
						if (obj3.TryGetValue("scale", out var value9))
						{
							JsonArray jsonArray6 = value9 as JsonArray;
							firstPersonScale = new Vector3(ObjConvertFloat(jsonArray6[0]), ObjConvertFloat(jsonArray6[1]), ObjConvertFloat(jsonArray6[2]));
						}
					}
				}
				if (jsonObject.TryGetValue("parent", out var value10))
				{
					text = value10 as string;
				}
				if (jsonObject.TryGetValue("textures", out var value11))
				{
					foreach (KeyValuePair<string, object> item in value11 as JsonObject)
					{
						dictionary2.Add(item.Key, (string)item.Value);
					}
				}
				if (jsonObject.TryGetValue("texture_size", out var value12))
				{
					JsonArray jsonArray7 = value12 as JsonArray;
					new Vector2(ObjConvertFloat(jsonArray7[0]), ObjConvertFloat(jsonArray7[1]));
				}
				if (jsonObject.TryGetValue("elements", out var value13))
				{
					JsonArray jsonArray8 = value13 as JsonArray;
					for (int i = 0; i < jsonArray8.Count; i++)
					{
						JsonObject obj4 = jsonArray8[i] as JsonObject;
						JsonArray jsonArray9 = obj4["from"] as JsonArray;
						JsonArray jsonArray10 = obj4["to"] as JsonArray;
						string text2 = "undefined";
						if (obj4.TryGetValue("name", out var value14))
						{
							text2 = value14 as string;
						}
						if (!dictionary.TryGetValue(text2, out var value15))
						{
							value15 = new ObjModelReader.ObjMesh(text2)
							{
								ElementIndex = i
							};
							dictionary.Add(text2, value15);
						}
						if (obj4.TryGetValue("rotation", out var value16))
						{
							JsonObject obj5 = value16 as JsonObject;
							_ = obj5["origin"];
							ObjConvertFloat(obj5["angle"]);
						}
						Vector3 vector = new Vector3(ObjConvertFloat(jsonArray9[0]), ObjConvertFloat(jsonArray9[1]), ObjConvertFloat(jsonArray9[2]));
						Vector3 vector2 = new Vector3(ObjConvertFloat(jsonArray10[0]), ObjConvertFloat(jsonArray10[1]), ObjConvertFloat(jsonArray10[2]));
						Matrix m = Matrix.CreateScale(vector2.X - vector.X, vector2.Y - vector.Y, vector2.Z - vector.Z) * Matrix.CreateTranslation(vector.X, vector.Y, vector.Z) * Matrix.CreateScale(0.0625f);
						if (!obj4.TryGetValue("faces", out var value17))
						{
							continue;
						}
						foreach (KeyValuePair<string, object> item2 in value17 as JsonObject)
						{
							ObjModelReader.ObjMesh objMesh = new ObjModelReader.ObjMesh(item2.Key);
							List<Vector3> list = FacesDic[item2.Key];
							JsonObject jsonObject2 = item2.Value as JsonObject;
							float key = 0f;
							string key2 = item2.Key;
							float[] array = new float[4];
							List<Vector2> list2 = new List<Vector2>();
							if (jsonObject2.TryGetValue("rotation", out var value18))
							{
								key = ObjConvertFloat(value18);
							}
							if (jsonObject2.TryGetValue("uv", out var value19))
							{
								JsonArray jsonArray11 = value19 as JsonArray;
								for (int j = 0; j < jsonArray11.Count; j++)
								{
									array[j] = ObjConvertFloat(jsonArray11[j]) / 16f;
								}
								_ = new Vector2(array[2] - array[0], array[3] - array[1]) / 2f + new Vector2(array[0], array[1]);
								list2.Add(new Vector2(array[TextureRotate[key][0]], array[TextureRotate[key][1]]));
								list2.Add(new Vector2(array[TextureRotate[key][2]], array[TextureRotate[key][3]]));
								list2.Add(new Vector2(array[TextureRotate[key][4]], array[TextureRotate[key][5]]));
								list2.Add(new Vector2(array[TextureRotate[key][6]], array[TextureRotate[key][7]]));
							}
							if (jsonObject2.TryGetValue("texture", out var value20))
							{
								string text3 = value20 as string;
								if (dictionary2.TryGetValue(text3.Substring(1), out var value21))
								{
									objMesh.TexturePath = value21;
								}
							}
							ObjModelReader.ObjPosition[] array2 = new ObjModelReader.ObjPosition[3];
							ObjModelReader.ObjTexCood[] array3 = new ObjModelReader.ObjTexCood[3];
							int index = FacedirecDic[key2][0];
							int index2 = FacedirecDic[key2][1];
							int index3 = FacedirecDic[key2][2];
							Vector3 vector3 = Vector3.Transform(list[index], m);
							Vector3 vector4 = Vector3.Transform(list[index2], m);
							Vector3 vector5 = Vector3.Transform(list[index3], m);
							array2[0] = new ObjModelReader.ObjPosition(vector3.X, vector3.Y, vector3.Z);
							array2[1] = new ObjModelReader.ObjPosition(vector4.X, vector4.Y, vector4.Z);
							array2[2] = new ObjModelReader.ObjPosition(vector5.X, vector5.Y, vector5.Z);
							Vector2 vector6 = list2[index];
							Vector2 vector7 = list2[index2];
							Vector2 vector8 = list2[index3];
							array3[0] = new ObjModelReader.ObjTexCood(vector6.X, vector6.Y);
							array3[1] = new ObjModelReader.ObjTexCood(vector7.X, vector7.Y);
							array3[2] = new ObjModelReader.ObjTexCood(vector8.X, vector8.Y);
							int count = objMesh.Vertices.Count;
							objMesh.Indices.Add((ushort)count++);
							objMesh.Indices.Add((ushort)count++);
							objMesh.Indices.Add((ushort)count++);
							objMesh.Vertices.Add(new ObjModelReader.ObjVertex
							{
								position = array2[0],
								objNormal = new ObjModelReader.ObjNormal(0f, 0f, 0f),
								texCood = array3[0]
							});
							objMesh.Vertices.Add(new ObjModelReader.ObjVertex
							{
								position = array2[1],
								objNormal = new ObjModelReader.ObjNormal(0f, 0f, 0f),
								texCood = array3[1]
							});
							objMesh.Vertices.Add(new ObjModelReader.ObjVertex
							{
								position = array2[2],
								objNormal = new ObjModelReader.ObjNormal(0f, 0f, 0f),
								texCood = array3[2]
							});
							index = FacedirecDic[key2][3];
							index2 = FacedirecDic[key2][4];
							index3 = FacedirecDic[key2][5];
							vector3 = Vector3.Transform(list[index], m);
							vector4 = Vector3.Transform(list[index2], m);
							vector5 = Vector3.Transform(list[index3], m);
							array2[0] = new ObjModelReader.ObjPosition(vector3.X, vector3.Y, vector3.Z);
							array2[1] = new ObjModelReader.ObjPosition(vector4.X, vector4.Y, vector4.Z);
							array2[2] = new ObjModelReader.ObjPosition(vector5.X, vector5.Y, vector5.Z);
							vector6 = list2[index];
							vector7 = list2[index2];
							vector8 = list2[index3];
							array3[0] = new ObjModelReader.ObjTexCood(vector6.X, vector6.Y);
							array3[1] = new ObjModelReader.ObjTexCood(vector7.X, vector7.Y);
							array3[2] = new ObjModelReader.ObjTexCood(vector8.X, vector8.Y);
							objMesh.Indices.Add((ushort)count++);
							objMesh.Indices.Add((ushort)count++);
							objMesh.Indices.Add((ushort)count++);
							objMesh.Vertices.Add(new ObjModelReader.ObjVertex
							{
								position = array2[0],
								objNormal = new ObjModelReader.ObjNormal(0f, 0f, 0f),
								texCood = array3[0]
							});
							objMesh.Vertices.Add(new ObjModelReader.ObjVertex
							{
								position = array2[1],
								objNormal = new ObjModelReader.ObjNormal(0f, 0f, 0f),
								texCood = array3[1]
							});
							objMesh.Vertices.Add(new ObjModelReader.ObjVertex
							{
								position = array2[2],
								objNormal = new ObjModelReader.ObjNormal(0f, 0f, 0f),
								texCood = array3[2]
							});
							value15.ChildMeshes.Add(objMesh);
						}
					}
				}
				List<ObjModelReader.ObjMesh> list3 = new List<ObjModelReader.ObjMesh>();
				foreach (KeyValuePair<string, ObjModelReader.ObjMesh> item3 in dictionary)
				{
					list3.Add(item3.Value);
				}
				if (jsonObject.TryGetValue("groups", out var value22))
				{
					Dictionary<string, ObjModelReader.ObjMesh> dictionary3 = new Dictionary<string, ObjModelReader.ObjMesh>();
					JsonArray jsonArray12 = value22 as JsonArray;
					for (int k = 0; k < jsonArray12.Count; k++)
					{
						JsonObject obj6 = jsonArray12[k] as JsonObject;
						string text4 = k.ToString();
						if (obj6.TryGetValue("name", out var value23))
						{
							text4 = (string)value23;
						}
						ObjModelReader.ObjMesh objMesh2 = new ObjModelReader.ObjMesh(text4);
						if (obj6.TryGetValue("oringin", out var value24))
						{
							JsonArray jsonArray13 = value24 as JsonArray;
							Vector3 vector9 = new Vector3(ObjConvertFloat(jsonArray13[0]), ObjConvertFloat(jsonArray13[1]), ObjConvertFloat(jsonArray13[2])) / 16f;
							objMesh2.MeshMatrix = Matrix.CreateTranslation(vector9.X, vector9.Y, vector9.Z);
						}
						if (obj6.TryGetValue("children", out var value25))
						{
							JsonArray jsonArray14 = value25 as JsonArray;
							for (int l = 0; l < jsonArray14.Count; l++)
							{
								int eleindex = (int)ObjConvertFloat(jsonArray14[l]);
								objMesh2.ChildMeshes.Add(list3.Find((ObjModelReader.ObjMesh xp) => xp.ElementIndex == eleindex));
							}
						}
						dictionary3.Add(text4, objMesh2);
					}
					JsonModel jsonModel = ObjModelReader.ObjMeshesToModel<JsonModel>(dictionary3);
					if (!string.IsNullOrEmpty(text))
					{
						try
						{
							jsonModel.ParentModel = ContentManager.Get<JsonModel>(text);
						}
						catch
						{
						}
					}
					jsonModel.InHandScale = inHandScale;
					jsonModel.InHandOffset = inHandOffset;
					jsonModel.InHandRotation = inHandRotation;
					jsonModel.FirstPersonOffset = firstPersonOffset;
					jsonModel.FirstPersonScale = firstPersonScale;
					jsonModel.FirstPersonRotation = firstPersonRotation;
					return jsonModel;
				}
			}
		}
		JsonModel jsonModel2 = ObjModelReader.ObjMeshesToModel<JsonModel>(dictionary);
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				jsonModel2.ParentModel = ContentManager.Get<JsonModel>(text);
			}
			catch
			{
			}
		}
		jsonModel2.InHandScale = inHandScale;
		jsonModel2.InHandOffset = inHandOffset;
		jsonModel2.InHandRotation = inHandRotation;
		jsonModel2.FirstPersonOffset = firstPersonOffset;
		jsonModel2.FirstPersonScale = firstPersonScale;
		jsonModel2.FirstPersonRotation = firstPersonRotation;
		return jsonModel2;
	}
}
