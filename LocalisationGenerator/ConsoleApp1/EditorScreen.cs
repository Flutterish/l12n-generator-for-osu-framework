using LocalisationGenerator.Curses;
using LocalisationGenerator.Tabs;
using LocalisationGenerator.Ui;

namespace LocalisationGenerator;

public class EditorScreen : ConsoleWindow {
	LocalisationTab left;
	KeyTreeTab right;
	Program program;
	Window focused;
	TextBox textBox = new() { Placeholder = "Text goes here..." };
	public EditorScreen ( Program program ) {
		this.program = program;

		var width = (int)( Width * 0.75 );
		left = new();
		right = new( new( "" ) {
			Nested = new() {
				["Mod"] = new( "Mod" ) {
					Keys = new() {
						["ModA"] = "Mod.ModA",
						["ModB"] = "Mod.ModB"
					}
				},
				["Setting"] = new( "Setting" ) {
					Keys = new() {
						["NoteSize"] = "Setting.NoteSize",
						["PlayfieldOpacity"] = "Setting.PlayfieldOpacity"
					}
				},
				["Tooltips"] = new( "Tooltips" ) {
					Nested = new() {
						["Extra"] = new( "Tooltips.Extra" ) {
							Keys = new() {
								["Magic"] = "Tooltips.Extra.Magic"
							}
						}
					}
				},
				["CollasedCategory"] = new( "CollasedCategory" ) {

				},
				["ExpandedCategory"] = new( "ExpandedCategory" ) {
					Keys = new() {
						["Empty"] = "ExpandedCategory.Empty"
					}
				}
			}
		} );
		AttachWindow( left );
		AttachWindow( right );

		focused = left;
	}

	protected override void Draw () {
		left.Clear();
		right.Clear();

		var width = (int)( Width * (focused == left ? 0.7 : 0.3) );
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

		right.PushScissors( right.LocalRect with { X = 1, Y = 1, Width = right.Width - 2, Height = right.Height - 2 } );
		left.PushScissors( left.LocalRect with { X = 1, Y = 1, Width = left.Width - 2, Height = left.Height - 2 } );
		right.SetCursor( 0, 0 );
		left.SetCursor( 0, 0 );

		left.Draw();
		right.Draw();

		textBox.Draw( left, wrap: true );
		var (from, to) = textBox.CaretPosition;
		(CursorX, CursorY) = (to.x + 1, to.y);

		left.PopScissors();
		right.PopScissors();
	}

	public void Run () {
		while ( true ) {
			if ( !KeyAvailable ) {
				Draw();
				Refresh();
			}

			var key = ReadKey();

			if ( key.Key == ConsoleKey.F1 ) {
				focused = left;
			}
			else if ( key.Key == ConsoleKey.F2 ) {
				focused = right;
			}
			else {
				textBox.Handle( key );
			}
		}
	}
}
