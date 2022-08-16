using Newtonsoft.Json;
using System.Collections.Immutable;

namespace LocalisationGenerator;

public class Project {
	public Config Config { get; private set; }
	public Locale Mainlocale { get; private set; }

	public LocaleNamespace Namespace { get; } = new( "" );
	public Dictionary<string, LocaleNamespace> LocaleNamespaces = new();

	public Dictionary<string, Locale> Locales = new();
	public Dictionary<string, HashSet<Locale>> LocalesContainingKey = new();

	public LocalisableString? GetBestGuide ( Locale locale, string key ) {
		if ( locale != Mainlocale && Mainlocale.Strings.TryGetValue( key, out var str ) )
			return str;

		return LocalesContainingKey[key].FirstOrDefault( x => x != locale )?.Strings[key];
	}

	public LocaleNamespace GetLocaleNamespace ( Locale locale, string? key = null ) {
		if ( !LocaleNamespaces.TryGetValue( locale.ISO, out var ns ) )
			LocaleNamespaces.Add( locale.ISO, ns = new( "" ) );

		if ( key is null )
			return ns;

		var path = key.Split( '.' );
		foreach ( var i in path[..^1] ) {
			if ( !ns.Nested.TryGetValue( i, out var nested ) )
				ns.Nested.Add( i, nested = new( ns.Name + '.' + i, ns ) );

			ns = nested;
		}

		return ns;
	}

	void addKeyToNamspace ( LocaleNamespace ns, string key ) {
		var path = key.Split( '.' );
		foreach ( var i in path[..^1] ) {
			if ( !ns.Nested.TryGetValue( i, out var nested ) )
				ns.Nested.Add( i, nested = new( ns.Name + '.' + i, ns ) );

			ns = nested;
		}
		if ( !ns.Keys.ContainsKey( path[^1] ) )
			ns.Keys.Add( path[^1], key );
		ns.MissingKeys.Remove( path[^1] );
	}

	void removeKeyFromNamspace ( LocaleNamespace ns, string key ) {
		var path = key.Split( '.' );
		foreach ( var i in path[..^1] ) {
			ns = ns.Nested[i];
		}

		ns.Keys.Remove( path[^1] );
		while ( ns.Parent != null && ns.Keys.Count == 0 && ns.Nested.Count == 0 ) {
			var (k, _) = ns.Parent.Nested.First( x => x.Value == ns );
			ns.Parent.Nested.Remove( k );
			ns = ns.Parent;
		}
	}

	public void OnLocaleKeyAdded ( Locale locale, string key ) {
		if ( !LocalesContainingKey.TryGetValue( key, out var list ) ) {
			LocalesContainingKey.Add( key, list = new() );

			addKeyToNamspace( Namespace, key );
		}
		list.Add( locale );

		if ( !LocaleNamespaces.TryGetValue( locale.ISO, out var ns ) )
			LocaleNamespaces.Add( locale.ISO, ns = new( "" ) );
		addKeyToNamspace( ns, key );
	}

	public void OnLocaleKeyRemoved ( Locale locale, string key ) {
		var list = LocalesContainingKey[key];
		list.Remove( locale );
		if ( list.Count == 0 ) {
			LocalesContainingKey.Remove( key );

			removeKeyFromNamspace( Namespace, key );
		}

		removeKeyFromNamspace( LocaleNamespaces[locale.ISO], key );
	}

	public Project ( Config config ) {
		Config = config;

		if ( Directory.Exists( config.L12NFilesLocation ) ) {
			foreach ( var i in Directory.EnumerateFiles( config.L12NFilesLocation, "*.json" ) ) {
				try {
					var file = JsonConvert.DeserializeObject<SaveFormat>( File.ReadAllText( i ) );
					if ( file is null )
						continue;

					var locale = new Locale( file.Iso );
					Locales.Add( file.Iso, locale );
					foreach ( var (key, value) in file.Data ) {
						locale.Strings.Add( key, new LocalisableString( key, locale.ISO ) { Value = value } );
						OnLocaleKeyAdded( locale, key );
					}
				}
				catch { }
			}
		}

		if ( !Locales.ContainsKey( config.DefaultLocale ) ) {
			Locales.Add( config.DefaultLocale, new( config.DefaultLocale ) );
		}
		Mainlocale = Locales[config.DefaultLocale];
	}

	public IEnumerable<string> GetMissingStrings ( Locale locale ) {
		foreach ( var (key, list) in LocalesContainingKey ) {
			if ( !list.Contains( locale ) )
				yield return key;
		}
	}

	public void UpdateMissing ( Locale locale ) {
		foreach ( var i in GetMissingStrings( locale ) ) {
			var tree = GetLocaleNamespace( locale, i );
			tree.MissingKeys.TryAdd( i.Split( '.' )[^1], i );
		}
	}

	public void Save ( Locale? locale = null ) {
		Directory.CreateDirectory( Config.L12NFilesLocation );
		List<string> strings = new();
		foreach ( var loc in locale != null ? (IEnumerable<Locale>)new[] { locale } : Locales.Values ) {
			void addStrings ( LocaleNamespace ns ) {
				foreach ( var i in ns.Keys.Where( x => !ns.KeysToBeRemoved.Contains( x.Key ) ).OrderBy( x => x.Key ) ) {
					strings.Add( i.Value );
				}
				foreach ( var nested in ns.Nested.Where( x => !x.Value.ScheduledForRemoval ).OrderBy( x => x.Key ) ) {
					addStrings( nested.Value );
				}
			}
			addStrings( GetLocaleNamespace( loc ) );

			File.WriteAllText(
				Path.Combine( Config.L12NFilesLocation, $"{loc.ISO}.json" ),
				JsonConvert.SerializeObject( new {
					iso = loc.ISO,
					data = strings.ToImmutableSortedDictionary(
						ks => ks,
						vs => loc.Strings[vs]
					)
				}, Formatting.Indented )
			);

			strings.Clear();
		}
	}

	public void ToggleKeyRemoval ( Locale locale, string key, bool? set = null ) {
		var ns = GetLocaleNamespace( locale, key );

		key = key.Split( '.' )[^1];
		set ??= !ns.KeysToBeRemoved.Contains( key );
		if ( set == true )
			ns.KeysToBeRemoved.Add( key );
		else
			ns.KeysToBeRemoved.Remove( key );
	}

	public void ToggleNamespaceRemoval ( LocaleNamespace ns, bool? set = null ) {
		ns.ScheduledForRemoval = set is bool b ? b : !ns.ScheduledForRemoval;
	}

	public LocalisableString AddKey ( Locale locale, string key ) {
		var str = new LocalisableString( key, locale.ISO );
		locale.Strings.Add( key, str );
		OnLocaleKeyAdded( locale, key );
		return str;
	}
}
