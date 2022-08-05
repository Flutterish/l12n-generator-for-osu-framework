﻿using System.Text;
using System.Text.RegularExpressions;

namespace LocalisationGenerator;

public class LocalisableString {
	public string Key;
	public string Value = string.Empty;
	static readonly Regex ArgsRegex = new Regex( @"({{|\\[nt\\])|({[^}]+})", RegexOptions.Compiled );
	static readonly Regex ColorRegex = new Regex( @"({{|}}|\\[^nt{}])|({[^}]*})|(\||\\t|\\n)", RegexOptions.Compiled );

	public LocalisableString ( string key ) {
		Key = key;
	}

	public IEnumerable<string> Args
		=> ArgsRegex.Matches( Value ).Where( x => x.Groups[2].Success )
			.Select( x => x.Groups[2].Value.Trim( '{', '}' ).Split( ':' )[0] ).Distinct();

	public static string Colorize ( string value, bool colorizeInnards ) {
		var split = ColorRegex.Split( value );
		StringBuilder sb = new( value.Length * 2 );
		foreach ( var i in split ) {
			if ( i == "{{" ) {
				if ( colorizeInnards ) sb.Append( Program.esc( 'N' ) );
				sb.Append( i[0] );
				if ( colorizeInnards ) sb.Append( Program.esc( '\0' ) );
				sb.Append( i[1] );
			}
			else if ( i == "}}" ) {
				sb.Append( i[0] );
				if ( colorizeInnards ) sb.Append( Program.esc( 'N' ) );
				sb.Append( i[1] );
				if ( colorizeInnards ) sb.Append( Program.esc( '\0' ) );
			}
			else if ( i.Length > 1 && i.StartsWith( "{" ) ) {
				sb.Append( Program.esc( 'G' ) );
				var n = i.Trim( '{', '}' ).Split( ':' );
				sb.Append( '{' );
				sb.Append( n[0] );
				if ( n.Length > 1 ) {
					if ( colorizeInnards ) sb.Append( Program.esc( 'B' ) );
					sb.Append( ':' );
					sb.Append( string.Join( ':', n[1..] ) );
					if ( colorizeInnards ) sb.Append( Program.esc( '\0' ) );
				}
				sb.Append( '}' );
				sb.Append( Program.esc( '\0' ) );
			}
			else if ( i == "|" || i == "\\n" || i == "\\t" ) {
				sb.Append( Program.esc( 'R' ) );
				sb.Append( i );
				sb.Append( Program.esc( '\0' ) );
			}
			else if ( i.Length == 2 && i.StartsWith( '\\' ) ) {
				sb.Append( Program.esc( 'N' ) );
				sb.Append( '\\' );
				sb.Append( Program.esc( '\0' ) );
				sb.Append( i[1] );
			}
			else {
				sb.Append( i );
			}
		}

		return sb.ToString();
	}

	public string ColoredValue
		=> Colorize( Value, colorizeInnards: true );

	public string ExportAsIndices ( Dictionary<string, int>? indices = null ) {
		if ( indices == null ) {
			var args = Args.ToArray();
			indices = new();

			int i = 0;
			foreach ( var n in args ) {
				indices.Add( n, i++ );
			}
		}

		StringBuilder sb = new( Value.Length );

		var split = ArgsRegex.Split( Value );
		foreach ( var n in split ) {
			if ( n == "\\\\" )
				sb.Append( '\\' );
			else if ( n == "\\t" )
				sb.Append( '\t' );
			else if ( n == "\\n" )
				sb.Append( '\n' );
			else if ( n == "{{" )
				sb.Append( n );
			else if ( n.StartsWith( "{" ) && n.Length > 2 ) {
				sb.Append( '{' );
				var s = n.Trim( '{', '}' ).Split( ':' );

				sb.Append( indices[s[0]] );
				if ( s.Length > 1 ) {
					sb.Append( ':' );
					sb.Append( string.Join( ':', s[1..] ) );
				}
				sb.Append( '}' );
			}
			else {
				sb.Append( n );
			}
		}

		return sb.ToString();
	}

	public string Format ( object?[] args, Dictionary<string, int>? indices = null )
		=> string.Format( ExportAsIndices( indices ), args );

	public string ColoredFormat ( object?[] args, Dictionary<string, int>? indices = null )
		=> string.Format( Colorize( ExportAsIndices( indices ), colorizeInnards: false ), args );
}
