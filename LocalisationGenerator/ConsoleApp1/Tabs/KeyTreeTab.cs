using LocalisationGenerator.Curses;
using LocalisationGenerator.UI;

namespace LocalisationGenerator.Tabs;

public class KeyTreeTab : Window {
	NamespaceTree tree;
	public KeyTreeTab ( LocaleNamespace ns ) {
		tree = new( ns );
	}

	public Dropdown<(int i, string str)> Selector = new( i => i.str ) { LeftSelection = " > ", RightSelection = " < " };
	List<(NamespaceTree tree, string? key)> options = new();
	public void Draw () {
		WriteLine( $"{Blue( "[<]" )}History{Blue( "[>]" )} {Underscore(Red( "R" ))}emove Add {Underscore(Blue( "N" ))}ew/{Underscore(Yellow( "M" ))}issing {Underscore(Blue( "L" ))}anguage (en)", performLayout: true );
		CursorX = 0;
		options.Clear();
		Selector.Options.Clear();

		void tree ( NamespaceTree ns, string indent = "" ) {
			int c = 0;
			bool isLast () {
				return c == ns.Value.Keys.Count + ns.Value.Nested.Count;
			}
			foreach ( var (shortKey, key) in ns.Value.Keys.OrderBy( x => x.Key ) ) {
				c++;
				//var str = summary!.Keys[key];
				Selector.Options.Add(( options.Count, indent + ( isLast() ? "└─" : "├─" ) + Yellow( shortKey ) + ": " + $"{Red( "\"" )}{key}{Red( "\"" )}" ));
				options.Add(( ns, shortKey ));
				//var lang = str.LocalisedIn.FirstOrDefault( x => x == mainlocale ) ?? str.LocalisedIn.First();
				//WriteLine( indent + ( isLast() ? "   " : "│ " ) + $"\tExample [{lang.ISO}]: {Red( "\"" )}{lang.Strings[key].ColoredValue}{Red( "\"" )}" );
				//if ( str.NotLocalisedIn.Any() ) {
				//	WriteLine( indent + ( isLast() ? "   " : "│ " ) + $"\tNot localised in: {string.Join( ", ", str.NotLocalisedIn.Select( x => Yellow( $"{x.Name} [{x.ISO}]" ) ) )}" );
				//}
				//if ( str.Arguments.Any() )
				//	WriteLine( indent + ( isLast() ? "   " : "│ " ) + $"\tArguments: {string.Join( ", ", str.Arguments.Select( x => $"{{{x.Key}}}" ) )}" );
			}
			foreach ( var (name, nested) in ns.Children.OrderBy( x => x.shortName ) ) {
				c++;
				Selector.Options.Add(( options.Count, indent + ( isLast() ? "└─" : "├─" ) + name ));
				options.Add(( nested, null ));
				tree( nested, indent + ( isLast() ? "  " : "│ " ) );
			}
		}
		Selector.Options.Add(( options.Count, "." ));
		options.Add(( this.tree, null ));
		tree( this.tree );

		Selector.Draw( this );
	}

	class NamespaceTree {
		public readonly LocaleNamespace Value;
		public readonly NamespaceTree? Parent;
		public bool IsExpanded = true;

		public NamespaceTree ( LocaleNamespace value, NamespaceTree? parent = null ) {
			Value = value;
			Parent = parent;
		}

		Dictionary<LocaleNamespace, NamespaceTree> children = new();
		public IEnumerable<(string shortName, NamespaceTree child)> Children {
			get {
				foreach ( var (shortName, i) in Value.Nested ) {
					if ( !children.TryGetValue( i, out var c ) )
						children.Add( i, c = new( i, this ) );

					yield return (shortName, c);
				}
			}
		}
	}
}
