namespace LocalisationGenerator.Curses;

public struct AnsiColor {
	public int Preset;

	public static bool operator == ( AnsiColor a, AnsiColor b )
		=> a.Preset == b.Preset;

	public static bool operator != ( AnsiColor a, AnsiColor b )
		=> a.Preset != b.Preset;

	public override bool Equals ( object? obj ) {
		return obj is AnsiColor color && color == this;
	}

	public override int GetHashCode () {
		return HashCode.Combine( Preset );
	}

	public static readonly AnsiColor Black = new() { Preset = 0 };
	public static readonly AnsiColor Red = new() { Preset = 1 };
	public static readonly AnsiColor Green = new() { Preset = 2 };
	public static readonly AnsiColor Yellow = new() { Preset = 3 };
	public static readonly AnsiColor Blue = new() { Preset = 4 };
	public static readonly AnsiColor Magenta = new() { Preset = 5 };
	public static readonly AnsiColor Cyan = new() { Preset = 6 };
	public static readonly AnsiColor White = new() { Preset = 7 };

	public static readonly AnsiColor Gray = new() { Preset = 60 };
	public static readonly AnsiColor BrightRed = new() { Preset = 61 };
	public static readonly AnsiColor BrightGreen = new() { Preset = 62 };
	public static readonly AnsiColor BrightYellow = new() { Preset = 63 };
	public static readonly AnsiColor BrightBlue = new() { Preset = 64 };
	public static readonly AnsiColor BrightMagenta = new() { Preset = 65 };
	public static readonly AnsiColor BrightCyan = new() { Preset = 66 };
	public static readonly AnsiColor BrightWhite = new() { Preset = 67 };

	public static implicit operator AnsiColor ( ConsoleColor c ) {
		return c switch {
			ConsoleColor.Black => Black,
			ConsoleColor.DarkBlue => Blue,
			ConsoleColor.DarkGreen => Green,
			ConsoleColor.DarkCyan => Cyan,
			ConsoleColor.DarkRed => Red,
			ConsoleColor.DarkMagenta => BrightMagenta,
			ConsoleColor.DarkYellow => Yellow,
			ConsoleColor.Gray => White,
			ConsoleColor.DarkGray => Gray,
			ConsoleColor.Blue => BrightBlue,
			ConsoleColor.Green => BrightGreen,
			ConsoleColor.Cyan => BrightCyan,
			ConsoleColor.Red => BrightRed,
			ConsoleColor.Magenta => Magenta,
			ConsoleColor.Yellow => BrightYellow,
			ConsoleColor.White => BrightWhite,
			_ => White
		};
	}
}
