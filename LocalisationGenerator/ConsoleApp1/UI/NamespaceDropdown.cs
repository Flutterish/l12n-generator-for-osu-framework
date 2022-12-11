namespace LocalisationGenerator.UI;

public class NamespaceDropdown : Dropdown<(string str, NamespaceTree ns, string? key)> {
	public NamespaceDropdown () : base ( i => i.str ) {
		LeftSelection = " > ";
		RightSelection = " < ";
	}

	protected override bool TryHandle ( ConsoleKeyInfo key ) {
		if ( key is { Key: ConsoleKey.UpArrow or ConsoleKey.NumPad8, Modifiers: ConsoleModifiers.Control } or { Key: ConsoleKey.LeftArrow or ConsoleKey.NumPad4 } ) {
			if ( SelectedIndex != 0 )
				SelectedIndex--;

			while ( SelectedIndex != 0 && Options[SelectedIndex].key != null )
				SelectedIndex--;

			return true;
		}
		else if ( key is { Key: ConsoleKey.DownArrow or ConsoleKey.NumPad2, Modifiers: ConsoleModifiers.Control } or { Key: ConsoleKey.RightArrow or ConsoleKey.NumPad6 } ) {
			if ( SelectedIndex != Options.Count - 1 )
				SelectedIndex++;

			while ( SelectedIndex != Options.Count - 1 && Options[SelectedIndex].key != null )
				SelectedIndex++;

			return true;
		}

		return false;
	}
}

public class NamespaceTree {
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
