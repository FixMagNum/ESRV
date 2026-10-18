using Engine;

namespace Game;

public struct VertexPositionNormalTexture
{
	public Vector3 Position;

	public Vector3 Normal;

	public Vector2 TextureCoordinate;

	public const int SizeInBytes = 32;
}
