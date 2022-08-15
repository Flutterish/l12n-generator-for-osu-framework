using LocalisationGenerator.Curses;
using LocalisationGenerator.UI;

namespace LocalisationGenerator.Tabs;

public class LocalisationTab : Window {
	Project project;
	public TextBox TextBox = new();
	public bool Editing = false;

	static string edit = $"{Underscore(Blue("E"))}dit";
	static string editArgs = $"Edit {Underscore( Blue( "V" ) )}ariables";
	static string changeGuide = $"Change {Underscore( Blue( "G" ) )}uide";
	static string addMissing = $"Add next {Underscore( Blue( "M" ) )}issing string";
	Dropdown<string> dropdown = new();

	Locale guideLocale => project.Locales[guide!.Locale];
	LocalisableString? guide;
	
	public Locale Locale;
	LocalisableString? key;
	public LocalisableString? String {
		get => key;
		set {
			if ( key == value )
				return;

			if ( value == null ) {
				guide = null;
				TextBox.Text = "";
				Editing = false;
			}
			else {
				if ( guide != null && project.Locales[guide.Locale].Strings.TryGetValue( value.Key, out var newGuide ) )
					guide = newGuide;
				else
					guide = project.GetBestGuide( Locale, value.Key );

				TextBox.Text = value.Value;
			}

			key = value;
		}
	}

	public LocalisationTab ( Project project, Locale locale ) {
		this.project = project;
		Locale = locale;

		dropdown.Selected += op => onDropdownSelected( op );
	}

	void updateDropdown () {
		dropdown.Options.Clear();

		if ( key == null )
			return;

		dropdown.Options.Add( edit );
		if ( project.GetMissingStrings( Locale ).Any() )
			dropdown.Options.Add( addMissing );
		if ( key.Args.Any() )
			dropdown.Options.Add( editArgs );
		if ( project.LocalesContainingKey[key.Key].Count > 2 )
			dropdown.Options.Add( changeGuide );
	}

	public void Draw () {
		updateDropdown();
		WriteLine( $"Locale: {Yellow( $"{Locale.Name} ({Locale.ISO})" )}", performLayout: true );

		if ( key == null ) {
			WriteLine( "No key selected...", performLayout: true );
			return;
		}
		WriteLine( $"Key: {Yellow( key.Key )}\n", performLayout: true );
		if ( guide != null ) {
			Write( $"Guide ({guideLocale.ISO}): {Red("\"")}", performLayout: true );
			WriteLine( $"{guide.ColoredValue}{Red( "\"" )}", performLayout: true );
		}
		else {
			WriteLine( $"Guide: {Red( "None" )}", performLayout: true );
		}

		Write( $"Value: {Red( "\"" )}", performLayout: true );
		if ( Editing ) {
			TextBox.Placeholder = $"Text goes here...{Red("\"")}";
			TextBox.Draw( this, wrap: true, colorizedText: $"{key.ColoredValue}{Red( "\"" )}" );
			WriteLine();
		}
		else {
			WriteLine( $"{key.ColoredValue}{Red( "\"" )}", performLayout: true );
		}

		WriteLine();

		drawResult( key );

		WriteLine();

		dropdown.Draw( this, !Editing );
	}

	Dictionary<string, int> indices = new();
	Dictionary<string, string> sampleArgs = new();
	void drawResult ( LocalisableString key ) {
		var args = key.Args.ToList();
		
		if ( args.Any() ) 
			WriteLine( "Sample arguments:", performLayout: true );
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

		WriteLine( "\nResult:", performLayout: true );
		try {
			Write( key.ColoredFormat( argList, indices ), performLayout: true, showFormatting: true );
			WriteLine();
		}
		catch {
			WriteError( "Incorrectly formated value", performLayout: true );
		}
		if ( guide != null ) {
			WriteLine( "\nGuide:", performLayout: true );
			bool okay = true;
			var missing = key.Args.Except( guide.Args );
			if ( missing.Any() ) {
				WriteError( $"This string has arguments the guide doesn't have: {string.Join( ", ", missing )}", performLayout: true );
				okay = false;
			}
			missing = guide.Args.Except( key.Args );
			if ( missing.Any() ) {
				WriteError( $"This string doesn't have arguments the guide has: {string.Join( ", ", missing )}", performLayout: true );
				okay = false;
			}

			try {
				if ( okay ) {
					Write( guide.ColoredFormat( argList, indices ), performLayout: true, showFormatting: true );
					WriteLine();
				}
			}
			catch {
				WriteError( "Incorrectly formated value", performLayout: true );
			}
		}
	}

	public void DrawHelp ( Window window ) {
		window.Clear();
		window.DrawBorder();
		window.CursorX = 2;
		window.CursorY = 0;
		window.Write( "Help [Edit Tab]", wrap: false );
		window.PushScissors( window.DrawRect with { X = 1, Y = 1, Width = window.Width - 2, Height = window.Height - 2 } );

		window.SetCursor( 0, 0 );
		window.WriteLine( $"You can edit and preview strings in this tab", performLayout: true );
		window.WriteLine();
		window.WriteLine( $"You can create placeholders with variable names surrounded by {Underscore(Green( "{}" ))} - for example {Red( "\"" )}Hello, {Green( "{name}" )}!{Red( "\"" )}", performLayout: true );
		window.WriteLine( $"Tabs and new-lines can be created with {Red( "\\t" )} and {Red( "\\n" )} respectively", performLayout: true );
		window.WriteLine( $"Literal {Underscore("{")}, {Underscore( "}" )} and {Underscore( "\\" )} can be created by doubling them up like this: {Red( "\"" )}{Gray("{")}{{ and }}{Gray( "}" )} and {Gray( "\\" )}\\{Red( "\"" )}", performLayout: true );
		window.WriteLine( $"Backslashes {Underscore( "\\" )} do not need to be doubled up if an {Underscore( "n" )} or {Underscore( "t" )} does not follow them", performLayout: true );
		window.WriteLine();
		window.WriteLine( $"You can specify how numbers, dates and such should be formated with a {Underscore(":")} inside a placeholder, for example {Green($"{{number{Cyan(":N2")}}}")}", performLayout: true );
		window.WriteLine( $"For more info refer to {Underscore( Cyan( "https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings" ) )}", performLayout: true );
		window.WriteLine();
		window.WriteLine( $"Press {Underscore( Blue( "E" ) )} to edit the current string", performLayout: true );
		window.WriteLine( $"Press {Underscore( Blue( "M" ) )} to edit the next missing string, if available", performLayout: true );
		window.WriteLine( $"Press {Underscore( Blue( "V" ) )} to edit sample variables if there are any", performLayout: true );
		window.WriteLine( $"Press {Underscore( Blue( "G" ) )} to change the guide string if there are other languages with this string", performLayout: true );

		window.PopScissors();
	}

	public bool Handle ( ConsoleKeyInfo key ) {
		if ( Editing ) {
			if ( key.Key is ConsoleKey.Enter or ConsoleKey.Escape ) {
				Editing = false;
				return true;
			}
			else if ( key.Key is ConsoleKey.Tab ) {
				TextBox.InsertString( "\\t" );
				this.key!.Value = TextBox.Text;
				return true;
			}

			var r = TextBox.Handle( key );
			this.key!.Value = TextBox.Text;
			return r;
		}
		else {
			if ( key.Key == ConsoleKey.E ) {
				return onDropdownSelected( edit );
			}
			else if ( key.Key == ConsoleKey.V ) {
				return onDropdownSelected( editArgs );
			}
			else if ( key.Key == ConsoleKey.G ) {
				return onDropdownSelected( changeGuide );
			}
			else if ( key.Key == ConsoleKey.M ) {
				return onDropdownSelected( addMissing );
			}
			else if ( dropdown.Handle( key ) )
				return true;
		}

		return false;
	}

	bool onDropdownSelected ( string option ) {
		if ( !dropdown.Options.Contains( option ) )
			return false;

		if ( option == edit ) {
			Editing = true;
			FocusRequested?.Invoke();
		}
		else if ( option == editArgs ) {

		}
		else if ( option == changeGuide ) {

		}
		else if ( option == addMissing ) {

		}

		return true;
	}

	public event Action? FocusRequested;
}
