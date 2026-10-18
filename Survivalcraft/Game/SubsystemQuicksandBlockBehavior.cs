namespace Game;

public class SubsystemQuicksandBlockBehavior : SubsystemFluidBlockBehavior, IUpdateable
{
	public override int[] HandledBlocks => new int[1] { 391 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public SubsystemQuicksandBlockBehavior()
		: base(BlocksManager.FluidBlocks[391], generateSources: false)
	{
	}

	public void Update(float dt)
	{
		if (SubsystemTime.PeriodicGameTimeEvent(3.0, 0.0))
		{
			SpreadFluid();
		}
	}
}
