namespace LocalisationGenerator;

public class Locale {
	public string ISO;
	public string Name => LocalesLUT.LocaleName( ISO );
	public readonly Dictionary<string, LocalisableString> Strings = new();

	public Locale ( string iso ) {
		ISO = iso;
	}
}
