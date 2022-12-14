using Humanizer;
using ICSharpCode.Decompiler.Util;
using Newtonsoft.Json;
using System.Text;

namespace LocalisationGenerator;

public class ResourceGenerator {
	Summary summary;
	Config config;
	public ResourceGenerator ( Config config, IEnumerable<Locale> locales ) {
		summary = new( locales );
		this.config = config;
	}

	string pascalise ( string str )
		=> str.Replace( '-', ' ' ).Pascalize();

	public void Save () {
		var rootPath = Path.GetFullPath( Path.Combine( config.ProjectPath, ".." ) );
		var ns = config.Namespace;
		if ( ns.StartsWith( config.RootNamespace + "." ) )
			ns = ns[( config.RootNamespace.Length + 1 )..];
		else if ( ns == config.RootNamespace )
			ns = "";

		rootPath = Path.Combine( rootPath, ns.Replace( '.', Path.DirectorySeparatorChar ) );
		Directory.CreateDirectory( rootPath );
		var resxPath = Path.Combine( rootPath, "Resx" );
		Directory.CreateDirectory( resxPath );

		foreach ( var i in Directory.GetFiles( rootPath, "*.l12n.generated.cs", SearchOption.AllDirectories ) ) {
			File.Delete( i );
		}
		foreach ( var i in Directory.GetFiles( rootPath, "*.resx", SearchOption.AllDirectories ) ) {
			File.Delete( i );
		}

		Dictionary<string, string> argNames = new();
		void saveNamespace ( string path, string name, IEnumerable<string> location, LocaleNamespace ns ) {
			foreach ( var i in ns.Nested ) {
				var key = pascalise(i.Key);
				saveNamespace( path, $"{name}.{key}", location.Append( key ), i.Value );
			}

			if ( !ns.Keys.Any() )
				return;

			var @namespace = string.Join( ".", location.Prepend( config.Namespace ).Take( Math.Max( 1, location.Count() ) ) );
			StringBuilder sb = new();
			sb.AppendLine( "// This file is auto-generated" );
			sb.AppendLine( "// Do not edit it manually as it will be overwritten" );
			sb.AppendLine();
			sb.AppendLine( "using osu.Framework.Localisation;" );
			sb.AppendLine();

			sb.AppendLine( $"namespace {@namespace} {{" );
			sb.AppendLine( $"	public static class {(location.Any() ? location.Last() : "")}Strings {{" );
			sb.AppendLine( $"		private const string PREFIX = {JsonConvert.SerializeObject($"{name}.Strings" )};" );
			sb.AppendLine( "		private static string getKey( string key ) => $\"{PREFIX}:{key}\";" );
			foreach ( var (shortKey, key) in ns.Keys ) {
				var str = summary.Keys[key];
				var example = (str.LocalisedIn.FirstOrDefault( x => x.ISO == config.DefaultLocale ) ?? str.LocalisedIn[0] ).Strings[key];

				sb.AppendLine();
				sb.AppendLine( "		/// <summary>" );
				sb.Append( "		/// " );
				sb.AppendLine( example.Value.ReplaceLineEndings( "		/// " ) );
				sb.AppendLine( "		/// </summary>" );
				if ( str.Arguments.Any() ) {
					argNames.Clear();
					string argName ( string arg ) {
						if ( !argNames.TryGetValue( arg, out var name ) ) {
							name = arg.Replace( '-', ' ' ).Camelize();

							int i = 2;
							var testName = name;
							while ( argNames.ContainsKey( testName ) ) {
								testName = name + i++;
							}
							name = testName;
						}

						return name;
					}

					sb.AppendLine( $"		public static LocalisableString {pascalise(shortKey)}( {string.Join( ", ", str.Arguments.Keys.Select( x => $"object {argName(x)}" ) )} ) => new TranslatableString(" );
					sb.AppendLine( $"			getKey( {JsonConvert.SerializeObject( shortKey )} )," );
					sb.AppendLine( $"			{JsonConvert.SerializeObject( example.Export(str.ArgIndices) )}," );
					sb.AppendLine( $"			{string.Join( ", ", str.Arguments.Keys.Select( argName ) )}" );
					sb.AppendLine( "		);" );
				}
				else {
					sb.AppendLine( $"		public static readonly LocalisableString {pascalise(shortKey)} = new TranslatableString(" );
					sb.AppendLine( $"			getKey( {JsonConvert.SerializeObject(shortKey)} )," );
					sb.AppendLine( $"			{JsonConvert.SerializeObject( example.Export() )}" );
					sb.AppendLine( "		);" );
				}
			}

			sb.AppendLine( "	}" );
			sb.AppendLine( "}" );

			File.WriteAllText( Path.Combine( path, $"{string.Join( '.', location )}Strings.l12n.generated.cs" ), sb.ToString() );

			foreach ( var (iso, locale) in summary.Locales ) {
				using var writer = new ResXResourceWriter( Path.Combine( resxPath, $"{(location.Any() ? string.Join( '.', location.Append("") ) : "")}Strings{(iso == config.DefaultLocale ? "" : $".{iso}")}.resx" ) );
				bool any = false;

				foreach ( var (shortKey, key) in ns.Keys ) {
					if ( locale.Locale.Strings.TryGetValue( key, out var str ) ) {
						writer.AddResource( shortKey, str.Export( summary.Keys[key].ArgIndices ) );
						any = true;
					}
				}

				if ( any )
					writer.Generate();
			}
		}
		saveNamespace( rootPath, config.Namespace + ".Resx", Array.Empty<string>(), summary.RootNamespace );
	}
}
