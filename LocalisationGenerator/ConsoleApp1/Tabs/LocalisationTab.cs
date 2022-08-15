using LocalisationGenerator.Curses;
using LocalisationGenerator.UI;

namespace LocalisationGenerator.Tabs;

public class LocalisationTab : Window {
	Project project;
	public TextBox TextBox = new();
	public bool Editing = false;
	public bool IsFocused;

	static string edit = $"{Underscore(Blue("E"))}dit";
	static string editArgs = $"Edit {Underscore( Blue( "V" ) )}ariables";
	static string changeGuide = $"Change {Underscore( Blue( "G" ) )}uide";
	static string addMissing = $"Add next {Underscore( Blue( "M" ) )}issing string";
	Dropdown<string> dropdown = new();

	bool selectingGuide;
	Dropdown<Locale> guideDropdown = new( i => $"{i.Name} ({i.ISO})" );

	bool selectingVariable;
	Dropdown<string> variableDropdown;
	public TextBox? VariableTextbox;

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
		guideDropdown.Selected += newGuide => {
			guide = newGuide.Strings[key!.Key];
			selectingGuide = false;
		};

		variableDropdown = new( i => $"{i}: {Green(sampleArgs[i])}" );
		variableDropdown.Selected += onVariableSelected;
	}

	void onVariableSelected ( string arg ) {
		selectingVariable = false;
		VariableTextbox = new() {
			Placeholder = "sample text",
		};
		VariableTextbox.InsertString( sampleArgs[arg] );
	}

	void updateDropdown () {
		dropdown.Options.Clear();

		if ( key != null )
			dropdown.Options.Add( edit );

		if ( project.GetMissingStrings( Locale ).Any() )
			dropdown.Options.Add( addMissing );
		if ( key?.Args.Any() == true )
			dropdown.Options.Add( editArgs );
		if ( key != null && project.LocalesContainingKey[key.Key].Count > 2 )
			dropdown.Options.Add( changeGuide );
	}

	public void Draw () {
		updateDropdown();
		WriteLine( $"Locale: {Yellow( $"{Locale.Name} ({Locale.ISO})" )}", performLayout: true );

		if ( key == null ) {
			WriteLine( "No key selected...", performLayout: true );
			selectingGuide = false;
			selectingVariable = false;
		}
		else {
			WriteLine( $"Key: {Yellow( key.Key )}\n", performLayout: true );
			if ( guide != null ) {
				Write( $"Guide ({guideLocale.ISO}): {Red( "\"" )}", performLayout: true );
				WriteLine( $"{guide.ColoredValue}{Red( "\"" )}", performLayout: true );
			}
			else {
				WriteLine( $"Guide: {Red( "None" )}", performLayout: true );
			}

			Write( $"Value: {Red( "\"" )}", performLayout: true );
			if ( Editing ) {
				TextBox.Placeholder = $"Text goes here...{Red( "\"" )}";
				TextBox.Draw( this, wrap: true, colorizedText: $"{key.ColoredValue}{Red( "\"" )}" );
				WriteLine();
			}
			else {
				WriteLine( $"{key.ColoredValue}{Red( "\"" )}", performLayout: true );
			}

			WriteLine();

			drawResult( key );
		}

		WriteLine();

		if ( selectingVariable || VariableTextbox != null ) { }
		else if ( selectingGuide ) {
			WriteLine( "Select guide:" );
			guideDropdown.Draw( this );
		}
		else {
			dropdown.Draw( this, !Editing && IsFocused );
		}
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

			if ( !selectingVariable ) {
				if ( VariableTextbox != null && i == variableDropdown.Options[variableDropdown.SelectedIndex] ) {
					Write( $"{i}: ", wrap: false );
					VariableTextbox.Draw( this, wrap: false, colorizedText: Green(VariableTextbox.Text) );
					WriteLine();
				}
				else {
					WriteLine( $"{i}: {Green( arg )}", wrap: false );
				}
			}
		}
		if ( selectingVariable )
			variableDropdown.Draw( this );

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
			if ( VariableTextbox != null ) {
				if ( key.Key is ConsoleKey.Escape or ConsoleKey.Enter or ConsoleKey.Tab ) {
					VariableTextbox = null;
					return true;
				}
				else {
					var r = VariableTextbox.Handle( key );
					sampleArgs[variableDropdown.Options[variableDropdown.SelectedIndex]] = VariableTextbox.Text;
					return r;
				}
			}
			else if ( selectingVariable ) {
				if ( key.Key == ConsoleKey.Escape ) {
					selectingVariable = false;
					return true;
				}
				else
					return variableDropdown.Handle( key );
			}
			else if ( selectingGuide ) {
				if ( key.Key == ConsoleKey.Escape ) {
					selectingGuide = false;
					return true;
				}
				else
					return guideDropdown.Handle( key );
			}
			else if ( key.Key == ConsoleKey.E ) {
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
			else
				return dropdown.Handle( key );
		}
	}

	bool onDropdownSelected ( string option ) {
		if ( !dropdown.Options.Contains( option ) )
			return false;

		if ( option == edit ) {
			Editing = true;
			FocusRequested?.Invoke();
		}
		else if ( option == editArgs ) {
			selectingVariable = true;
			variableDropdown.Options.Clear();
			variableDropdown.Options.AddRange( key!.Args );

			if ( variableDropdown.Options.Count == 1 ) {
				variableDropdown.SelectedIndex = 0;
				onVariableSelected( variableDropdown.Options[0] );
			}
		}
		else if ( option == changeGuide ) {
			selectingGuide = true;
			guideDropdown.Options.Clear();
			guideDropdown.Options.AddRange( project.LocalesContainingKey[ key!.Key ] );
			guideDropdown.Options.Remove( Locale );
		}
		else if ( option == addMissing ) {
			AddMissingRequested?.Invoke();
		}

		return true;
	}

	public event Action? FocusRequested;
	public event Action? AddMissingRequested;
}
