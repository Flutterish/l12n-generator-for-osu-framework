using LocalisationGenerator.Curses;
using LocalisationGenerator.UI;

namespace LocalisationGenerator.Tabs;

public class KeyTreeTab : Window {
	NamespaceTree tree;
	Project project;
	public KeyTreeTab ( LocaleNamespace ns, Project project ) {
		tree = new( ns );
		this.project = project;

		Selector.Selected += v => {
			var (_, tree, key) = v;
			if ( key == null ) {
				tree.IsExpanded = !tree.IsExpanded;
			}
			else {
				KeySelected?.Invoke( tree.Value.Keys[key] );
			}
		};
	}

	public NamespaceDropdown Selector = new();
	public void Draw () {
		WriteLine( $"{Blue( "[<]" )}History{Blue( "[>]" )} {Underscore(Red( "R" ))}emove Add {Underscore(Blue( "N" ))}ew {Underscore(Blue( "L" ))}anguage (en)", performLayout: true );
		CursorX = 0;
		Selector.Options.Clear();

		void tree ( NamespaceTree ns, string indent = "" ) {
			int c = 0;
			bool isLast () {
				return c == ns.Value.Keys.Count + ns.Value.Nested.Count;
			}
			foreach ( var (shortKey, key) in ns.Value.Keys.OrderBy( x => x.Key ) ) {
				c++;
				var guide = project.GetBestGuide( key );
				if ( ns.Value.KeysToBeRemoved.Contains( shortKey ) ) {
					Selector.Options.Add( (indent + ( isLast() ? "└─" : "├─" ) + Red(shortKey + ": " + $"\"{guide.Value}\""), ns, shortKey) );
				}
				else {
					Selector.Options.Add(( indent + ( isLast() ? "└─" : "├─" ) + Yellow( shortKey ) + ": " + $"{Red( "\"" )}{guide.ColoredValue}{Red( "\"" )}", ns, shortKey ));
				}
			}
			foreach ( var (name, nested) in ns.Children.OrderBy( x => x.shortName ) ) {
				c++;
				Selector.Options.Add(( indent + ( isLast() ? "└─" : "├─" ) + nested switch {
					{ Value.ScheduledForRemoval: true } => Red( name ),
					{ IsExpanded: true } => name,
					_ => Gray( name )
				}, nested, null ));

				if ( nested.IsExpanded && !nested.Value.ScheduledForRemoval )
					tree( nested, indent + ( isLast() ? "  " : "│ " ) );
			}
		}
		Selector.Options.Add(( ".", this.tree, null ));
		tree( this.tree );

		Selector.Draw( this );
	}

	event Action<string>? KeySelected;
}
