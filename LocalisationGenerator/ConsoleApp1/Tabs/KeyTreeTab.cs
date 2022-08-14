using LocalisationGenerator.Curses;

namespace LocalisationGenerator.Tabs;

public class KeyTreeTab : Window {
	NamespaceTree tree;
	public KeyTreeTab ( LocaleNamespace ns ) {
		tree = new( ns );
	}

	public void Draw () {
		WriteLine( $"{Blue( "[<]" )}History{Blue( "[>]" )} {Red( "[R]" )}emove {Blue( "[A]" )}dd {Blue( "[L]" )}anguage (en)", performLayout: true );
		PushScissors( DrawRect with { X = DrawRect.X + 3, Width = DrawRect.Width - 6 } );
		CursorX = 0;

		void tree ( NamespaceTree ns, string indent = "" ) {
			int c = 0;
			bool isLast () {
				return c == ns.Value.Keys.Count + ns.Value.Nested.Count;
			}
			foreach ( var (shortKey, key) in ns.Value.Keys.OrderBy( x => x.Key ) ) {
				c++;
				//var str = summary!.Keys[key];
				WriteLine( indent + ( isLast() ? "└─" : "├─" ) + Yellow( shortKey ) + ": " + $"{Red( "\"" )}{key}{Red( "\"" )}", wrap: false );
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
				WriteLine( indent + ( isLast() ? "└─" : "├─" ) + name, wrap: false );
				tree( nested, indent + ( isLast() ? "  " : "│ " ) );
			}
		}
		WriteLine( "." );
		tree( this.tree );

		PopScissors();
	}

	class NamespaceTree {
		public readonly LocaleNamespace Value;
		public readonly NamespaceTree? Parent;

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
