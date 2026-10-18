using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Graphics;

internal static class GLWrapper
{
	internal static int m_mainFramebuffer;

	internal static int m_mainColorbuffer;

	private static int m_arrayBuffer;

	private static int m_elementArrayBuffer;

	private static int m_texture2D;

	private static int[] m_activeTexturesByUnit;

	private static int m_program;

	private static int m_framebuffer;

	private static Vector4? m_clearColor;

	private static float? m_clearDepth;

	private static int? m_clearStencil;

	private static int? m_colorMask;

	private static bool? m_depthMask;

	private static float m_polygonOffsetFactor;

	private static float m_polygonOffsetUnits;

	private static Vector4 m_blendColor;

	private static bool?[] m_vertexAttribArray;

	private static RasterizerState m_rasterizerState;

	private static DepthStencilState m_depthStencilState;

	private static BlendState m_blendState;

	private static Dictionary<int, SamplerState> m_textureSamplerStates;

	private static Shader m_lastShader;

	private static VertexDeclaration m_lastVertexDeclaration;

	private static IntPtr m_lastVertexOffset;

	private static int m_lastArrayBuffer;

	private static Viewport? m_viewport;

	private static Rectangle? m_scissorRectangle;

	private static bool GL_EXT_texture_filter_anisotropic;

	private static bool GL_OES_packed_depth_stencil;

	public static void Initialize()
	{
	}

	public static void InitializeCache()
	{
		m_arrayBuffer = -1;
		m_elementArrayBuffer = -1;
		m_texture2D = -1;
		m_activeTexturesByUnit = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };
		m_program = -1;
		m_framebuffer = -1;
		m_clearColor = null;
		m_clearDepth = null;
		m_clearStencil = null;
		m_colorMask = null;
		m_depthMask = null;
		m_polygonOffsetFactor = 0f;
		m_polygonOffsetUnits = 0f;
		m_blendColor = new Vector4(float.MinValue);
		m_vertexAttribArray = new bool?[16];
		m_rasterizerState = null;
		m_depthStencilState = null;
		m_blendState = null;
		m_textureSamplerStates = new Dictionary<int, SamplerState>();
		m_lastShader = null;
		m_lastVertexDeclaration = null;
		m_lastVertexOffset = IntPtr.Zero;
		m_lastArrayBuffer = -1;
		m_viewport = null;
		m_scissorRectangle = null;
	}

	public static void ClearColor(Vector4 color)
	{
		Vector4? clearColor = m_clearColor;
		if (color != clearColor)
		{
			m_clearColor = color;
		}
	}

	public static void ClearDepth(float depth)
	{
		if (depth != m_clearDepth)
		{
			m_clearDepth = depth;
		}
	}

	public static void ClearStencil(int stencil)
	{
		if (stencil != m_clearStencil)
		{
			m_clearStencil = stencil;
		}
	}

	public static void ColorMask(int colorMask)
	{
		colorMask &= 0xF;
		if (colorMask != m_colorMask)
		{
			m_colorMask = colorMask;
		}
	}

	public static bool DepthMask(bool depthMask)
	{
		if (depthMask != m_depthMask)
		{
			m_depthMask = depthMask;
			return true;
		}
		return false;
	}

	public static void PolygonOffset(float factor, float units)
	{
		if (factor != m_polygonOffsetFactor || units != m_polygonOffsetUnits)
		{
			m_polygonOffsetFactor = factor;
			m_polygonOffsetUnits = units;
		}
	}

	public static void BlendColor(Vector4 blendColor)
	{
		if (blendColor != m_blendColor)
		{
			m_blendColor = blendColor;
		}
	}

	public static void VertexAttribArray(int index, bool enable)
	{
		if (enable && (!m_vertexAttribArray[index].HasValue || !m_vertexAttribArray[index].Value))
		{
			m_vertexAttribArray[index] = true;
		}
		else if (!enable && (!m_vertexAttribArray[index].HasValue || m_vertexAttribArray[index].Value))
		{
			m_vertexAttribArray[index] = false;
		}
	}

	public static void BindFramebuffer(int framebuffer)
	{
		if (framebuffer != m_framebuffer)
		{
			m_framebuffer = framebuffer;
		}
	}

	public static void UseProgram(int program)
	{
		if (program != m_program)
		{
			m_program = program;
		}
	}

	public static void DeleteProgram(int program)
	{
		if (m_program == program)
		{
			m_program = -1;
		}
	}

	public static void DeleteTexture(int texture)
	{
		if (m_texture2D == texture)
		{
			m_texture2D = -1;
		}
		for (int i = 0; i < m_activeTexturesByUnit.Length; i++)
		{
			if (m_activeTexturesByUnit[i] == texture)
			{
				m_activeTexturesByUnit[i] = -1;
			}
		}
		m_textureSamplerStates.Remove(texture);
	}

	public static void DeleteFramebuffer(int framebuffer)
	{
		if (m_framebuffer == framebuffer)
		{
			m_framebuffer = -1;
		}
	}

	public static void ApplyViewportScissor(Viewport viewport, Rectangle scissorRectangle, bool isScissorEnabled)
	{
		if (!m_viewport.HasValue || viewport.X != m_viewport.Value.X || viewport.Y != m_viewport.Value.Y || viewport.Width != m_viewport.Value.Width || viewport.Height != m_viewport.Value.Height)
		{
			int num = ((Display.RenderTarget == null) ? (Display.BackbufferSize.Y - viewport.Y - viewport.Height) : viewport.Y);
		}
		if (!m_viewport.HasValue || viewport.MinDepth != m_viewport.Value.MinDepth || viewport.MaxDepth != m_viewport.Value.MaxDepth)
		{
		}
		m_viewport = viewport;
		if (!isScissorEnabled)
		{
			return;
		}
		if (m_scissorRectangle.HasValue)
		{
			Rectangle value = scissorRectangle;
			Rectangle? scissorRectangle2 = m_scissorRectangle;
			if (!(value != scissorRectangle2))
			{
				return;
			}
		}
		if (Display.RenderTarget == null)
		{
			scissorRectangle.Top = Display.BackbufferSize.Y - scissorRectangle.Top - scissorRectangle.Height;
		}
		m_scissorRectangle = scissorRectangle;
	}

	public static void ApplyRasterizerState(RasterizerState state)
	{
		if (state != m_rasterizerState)
		{
			m_rasterizerState = state;
		}
	}

	public static void ApplyDepthStencilState(DepthStencilState state)
	{
		if (state != m_depthStencilState)
		{
			m_depthStencilState = state;
			if (state.DepthBufferTestEnable || state.DepthBufferWriteEnable)
			{
				DepthMask(state.DepthBufferWriteEnable);
			}
		}
	}

	public static void ApplyBlendState(BlendState state)
	{
		if (state != m_blendState)
		{
			m_blendState = state;
			if (state.ColorBlendFunction != BlendFunction.Add || state.ColorSourceBlend != Blend.One || state.ColorDestinationBlend != Blend.Zero || state.AlphaBlendFunction != BlendFunction.Add || state.AlphaSourceBlend != Blend.One || state.AlphaDestinationBlend != Blend.Zero)
			{
				BlendColor(state.BlendFactor);
			}
		}
	}

	public static void ApplyRenderTarget(RenderTarget2D renderTarget)
	{
		if (renderTarget != null)
		{
			BindFramebuffer(renderTarget.m_frameBuffer);
		}
		else
		{
			BindFramebuffer(m_mainFramebuffer);
		}
	}

	public static void ApplyShaderAndBuffers(Shader shader, VertexDeclaration vertexDeclaration, IntPtr vertexOffset, int arrayBuffer, int? elementArrayBuffer)
	{
		shader.PrepareForDrawing();
		if (elementArrayBuffer.HasValue)
		{
		}
		UseProgram(shader.m_program);
		if (shader != m_lastShader || vertexOffset != m_lastVertexOffset || arrayBuffer != m_lastArrayBuffer || vertexDeclaration.m_elements != m_lastVertexDeclaration.m_elements)
		{
			Shader.VertexAttributeData[] vertexAttribData = shader.GetVertexAttribData(vertexDeclaration);
			for (int i = 0; i < vertexAttribData.Length; i++)
			{
				if (vertexAttribData[i].Size != 0)
				{
					VertexAttribArray(i, enable: true);
				}
				else
				{
					VertexAttribArray(i, enable: false);
				}
			}
			m_lastShader = shader;
			m_lastVertexDeclaration = vertexDeclaration;
			m_lastVertexOffset = vertexOffset;
			m_lastArrayBuffer = arrayBuffer;
		}
		int num = 0;
		int num2 = 0;
		ShaderParameter shaderParameter;
		while (true)
		{
			if (num2 >= shader.m_parameters.Length)
			{
				return;
			}
			shaderParameter = shader.m_parameters[num2];
			if (shaderParameter.IsChanged)
			{
				switch (shaderParameter.Type)
				{
				case ShaderParameterType.Float:
					shaderParameter.IsChanged = false;
					break;
				case ShaderParameterType.Vector2:
					shaderParameter.IsChanged = false;
					break;
				case ShaderParameterType.Vector3:
					shaderParameter.IsChanged = false;
					break;
				case ShaderParameterType.Vector4:
					shaderParameter.IsChanged = false;
					break;
				case ShaderParameterType.Matrix:
					shaderParameter.IsChanged = false;
					break;
				default:
					throw new InvalidOperationException("Unsupported shader parameter type.");
				case ShaderParameterType.Texture2D:
				case ShaderParameterType.Sampler2D:
					break;
				}
			}
			if (shaderParameter.Type == ShaderParameterType.Texture2D)
			{
				if (num >= 8)
				{
					throw new InvalidOperationException("Too many simultaneous textures.");
				}
				if (shaderParameter.IsChanged)
				{
				}
				ShaderParameter shaderParameter2 = shader.m_parameters[num2 + 1];
				Texture2D texture2D = (Texture2D)shaderParameter.Resource;
				SamplerState samplerState = (SamplerState)shaderParameter2.Resource;
				if (texture2D != null)
				{
					if (samplerState == null)
					{
						break;
					}
					if (m_activeTexturesByUnit[num] != texture2D.m_texture)
					{
					}
					if (!m_textureSamplerStates.TryGetValue(texture2D.m_texture, out var value) || value != samplerState)
					{
						m_textureSamplerStates[texture2D.m_texture] = samplerState;
					}
				}
				else if (m_activeTexturesByUnit[num] == 0)
				{
				}
				num++;
				shaderParameter.IsChanged = false;
			}
			num2++;
		}
		throw new InvalidOperationException("Associated SamplerState is not set for texture \"" + shaderParameter.Name + "\".");
	}

	public static void Clear(RenderTarget2D renderTarget, Vector4? color, float? depth, int? stencil)
	{
		if (color.HasValue)
		{
			ClearColor(color.Value);
			ColorMask(15);
		}
		if (depth.HasValue)
		{
			ClearDepth(depth.Value);
			if (DepthMask(depthMask: true))
			{
				m_depthStencilState = null;
			}
		}
		if (stencil.HasValue)
		{
			ClearStencil(stencil.Value);
		}
	}

	public static void HandleContextLost()
	{
		try
		{
			Log.Information("Device lost");
			Display.HandleDeviceLost();
			GC.Collect();
			InitializeCache();
			Display.Resize();
			Display.HandleDeviceReset();
			Log.Information("Device reset");
		}
		catch (Exception ex)
		{
			Log.Error("Failed to recreate graphics resources. Reason: {0}", ex.Message);
		}
	}

	[Conditional("DEBUG")]
	public static void CheckGLError()
	{
	}
}
