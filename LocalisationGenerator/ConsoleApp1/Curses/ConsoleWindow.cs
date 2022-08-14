using System.Runtime.InteropServices;
using System.Text;

namespace LocalisationGenerator.Curses;

public class ConsoleWindow : Window {
	Symbol[,] pushed;

	public ConsoleWindow () : base( Console.WindowWidth, Console.WindowHeight ) {
		pushed = new Symbol[Width, Height];

		Console.Clear();
		if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
			AnsiFix.Fix();

		var empty = new Symbol { Char = ' ', Fg = Console.ForegroundColor, Bg = Console.BackgroundColor, Attributes = Attribute.Normal };
		for ( int x = 0; x < Width; x++ ) {
			for ( int y = 0; y < Height; y++ ) {
				pushed[x, y] = empty;
			}
		}
	}

	void clearPushed () {
		var empty = new Symbol { Char = ' ', Fg = Console.ForegroundColor, Bg = Console.BackgroundColor, Attributes = Attribute.Normal };
		for ( int x = 0; x < Width; x++ ) {
			for ( int y = 0; y < Height; y++ ) {
				pushed[x, y] = empty;
			}
		}
	}

	void checkForResize () {
		if ( Console.WindowWidth != Width || Console.WindowHeight != Height ) {
			var w = Console.WindowWidth;
			var h = Console.WindowHeight;
			Resize( w, h );
			pushed = new Symbol[w, h];
			clearPushed();

			Draw();
			Console.Clear();
			Refresh();
		}
	}

	protected virtual void Draw () { }

	const string csi = "\u001B[";
	StringBuilder updater = new();
	Symbol selectedAnsi = new Symbol { Fg = Console.ForegroundColor, Bg = Console.BackgroundColor, Attributes = Attribute.Normal };
	public void Refresh () {
		foreach ( var i in windows ) {
			for ( int x = i.X; x < i.X + i.Width && x < Width; x++ ) {
				for ( int y = i.Y; y < i.Y + i.Height && y < Height; y++ ) {
					this[x, y] = i[x - i.X, y - i.Y];
				}
			}
		}

		updater.Clear();

		bool changeScan = false;
		for ( int y = 0; y < Height; y++ ) {
			for ( int x = 0; x < Width; x++ ) {
				Symbol symbol = this[x, y];
				Symbol pushedSymbol = pushed[x, y];
				pushed[x, y] = symbol;

				if ( symbol == pushedSymbol ) {
					changeScan = false;
				}
				else {
					if ( !changeScan ) {
						changeScan = true;

						updater.Append( csi );
						updater.Append( y + 1 );
						updater.Append( ';' );
						updater.Append( x + 1 );
						updater.Append( 'H' );
					}

					if ( Symbol.RequiresAnsiChange( selectedAnsi, symbol ) ) {
						if ( selectedAnsi.Fg != symbol.Fg ) {
							updater.Append( csi );
							updater.Append( 30 + symbol.Fg.Preset );
							updater.Append( 'm' );
						}
						if ( selectedAnsi.Bg != symbol.Bg ) {
							updater.Append( csi );
							updater.Append( 40 + symbol.Bg.Preset );
							updater.Append( 'm' );
						}
						if ( selectedAnsi.Attributes != symbol.Attributes ) {
							if ( symbol.Attributes == Attribute.Normal ) {
								updater.Append( csi );
								updater.Append( 'm' );
							}
							else {
								for ( int i = 1; i <= 9; i++ ) {
									bool isActive = ((int)selectedAnsi.Attributes & (1 << i)) != 0;
									bool shouldBe = ((int)symbol.Attributes & (1 << i)) != 0;

									if ( isActive != shouldBe ) {
										updater.Append( csi );
										updater.Append( isActive ? (20 + i) : i );
										updater.Append( 'm' );
									}
								}
							}
						}

						selectedAnsi = symbol;
					}
					updater.Append( symbol.Char );
				}
			}
		}

		if ( (CursorX, CursorY) != (0, 0) ) {
			updater.Append( csi );
			updater.Append( CursorY + 1 );
			updater.Append( ';' );
			updater.Append( CursorX + 1 );
			updater.Append( 'H' );
		}

		Console.Write( updater.ToString() );
	}

	List<Window> windows = new();
	public void AttachWindow ( Window window ) {
		windows.Add( window );
	}

	public bool KeyAvailable => Console.KeyAvailable;
	public ConsoleKeyInfo ReadKey () {
		while ( !Console.KeyAvailable ) {
			Thread.Sleep( 1 );
			checkForResize();
		}

		return Console.ReadKey( true );
	}
}
