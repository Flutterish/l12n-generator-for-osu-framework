using LocalisationGenerator.UI;

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
	Stack<Attrib> attrStack = new();

	public AnsiColor Background => bgStack.Peek();
	public AnsiColor Foregreound => fgStack.Peek();
	public Attrib Attributes => attrStack.Peek();

	public Symbol EmptySymbol => new Symbol { Attributes = Attributes, Bg = Background, Fg = Foregreound };

	Symbol[,] buffer;
	public Symbol this[int x, int y] {
		get => buffer[x, y];
		set {
			if ( x < 0 || y < 0 || x >= Width || y >= Height )
				return;

			buffer[x, y] = value;
		}
	}

	public Window () : this( 0, 0 ) { }
	public Window ( int width, int height ) {
		fgStack.Push( ConsoleColor.Gray );
		bgStack.Push( ConsoleColor.Black );
		attrStack.Push( Attrib.Normal );
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
	public void PushAttribute ( Attrib attr ) {
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

	public void Clear ( char c = ' ', AnsiColor? fg = null, AnsiColor? bg = null, Attrib attributes = Attrib.Normal ) {
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

	public const int tabAlign = 8;
	public void Write ( Symbol s ) {
		var rect = DrawRect;

		if ( s.Char == '\n' ) {
			WriteLine();
		}
		else if ( s.Char == '\t' ) {
			int count = (( CursorX + tabAlign ) / tabAlign) * tabAlign - CursorX;
			for ( int i = 0; i < count && Width > CursorX + rect.X; i++ ) {
				this[CursorX + rect.X, CursorY + rect.Y] = s with { Char = ' ' };
				CursorX++;
			}
		}
		else {
			if ( CursorX >= rect.Width ) {
				WriteLine();
			}

			this[CursorX + rect.X, CursorY + rect.Y] = s;
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
				'_' => Attrib.Underline,
				_ => Attrib.Normal
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
				'Z' => ConsoleColor.Black,
				_ => AnsiColor.White
			} );
		}
	}

	public delegate void LayoutCallback ( int printableIndex, (int x, int y) position, Symbol symbol, bool truncated = false );
	public void Write ( string str, bool performLayout = false, bool showFormatting = false, AnsiColor? fg = null, AnsiColor? bg = null, Attrib? attr = null, bool wrap = true, LayoutCallback? cb = null ) {
		int printableIndex = -1;

		if ( fg is AnsiColor f )
			PushForeground( f );
		if ( bg is AnsiColor b )
			PushBackground( b );
		if ( attr is Attrib a )
			PushAttribute( a );

		var empty = EmptySymbol;
		var rect = DrawRect;

		if ( performLayout ) {
			bool firstLine = true;
			var layout = new TextLayout( str, rect, CursorX, CursorY, wrap );
			foreach ( var line in layout.Lines ) {
				if ( !firstLine ) {
					if ( showFormatting && CursorX < rect.Width )
						this[rect.X + CursorX, rect.Y + CursorY] = empty with { Char = '⏎', Fg = ConsoleColor.DarkGray };
					WriteLine();
				}

				bool rightAlign = line.DirectionBlocks.Any() && line.DirectionBlocks.First().isRtl;

				if ( rightAlign ) {
					CursorX = rect.Width - line.Width;
				}

				if ( printableIndex == -1 ) {
					if ( rightAlign )
						cb?.Invoke( printableIndex, (rect.Right - 1, rect.Y + CursorY), empty with { Char = '\0' } );
					else
						cb?.Invoke( printableIndex, (rect.X + CursorX - 1, rect.Y + CursorY), empty with { Char = '\0' } );
					printableIndex++;
				}

				bool isEscaped = false;
				int offset = 0;
				for ( var i = 0; i < line.DirectionBlocks.Count; i++ ) {
					var (count, rtl) = line.DirectionBlocks[i];

					int rtlOffset = CursorX * 2 + line.Words.Skip(offset).Take(count).Sum( x => x.Width ) - 2;
					foreach ( var (word, visible, whitespace) in line.Words.Skip(offset).Take(count) ) {
						if ( visible ) {
							foreach ( char c in word ) {
								if ( isEscaped ) {
									isEscaped = false;
									applyEscape( c );
									empty = EmptySymbol;
									continue;
								}
								else if ( c == '\u0001' ) {
									isEscaped = true;
									continue;
								}

								if ( rtl )
									cb?.Invoke( printableIndex++, (rect.X + rtlOffset - CursorX, rect.Y + CursorY), empty with { Char = c } );
								else
									cb?.Invoke( printableIndex++, (rect.X + CursorX, rect.Y + CursorY), empty with { Char = c } );

								if ( c == ' ' ) {
									if ( showFormatting )
										this[rect.X + CursorX, rect.Y + CursorY] = empty with { Char = '·', Fg = ConsoleColor.DarkGray };
									else
										this[rect.X + CursorX, rect.Y + CursorY] = empty with { Char = rtl ? '\u200F' : ' ' };
								}
								else {
									this[rect.X + CursorX, rect.Y + CursorY] = empty with { Char = c };
								}
									
								CursorX++;
							}
						}
						else {
							foreach ( char c in word ) {
								if ( rtl )
									cb?.Invoke( printableIndex++, (rect.X + rtlOffset - CursorX + 1, rect.Y + CursorY), empty with { Char = c }, truncated: true );
								else
									cb?.Invoke( printableIndex++, (rect.X + CursorX - 1, rect.Y + CursorY), empty with { Char = c }, truncated: true );
							}
						}
					}

					offset += count;
				}

				firstLine = false;
			}
		}
		else {
			bool escaped = false;
			foreach ( var c in str ) {
				if ( (CursorX >= rect.Width && wrap) || c == '\n' ) {
					WriteLine();
					continue;
				}

				if ( escaped ) {
					applyEscape( c );
					empty = EmptySymbol;
					escaped = false;
				}
				else if ( c == '\u0001' ) {
					escaped = true;
				}
				else if ( wrap || CursorX < rect.Width ) {
					this[rect.X + CursorX, rect.Y + CursorY] = empty with { Char = c };
					CursorX++;
				}
			}
		}

		if ( fg is AnsiColor )
			PopForeground();
		if ( bg is AnsiColor )
			PopBackground();
		if ( attr is Attrib )
			PopAttribute();
	}
	public void WriteLine ( string str, bool performLayout = false, bool showFormatting = false, AnsiColor? fg = null, AnsiColor? bg = null, Attrib? attr = null, bool wrap = true, LayoutCallback? cb = null ) {
		Write( str + '\n', performLayout, showFormatting, fg, bg, attr, wrap, cb );
	}

	public void WriteError ( string str, bool performLayout = false, bool showFormatting = false, AnsiColor? fg = null, AnsiColor? bg = null, Attrib? attr = null, bool wrap = true, LayoutCallback? cb = null ) {
		Write( RedBg( Black( str ) ) + '\n', performLayout, showFormatting, fg, bg, attr, wrap, cb );
	}

	public static int WidthOf ( string str ) {
		return str.Length - TextLayout.EscapeRegex.Matches( str ).Count * 2;
	}

	public void DrawBorder ( char top = '─', char bottom = '─', char left = '│', char right = '│', 
		char topLeft = '┌', char topRight = '┐', char bottomLeft = '└', char bottomRight = '┘' ) 
	{
		var rect = DrawRect;
		var empty = EmptySymbol;

		for ( int x = 1; x < rect.Width - 1; x++ ) {
			this[rect.X + x, rect.Y] = empty with { Char = top };
			this[rect.X + x, rect.Bottom - 1] = empty with { Char = bottom };
		}

		for ( int y = 1; y < rect.Height - 1; y++ ) {
			this[rect.X, rect.Y + y] = empty with { Char = left };
			this[rect.Right - 1, rect.Y + y] = empty with { Char = right };
		}

		this[rect.X, rect.Y] = empty with { Char = topLeft };
		this[rect.Right - 1, rect.Y] = empty with { Char = topRight };
		this[rect.X, rect.Bottom - 1] = empty with { Char = bottomLeft };
		this[rect.Right - 1, rect.Bottom - 1] = empty with { Char = bottomRight };
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
	public static string Black ( string str )
		=> $"{esc( 'Z' )}{str}{esc( ':' )}";
	public static string Gray ( string str )
		=> $"{esc( 'N' )}{str}{esc( ':' )}";

	public static string RedBg ( string str )
		=> $"{esc( 'r' )}{str}{esc( ';' )}";

	public static string Underscore ( string str )
		=> $"{esc( '_' )}{str}{esc( '|' )}";
}
