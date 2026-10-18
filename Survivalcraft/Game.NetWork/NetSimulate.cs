using System;

namespace Game.NetWork;

public abstract class NetSimulate<T>
{
	protected T Base;

	protected T Next;

	private float ElapsedTime;

	protected Func<T, T, float, T> LerpFunc;

	public NetSimulate(T v)
	{
		Base = v;
		Next = v;
		ElapsedTime = 0f;
	}

	public virtual void SetNext(T v)
	{
		Base = Next;
		Next = v;
		ElapsedTime = 0f;
	}

	public virtual T Get(float dt)
	{
		ElapsedTime += dt * 8f;
		return LerpFunc(Base, Next, ElapsedTime);
	}
}
