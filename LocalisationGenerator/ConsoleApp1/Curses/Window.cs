using System.Text.RegularExpressions;

namespace LocalisationGenerator.Curses;

public class Window {
	public int X;
	public int Y;

	public int Width { get; private set; }
	public int Height { get; private set; }

	Stack<Rect?> scissorStack = new();
	public Rect? Scissors => scissorStack.Peek();
	public void PushScissors ( Rect? value ) {
		setScissors( Scissors, value );
		scissorStack.Push( value );
	}

	public void PopScissors () {
		var last = Scissors;
		scissorStack.Pop();
		setScissors( last, Scissors );
	}

	void setScissors ( Rect? prev, Rect? value ) {
		var last = prev ?? LocalRect;
		var next = value ?? LocalRect;

		CursorX += last.X - next.X;
		CursorY += last.Y - next.Y;
	}

	public Rect LocalRect => new() { X = 0, Y = 0, Width = Width, Height = Height };
	public Rect Rect => new() { X = X, Y = Y, Width = Width, Height = Height };
	public Rect DrawRect => Scissors ?? LocalRect;

	public int CursorX;
	public int CursorY;

	public void SetCursor ( int x, int y ) {
		CursorX = x;
		CursorY = y;
	}

	Stack<AnsiColor> fgStack = new();
	Stack<AnsiColor> bgStack = new();
	Stack<Attribute> attrStack = new();

	public AnsiColor Background => bgStack.Peek();
	public AnsiColor Foregreound => fgStack.Peek();
	public Attribute Attributes => attrStack.Peek();

	public Symbol EmptySymbol => new Symbol { Attributes = Attributes, Bg = Background, Fg = Foregreound };

	Symbol[,] buffer;
	public Symbol this[int x, int y] {
		get => buffer[x, y];
		set => buffer[x, y] = value;
	}

	public Window () : this( 0, 0 ) { }
	public Window ( int width, int height ) {
		fgStack.Push( ConsoleColor.Gray );
		bgStack.Push( ConsoleColor.Black );
		attrStack.Push( Attribute.Normal );
		scissorStack.Push( null );

		buffer = new Symbol[width, height];
		Width = width;
		Height = height;
		Clear();
	}

	public void PushForeground ( AnsiColor fg ) {
		fgStack.Push( fg );
	}
	public void PushBackground ( AnsiColor bg ) {
		bgStack.Push( bg );
	}
	public void PushAttribute ( Attribute attr ) {
		attrStack.Push( attr );
	}
	public void PopForeground () {
		fgStack.Pop();
	}
	public void PopBackground () {
		bgStack.Pop();
	}
	public void PopAttribute () {
		attrStack.Pop();
	}

	public void Resize ( int width, int height ) {
		if ( width == Width && height == Height )
			return;

		var newBuffer = new Symbol[width, height];
		var empty = EmptySymbol with { Char = ' ', Bg = ConsoleColor.Black, Fg = ConsoleColor.Gray };
		for ( int x = 0; x < width; x++ ) {
			for ( int y = 0; y < height; y++ ) {
				newBuffer[x, y] = ( x < Width && y < Height ) ? buffer[x, y] : empty;
			}
		}

		buffer = newBuffer;
		Width = width;
		Height = height;
	}

	public void Clear ( char c = ' ', AnsiColor? fg = null, AnsiColor? bg = null, Attribute attributes = Attribute.Normal ) {
		Clear( new Symbol { Char = c, Attributes = attributes, Bg = bg ?? ConsoleColor.Black, Fg = fg ?? ConsoleColor.Gray } );
	}

	public void Clear ( Symbol symbol ) {
		Rect rect = DrawRect;

		for ( int x = rect.X; x < rect.Right; x++ ) {
			for ( int y = rect.Y; y < rect.Bottom; y++ ) {
				buffer[x, y] = symbol;
			}
		}
	}

	public void Write ( Symbol s, LayoutCallback? cb = null, int printableIndex = 0 ) {
		var rect = DrawRect;

		if ( s.Char == '\n' ) {
			WriteLine();
			cb?.Invoke( printableIndex, (rect.X + CursorX - 1, rect.Y + CursorY), s );
		}
		else {
			if ( CursorX >= rect.Width ) {
				WriteLine();
			}

			cb?.Invoke( printableIndex, (rect.X + CursorX, rect.Y + CursorY), s );
			buffer[CursorX + rect.X, CursorY + rect.Y] = s;
			CursorX++;
		}
	}

	public void WriteLine () {
		CursorY = Math.Min( DrawRect.Height - 1, CursorY + 1 );
		CursorX = 0;
	}

	public void Write ( char c ) {
		Write( EmptySymbol with { Char = c } );
	}

	void applyEscape ( char e ) {
		if ( e == ':' )
			PopForeground();
		else if ( e == ';' )
			PopBackground();
		else if ( e == '|' )
			PopAttribute();
		else if ( e == '_' ) {
			PushAttribute( e switch {
				'_' => Attribute.Underline,
				_ => Attribute.Normal
			} );
		}
		else if ( char.IsLower( e ) ) {
			PushBackground( e switch {
				'r' => ConsoleColor.Red,
				_ => AnsiColor.Gray
			} );
		}
		else {
			PushForeground( e switch {
				'G' => ConsoleColor.Green,
				'Y' => ConsoleColor.Yellow,
				'R' => ConsoleColor.Red,
				'C' => ConsoleColor.Cyan,
				'B' => ConsoleColor.Blue,
				'N' => ConsoleColor.DarkGray,
				_ => AnsiColor.White
			} );
		}
	}

	public delegate void LayoutCallback ( int printableIndex, (int x, int y) position, Symbol symbol, bool truncated = false );
	static readonly Regex wordRegex = new( @"(?:[\S-[/]]|\u0001.)+", RegexOptions.Compiled );
	public void Write ( string str, AnsiColor? fg = null, AnsiColor? bg = null, Attribute? attr = null, bool wrap = true, LayoutCallback? cb = null ) {
		int printableIndex = 0;

		if ( fg is AnsiColor f )
			PushForeground( f );
		if ( bg is AnsiColor b )
			PushBackground( b );
		if ( attr is Attribute a )
			PushAttribute( a );

		var empty = EmptySymbol;
		var rect = DrawRect;

		bool justWrapped = false;

		void writeString ( string str ) {
			if ( str.Length == 0 )
				return;

			bool deleteSpace = ( ( justWrapped && CursorX == 0 ) || ( CursorX >= rect.Width ) ) && str[0] == ' ';
			if ( deleteSpace )
				cb?.Invoke( printableIndex++, (rect.X + CursorX, rect.Y + CursorY), empty with { Char = ' ' }, truncated: true );

			foreach ( char c in deleteSpace ? str[1..] : str ) {
				if ( !wrap && c != '\n' && CursorX >= rect.Width ) {
					cb?.Invoke( printableIndex++, (rect.X + CursorX, rect.Y + CursorY), empty with { Char = c }, truncated: true );
					continue;
				}

				Write( empty with { Char = c }, cb, printableIndex++ );
			}
		}

		var whitespaces = wordRegex.Split( str );
		var words = wordRegex.Matches( str );
		for ( int i = 0; i < whitespaces.Length; i++ ) {
			if ( i != 0 ) {
				var word = words[i - 1].Value;
				var parts = word.Split( escChar );
				var length = word.Length - (parts.Length - 1) * 2;
				if ( CursorX != 0 && CursorX + length > rect.Width && wrap ) {
					CursorY = Math.Min( rect.Height - 1, CursorY + 1 );
					CursorX = 0;
					justWrapped = true;
				}

				for ( int j = 0; j < parts.Length; j++ ) {
					if ( j > 0 ) {
						applyEscape( parts[j][0] );
						empty = EmptySymbol;

						writeString( parts[j][1..] );
					}
					else {
						writeString( parts[j] );
					}
				}
			}

			writeString( whitespaces[i] );
			justWrapped = false;
		}

		if ( fg is AnsiColor )
			PopForeground();
		if ( bg is AnsiColor )
			PopBackground();
		if ( attr is Attribute )
			PopAttribute();
	}
	public void WriteLine ( string str, AnsiColor? fg = null, AnsiColor? bg = null, Attribute? attr = null, bool wrap = true, LayoutCallback? cb = null ) {
		Write( str + '\n', fg, bg, attr, wrap, cb );
	}

	public void DrawBorder ( char top = '─', char bottom = '─', char left = '│', char right = '│', 
		char topLeft = '┌', char topRight = '┐', char bottomLeft = '└', char bottomRight = '┘' ) 
	{
		var rect = DrawRect;
		var empty = EmptySymbol;

		for ( int x = 1; x < rect.Width - 1; x++ ) {
			buffer[rect.X + x, rect.Y] = empty with { Char = top };
			buffer[rect.X + x, rect.Bottom - 1] = empty with { Char = bottom };
		}

		for ( int y = 1; y < rect.Height - 1; y++ ) {
			buffer[rect.X, rect.Y + y] = empty with { Char = left };
			buffer[rect.Right - 1, rect.Y + y] = empty with { Char = right };
		}

		buffer[rect.X, rect.Y] = empty with { Char = topLeft };
		buffer[rect.Right - 1, rect.Y] = empty with { Char = topRight };
		buffer[rect.X, rect.Bottom - 1] = empty with { Char = bottomLeft };
		buffer[rect.Right - 1, rect.Bottom - 1] = empty with { Char = bottomRight };
	}

	public static char escChar = '\u0001';
	public static string esc ( char c ) {
		return $"{escChar}{c}";
	}
	public static string Red ( string str )
		=> $"{esc( 'R' )}{str}{esc( ':' )}";
	public static string Yellow ( string str )
		=> $"{esc( 'Y' )}{str}{esc( ':' )}";
	public static string Green ( string str )
		=> $"{esc( 'G' )}{str}{esc( ':' )}";
	public static string Cyan ( string str )
		=> $"{esc( 'C' )}{str}{esc( ':' )}";
	public static string Blue ( string str )
		=> $"{esc( 'B' )}{str}{esc( ':' )}";

	public static string RedBg ( string str )
		=> $"{esc( 'r' )}{str}{esc( ';' )}";

	public static string Underscore ( string str )
		=> $"{esc( '_' )}{str}{esc( '|' )}";
}
