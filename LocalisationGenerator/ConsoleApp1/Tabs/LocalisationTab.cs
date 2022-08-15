using LocalisationGenerator.Curses;
using LocalisationGenerator.UI;

namespace LocalisationGenerator.Tabs;

public class LocalisationTab : Window {
	Project project;
	public TextBox TextBox = new();
	public Locale Locale;

	LocalisableString? key;
	public LocalisableString? String {
		get => key;
		set {
			if ( key == value )
				return;

			key = value;
		}
	}

	public LocalisationTab ( Project project, Locale locale ) {
		this.project = project;
		Locale = locale;
	}

	public void Draw () {
		//WriteLine( $"Locale: {Yellow( "English (en)" )}", performLayout: true );
		//WriteLine( $"Key: {Yellow( "mod.name" )}\n", performLayout: true );
		//WriteLine( $"To create a place for a value to be inserted, use a number or text surrounded by {Green( "{}" )}, for example {Red( "\"" )}Hello, {Green( "{name}" )}!{Red( "\"" )}", performLayout: true );
		//WriteLine( $"To insert a tab or new-line you can use {Red( "\\t" )} and {Red( "\\n" )} respectively", performLayout: true );
		//WriteLine( $"To insert a literal {{, }} or \\, double them up like {Red( "\"" )}{{{{ and }}}} and \\\\{Red( "\"" )}", performLayout: true );
		//WriteLine( $"You can also specify how numbers and dates should be formated like {Green( $"{{number{Cyan( ":N2" )}}}" )}", performLayout: true );
		//WriteLine( $"For more info refer to {Underscore(Cyan( "https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings" ))}\n", performLayout: true );
		////if ( guideStr != null ) {
		////	WriteLine( $"Guide [{guideLocale!.ISO}]: {esc( 'R' )}\"{esc( '\0' )}{guideStr.ColoredValue}{esc( 'R' )}\"{esc( '\0' )}" );
		////}
		////else {
		//WriteLine( $"Guide: {Red( "None" )}", performLayout: true );
		////}
		//Write( $"Value: {Red( "\"" )}", performLayout: true );
		//WriteLine( $"lorem ipsum{Red( "\"" )}\n", performLayout: true );
	}
}
