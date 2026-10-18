using Engine;
using Engine.Graphics;

namespace Game;

public struct VertexPositionNormalColor(Vector3 position, Vector3 normal, Color color)
{
	public Vector3 Position = position;

	public Vector3 Normal = normal;

	public Color Color = color;

	public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position), new VertexElement(12, VertexElementFormat.Vector3, VertexElementSemantic.Normal), new VertexElement(24, VertexElementFormat.NormalizedByte4, VertexElementSemantic.Color));
}
