using System;

namespace Game.NetWork;

[Flags]
public enum InteractType : byte
{
	None = 0,
	Interact = 1,
	Hit = 2,
	Aim = 4,
	Dig = 8,
	EndDig = 0x10,
	EndAim = 0x20,
	CancelAim = 0x40
}
