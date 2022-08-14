namespace LocalisationGenerator.UI;

public class NamespaceDropdown : Dropdown<(string str, NamespaceTree ns, string? key)> {
	public NamespaceDropdown () : base ( i => i.str ) {
		LeftSelection = " > ";
		RightSelection = " < ";
	}

	protected override bool TryHandle ( ConsoleKeyInfo key ) {
		if ( key is { Key: ConsoleKey.UpArrow, Modifiers: ConsoleModifiers.Control } ) {
			if ( SelectedIndex != 0 )
				SelectedIndex--;

			while ( SelectedIndex != 0 && Options[SelectedIndex].key != null )
				SelectedIndex--;

			return true;
		}
		else if ( key is { Key: ConsoleKey.DownArrow, Modifiers: ConsoleModifiers.Control } ) {
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
