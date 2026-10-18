namespace Engine.Graphics;

public sealed class ShaderParameter
{
	internal int Location;

	internal float[] Value;

	internal object Resource;

	internal bool IsChanged = true;

	public readonly Shader Shader;

	public readonly string Name;

	public readonly ShaderParameterType Type;

	public readonly int Count;

	internal ShaderParameter()
	{
	}

	public void SetValue(float value)
	{
	}

	public void SetValue(float[] value, int count)
	{
	}

	public void SetValue(Vector2 value)
	{
	}

	public void SetValue(Vector2[] value, int count)
	{
	}

	public void SetValue(Vector3 value)
	{
	}

	public void SetValue(Vector3[] value, int count)
	{
	}

	public void SetValue(Vector4 value)
	{
	}

	public void SetValue(Vector4[] value, int count)
	{
	}

	public void SetValue(Matrix value)
	{
	}

	public void SetValue(Matrix[] value, int count)
	{
	}

	public void SetValue(Texture2D value)
	{
	}

	public void SetValue(SamplerState value)
	{
	}
}
