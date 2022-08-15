using LocalisationGenerator;
using LocalisationGenerator.Curses;
using Newtonsoft.Json;
using System.Diagnostics;
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
		=> $"{esc( 'B' )}{str}{esc( ':' )}";

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
	Locale currentLocale = null!;
	void Edit ( Locale locale ) {
		currentLocale = locale;
		EditorScreen screen = new( project, locale );

		screen.Run();
		Console.CursorVisible = true;

		return;





		string edit = "Edit string";
		string addMissing = "Add missing string";
		string add = "Add new string";
		string remove = $"{esc( 'R' )}Remove string{esc( ':' )}";

		List<string> options = new();
		int keyIndex = 0;
		LocalisableString? selectString () {
			selectIndex = keyIndex;
			keepSelectIndex = true;
			var key = Select( locale.Strings.Keys.OrderBy( x => x ).ToList(), s => $"{Yellow( s )}: {locale.Strings[s].ColoredValue}", allowCancel: true );
			if ( key == null )
				return null;

			keyIndex = selectIndex;
			return locale.Strings[key];
		}
		List<string> missing = new();
		void updateMissing () {
			missing.Clear();
			foreach ( var (key, list) in localesContainingKey ) {
				if ( !list.Contains( locale ) )
					missing.Add( key );
			}
		}
		updateMissing();
		while ( true ) {
			Split();
			WriteLine( $"Locale: {esc( 'Y' )}{locale.Name} [{locale.ISO}]{esc( ':' )}" );

			options.Clear();

			if ( missing.Any() )
				options.Add( addMissing );
			options.Add( add );
			if ( locale.Strings.Count != 0 )
				options.Add( edit );
			if ( locale.Strings.Count != 0 )
				options.Add( remove );

			var option = Select( options, allowCancel: true );
			Split();
			if ( option == null ) {
				return;
			}

			select:
			if ( option == edit ) {
				var str = selectString();
				if ( str == null )
					continue;

				keyIndex = selectIndex;
				var op = EditString( str, missing.Any() );
				project.Save( locale );
				switch ( op ) {
					case 1:
						option = addMissing;
						goto select;
					case 2:
						option = add;
						goto select;
				}
			}
			else if ( option == addMissing ) {
				var guides = new Dictionary<string, string>();
				foreach ( var k in missing.OrderBy( x => x ) ) {
					var possibleGuides = localesContainingKey[k].ToList();
					var guideLocale = possibleGuides.FirstOrDefault( x => x == mainlocale ) ?? possibleGuides.ElementAtOrDefault( 0 );
					var guideStr = guideLocale?.Strings[k];
					guides.Add( k, guideStr is null ? Yellow(k) : $"{Yellow(k)}: {Red("\"")}{guideStr.ColoredValue}{Red( "\"" )}" );
				}

				var key = Select( missing, k => guides[k], allowCancel: true );
				if ( key == null )
					continue;

				LocalisableString str = new( key, locale.ISO );
				locale.Strings.Add( key, str );
				project.OnLocaleKeyAdded( locale, key );
				updateMissing();

				var op = EditString( str, missing.Any() );
				project.Save(locale );
				switch ( op ) {
					case 1:
						option = addMissing;
						goto select;
					case 2:
						option = add;
						goto select;
				}
			}
			else if ( option == add ) {
				WriteLine( "Key:" );
				WriteLine( $"You can group keys with dots, for example {Yellow("chat.send")} or {Yellow("options.general")}" );
				var key = Prompt().Trim();
				if ( locale.Strings.ContainsKey( key ) )
					Error( "Key already exists" );
				else if ( !keyRegex.IsMatch( key ) )
					Error( "Invalid key" );
				else {
					LocalisableString str = new( key, locale.ISO );
					locale.Strings.Add( key, str );
					project.OnLocaleKeyAdded( locale, key );
					updateMissing();

					var op = EditString( str, missing.Any() );
					project.Save( locale );
					switch ( op ) {
						case 1:
							option = addMissing;
							goto select;
						case 2:
							option = add;
							goto select;
					}
				}
			}
			else if ( option == remove ) {
				var str = selectString();
				if ( str == null )
					continue;

				locale.Strings.Remove( str.Key );
				project.OnLocaleKeyRemoved( locale, str.Key );
				updateMissing();
				project.Save( locale );
			}
		}
	}

	Dictionary<string, string> sampleArgs = new();
	int EditString ( LocalisableString str, bool anyMissing ) {
		var immediateEdit = true;
		var edit = "Edit";
		var editArgs = "Edit sample arguments";
		var changeGuide = "Change guide";
		var finish = $"{esc( 'R' )}Finish{esc( ':' )}";
		var addMissing = "Add next missing string";
		var addNext = "Add next string";

		var possibleGuides = localesContainingKey[str.Key].Where( x => x != currentLocale ).ToList();
		var guideLocale = possibleGuides.FirstOrDefault( x => x == mainlocale ) ?? possibleGuides.ElementAtOrDefault( 0 );
		var guideStr = guideLocale?.Strings[str.Key];

		List<string> options = new();

		Dictionary<string, int> indices = new();
		void showResult () {
			var args = str.Args.ToList();
			Write( "\r\u001B[0J" );
			if ( args.Any() ) WriteLine( "Sample arguments:" );
			var argList = new object[args.Count];
			indices.Clear();
			int index = 0;
			foreach ( var i in args ) {
				if ( !sampleArgs.TryGetValue( i, out var arg ) ) {
					sampleArgs.Add( i, arg = "sample text" );
				}

				indices.Add( i, index );
				if ( double.TryParse( arg, out var number ) )
					argList[index++] = number;
				else if ( DateTime.TryParse( arg, out var date ) )
					argList[index++] = date;
				else
					argList[index++] = arg;

				WriteLine( $"{i}: {Green( arg )}" );
			}

			WriteLine( "\nResult:" );
			try {
				WriteLine( str.ColoredFormat( argList, indices ) );
			}
			catch {
				Error( "Incorrectly formated value" );
			}
			if ( guideStr != null ) {
				WriteLine( "\nGuide:" );
				bool okay = true;
				var missing = str.Args.Except( guideStr.Args );
				if ( missing.Any() ) {
					Error( $"This string has arguments the guide doesn't have: {string.Join( ", ", missing )}" );
					okay = false;
				}
				missing = guideStr.Args.Except( str.Args );
				if ( missing.Any() ) {
					Error( $"This string doesn't have arguments the guide has: {string.Join( ", ", missing )}" );
					okay = false;
				}

				try {
					if ( okay ) WriteLine( guideStr.ColoredFormat( argList, indices ) );
				}
				catch {
					Error( "Incorrectly formated value" );
				}
			}
			WriteLine( "" );
		}

		while ( true ) {
			Console.Clear();
			WriteLine( $"Locale: {esc( 'Y' )}{currentLocale.Name} [{currentLocale.ISO}]{esc( ':' )}" );
			WriteLine( $"Key: {esc('Y')}{str.Key}{esc(':')}\n" );
			WriteLine( $"To create a place for a value to be inserted, use a number or text surrounded by {Green("{}")}, for example {Red("\"")}Hello, {Green("{name}")}!{Red( "\"" )}" );
			WriteLine( $"To insert a tab or new-line you can use {Red( "\\t" )} and {Red( "\\n" )} respectively" );
			WriteLine( $"To insert a literal {{, }} or \\, double them up like {Red( "\"" )}{{{{ and }}}} and \\\\{Red( "\"" )}" );
			WriteLine( $"You can also specify how numbers and dates should be formated like {Green($"{{number{Cyan(":N2")}}}")}" );
			WriteLine( $"For more info refer to {Cyan( "https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings" )}\n" );
			if ( guideStr != null ) {
				WriteLine( $"Guide [{guideLocale!.ISO}]: {esc( 'R' )}\"{esc( ':' )}{guideStr.ColoredValue}{esc( 'R' )}\"{esc( ':' )}" );
			}
			else {
				WriteLine( $"Guide: {esc('R')}None{esc(':')}" );
			}
			Write( $"Value: {esc( 'R' )}\"{esc( ':' )}" );
			int x = Console.CursorLeft;
			int y = Console.CursorTop;
			WriteLine( $"{str.ColoredValue}{esc( 'R' )}\"{esc( ':' )}\n" );

			int rx = Console.CursorLeft;
			int ry = Console.CursorTop;
			showResult();

			options.Clear();
			if ( anyMissing )
				options.Add( addMissing );
			options.Add( addNext );
			options.Add( edit );
			if ( str.Args.Any() )
				options.Add( editArgs );
			if ( possibleGuides.Count > 1 )
				options.Add( changeGuide );
			options.Add( finish );

			var option = immediateEdit ? edit : Select( options );
			immediateEdit = false;
			if ( option == edit ) {
				EditField( x, y, str.Value, s => {
					str.Value = s;
					Write( $"{str.ColoredValue}{esc( 'R' )}\"{esc( ':' )}\u001b[0J" );
					Console.CursorLeft = rx;
					Console.CursorTop = ry;
					showResult();
				} );
			}
			else if ( option == editArgs ) {
				Split();
				var arg = Select( str.Args.ToList(), allowCancel: true );
				if ( arg is null )
					continue;

				var value = Prompt();
				sampleArgs[arg] = value;
			}
			else if ( option == changeGuide ) {
				Split();
				var guide = Select( possibleGuides, l => $"{l.Name} [{l.ISO}]: {Red( "\"" )}{l.Strings[str.Key].ColoredValue}{Red( "\"" )}", allowCancel: true );
				if ( guide == null )
					continue;

				guideLocale = guide;
				guideStr = guide.Strings[str.Key];
			}
			else if ( option == finish ) {
				Console.Clear();
				return 0;
			}
			else if ( option == addMissing ) {
				Console.Clear();
				return 1;
			}
			else if ( option == addNext ) {
				Console.Clear();
				return 2;
			}
		}
	}

	string EditField ( int x, int y, string str, Action<string>? renderer ) {
		Stopwatch stopwatch = new();
		List<(string, int)> history = new() { (str, 0) };
		int historyIndex = 0;

		var initial = str;
		Console.CursorVisible = false;
		var rx = Console.CursorLeft;
		var ry = Console.CursorTop;
		renderer ??= Console.Write;

		int index = 0;
		while ( true ) {
			Console.CursorLeft = x;
			Console.CursorTop = y;
			Console.Write( new string( ' ', Console.BufferWidth - x ) );
			Console.CursorLeft = x;
			Console.CursorTop = y;
			renderer( str );
			Console.CursorLeft = (x + index) % Console.BufferWidth;
			Console.CursorTop = y + ( x + index ) / Console.BufferWidth;
			Bg( ConsoleColor.White );
			Fg( ConsoleColor.Black );
			Console.Write( index == str.Length ? " " : str[index] );
			Fg( null );
			Bg( null );

			var key = Console.ReadKey();

			if ( stopwatch.ElapsedMilliseconds > 500 ) {
				stopwatch.Stop();
				stopwatch.Reset();

				while ( history.Count > historyIndex + 1 ) {
					history.RemoveAt( history.Count - 1 );
				}
				history.Add( (str, index) );
				historyIndex++;
			}

			bool isAtWordBoundary () {
				return ( str[index - 1] == ' ' ) != ( str[index] == ' ' );
			}

			bool edited = false;
			if ( key.Key is ConsoleKey.Escape ) {
				str = initial;
				Console.CursorLeft = x;
				Console.CursorTop = y;
				Console.Write( new string( ' ', Console.BufferWidth - x ) );
				Console.CursorLeft = x;
				Console.CursorTop = y;
				renderer( str );
				break;
			}
			else if ( key.Key is ConsoleKey.Enter ) {
				break;
			}
			else if ( key.Key is ConsoleKey.LeftArrow ) {
				index = Math.Max( 0, index - 1 );

				if ( key.Modifiers == ConsoleModifiers.Control ) {
					while ( index != 0 && !isAtWordBoundary() ) {
						index = Math.Max( 0, index - 1 );
					}
				}
			}
			else if ( key.Key is ConsoleKey.RightArrow ) {
				index = Math.Min( str.Length, index + 1 );

				if ( key.Modifiers == ConsoleModifiers.Control ) {
					while ( index != str.Length && !isAtWordBoundary() ) {
						index = Math.Min( str.Length, index + 1 );
					}
				}
			}
			else if ( key.Key is ConsoleKey.UpArrow ) {
				index = Math.Max( 0, index - Console.BufferWidth );
			}
			else if ( key.Key is ConsoleKey.DownArrow ) {
				index = Math.Min( str.Length, index + Console.BufferWidth );
			}
			else if ( key.Key is ConsoleKey.Backspace ) {
				if ( index != 0 ) {
					int to = index;
					index--;
					if ( key.Modifiers == ConsoleModifiers.Control ) {
						while ( index != 0 && !isAtWordBoundary() ) {
							index = Math.Max( 0, index - 1 );
						}
					}

					str = str[0..index] + str[to..];
					edited = true;
				}
			}
			else if ( key.Key is ConsoleKey.Delete ) {
				if ( index != str.Length ) {
					var from = index;

					index++;
					if ( key.Modifiers == ConsoleModifiers.Control ) {
						while ( index != str.Length && !isAtWordBoundary() ) {
							index = Math.Min( str.Length, index + 1 );
						}
					}

					str = str[0..from] + str[index..];
					index = from;
					edited = true;
				}
			}
			else if ( !char.IsControl( key.KeyChar ) ) {
				str = str[0..index] + key.KeyChar + str[index..];
				index++;
				edited = true;
			}
			else if ( (key.Key == ConsoleKey.Y && key.Modifiers == ConsoleModifiers.Control) 
				|| (key.Key == ConsoleKey.Z && key.Modifiers.HasFlag( ConsoleModifiers.Control | ConsoleModifiers.Shift )) ) {
				historyIndex = Math.Min( history.Count - 1, historyIndex + 1 );
				(str, index) = history[historyIndex];
				stopwatch.Stop();
				stopwatch.Reset();
			}
			else if ( key.Key == ConsoleKey.Z && key.Modifiers == ConsoleModifiers.Control ) {
				historyIndex = Math.Max( 0, historyIndex - 1 );
				(str, index) = history[historyIndex];
				stopwatch.Stop();
				stopwatch.Reset();
			}

			if ( edited ) {
				stopwatch.Restart();
			}
		}

		Console.CursorLeft = rx;
		Console.CursorTop = ry;
		Console.CursorVisible = true;
		return str;
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
					'B' => ConsoleColor.Cyan,
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