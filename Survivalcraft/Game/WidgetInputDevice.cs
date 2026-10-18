using System;

namespace Game;

[Flags]
public enum WidgetInputDevice
{
	None = 0,
	Keyboard = 1,
	Mouse = 2,
	Touch = 4,
	GamePad1 = 8,
	GamePad2 = 0x10,
	GamePad3 = 0x20,
	GamePad4 = 0x40,
	Gamepads = GamePad1 | GamePad2 | GamePad3 | GamePad4,
	VrControllers = 0x80,
	All = Gamepads | Keyboard | Mouse | Touch | VrControllers
}
