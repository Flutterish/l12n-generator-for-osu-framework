using LocalisationGenerator;
using LocalisationGenerator.Curses;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;

public class Program {
	public static void Main () {
		if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
			AnsiFix.Fix();
		new Program().Run();
	}

	Project project = null!;
	Config config => project.Config;
	Locale mainlocale => project.Mainlocale;
	Dictionary<string, Locale> locales => project.Locales;
	Dictionary<string, HashSet<Locale>> localesContainingKey => project.LocalesContainingKey;

	string Red ( string str )
		=> $"{esc( 'R' )}{str}{esc( ':' )}";
	string Yellow ( string str )
		=> $"{esc( 'Y' )}{str}{esc( ':' )}";
	string Green ( string str )
		=> $"{esc( 'G' )}{str}{esc( ':' )}";
	string Cyan ( string str )
		=> $"{esc( 'C' )}{str}{esc( ':' )}";

	string startingPath = Directory.GetCurrentDirectory();
	void Run () {
		AppDomain.CurrentDomain.ProcessExit += (_, _) => {
			project?.Save();
		};

		fgColorStack.Push( ConsoleColor.Gray );
		bgColorStack.Push( ConsoleColor.Black );

		WriteLine( "Welcome to o!f-l12n!\n" );

		if ( File.Exists( configPath ) ) {
			WriteLine( "There is a config file in this directory - load?" );
			if ( Select( new[] { "Load", "Use different one" } ) == "Load" ) {
				var config = JsonConvert.DeserializeObject<Config>( File.ReadAllText( configPath ) )!;
				project = new( config );
				Console.Clear();
			}
		}

	selectProject:
		Directory.SetCurrentDirectory( startingPath );
		if ( project is null ) {
			while ( true ) {
				WriteLine( $"Where should I load the project from? (it should have an {Green("l12nConfig.json")} file)" );
				if ( Select( new[] { "Select path", "Create new" } ) == "Select path" ) {
					var path = Prompt();
					if ( Directory.Exists( path ) ) {
						if ( File.Exists( Path.Combine( path, configPath ) ) ) {
							Directory.SetCurrentDirectory( path );
							var config = JsonConvert.DeserializeObject<Config>( File.ReadAllText( configPath ) )!;
							project = new( config );
							break;
						}
						else {
							Error( "No config file in that directory" );
						}
					}
					else {
						Error( "Directory doesn't exist or I have no access to it" );
					}
				}
				else {
					var config = Setup();
					project = new( config );
					SaveConfig();
					break;
				}
			}

			Console.Clear();
			WriteLine( "Welcome to o!f-l12n!\n" );
		}

		if ( string.IsNullOrWhiteSpace( config.ProjectPath ) ) {
			WriteLine( $"Local project at {Green(Directory.GetCurrentDirectory())}" );
		}
		else {
			WriteLine( $"Project       : {esc( 'G' )}{Path.GetFileName( config.ProjectPath )}{esc( ':' )}" );
			WriteLine( $"Namespace     : {esc( 'G' )}{config.Namespace}{esc( ':' )}" );
		}
		WriteLine( $"Default Locale: {esc( 'Y' )}{LocalesLUT.LocaleName( config.DefaultLocale )} [{config.DefaultLocale}]{esc( ':' )}" );
		WriteLine( "" );

		string add = "Add new locale";
		string edit = "Edit locale";
		string rename = "Rename key";
		string delete = Red("Delete key");
		string export = $"Generate {esc( 'G' )}.cs{esc( ':' )} and {esc( 'G' )}.resx{esc( ':' )} files";
		string link = $"Link a {Green(".csproj")} file";
		string summarise = $"Summary";
		string change = $"Change project";
		string exit = $"{esc( 'R' )}Exit{esc( ':' )}";

		List<string> options = new();
		while ( true ) {
			Split();
			options.Clear();
			options.Add( edit );
			options.Add( add );
			if ( localesContainingKey.Any() ) {
				options.Add( rename );
				options.Add( delete );
			}
			options.Add( summarise );
			if ( !string.IsNullOrWhiteSpace( config.ProjectPath ) )
				options.Add( export );
			else
				options.Add( link );
			options.Add( change );
			options.Add( exit );
			var option = Select( options );
			Split();
			if ( option == add ) {
				var locale = SelectNewLocale( allowCancel: true );
				if ( locale != null ) {
					Split();
					Edit( locale );
				}
			}
			else if ( option == edit ) {
				var locale = SelectLocale( allowCancel: true );
				if ( locale != null ) {
					Split();
					Edit( locale );
				}
			}
			else if ( option == rename ) {
				var key = Select( localesContainingKey.Keys.OrderBy( x => x ).ToList(), k => $"{Yellow(k)} [in {localesContainingKey[k].Count}/{locales.Count} locales]", allowCancel: true );
				if ( key == null )
					continue;

				WriteLine( "Key:" );
				WriteLine( $"You can group keys with dots, for example {Yellow( "chat.send" )} or {Yellow( "options.general" )}" );
				var newKey = Prompt().Trim();
				if ( key == newKey ) {
					Error( "That's the same name" );
					continue;
				}
				else if ( !keyRegex.IsMatch( newKey ) )
					Error( "Invalid key" );
				else if ( localesContainingKey.ContainsKey( newKey ) ) {
					WriteLine( $"Key already exists. This will merge {Yellow( key )} into {Yellow( newKey )}" );
					WriteLine( "Are you sure?" );
					if ( Select( new[] { "Nope", "Do it" } ) == "Do it" ) {
						foreach ( var i in localesContainingKey[key].ToArray() ) {
							if ( i.Strings.ContainsKey( newKey ) ) {
								i.Strings[key].Key = newKey;
								i.Strings[newKey] = i.Strings[key];
								i.Strings.Remove( key );
								project.OnLocaleKeyRemoved( i, key );
							}
							else {
								i.Strings[key].Key = newKey;
								i.Strings[newKey] = i.Strings[key];
								i.Strings.Remove( key );
								project.OnLocaleKeyRemoved( i, key );
								project.OnLocaleKeyAdded( i, newKey );
							}
						}

						project.Save();
					}
				}
				else {
					localesContainingKey[newKey] = localesContainingKey[key];
					localesContainingKey.Remove( key );

					foreach ( var i in localesContainingKey[newKey] ) {
						i.Strings[key].Key = newKey;
						i.Strings[newKey] = i.Strings[key];
						i.Strings.Remove( key );
					}

					project.Save();
				}
			}
			else if ( option == delete ) {
				var key = Select( localesContainingKey.Keys.OrderBy( x => x ).ToList(), k => $"{Yellow( k )} [in {localesContainingKey[k].Count}/{locales.Count} locales]", allowCancel: true );
				if ( key == null )
					continue;

				WriteLine( $"Are you sure you want to delete this key? Type `{Yellow(key)}` to confirm" );
				if ( Prompt() == key ) {
					foreach ( var i in localesContainingKey[key] ) {
						i.Strings.Remove( key );
					}
					localesContainingKey.Remove( key );
					WriteLine( Red("Removed") );
					project.Save();
				}
				else {
					WriteLine( "Cancelled" );
				}
			}
			else if ( option == summarise ) {
				var summary = new Summary( locales.Values );
				WriteLine( "Locales:" );
				foreach ( var (iso, locale) in summary.Locales ) {
					Write( Yellow($"{locale.Locale.Name} [{iso}]" ) + ": " );
					WriteLine( $"{bar((float)locale.LocalisedStrings.Count / summary.Keys.Count)}" );
				}

				Split();
				WriteLine( "Keys:" );
				void tree ( LocaleNamespace ns, string indent = "" ) {
					int c = 0;
					bool isLast () {
						return c == ns.Keys.Count + ns.Nested.Count;
					}
					foreach ( var (shortKey, key) in ns.Keys.OrderBy( x => x.Key ) ) {
						c++;
						var str = summary!.Keys[key];
						WriteLine( indent + ( isLast() ? "└─" : "├─" ) + Yellow( shortKey ) + ": " + bar( (float)str.LocalisedIn.Count / summary.Locales.Count ) );
						var lang = str.LocalisedIn.FirstOrDefault( x => x == mainlocale ) ?? str.LocalisedIn.First();
						WriteLine( indent + ( isLast() ? "   " : "│ ") + $"\tExample [{lang.ISO}]: {Red( "\"" )}{lang.Strings[key].ColoredValue}{Red( "\"" )}" );
						if ( str.NotLocalisedIn.Any() ) {
							WriteLine( indent + ( isLast() ? "   " : "│ " ) + $"\tNot localised in: {string.Join( ", ", str.NotLocalisedIn.Select( x => Yellow( $"{x.Name} [{x.ISO}]" ) ) )}" );
						}
						if ( str.Arguments.Any() )
							WriteLine( indent + ( isLast() ? "   " : "│ " ) + $"\tArguments: {string.Join( ", ", str.Arguments.Select( x => $"{{{x.Key}}}" ) )}" );
					}
					foreach ( var (name, nested) in ns.Nested.OrderBy( x => x.Key ) ) {
						c++;
						WriteLine( indent + (isLast() ? "└─" : "├─") + name );
						tree( nested, indent + (isLast() ? "  " : "│ ") );
					}
				}
				tree( summary.RootNamespace );

				Split();
				WriteLine( "Issues:" );
				foreach ( var (iso, locale) in summary.Locales.Where( x => x.Value.MissingStrings.Any() ) ) {
					Error( Yellow( $"{locale.Locale.Name} [{iso}]" ) + $": Has {locale.MissingStrings.Count} missing string{(locale.MissingStrings.Count == 1 ? "" : "s")}" );
				}
				foreach ( var (key, str) in summary.Keys.OrderBy( x => x.Key ) ) {
					foreach ( var missing in str.Arguments.Where( x => x.Value.Count != str.LocalisedIn.Count ) ) {
						Error( $"{Yellow(key)}: The argument {Green(missing.Key)} is not used in {string.Join( ", ", str.LocalisedIn.Except( missing.Value ).Select( x => Yellow( $"{x.Name} [{x.ISO}]" ) ) )}" );
					}
				}
			}
			else if ( option == link ) {
				LinkProject( config );
				SaveConfig();
			}
			else if ( option == export ) {
				try {
					var res = new ResourceGenerator( config, locales.Values );
					res.Save();
					WriteLine( Green( "Done" ) );
				}
				catch {
					Error( "Failed." );
				}
			}
			else if ( option == change ) {
				project = null;
				goto selectProject;
			} 
			else if ( option == exit ) {
				return;
			}
		}
	}

	string bar ( float progress, int width = 20 ) {
		progress = Math.Clamp( progress, 0, 1 );
		var fill = (int)Math.Floor( progress * width );
		return $"[{new string( '#', fill )}{new string( ' ', width - fill )}] {progress*100:N2}%";
	}

	void SaveConfig () {
		File.WriteAllText( configPath, JsonConvert.SerializeObject( config, Newtonsoft.Json.Formatting.Indented ) );
	}

	public static readonly Regex keyRegex = new( "^[a-zA-Z_][a-zA-Z_0-9-]*(\\.[a-zA-Z_][a-zA-Z_0-9-]*)*$", RegexOptions.Compiled );
	void Edit ( Locale locale ) {
		EditorScreen screen = new( project, locale );

		screen.Run();
		project.Save();
		Console.Clear();
		Console.CursorVisible = true;
	}

	void Split () {
		WriteLine( "--------------------" );
	}

	Locale? SelectNewLocale ( [DoesNotReturnIf( false )] bool allowCancel = false ) {
		var left = LocalesLUT.IsoToName.Values.Except( locales.Keys.Select( x => LocalesLUT.LocaleName( x ) ) );
		var locale = Select( left.Prepend( "Other" ).ToList(), allowCancel );
		if ( locale == "Other" ) {
			WriteLine( "Please provide an ISO language code:" );
			locale = Prompt( iso => {
				if ( locales.ContainsKey( iso ) ) {
					Error( "Locale already exists" );
					return false;
				}
				return true;
			} );
		}
		else if ( locale == null ) {
			return null;
		}
		else {
			locale = LocalesLUT.NameToIso[locale];
		}

		var loc = new Locale( locale );
		locales.Add( locale, loc );
		return loc;
	}

	Locale? SelectLocale ( [DoesNotReturnIf( false )] bool allowCancel = false ) {
		Dictionary<string, int> missingKeys = new();
		foreach ( var i in locales ) {
			missingKeys[i.Key] = 0;
			foreach ( var k in localesContainingKey ) {
				if ( !k.Value.Contains( i.Value ) )
					missingKeys[i.Key]++;
			}
		}

		string missingKeysString ( string iso ) {
			var missing = missingKeys[iso];
			if ( missing == 0 )
				return string.Empty;
			else if ( missing == 1 )
				return Red(" [1 missing key]");
			else 
				return Red( $" [{missing} missing keys]" );
		}

		var iso = Select( locales.Keys.ToList(), k => $"{LocalesLUT.LocaleName( k )} [{k}]{missingKeysString(k)}", allowCancel );
		return iso == null ? null : locales[iso];
	}

	static string configPath = "./l12nConfig.json";
	public static string esc ( char c ) {
		return $"{Window.escChar}{c}";
	}

	Stack<ConsoleColor> fgColorStack = new();
	Stack<ConsoleColor> bgColorStack = new();
	void Fg ( ConsoleColor? color ) {
		if ( color is ConsoleColor c ) {
			fgColorStack.Push( c );
			Console.ForegroundColor = c;
		}
		else {
			fgColorStack.Pop();
			Console.ForegroundColor = fgColorStack.Peek();
		}
	}

	void Bg ( ConsoleColor? color ) {
		if ( color is ConsoleColor c ) {
			bgColorStack.Push( c );
			Console.BackgroundColor = c;
		}
		else {
			bgColorStack.Pop();
			Console.BackgroundColor = bgColorStack.Peek();
		}
	}

	void Write ( string str ) {
		var parts = str.Split( Window.escChar );
		for ( int i = 0; i < parts.Length; i++ ) {
			if ( i > 0 ) {
				Fg( parts[i][0] switch {
					'G' => ConsoleColor.Green,
					'Y' => ConsoleColor.Yellow,
					'R' => ConsoleColor.Red,
					'C' => ConsoleColor.Cyan,
					'B' => ConsoleColor.Blue,
					'N' => ConsoleColor.DarkGray,
					_ => null
				} );
				Console.Write( parts[i][1..] );
			}
			else {
				Console.Write( parts[i] );
			}
		}
	}

	void WriteLine ( string str ) {
		Write( $"{str}\n" );
	}

	void Error ( string str ) {
		Bg( ConsoleColor.Red );
		WriteLine( str );
		Bg( null );
	}

	string Prompt () {
		Fg( ConsoleColor.Yellow );
		Console.Write( "> " );
		Fg( null );
		return Console.ReadLine() ?? string.Empty;
	}

	string Prompt ( Func<string, bool> validator ) {
		string str;
		while ( true ) {
			Fg( ConsoleColor.Yellow );
			Console.Write( "> " );
			Fg( null );
			str = Console.ReadLine() ?? string.Empty;
			if ( validator( str ) )
				break;
		}
		return str;
	}

	T? Select<T> ( IList<T> options, [DoesNotReturnIf( false )] bool allowCancel = false ) {
		return Select( options, s => $"{s}", allowCancel );
	}

	int selectIndex;
	bool keepSelectIndex = false;
	T? Select<T> ( IList<T> options, Func<T, string> stringifier, [DoesNotReturnIf( false )] bool allowCancel = false ) {
		Console.CursorVisible = false;
		var loc = Console.CursorTop;
		if ( !keepSelectIndex )
			selectIndex = 0;
		else {
			selectIndex = Math.Clamp( selectIndex, 0, options.Count - 1 );
		}
		keepSelectIndex = false;

		void writeLine ( string str ) {
			WriteLine( new string( ' ', Console.BufferWidth ) + '\r' + str );
		}

		void draw () {
			Console.CursorTop = loc;
			Console.CursorLeft = 0;
			var from = Math.Max( 0, selectIndex - 5 );
			var to = Math.Min( options.Count, from + 10 );
			from = Math.Max( 0, to - 10 );
			if ( from != 0 && to != options.Count ) {
				to = Math.Max( 0, to - 1 );
			}
			if ( from == 1 ) {
				from = 0;
			}

			if ( from != 0 )
				writeLine( $"{esc( 'Y' )} ^{esc( ':' )}" );
			for ( var i = from; i < to; i++ ) {
				if ( selectIndex == i )
					writeLine( $"{esc( 'Y' )}[{esc( ':' )}{stringifier( options[i] )}{esc( 'Y' )}]{esc( ':' )}" );
				else
					writeLine( $" {stringifier( options[i] )} " );
			}
			if ( to != options.Count )
				writeLine( $"{esc( 'Y' )} v{esc( ':' )}" );

			if ( allowCancel ) {
				if ( selectIndex == options.Count )
					writeLine( $"{esc( 'Y' )}[{esc( ':' )}{esc( 'R' )}Cancel{esc( ':' )}{esc( 'Y' )}]{esc( ':' )}" );
				else
					writeLine( $" {esc( 'R' )}Cancel{esc( ':' )} " );
			}
		}

		try {
			while ( true ) {
				draw();

				var key = Console.ReadKey( true );
				if ( key.Key == ConsoleKey.UpArrow ) {
					selectIndex = Math.Max( 0, selectIndex - 1 );
				}
				else if ( key.Key == ConsoleKey.DownArrow ) {
					selectIndex = Math.Min( allowCancel ? ( options.Count ) : ( options.Count - 1 ), selectIndex + 1 );
				}
				else if ( key.IsConfirmAction() ) {
					if ( selectIndex == options.Count )
						return default;
					else
						return options[selectIndex];
				}
				else if ( allowCancel && key.Key is ConsoleKey.Escape or ConsoleKey.Backspace or ConsoleKey.Delete ) {
					selectIndex = options.Count;
					draw();
					return default;
				}
			}
		}
		finally {
			Console.CursorVisible = true;
		}
	}

	Config Setup () {
		WriteLine( $"Where should the config file be located? (if the path doesn't exist, it will be created)" );
		if ( Select( new[] { "Here", "Somewhere else" } ) != "Here" ) {
			var path = Prompt( loc => {
				try {
					Path.GetFullPath( loc );
					if ( File.Exists( loc ) ) {
						Error( "A file with that name exists there -- can't create a directory" );
						return false;
					}
					return true;
				}
				catch {
					Error( "Not a valid path, or I have no access to it" );
					return false;
				}
			} );
			Directory.CreateDirectory( path );
			Directory.SetCurrentDirectory( path );
		}

		WriteLine( $"Do you have a {Green(".csproj")} you want to link, or do you want to just edit localisation files locally?" );
		if ( Select( new[] { "Link", "Local" } ) == "Local" ) {
			WriteLine( "Now, before we begin, what will the default locale be?" );
			var locale2 = Select( LocalesLUT.IsoToName.Values.Take( 1 ).Append( "Other" ).Concat( LocalesLUT.IsoToName.Values.Skip( 1 ) ).ToList() );
			if ( locale2 == "Other" ) {
				WriteLine( "Please provide an ISO language code:" );
				locale2 = Prompt();
			}
			else {
				locale2 = LocalesLUT.NameToIso[locale2];
			}

			return new Config {
				DefaultLocale = locale2,
				L12NFilesLocation = "./Source"
			};
		}

		Config config = new();
		LinkProject( config );

		WriteLine( "Now, before we begin, what will the default locale be?" );
		var locale = Select( LocalesLUT.IsoToName.Values.Take(1).Append( "Other" ).Concat( LocalesLUT.IsoToName.Values.Skip( 1 ) ).ToList() );
		if ( locale == "Other" ) {
			WriteLine( "Please provide an ISO language code:" );
			locale = Prompt();
		}
		else {
			locale = LocalesLUT.NameToIso[locale];
		}

		config.DefaultLocale = locale;
		return config;
	}

	void LinkProject ( Config config ) {
		WriteLine( $"First, where is your project located? (that's the {esc( 'G' )}.csproj{esc( ':' )} file)" );
		WriteLine( $"The current location is {esc( 'G' )}{Directory.GetCurrentDirectory()}{esc( ':' )}" );
		WriteLine( "The exact file location, or the folder it's in is fine:" );
		string location = "";
		Prompt( loc => {
			if ( Directory.Exists( loc ) ) {
				var files = Directory.GetFiles( loc, "*.csproj" );
				if ( files.Any() ) {
					WriteLine( "Please select one:" );
					var file = Select( files, allowCancel: true );
					if ( file == null )
						return false;

					location = file;
					return true;
				}
				else {
					Error( $"I can't find any {esc( 'G' )}.csproj{esc( ':' )} files there" );
					return false;
				}
			}
			else if ( File.Exists( loc ) ) {
				if ( loc.EndsWith( ".csproj" ) ) {
					location = loc;
					return true;
				}
				else {
					Error( $"This is not a {esc( 'G' )}.csproj{esc( ':' )} file" );
					return false;
				}
			}
			else {
				Error( "That path is not valid (or I have no access to it)" );
				return false;
			}
		} );

		WriteLine( $"Selected: {esc( 'G' )}{Path.GetFileName( location )}{esc( ':' )}" );
		using var contents = XmlReader.Create( location );
		var @namespace = Path.GetFileNameWithoutExtension( location );
		if ( contents.ReadToFollowing( "RootNamespace" ) ) {
			@namespace = contents.ReadElementContentAsString();
		}
		var rootNamespace = @namespace;
		WriteLine( $"The root namespace is {esc( 'G' )}{@namespace}{esc( ':' )}" );
		@namespace = @namespace + ".Localisation";
		while ( true ) {
			WriteLine( $"We will use {esc( 'G' )}{@namespace}{esc( ':' )} for l12n files" );
			WriteLine( "Is that okay or do you want to change it?" );
			if ( Select( new[] { "Okay", "Change" } ) == "Okay" ) {
				break;
			}
			else {
				var ns = Prompt();
				if ( new Regex( "^[a-zA-Z_][a-zA-Z_0-9]*(\\.[a-zA-Z_][a-zA-Z_0-9]*)*$" ).IsMatch( ns ) ) {
					@namespace = ns;
				}
				else {
					Error( "That is not a valid namespace" );
				}
			}
		}

		if ( string.IsNullOrWhiteSpace( config.L12NFilesLocation ) ) {
			WriteLine( $"Where would you like to store the {esc( 'G' )}.json{esc( ':' )} files?" );
			const string project = "In the project files";
			const string here = "Next to this executable";
			const string custom = "Somewhere else";
			var store = Select( new[] { project, here, custom } );
			switch ( store ) {
				case here:
					store = "./Source";
					break;

				case project:
					var name = @namespace;
					if ( name.StartsWith( rootNamespace + "." ) )
						name = name[( rootNamespace.Length + 1 )..];
					else if ( name == rootNamespace )
						name = "";

					store = Path.Combine( location, "..", name.Replace( '.', Path.DirectorySeparatorChar ) );
					break;

				case custom:
					WriteLine( "Where? (if the path doesn't exist, it will be created)" );
					store = Prompt( loc => {
						try {
							Path.GetFullPath( loc );
							if ( File.Exists( loc ) ) {
								Error( "A file with that name exists there -- can't create a directory" );
								return false;
							}
							return true;
						}
						catch {
							Error( "Not a valid path, or I have no access to it" );
							return false;
						}
					} );
					break;
			}
			config.L12NFilesLocation = store;
		}

		config.ProjectPath = location;
		config.Namespace = @namespace;
		config.RootNamespace = rootNamespace;
	}
}