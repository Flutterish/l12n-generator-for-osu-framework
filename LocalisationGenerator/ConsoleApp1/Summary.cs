namespace LocalisationGenerator;

public class Summary {
	public readonly Dictionary<string, KeySummary> Keys = new();
	public readonly Dictionary<string, LocaleSummary> Locales = new();
	public LocaleNamespace RootNamespace = new( "" );

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

		foreach ( var (key, keySummary) in Keys ) {
			var ns = RootNamespace;
			var split = key.Split( '.', '/' );

			if ( split.Length > 1 ) {
				foreach ( var nested in split[..^1] ) {
					if ( !ns.Nested.TryGetValue( nested, out var next ) )
						ns.Nested.Add( nested, next = new( ns.Name + '.' + nested ) );

					ns = next;
				}
			}

			int i = 0;
			foreach ( var arg in keySummary.Arguments.Keys ) {
				keySummary.ArgIndices.Add( arg, i++ );
			}

			ns.Keys.Add( split[^1], key );
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
	public Dictionary<string, int> ArgIndices = new();
}

public class LocaleSummary {
	public Locale Locale;
	public LocaleSummary ( Locale locale ) {
		Locale = locale;
	}

	public List<string> LocalisedStrings = new();
	public List<string> MissingStrings = new();
}

public class LocaleNamespace {
	public string Name;
	public LocaleNamespace ( string name ) {
		Name = name;
	}

	public Dictionary<string, string> Keys = new();
	public Dictionary<string, LocaleNamespace> Nested = new();
}