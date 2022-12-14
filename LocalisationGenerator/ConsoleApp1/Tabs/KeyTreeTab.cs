using LocalisationGenerator.Curses;
using LocalisationGenerator.UI;

namespace LocalisationGenerator.Tabs;

public class KeyTreeTab : Window {
	public NamespaceTree Tree;
	Project project;
	public Locale Locale;
	public TextBox? TextBox;

	public string? NextMissing;

	string? keyToSelect;
	public void SelectKey ( string key ) {
		keyToSelect = key;
		var path = key.Split( '.' );
		var ns = Tree;
		foreach ( var i in path[..^1] ) {
			ns = ns.Children.First( x => x.shortName == i ).child;
			ns.IsExpanded = true;
		}
	}

	public KeyTreeTab ( LocaleNamespace ns, Project project, Locale locale ) {
		Tree = new( ns );
		this.project = project;
		Locale = locale;

		Selector.Selected += v => {
			var (_, tree, key) = v;
			if ( key == null ) {
				tree.IsExpanded = !tree.IsExpanded;
			}
			else {
				if ( tree.Value.MissingKeys.ContainsKey( key ) ) {
					StringSelected?.Invoke( project.AddKey( Locale, tree.Value.MissingKeys[key] ) );
				}
				else {
					StringSelected?.Invoke( Locale.Strings[tree.Value.Keys[key]] );
				}
			}
		};

		LocaleSelector.Selected += newLocale => {
			if ( newLocale == AddOther ) {
				TextBox = new();
			}
			else if ( newLocale == AddNew ) {
				LocaleSelector.Options.Clear();
				LocaleSelector.Options.Add( AddOther );
				foreach ( var i in LocalesLUT.IsoToName.Keys.Except( project.Locales.Keys ) ) {
					LocaleSelector.Options.Add( new Locale( i ) );
				}
			}
			else {
				if ( !project.Locales.ContainsKey( newLocale.ISO ) ) {
					project.Locales.Add( newLocale.ISO, newLocale );
				}

				LocaleSelected?.Invoke( newLocale );
				SelectingLocale = false;
			}
		};
	}

	public bool SelectingLocale = false;
	public NamespaceDropdown Selector = new();
	public static readonly Locale AddNew = new("");
	public static readonly Locale AddOther = new("");
	public Dropdown<Locale> LocaleSelector = new( i => 
		i == AddNew 
		? "Add new" 
		: i == AddOther
		? "Other"
		: $"{i.Name} ({i.ISO})"
	);
	public void Draw () {
		WriteLine( $"{Underscore( Blue( "A" ) )}dd New {Underscore(Red( "R" ))}emove {Underscore(Blue( "L" ))}anguage ({Locale.ISO})", performLayout: true );
		CursorX = 0;
		Selector.Options.Clear();

		int index = 0;
		NextMissing = null;
		void tree ( NamespaceTree ns, string indent = "" ) {
			int c = 0;
			bool isLast () {
				return c == ns.Value.Keys.Count + ns.Value.Nested.Count + ns.Value.MissingKeys.Count;
			}
			foreach ( var (shortKey, key) in ns.Value.Keys.Concat( ns.Value.MissingKeys ).OrderBy( x => x.Key ) ) {
				c++;
				index++;

				if ( key == keyToSelect )
					Selector.SelectedIndex = index;

				if ( ns.Value.MissingKeys.ContainsKey( shortKey ) ) {
					NextMissing ??= key;
					Selector.Options.Add( (indent + ( isLast() ? "└─" : "├─" ) + Red( shortKey + " [Missing]" ), ns, shortKey) );
				}
				else if ( ns.Value.KeysToBeRemoved.Contains( shortKey ) ) {
					var guide = Locale.Strings[key];
					Selector.Options.Add( (indent + ( isLast() ? "└─" : "├─" ) + Red(shortKey + ": " + $"\"{guide.Value}\""), ns, shortKey) );
				}
				else {
					var guide = Locale.Strings[key];
					Selector.Options.Add(( indent + ( isLast() ? "└─" : "├─" ) + Yellow( shortKey ) + ": " + $"{Red( "\"" )}{guide.ColoredValue}{Red( "\"" )}", ns, shortKey ));
				}
			}
			foreach ( var (name, nested) in ns.Children.OrderBy( x => x.shortName ) ) {
				c++;
				index++;
				Selector.Options.Add(( indent + ( isLast() ? "└─" : "├─" ) + nested switch {
					{ Value.ScheduledForRemoval: true } => Red( name ),
					{ IsExpanded: true } => name,
					_ => Gray( name )
				}, nested, null ));

				if ( nested.IsExpanded && !nested.Value.ScheduledForRemoval )
					tree( nested, indent + ( isLast() ? "  " : "│ " ) );
			}
		}
		

		if ( SelectingLocale ) {
			if ( TextBox != null ) {
				if ( TextBox != null ) {
					WriteLine( $"{Yellow( "[New Locale]" )} Please type the {Underscore( Yellow( "ISO" ) )} code to add", performLayout: true );

					PushForeground( ConsoleColor.Yellow );
					TextBox.Draw( this, wrap: true );

					PopForeground();
					WriteLine();
				}
			}
			else {
				LocaleSelector.Draw( this );
			}
		}
		else {
			if ( TextBox != null ) {
				WriteLine( $"{Yellow( "[New Key]" )} You can use a dot {Underscore( Yellow( "." ) )} to group keys", performLayout: true );
				if ( !Program.keyRegex.IsMatch( TextBox.Text ) ) {
					PushForeground( ConsoleColor.Red );
					TextBox.Draw( this, wrap: true );
					Write( " [Invalid key]", performLayout: true );
				}
				else if ( Locale.Strings.ContainsKey( TextBox.Text ) ) {
					PushForeground( ConsoleColor.Red );
					TextBox.Draw( this, wrap: true );
					Write( " [Key already exists]", performLayout: true );
				}
				else {
					PushForeground( ConsoleColor.Yellow );
					TextBox.Draw( this, wrap: true );
				}


				PopForeground();
				WriteLine();
			}

			Selector.Options.Add( (".", Tree, null) );
			tree( Tree );
			keyToSelect = null;
			Selector.Draw( this );
		}
	}

	public bool Handle ( ConsoleKeyInfo key ) {
		if ( key.Key == ConsoleKey.Escape ) {
			if ( TextBox != null ) {
				TextBox = null;
				return true;
			}
			else if ( SelectingLocale ) {
				SelectingLocale = false;
				return true;
			}
		}

		if ( TextBox != null ) {
			if ( key.IsConfirmAction() ) {
				if ( SelectingLocale ) {
					var iso = TextBox.Text;
					if ( !project.Locales.TryGetValue( iso, out var loc ) )
						project.Locales.Add( iso, loc = new( iso ) );

					LocaleSelected?.Invoke( loc );
					SelectingLocale = false;
				}
				else {
					var k = TextBox.Text;
					if ( Program.keyRegex.IsMatch( k ) && !Locale.Strings.ContainsKey( k ) ) {
						StringSelected?.Invoke( project.AddKey( Locale, k ) );
					}
				}

				TextBox = null;
				return true;
			}

			if ( TextBox.Handle( key ) )
				return true;
		}

		if ( SelectingLocale ) {
			LocaleSelector.Handle( key );

			return true;
		}

		if ( Selector.Handle( key ) )
			return true;

		if ( key.Key == ConsoleKey.R ) {
			var (_, ns, k) = Selector.Options[Selector.SelectedIndex];
			if ( k != null ) {
				if ( !ns.Value.MissingKeys.ContainsKey( k ) ) {
					project.ToggleKeyRemoval( Locale, ns.Value.Keys[k] );
				}
			}
			else if ( ns.Parent != null ) {
				project.ToggleNamespaceRemoval( ns.Value );
			}

			return true;
		}
		else if ( key.Key == ConsoleKey.A ) {
			var (_, ns, _) = Selector.Options[Selector.SelectedIndex];
			var k = ns.Value.Name;
			if ( k != "" )
				k = k[1..] + '.';

			TextBox = new() { Text = k };
			return true;
		}
		else if ( key.Key == ConsoleKey.L ) {
			LocaleSelector.Options.Clear();
			LocaleSelector.Options.Add( AddNew );
			foreach ( var i in project.Locales.Values ) {
				LocaleSelector.Options.Add( i );
			}

			SelectingLocale = true;
			return true;
		}

		return false;
	}

	public void DrawHelp ( Window window ) {
		window.Clear();
		window.DrawBorder();
		window.CursorX = 2;
		window.CursorY = 0;
		window.Write( "Help [Tree Tab]", wrap: false );
		window.PushScissors( window.DrawRect with { X = 1, Y = 1, Width = window.Width - 2, Height = window.Height - 2 } );

		window.SetCursor( 0, 0 );
		window.WriteLine( $"You can see all strings of the project in this tab", performLayout: true );
		window.WriteLine();
		window.WriteLine( $"Use arrow keys or numpad keys to move your selection", performLayout: true );
		window.WriteLine( $"Numpad keys will work even when this tab is not focused", performLayout: true );
		window.WriteLine( $"Left/Right or Ctrl+Up/Down will seek to the next group", performLayout: true );
		window.WriteLine( $"Clicking a string will select it for editing", performLayout: true );
		window.WriteLine( $"Clicking a group will collapse/expand it", performLayout: true );
		window.WriteLine( $"The middle numpad key ({Underscore("5")}) can be used to click", performLayout: true );
		window.WriteLine();
		window.WriteLine( $"Press {Underscore( Blue( "A" ) )} to add a new string", performLayout: true );
		window.WriteLine( $"Press {Underscore( Red( "R" ) )} to remove or re-add a string or a whole group", performLayout: true );
		window.WriteLine( $"Press {Underscore( Blue( "L" ) )} to change the language you're working on", performLayout: true );
		window.WriteLine();
		window.WriteLine( $"Please note that not all terminals support all keys this program supports", performLayout: true );

		window.PopScissors();
	}

	public event Action<Locale>? LocaleSelected;
	public event Action<LocalisableString>? StringSelected;
}
