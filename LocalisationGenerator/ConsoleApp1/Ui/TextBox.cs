// Literally copied from osu!framework lol
// credit to the devs of https://github.com/ppy/osu-framework/blob/master/osu.Framework/Graphics/UserInterface/TextBox.cs

using LocalisationGenerator.Curses;

namespace LocalisationGenerator.UI;

public class TextBox {
	public string Placeholder = string.Empty;

	private int lastPrintedCount;
	private List<(int x, int y, bool truncated)> lastLayout = new() { (0, 0, true) };
	private string text = string.Empty;
	public string Text {
		get => text;
		set {
			selectionStart = selectionEnd = 0;
			text = string.Empty;

			insertString( value );
		}
	}
	public string SelectedText => selectionLength > 0 ? Text.Substring( selectionLeft, selectionLength ) : string.Empty;

	public ((int x, int y) from, (int x, int y) to) CaretPosition
		=> (PositionAtIndex( selectionStart ), PositionAtIndex( selectionEnd ));

	public (int x, int y) PositionAtIndex ( int i ) {
		if ( lastPrintedCount < i )
			i = 0;

		var (x, y, _) = lastLayout[i];
		return (x, y);
	}

	public void Draw ( Window window, bool wrap ) {
		lastPrintedCount = 0;
		lastLayout[0] = (window.DrawRect.X + window.CursorX - 1, window.DrawRect.Y + window.CursorY, true);

		if ( string.IsNullOrEmpty( text ) ) window.Write( Placeholder, fg: ConsoleColor.DarkGray, wrap: wrap );
		else {
			while ( lastLayout.Count <= text.Length )
				lastLayout.Add( (0, 0, true) );

			window.Write( $"{text[..selectionLeft]}{Window.RedBg( Window.Black( SelectedText ) )}{text[selectionRight..]}", performLayout: true, wrap: wrap, cb: ( index, pos, symbol, truncated ) => {
				lastLayout[index + 1] = (pos.x, pos.y, truncated);
				lastPrintedCount = index + 1;
			} );

			retainedCursorX ??= PositionAtIndex( selectionEnd ).x;
		}
	}

	public bool Handle ( ConsoleKeyInfo key ) {
		switch ( key ) {
			// Clipboard
			case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.X }:
			case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.C }:
				if ( string.IsNullOrEmpty( SelectedText ) ) return true;

				//clipboard?.SetText( SelectedText );

				if ( key.Key is ConsoleKey.X )
					DeleteBy( 0 );

				return true;

			case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.V }:
				//InsertString( clipboard?.GetText() );
				return true;

			case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.A }:
				selectionStart = 0;
				selectionEnd = text.Length;
				return true;

			// Deletion
			case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.Backspace }:
				DeleteBy( GetBackwardWordAmount() );
				return true;

			case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.Delete }:
				DeleteBy( GetForwardWordAmount() );
				return true;

			case { Key: ConsoleKey.Backspace }:
				DeleteBy( -1 );
				return true;

			case { Key: ConsoleKey.Delete }:
				DeleteBy( 1 );
				return true;

			// Expand selection
			case { Modifiers: ConsoleModifiers.Shift, Key: ConsoleKey.LeftArrow }:
				retainedCursorX = null;
				ExpandSelectionBy( -1 );
				return true;

			case { Modifiers: ConsoleModifiers.Shift, Key: ConsoleKey.RightArrow }:
				retainedCursorX = null;
				ExpandSelectionBy( 1 );
				return true;

			case { Modifiers: ConsoleModifiers.Control | ConsoleModifiers.Shift, Key: ConsoleKey.LeftArrow }:
				retainedCursorX = null;
				ExpandSelectionBy( GetBackwardWordAmount() );
				return true;

			case { Modifiers: ConsoleModifiers.Control | ConsoleModifiers.Shift, Key: ConsoleKey.RightArrow }:
				retainedCursorX = null;
				ExpandSelectionBy( GetForwardWordAmount() );
				return true;

			case { Modifiers: ConsoleModifiers.Shift, Key: ConsoleKey.UpArrow }:
			case { Modifiers: ConsoleModifiers.Control | ConsoleModifiers.Shift, Key: ConsoleKey.UpArrow }:
				ExpandSelectionBy( GetBackwardLineAmount() );
				return true;

			case { Modifiers: ConsoleModifiers.Shift, Key: ConsoleKey.DownArrow }:
			case { Modifiers: ConsoleModifiers.Control | ConsoleModifiers.Shift, Key: ConsoleKey.DownArrow }:
				ExpandSelectionBy( GetForwardLineAmount() );
				return true;

			// Cursor Manipulation
			case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.LeftArrow }:
				retainedCursorX = null;
				MoveCursorBy( GetBackwardWordAmount() );
				return true;

			case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.RightArrow }:
				retainedCursorX = null;
				MoveCursorBy( GetForwardWordAmount() );
				return true;

			case { Key: ConsoleKey.UpArrow }:
				MoveCursorBy( GetBackwardLineAmount( breakRetain: true ) );
				return true;

			case { Key: ConsoleKey.DownArrow }:
				MoveCursorBy( GetForwardLineAmount( breakRetain: true ) );
				return true;

			case { Key: ConsoleKey.LeftArrow }:
				retainedCursorX = null;
				MoveCursorBy( -1 );
				return true;

			case { Key: ConsoleKey.RightArrow }:
				retainedCursorX = null;
				MoveCursorBy( 1 );
				return true;

			case { Key: ConsoleKey.Enter }:
				insertString( "\n" );
				return true;

			case { Key: ConsoleKey.Tab }:
				insertString( "    " );
				return true;
		}

		if ( !char.IsControl( key.KeyChar ) ) {
			InsertString( key.KeyChar.ToString() );
			return true;
		}
		return false;
	}

	protected int GetBackwardWordAmount () {
		if ( selectionEnd == 0 )
			return 0;

		int count = -1;
		int index = selectionEnd - 1;
		while ( index > 0 ) {
			if ( char.IsLetterOrDigit( text[index] ) && !char.IsLetterOrDigit( text[index - 1] ) )
				break;

			count--;
			index--;
		}

		return count;
	}

	protected int GetForwardWordAmount () {
		if ( selectionEnd == text.Length )
			return 0;

		int count = 1;
		int index = selectionEnd + 1;
		while ( index < text.Length ) {
			if ( !char.IsLetterOrDigit( text[index] ) && char.IsLetterOrDigit( text[index - 1] ) )
				break;

			count++;
			index++;
		}

		return count;
	}

	int? retainedCursorX;
	protected int GetBackwardLineAmount ( bool breakRetain = false ) {
		int i = selectionEnd;
		int count = -1;
		var (x, y) = PositionAtIndex( i-- );
		x = retainedCursorX ?? x;
		while ( i > 0 ) {
			var pos = PositionAtIndex( i );
			if ( pos.y == y - 1 && pos.x <= x )
				break;

			i--;
			count--;
		}

		if ( i == 0 && breakRetain )
			retainedCursorX = null;
		return count;
	}

	protected int GetForwardLineAmount ( bool breakRetain = false ) {
		int i = selectionEnd;
		int count = 1;
		var (x, y) = PositionAtIndex( i++ );
		x = retainedCursorX ?? x;
		int lastValid = count;
		while ( i < lastPrintedCount ) {
			var pos = PositionAtIndex( i );
			if ( pos.y > y + 1 )
				break;

			if ( pos.y == y + 1 ) {
				lastValid = count;
				if ( pos.x >= x )
					break;
			}

			i++;
			count++;
		}

		if ( i == lastPrintedCount ) {
			if ( breakRetain ) retainedCursorX = null;
			return count;
		}
		return lastValid;
	}

	protected void MoveCursorBy ( int amount ) {
		selectionStart = selectionEnd;
		moveSelection( amount, false );
	}

	protected void ExpandSelectionBy ( int amount ) {
		moveSelection( amount, true );
	}

	protected void DeleteBy ( int amount ) {
		retainedCursorX = null;

		if ( selectionLength == 0 )
			selectionEnd = Math.Clamp( selectionStart + amount, 0, text.Length );

		if ( selectionLength > 0 ) removeSelection();
	}

	private int selectionStart;
	private int selectionEnd;

	private int selectionLength => Math.Abs( selectionEnd - selectionStart );

	private int selectionLeft => Math.Min( selectionStart, selectionEnd );
	private int selectionRight => Math.Max( selectionStart, selectionEnd );

	private void moveSelection ( int offset, bool expand ) {
		if ( expand )
			selectionEnd = Math.Clamp( selectionEnd + offset, 0, text.Length );
		else {
			if ( selectionLength > 0 && Math.Abs( offset ) <= 1 ) {
				//we don't want to move the location when "removing" an existing selection, just set the new location.
				if ( offset > 0 )
					selectionEnd = selectionStart = selectionRight;
				else
					selectionEnd = selectionStart = selectionLeft;
			}
			else
				selectionEnd = selectionStart = Math.Clamp( ( offset > 0 ? selectionRight : selectionLeft ) + offset, 0, text.Length );
		}
	}

	private string removeSelection () => removeCharacters( selectionLength );

	private string removeCharacters ( int number = 1 ) {
		if ( text.Length == 0 )
			return string.Empty;

		int removeStart = Math.Clamp( selectionRight - number, 0, selectionRight );
		int removeCount = selectionRight - removeStart;

		if ( removeCount == 0 )
			return string.Empty;

		string removedText = text.Substring( removeStart, removeCount );

		text = text.Remove( removeStart, removeCount );

		selectionStart = selectionEnd = removeStart;

		return removedText;
	}

	protected void InsertString ( string value ) {
		retainedCursorX = null;
		insertString( value );
	}

	private void insertString ( string value ) {
		if ( string.IsNullOrEmpty( value ) ) return;

		foreach ( char c in value ) {
			//if ( char.IsControl( c ) ) {
			//	continue;
			//}

			if ( selectionLength > 0 )
				removeSelection();

			text = text.Insert( selectionLeft, c.ToString() );
			selectionStart = selectionEnd = selectionLeft + 1;
		}
	}
}
