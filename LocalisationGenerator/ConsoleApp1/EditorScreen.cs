using LocalisationGenerator.Curses;
using LocalisationGenerator.Tabs;

namespace LocalisationGenerator;

public class EditorScreen : ConsoleWindow {
	Window focused;
	Locale currentLocale;
	LocalisationTab localisation;
	KeyTreeTab tree;
	Project project;
	Window? helpWindow;

	public EditorScreen ( Project project, Locale locale ) {
		this.project = project;
		currentLocale = locale;

		localisation = new( project, locale );

		tree = new( project.GetLocaleNamespace( locale ), project, locale );
		AttachWindow( localisation );
		AttachWindow( tree );

		focused = tree;
		tree.LocaleSelected += SetLocale;
		tree.StringSelected += SetString;

		localisation.FocusRequested += () => focused = localisation;
		localisation.AddMissingRequested += () => {
			if ( tree.NextMissing != null )
				SetString( project.AddKey( currentLocale, tree.NextMissing ) );
			else
				SetString( project.AddKey( currentLocale, project.GetMissingStrings( currentLocale ).First() ) );
		};

		SetLocale( locale );
	}

	public void SetLocale ( Locale newLocale ) {
		currentLocale = newLocale;
		localisation.Locale = newLocale;
		tree.Locale = newLocale;
		var rootNs = project.GetLocaleNamespace( newLocale );
		tree.Tree = new( rootNs );

		if ( localisation.String is LocalisableString str && newLocale.Strings.TryGetValue( str.Key, out var newString ) )
			SetString( newString );
		else
			SetString( null );

		project.UpdateMissing( newLocale );
	}

	public void SetString ( LocalisableString? str ) {
		localisation.String = str;

		if ( str != null ) {
			focused = localisation;
			localisation.Editing = true;

			tree.SelectKey( str.Key );
		}
		else {
			focused = tree;
			localisation.Editing = false;
		}
	}

	protected override void Draw () {
		localisation.Clear();
		tree.Clear();

		var width = (int)( Width * (focused == localisation ? 0.7 : 0.3) );
		localisation.Resize( width, Height );
		tree.Resize( Width - width, Height );
		tree.X = width;

		localisation.DrawBorder();
		tree.DrawBorder();

		void label ( Window w, string text ) {
			if ( w.Width >= 14 ) {
				w.CursorY = 0;
				w.CursorX = w.Width - 5;
				w.Write( $"[{Underscore("?")}]" );
			}

			w.CursorY = 0;
			w.CursorX = 2;
			if ( w == focused )
				w.Write( text, fg: ConsoleColor.Black, bg: ConsoleColor.Yellow, wrap: false );
			else
				w.Write( text, fg: ConsoleColor.Yellow, wrap: false );
		}

		label( localisation, "  F1  " );
		label( tree, "  F2  " );

		tree.PushScissors( tree.LocalRect with { X = 1, Y = 1, Width = tree.Width - 2, Height = tree.Height - 2 } );
		localisation.PushScissors( localisation.LocalRect with { X = 1, Y = 1, Width = localisation.Width - 2, Height = localisation.Height - 2 } );
		tree.SetCursor( 0, 0 );
		localisation.SetCursor( 0, 0 );

		localisation.Draw();
		tree.Draw();

		localisation.PopScissors();
		tree.PopScissors();

		if ( focused == tree && tree.TextBox != null ) {
			CursorVisible = true;
			var (_, (x, y)) = tree.TextBox.CaretPosition;
			SetCursor( tree.X + x + 1, tree.Y + y );
		}
		else if ( focused == localisation && localisation.Editing ) {
			CursorVisible = true;
			var (_, (x, y)) = localisation.TextBox.CaretPosition;
			SetCursor( localisation.X + x + 1, localisation.Y + y );
		}
		else if ( tree.TextBox != null ) {
			CursorVisible = true;
			var (_, (x, y)) = tree.TextBox.CaretPosition;
			SetCursor( tree.X + x + 1, tree.Y + y );
		}
		else if ( localisation.Editing ) {
			CursorVisible = true;
			var (_, (x, y)) = localisation.TextBox.CaretPosition;
			SetCursor( localisation.X + x + 1, localisation.Y + y );
		}
		else if ( localisation.VariableTextbox != null ) {
			CursorVisible = true;
			var (_, (x, y)) = localisation.VariableTextbox.CaretPosition;
			SetCursor( localisation.X + x + 1, localisation.Y + y );
		}
		else {
			CursorVisible = false;
		}

		if ( helpWindow != null ) {
			helpWindow.Resize( (int)( Width * 0.7 ), (int)( Height * 0.7 ) );
			helpWindow.X = ( Width - helpWindow.Width ) / 2;
			helpWindow.Y = ( Height - helpWindow.Height ) / 2;

			if ( focused == tree ) {
				tree.DrawHelp( helpWindow );
			}
			else {
				localisation.DrawHelp( helpWindow );
			}
		}
	}

	public void Run () {
		while ( true ) {
			if ( !KeyAvailable ) {
				localisation.IsFocused = focused == localisation;
				Draw();
				Refresh();
			}

			var key = ReadKey();

			if ( helpWindow != null ) {
				DetachWindow( helpWindow );
				helpWindow = null;
			}
			else if ( key.Key == ConsoleKey.F1 ) {
				focused = localisation;
			}
			else if ( key.Key == ConsoleKey.F2 ) {
				focused = tree;
			}
			else {
				if ( key.Key is >= ConsoleKey.NumPad0 and <= ConsoleKey.NumPad9 ) {
					tree.Handle( key );
				}
				else if ( tree.TextBox != null ) {
					tree.Handle( key );
				}
				else if ( focused == tree ? tree.Handle( key ) : localisation.Handle( key ) ) { }
				else if ( key.KeyChar == '?' ) {
					helpWindow = new();
					AttachWindow( helpWindow );
				}
				else if ( focused == tree ? localisation.Handle( key ) : tree.Handle( key ) ) { }
				else if ( key.Key == ConsoleKey.Escape ) {
					if ( focused == tree )
						break;
					else
						focused = tree;
				}
			}
		}
	}
}
