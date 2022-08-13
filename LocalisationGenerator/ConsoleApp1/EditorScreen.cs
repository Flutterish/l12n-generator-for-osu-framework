using LocalisationGenerator.Curses;

namespace LocalisationGenerator;

public class EditorScreen : ConsoleWindow {
	Window left;
	Window right;
	Program program;
	Window focused;
	public EditorScreen ( Program program ) {
		this.program = program;

		var width = (int)( Width * 0.75 );
		left = new Window( width, Height );
		right = new Window( Width - width, Height ) { X = width };
		AttachWindow( left );
		AttachWindow( right );

		focused = left;
	}

	protected override void Draw () {
		left.Scissors = null;
		right.Scissors = null;
		left.Clear();
		right.Clear();

		var width = (int)( Width * 0.75 );
		left.Resize( width, Height );
		right.Resize( Width - width, Height );
		right.X = width;

		left.DrawBorder();
		right.DrawBorder();

		void label ( Window w, string text ) {
			w.CursorY = 0;
			w.CursorX = 2;
			if ( w == focused )
				w.Write( text, fg: ConsoleColor.Black, bg: ConsoleColor.Yellow );
			else
				w.Write( text, fg: ConsoleColor.Yellow );
		}

		label( left, "  F1  " );
		label( right, "  F2  " );

		right.Scissors = right.LocalRect with { X = 1, Y = 1, Width = right.Width - 2, Height = right.Height - 2 };
		left.Scissors = left.LocalRect with { X = 1, Y = 1, Width = left.Width - 2, Height = left.Height - 2 };
		right.SetCursor( 0, 0 );
		left.SetCursor( 0, 0 );
	}

	public void Run () {
		while ( true ) {
			Draw();
			Refresh();

			var key = ReadKey();

			if ( key.Key == ConsoleKey.F1 ) {
				focused = left;
			}
			else if ( key.Key == ConsoleKey.F2 ) {
				focused = right;
			}
			else {
				break;
			}
		}
	}
}
