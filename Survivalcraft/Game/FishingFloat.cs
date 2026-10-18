using Engine;

namespace Game;

public class FishingFloat : WorldItem
{
	public enum FishFightState
	{
		Calm,
		Run,
		Tired
	}

	public Vector3 Rotation;

	public bool IsInWater;

	public Vector3 AngularVelocity;

	public new double CreationTime;

	public ComponentCreature Owner;

	public bool IsFloating;

	public float VerticalAlignFactor;

	public bool HasSplash;

	public ComponentBody StuckBody;

	public Vector3 BodyOffset;

	public bool IsRetracting;

	public int BobberValue;

	public float ReelSpeedScale;

	public float ReelAbortProbPerSec;

	public int ReelAttempts;

	public bool ReelWasAborted;

	public float EffectTimer;

	public FishFightState FightState;

	public float FightTimer;

	public float Fatigue;

	public float ReelImpulseTimeLeft;
}
