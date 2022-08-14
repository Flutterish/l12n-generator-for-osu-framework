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

	public void Draw ( Window window, int? heightLimit = null ) {
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
				if ( i == SelectedIndex ) {
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
			case { Key: ConsoleKey.UpArrow, Modifiers: ConsoleModifiers.Control }:
				SelectedIndex = 0;
				return true;

			case { Key: ConsoleKey.DownArrow, Modifiers: ConsoleModifiers.Control }:
				SelectedIndex = Options.Count - 1;
				return true;

			case { Key: ConsoleKey.UpArrow }:
				if ( SelectedIndex != 0 )
					SelectedIndex--;
				return true;

			case { Key: ConsoleKey.DownArrow }:
				if ( SelectedIndex != Options.Count - 1 )
					SelectedIndex++;
				return true;

			case { Key: ConsoleKey.Enter or ConsoleKey.Spacebar or ConsoleKey.Tab }:
				Selected?.Invoke( Options[SelectedIndex] );
				return true;
		}

		return false;
	}

	public event Action<T>? Selected;
}
