namespace LocalisationGenerator;

public static class Extensions {
	public static bool IsConfirmAction ( this ConsoleKeyInfo key )
		=> key.Key is ConsoleKey.Enter or ConsoleKey.Tab or ConsoleKey.Spacebar or ConsoleKey.NumPad5;
}
