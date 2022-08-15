using LocalisationGenerator.Curses;
using LocalisationGenerator.Tabs;

namespace LocalisationGenerator;

public class EditorScreen : ConsoleWindow {
	Window focused;
	Locale currentLocale;
	LocalisationTab localisation;
	KeyTreeTab tree;
	Project project;

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

		if ( str != null )
			focused = localisation;
		else
			focused = tree;
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
				w.Write( "[?]" );
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

		if ( tree.TextBox != null ) {
			CursorVisible = true;
			var (_, (x, y)) = tree.TextBox.CaretPosition;
			SetCursor( tree.X + x + 1, tree.Y + y );
		}
		else {
			CursorVisible = false;
		}
	}

	public void Run () {
		while ( true ) {
			if ( !KeyAvailable ) {
				Draw();
				Refresh();
			}

			var key = ReadKey();

			if ( key.Key == ConsoleKey.F1 ) {
				focused = localisation;
			}
			else if ( key.Key == ConsoleKey.F2 ) {
				focused = tree;
			}
			else {
				if ( key.Key is >= ConsoleKey.NumPad0 and <= ConsoleKey.NumPad9 ) {
					tree.Handle( key );
				}
				else {
					tree.Handle( key );
				}
			}
		}
	}
}
