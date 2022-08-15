using LocalisationGenerator.Curses;

namespace LocalisationGenerator.UI;

public class Dropdown<T> {
	public Func<T, string> Stringifier;
	public List<T> Options = new();
	public int SelectedIndex;

	public string LeftSelection = "<";
	public string RightSelection = ">";

	public Dropdown ( Func<T, string>? stringifier = null ) {
		Stringifier = stringifier ?? ( x => $"{x}" );
	}

	public void Draw ( Window window, bool drawSelected = true, int? heightLimit = null ) {
		if ( Options.Count != 0 )
			SelectedIndex = Math.Clamp( SelectedIndex, 0, Options.Count - 1 );

		var x = window.CursorX;
		var startY = window.CursorY;
		var height = Math.Min( window.DrawRect.Height - startY, heightLimit ?? int.MaxValue );

		if ( height < 3 )
			return;

		var half = height / 2;
		var otherHalf = height - half;
		int from = Math.Max( 0, SelectedIndex - half );
		int to = Math.Min( Options.Count, SelectedIndex + otherHalf );
		if ( from == 0 ) to = Math.Min( Options.Count, from + height );
		else if ( to == Options.Count ) {
			from = Math.Max( 0, to - height );
		}

		for ( int i = from; i < to; i++ ) {
			if ( from != 0 && i == from ) window.WriteLine( Window.Yellow( " ^" ), wrap: false );
			else if ( to != Options.Count && i == to - 1 ) {
				window.WriteLine( Window.Yellow( " v" ), wrap: false );
			}
			else {
				if ( i == SelectedIndex && drawSelected ) {
					window.Write( $"{Window.Yellow( LeftSelection )}{Stringifier( Options[i] )}{Window.Yellow( RightSelection )}", wrap: false );
					window.CursorX -= RightSelection.Length;
					window.WriteLine( Window.Yellow( RightSelection ), wrap: false );
				}
				else {
					window.Write( $"{new string( ' ', LeftSelection.Length )}{Stringifier( Options[i] )}{new string( ' ', RightSelection.Length )}", wrap: false );
					window.CursorX -= RightSelection.Length;
					window.WriteLine( new string( ' ', RightSelection.Length ), wrap: false );
				}
			}

			window.CursorX = x;
		}
	}

	protected virtual bool TryHandle ( ConsoleKeyInfo key ) {
		return false;
	}

	public bool Handle ( ConsoleKeyInfo key ) {
		if ( Options.Count == 0 )
			return false;
			
		SelectedIndex = Math.Clamp( SelectedIndex, 0, Options.Count - 1 );

		if ( TryHandle( key ) )
			return true;

		switch ( key ) {
			case { Key: ConsoleKey.UpArrow or ConsoleKey.NumPad8, Modifiers: ConsoleModifiers.Control } or { Key: ConsoleKey.LeftArrow or ConsoleKey.NumPad4 }:
				SelectedIndex = 0;
				return true;

			case { Key: ConsoleKey.DownArrow or ConsoleKey.NumPad2, Modifiers: ConsoleModifiers.Control } or { Key: ConsoleKey.RightArrow or ConsoleKey.NumPad6 }:
				SelectedIndex = Options.Count - 1;
				return true;

			case { Key: ConsoleKey.UpArrow or ConsoleKey.NumPad8 }:
				if ( SelectedIndex != 0 )
					SelectedIndex--;
				return true;

			case { Key: ConsoleKey.DownArrow or ConsoleKey.NumPad2 }:
				if ( SelectedIndex != Options.Count - 1 )
					SelectedIndex++;
				return true;

			case var k when k.IsConfirmAction():
				Selected?.Invoke( Options[SelectedIndex] );
				return true;
		}

		return false;
	}

	public event Action<T>? Selected;
}
