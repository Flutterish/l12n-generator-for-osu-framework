namespace LocalisationGenerator.Curses;

[Flags]
public enum Attribute {
	Normal = 0,
	Bold = 1 << 1,
	Dim = 1 << 2,
	Italic = 1 << 3,
	Underline = 1 << 4,
	SlowBlink = 1 << 5,
	RapidBlink = 1 << 6,
	Reverse = 1 << 7,
	Invisible = 1 << 8,
	CrossedOut = 1 << 9
}
