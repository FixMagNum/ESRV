using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Engine.Graphics;

public class UnlitShader : Shader
{
	public ShaderParameter m_worldViewProjectionMatrixParameter;

	public ShaderParameter m_textureParameter;

	public ShaderParameter m_samplerStateParameter;

	public ShaderParameter m_colorParameter;

	public ShaderParameter m_alphaThresholdParameter;

	public ShaderParameter m_time;

	public readonly ShaderTransforms Transforms;

	public Texture2D Texture
	{
		set
		{
			m_textureParameter.SetValue(value);
		}
	}

	public SamplerState SamplerState
	{
		set
		{
			m_samplerStateParameter.SetValue(value);
		}
	}

	public Vector4 Color
	{
		set
		{
			m_colorParameter.SetValue(value);
		}
	}

	public float AlphaThreshold
	{
		set
		{
			m_alphaThresholdParameter.SetValue(value);
		}
	}

	public float Time
	{
		set
		{
			m_time.SetValue(value);
		}
	}

	public UnlitShader(string vsc, string psc, bool useVertexColor, bool useTexture, bool useAlphaThreshold)
		: base(vsc, psc, PrepareShaderMacros(useVertexColor, useTexture, useAlphaThreshold))
	{
		m_worldViewProjectionMatrixParameter = GetParameter("u_worldViewProjectionMatrix", allowNull: true);
		m_textureParameter = GetParameter("u_texture", allowNull: true);
		m_samplerStateParameter = GetParameter("u_samplerState", allowNull: true);
		m_colorParameter = GetParameter("u_color", allowNull: true);
		m_alphaThresholdParameter = GetParameter("u_alphaThreshold", allowNull: true);
		m_time = GetParameter("u_time", allowNull: true);
		Transforms = new ShaderTransforms(1);
		Color = Vector4.One;
	}

	public UnlitShader(bool useVertexColor, bool useTexture, bool useAlphaThreshold)
		: base(new StreamReader(typeof(Shader).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.Unlit.vsh")).ReadToEnd(), new StreamReader(typeof(Shader).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.Unlit.psh")).ReadToEnd(), PrepareShaderMacros(useVertexColor, useTexture, useAlphaThreshold))
	{
		m_worldViewProjectionMatrixParameter = GetParameter("u_worldViewProjectionMatrix", allowNull: true);
		m_textureParameter = GetParameter("u_texture", allowNull: true);
		m_samplerStateParameter = GetParameter("u_samplerState", allowNull: true);
		m_colorParameter = GetParameter("u_color", allowNull: true);
		m_alphaThresholdParameter = GetParameter("u_alphaThreshold", allowNull: true);
		Transforms = new ShaderTransforms(1);
		Color = Vector4.One;
	}

	public override void PrepareForDrawingOverride()
	{
		Transforms.UpdateMatrices(1, worldView: false, viewProjection: false, worldViewProjection: true);
		m_worldViewProjectionMatrixParameter.SetValue(Transforms.WorldViewProjection, 1);
	}

	public static ShaderMacro[] PrepareShaderMacros(bool useVertexColor, bool useTexture, bool useAlphaThreshold)
	{
		List<ShaderMacro> list = new List<ShaderMacro>();
		if (useVertexColor)
		{
			list.Add(new ShaderMacro("USE_VERTEXCOLOR"));
		}
		if (useTexture)
		{
			list.Add(new ShaderMacro("USE_TEXTURE"));
		}
		if (useAlphaThreshold)
		{
			list.Add(new ShaderMacro("USE_ALPHATHRESHOLD"));
		}
		return list.ToArray();
	}
}
