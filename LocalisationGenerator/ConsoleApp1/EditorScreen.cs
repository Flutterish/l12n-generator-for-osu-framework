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

		localisation = new( project );

		tree = new( project.GetLocaleNamespace( locale ), project );
		AttachWindow( localisation );
		AttachWindow( tree );

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
			w.CursorY = 0;
			w.CursorX = 2;
			if ( w == focused )
				w.Write( text, fg: ConsoleColor.Black, bg: ConsoleColor.Yellow );
			else
				w.Write( text, fg: ConsoleColor.Yellow );
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
				if ( tree.Selector.Handle( key ) )
					continue;

				if ( key.Key == ConsoleKey.R ) {
					var (_, ns, k) = tree.Selector.Options[tree.Selector.SelectedIndex];
					if ( k != null ) {
						project.ToggleKeyRemoval( currentLocale, ns.Value.Keys[k] );
					}
					else if ( ns.Parent != null ) {
						project.ToggleNamespaceRemoval( currentLocale, ns.Value );
					}
				}
				else if ( key.Key == ConsoleKey.N ) {

				}
			}
		}
	}
}
