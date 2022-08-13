namespace LocalisationGenerator.Curses;

public struct Symbol {
	public char Char;
	public AnsiColor Fg;
	public AnsiColor Bg;
	public Attribute Attributes;

	public static bool RequiresAnsiChange ( in Symbol a, in Symbol b ) {
		return a.Fg != b.Fg || a.Bg != b.Bg || a.Attributes != b.Attributes;
	}

	public override bool Equals ( object? obj ) {
		return obj is Symbol symbol && symbol == this;
	}

	public override int GetHashCode () {
		return HashCode.Combine( Char, Fg, Bg, Attributes );
	}

	public static bool operator == ( Symbol a, Symbol b )
		=> a.Char == b.Char && a.Fg == b.Fg && a.Bg == b.Bg && a.Attributes == b.Attributes;

	public static bool operator != ( Symbol a, Symbol b )
		=> a.Char != b.Char || a.Fg != b.Fg || a.Bg != b.Bg || a.Attributes != b.Attributes;
}