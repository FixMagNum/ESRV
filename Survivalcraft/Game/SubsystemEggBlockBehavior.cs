using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemEggBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemCreatureSpawn m_subsystemCreatureSpawn;

	public EggBlock m_eggBlock = (EggBlock)BlocksManager.Blocks[118];

	public Random m_random = new Random();

	public static string fName = "SubsystemEggBlockBehavior";

	public override int[] HandledBlocks => new int[0];

	public override bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem)
	{
		int categoryIndex = Terrain.ExtractData(worldItem.Value);
		bool isCooked = EggBlock.GetIsCooked(categoryIndex);
		bool categoryIndex2 = EggBlock.GetIsLaid(categoryIndex);
		if (!isCooked && (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || m_random.Float(0f, 1f) <= (categoryIndex2 ? 0.15f : 1f)))
		{
			if (m_subsystemCreatureSpawn.Creatures.Count < 35)
			{
				EggBlock.EggType componentPlayer = m_eggBlock.GetEggType(categoryIndex);
				m_subsystemCreatureSpawn.CreateEntity(new SpawnEntityData
				{
					TemplateName = componentPlayer.TemplateName,
					Position = worldItem.Position,
					ConstantSpawn = true
				}, null, null);
			}
			else
			{
				((worldItem as Projectile)?.Owner as ComponentPlayer)?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, blinking: true, playNotificationSound: false);
			}
		}
		return true;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemCreatureSpawn = Project.FindSubsystem<SubsystemCreatureSpawn>(throwOnError: true);
	}
}
