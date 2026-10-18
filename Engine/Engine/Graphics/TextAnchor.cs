using System;

namespace Engine.Graphics;

[Flags]
public enum TextAnchor
{
	Default = 0,
	Left = 0,
	Top = 0,
	HorizontalCenter = 1,
	VerticalCenter = 2,
	Right = 4,
	Bottom = 8,
	DisableSnapToPixels = 0x10
}
