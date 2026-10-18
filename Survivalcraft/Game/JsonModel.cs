using Engine;
using Engine.Graphics;

namespace Game;

public class JsonModel : Model
{
	public Model ParentModel;

	public string ParticleTexture;

	public Vector3 FirstPersonOffset;

	public Vector3 FirstPersonRotation;

	public Vector3 FirstPersonScale;

	public Vector3 InHandOffset;

	public Vector3 InHandRotation;

	public Vector3 InHandScale;
}
