namespace LocalisationGenerator;

public class Summary {
	public readonly Dictionary<string, KeySummary> Keys = new();
	public readonly Dictionary<string, LocaleSummary> Locales = new();

	public Summary ( IEnumerable<Locale> locales ) {
		foreach ( var locale in locales ) {
			var localeSummary = new LocaleSummary( locale );
			Locales.Add( locale.ISO, localeSummary );

			foreach ( var (key, str) in locale.Strings ) {
				if ( !Keys.TryGetValue( key, out var keySummary ) )
					Keys.Add( key, keySummary = new( key ) );

				foreach ( var arg in str.Args ) {
					if ( !keySummary.Arguments.TryGetValue( arg, out var list ) )
						keySummary.Arguments.Add( arg, list = new() );

					list.Add( locale );
				}
			}
		}

		foreach ( var locale in locales ) {
			var localeSummary = Locales[locale.ISO];
			foreach ( var (key, keySummary) in Keys ) {
				if ( locale.Strings.ContainsKey( key ) ) {
					keySummary.LocalisedIn.Add( locale );
					localeSummary.LocalisedStrings.Add( key );
				}
				else {
					keySummary.NotLocalisedIn.Add( locale );
					localeSummary.MissingStrings.Add( key );
				}
			}
		}
	}
}

public class KeySummary {
	public string Key;
	public KeySummary ( string key ) {
		Key = key;
	}
	
	public List<Locale> LocalisedIn = new();
	public List<Locale> NotLocalisedIn = new();

	public Dictionary<string, List<Locale>> Arguments = new();
}

public class LocaleSummary {
	public Locale Locale;
	public LocaleSummary ( Locale locale ) {
		Locale = locale;
	}

	public List<string> LocalisedStrings = new();
	public List<string> MissingStrings = new();
}