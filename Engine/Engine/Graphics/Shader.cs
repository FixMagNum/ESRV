using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Engine.Graphics;

public class Shader : GraphicsResource
{
	internal struct ShaderAttributeData
	{
		public string Semantic;

		public int Location;
	}

	internal struct VertexAttributeData
	{
		public int Size;

		public bool Normalize;

		public int Offset;
	}

	internal int m_program;

	internal int m_vertexShader;

	internal int m_pixelShader;

	internal Dictionary<VertexDeclaration, VertexAttributeData[]> m_vertexAttributeDataByDeclaration = new Dictionary<VertexDeclaration, VertexAttributeData[]>();

	internal List<ShaderAttributeData> m_shaderAttributeData = new List<ShaderAttributeData>();

	private ShaderParameter m_glymulParameter;

	private Dictionary<string, ShaderParameter> m_parametersByName;

	internal ShaderParameter[] m_parameters;

	private string m_vertexShaderCode;

	private string m_pixelShaderCode;

	private ShaderMacro[] m_shaderMacros;

	public string DebugName
	{
		get
		{
			return string.Empty;
		}
		set
		{
		}
	}

	public object Tag { get; set; }

	public ReadOnlyList<ShaderParameter> Parameters => new ReadOnlyList<ShaderParameter>(m_parameters);

	public Shader(string vertexShaderCode, string pixelShaderCode, params ShaderMacro[] shaderMacros)
	{
		try
		{
			InitializeShader(vertexShaderCode, pixelShaderCode, shaderMacros);
			CompileShaders();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		DeleteShaders();
	}

	internal void PrepareForDrawing()
	{
		m_glymulParameter.SetValue((Display.RenderTarget != null) ? (-1f) : 1f);
		PrepareForDrawingOverride();
	}

	internal VertexAttributeData[] GetVertexAttribData(VertexDeclaration vertexDeclaration)
	{
		if (!m_vertexAttributeDataByDeclaration.TryGetValue(vertexDeclaration, out var value))
		{
			value = new VertexAttributeData[8];
			foreach (ShaderAttributeData shaderAttributeDatum in m_shaderAttributeData)
			{
				VertexElement vertexElement = null;
				for (int i = 0; i < vertexDeclaration.m_elements.Length; i++)
				{
					if (vertexDeclaration.m_elements[i].Semantic == shaderAttributeDatum.Semantic)
					{
						vertexElement = vertexDeclaration.m_elements[i];
						break;
					}
				}
				if (!(vertexElement != null))
				{
					throw new InvalidOperationException("VertexElement not found for shader attribute \"" + shaderAttributeDatum.Semantic + "\".");
				}
				value[shaderAttributeDatum.Location] = new VertexAttributeData
				{
					Size = vertexElement.Format.GetElementsCount(),
					Offset = vertexElement.Offset
				};
			}
			m_vertexAttributeDataByDeclaration.Add(vertexDeclaration, value);
		}
		return value;
	}

	private static void ParseShaderMetadata(string shaderCode, Dictionary<string, string> semanticsByAttribute, Dictionary<string, string> samplersByTexture)
	{
		string[] array = shaderCode.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				string text = array[i];
				text = text.Trim();
				if (!text.StartsWith("//"))
				{
					continue;
				}
				text = text.Substring(2).TrimStart();
				if (!text.StartsWith("<") || !text.EndsWith("/>"))
				{
					continue;
				}
				XElement xElement = XElement.Parse(text);
				if (xElement.Name == "Semantic")
				{
					if (xElement.Attribute("Attribute") == null)
					{
						throw new InvalidOperationException("Missing \"Attribute\" attribute in shader metadata.");
					}
					if (xElement.Attribute("Name") == null)
					{
						throw new InvalidOperationException("Missing \"Name\" attribute in shader metadata.");
					}
					semanticsByAttribute.Add(xElement.Attribute("Attribute").Value, xElement.Attribute("Name").Value);
					continue;
				}
				if (!(xElement.Name == "Sampler"))
				{
					throw new InvalidOperationException("Unrecognized shader metadata node.");
				}
				if (xElement.Attribute("Texture") == null)
				{
					throw new InvalidOperationException("Missing \"Texture\" attribute in shader metadata.");
				}
				if (xElement.Attribute("Name") == null)
				{
					throw new InvalidOperationException("Missing \"Name\" attribute in shader metadata.");
				}
				samplersByTexture.Add(xElement.Attribute("Texture").Value, xElement.Attribute("Name").Value);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Error in shader metadata, line {i + 1}. {ex.Message}");
			}
		}
	}

	private string PrependShaderMacros(string shaderCode, ShaderMacro[] shaderMacros, bool isVertexShader)
	{
		string text = "#version 110" + Environment.NewLine;
		text = text + "#define GLSL" + Environment.NewLine;
		if (isVertexShader)
		{
			text = ((!Display.UseReducedZRange) ? (text + "#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul; gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;" + Environment.NewLine) : (text + "#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul;" + Environment.NewLine));
			text = text + "uniform float u_glymul;" + Environment.NewLine;
		}
		foreach (ShaderMacro shaderMacro in shaderMacros)
		{
			text = text + "#define " + shaderMacro.Name + " " + shaderMacro.Value + Environment.NewLine;
		}
		text = text + "#line 1" + Environment.NewLine;
		return text + shaderCode;
	}

	internal override void HandleDeviceLost()
	{
		DeleteShaders();
	}

	internal override void HandleDeviceReset()
	{
		CompileShaders();
	}

	internal void CompileShaders()
	{
		DeleteShaders();
		Dictionary<string, string> semanticsByAttribute = new Dictionary<string, string>();
		Dictionary<string, string> samplersByTexture = new Dictionary<string, string>();
		ParseShaderMetadata(m_vertexShaderCode, semanticsByAttribute, samplersByTexture);
		ParseShaderMetadata(m_pixelShaderCode, semanticsByAttribute, samplersByTexture);
		m_parameters = new ShaderParameter[0];
		m_parametersByName = new Dictionary<string, ShaderParameter>();
		m_glymulParameter = GetParameter("u_glymul");
		if (m_glymulParameter.Type != ShaderParameterType.Float)
		{
			throw new InvalidOperationException("u_glymul parameter has invalid type.");
		}
	}

	internal void DeleteShaders()
	{
		if (m_program != 0)
		{
			if (m_vertexShader != 0)
			{
			}
			if (m_pixelShader != 0)
			{
			}
			GLWrapper.DeleteProgram(m_program);
			m_program = 0;
		}
		if (m_vertexShader != 0)
		{
			m_vertexShader = 0;
		}
		if (m_pixelShader != 0)
		{
			m_pixelShader = 0;
		}
	}

	public ShaderParameter GetParameter(string name, bool allowNull = false)
	{
		if (!m_parametersByName.TryGetValue(name, out var value))
		{
			m_parametersByName[name] = new ShaderParameter();
			return m_parametersByName[name];
		}
		return value;
	}

	public override int GetGpuMemoryUsage()
	{
		return 16384;
	}

	public virtual void PrepareForDrawingOverride()
	{
	}

	private void InitializeShader(string vertexShaderCode, string pixelShaderCode, ShaderMacro[] shaderMacros)
	{
		if (vertexShaderCode == null)
		{
			throw new ArgumentNullException("vertexShaderCode");
		}
		if (pixelShaderCode == null)
		{
			throw new ArgumentNullException("pixelShaderCode");
		}
		if (shaderMacros == null)
		{
			throw new ArgumentNullException("shaderMacros");
		}
		m_vertexShaderCode = vertexShaderCode;
		m_pixelShaderCode = pixelShaderCode;
		m_shaderMacros = (ShaderMacro[])shaderMacros.Clone();
	}
}
