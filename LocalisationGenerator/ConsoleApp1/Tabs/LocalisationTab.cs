using LocalisationGenerator.Curses;

namespace LocalisationGenerator.Tabs;

public class LocalisationTab : Window {
	public void Draw () {
		WriteLine( $"Locale: {Yellow( "English (en)" )}" );
		WriteLine( $"Key: {Yellow( "mod.name" )}\n" );
		WriteLine( $"To create a place for a value to be inserted, use a number or text surrounded by {Green( "{}" )}, for example {Red( "\"" )}Hello, {Green( "{name}" )}!{Red( "\"" )}" );
		WriteLine( $"To insert a tab or new-line you can use {Red( "\\t" )} and {Red( "\\n" )} respectively" );
		WriteLine( $"To insert a literal {{, }} or \\, double them up like {Red( "\"" )}{{{{ and }}}} and \\\\{Red( "\"" )}" );
		WriteLine( $"You can also specify how numbers and dates should be formated like {Green( $"{{number{Cyan( ":N2" )}}}" )}" );
		WriteLine( $"For more info refer to {Underscore(Cyan( "https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings" ))}\n" );
		//if ( guideStr != null ) {
		//	WriteLine( $"Guide [{guideLocale!.ISO}]: {esc( 'R' )}\"{esc( '\0' )}{guideStr.ColoredValue}{esc( 'R' )}\"{esc( '\0' )}" );
		//}
		//else {
		WriteLine( $"Guide: {Red( "None" )}" );
		//}
		Write( $"Value: {Red( "\"" )}" );
		WriteLine( $"lorem ipsum{Red( "\"" )}\n" );
	}
}
