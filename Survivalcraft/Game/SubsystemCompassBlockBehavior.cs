using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemCompassBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemAudio m_subsystemAudio;

	public SubsystemPlayers m_subsystemPlayers;

	public Random m_random;

	private double m_lastUseTime;

	public override int[] HandledBlocks => new int[1] { 117 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_random = new Random();
		m_lastUseTime = 0.0;
	}

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		double realTime = Time.RealTime;
		if (realTime - m_lastUseTime < 0.4)
		{
			return false;
		}
		m_lastUseTime = realTime;
		Vector3 num = componentMiner.ComponentCreature.ComponentBody.Position;
		string num2 = $"X: {MathUtils.Round(num.X)}, Y: {MathUtils.Round(num.Y)}, Z: {MathUtils.Round(num.Z)}";
		componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(num2, Color.Yellow, blinking: true, playNotificationSound: true);
		m_subsystemAudio.PlaySound("Audio/UseCompass", 1f, m_random.Float(-0.2f, 0.2f), ray.Position, 4f, autoDelay: true);
		componentMiner.DamageActiveTool(1);
		return true;
	}
}
