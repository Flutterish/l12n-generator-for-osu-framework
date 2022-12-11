using System.Text.RegularExpressions;

namespace LocalisationGenerator;

public static class Extensions {
	public static bool IsConfirmAction ( this ConsoleKeyInfo key )
		=> key.Key is ConsoleKey.Enter or ConsoleKey.Tab or ConsoleKey.Spacebar or ConsoleKey.NumPad5;

	public static string ColorizedSubstring ( this string str, int from, int to ) {
		// lmao
		return new Regex( $@"^(?:(?:(?:\u0001.)*[^\u0001](?:\u0001.)*){{{from}}})((?:(?:\u0001.)*[^\u0001](?:\u0001.)*){{{to - from}}})" ).Match( str ).Groups[1].Value;
	}
}
