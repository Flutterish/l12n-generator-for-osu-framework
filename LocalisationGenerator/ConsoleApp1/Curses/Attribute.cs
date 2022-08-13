namespace LocalisationGenerator.Curses;

[Flags]
public enum Attribute {
	Normal = 0,
	Underline = 1 << 0,
	Reverse = 1 << 1,
	Blink = 1 << 2,
	Dim = 1 << 3,
	Bold = 1 << 4,
	Invis = 1 << 5
}
