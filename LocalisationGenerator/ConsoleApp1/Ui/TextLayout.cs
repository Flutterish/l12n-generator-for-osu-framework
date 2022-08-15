using LocalisationGenerator.Curses;
using System.Text.RegularExpressions;

namespace LocalisationGenerator.UI;

public class TextLayout {
	public List<Line> Lines = new();

	public static readonly string RtlSyntax = @"[\u04c7-\u0591\u05D0-\u05EA\u05F0-\u05F4\u0600-\u06FF]";
	public static readonly string RtlSyntaxns = @"[/\u04c7-\u0591\u05D0-\u05EA\u05F0-\u05F4\u0600-\u06FF]";
	public static readonly Regex WordRegex = new( @$"(?:/)|(?:[\S-{RtlSyntaxns}]|\u0001.)+|(?:{RtlSyntaxns}|\u0001.)+", RegexOptions.Compiled );
	public static readonly Regex RtlRegex = new( @$"^(?:{RtlSyntax}|\u0001.)+$", RegexOptions.Compiled );
	public static readonly Regex IsEscapes = new( @"^(?:\u0001.)+$", RegexOptions.Compiled );
	public static readonly Regex EscapeRegex = new( @"\u0001.", RegexOptions.Compiled );
	public TextLayout ( string str, Rect rect, int x, int y, bool wrap = true ) {
		bool justWrapped = false;

		List<Word> lineWords = new();
		string currentWord = "";
		bool isWordVisible = true;
		bool isWordWhitespace = false;
		bool wordSplit = false;
		int lineWidth = 0;

		void addLetter ( char c, bool visible = true, bool control = false ) {
			bool whitespace = char.IsWhiteSpace( c );
			string str = c.ToString();

			if ( control ) {
				whitespace = false;
				visible = true;
				str = "\u0001" + c;
			}

			bool push = wordSplit && !whitespace && !isWordWhitespace;
			if ( visible != isWordVisible ) push = true;

			if ( whitespace != isWordWhitespace ) push = true;

			if ( push && currentWord != "" ) {
				lineWords.Add( new( currentWord, isWordVisible, isWordWhitespace ) );
				currentWord = "";
			}

			currentWord += str;
			if ( visible && !control ) lineWidth++;
			isWordVisible = visible;
			isWordWhitespace = whitespace;
			wordSplit = false;
		}

		bool? rtlLineContext = null;
		void newLine ( bool wrap = true ) {
			y = Math.Min( rect.Height - 1, y + 1 );
			x = 0;

			if ( currentWord != "" && !IsEscapes.IsMatch( currentWord ) ) {
				lineWords.Add( new( currentWord, isWordVisible, isWordWhitespace ) );
				currentWord = "";
			}

			bool removeSpace = false;
			if ( wrap ) {
				if ( lineWords.Count >= 2 && lineWords[^1].Value == " " && RtlRegex.IsMatch( lineWords[^2].Value ) ) {
					removeSpace = true;
					lineWords.RemoveAt( lineWords.Count - 1 );
					lineWidth--;
				}
			}

			Lines.Add( new Line( lineWords, lineWidth, rtlLineContext ) );
			var dir = Lines.Last().DirectionBlocks.LastOrDefault();
			if ( dir.count != 0 )
				rtlLineContext = dir.isRtl;

			lineWidth = 0;
			lineWords = new();
			if ( removeSpace ) lineWords.Add( new( " ", false, true ) );
		}

		void write ( char s ) {
			if ( s == '\n' ) {
				newLine();
				addLetter( s, visible: false );
			}
			else if ( s == '\t' ) {
				int count = (( x + Window.tabAlign ) / Window.tabAlign) * Window.tabAlign - x;
				for ( int i = 0; i < count && rect.Width > x + rect.X; i++ ) {
					addLetter( ' ' );
					x++;
				}
			}
			else {
				if ( x >= rect.Width ) newLine();

				addLetter( s );
				x++;
			}
		}

		void writeString ( string str ) {
			if ( str.Length == 0 )
				return;

			bool deleteSpace = ( justWrapped && x == 0 || x >= rect.Width ) && str[0] == ' ';
			if ( deleteSpace ) {
				if ( x != 0 )
					newLine();
				addLetter( ' ', visible: false );
			}

			foreach ( char c in deleteSpace ? str[1..] : str ) {
				if ( !wrap && c != '\n' && x >= rect.Width ) {
					addLetter( c, visible: false );
					continue;
				}

				write( c );
			}
		}

		var whitespaces = WordRegex.Split( str );
		var words = WordRegex.Matches( str );
		for ( int i = 0; i < whitespaces.Length; i++ ) {
			if ( i != 0 ) {
				wordSplit = true;
				var word = words[i - 1].Value;
				var parts = word.Split( '\u0001' );
				var length = word.Length - ( parts.Length - 1 ) * 2;

				if ( x != 0 && x + length > rect.Width && wrap ) {
					newLine();
					justWrapped = true;
				}

				for ( int j = 0; j < parts.Length; j++ ) {
					if ( j > 0 ) {
						addLetter( parts[j][0], control: true );

						writeString( parts[j][1..] );
					}
					else {
						writeString( parts[j] );
					}
				}
			}

			writeString( whitespaces[i] );
			justWrapped = false;
		}

		if ( currentWord != "" ) {
			lineWords.Add( new( currentWord, isWordVisible, isWordWhitespace ) );
			currentWord = "";
		}
		newLine( false );
	}
}

public class Line {
	public List<Word> Words = new();
	public List<(int count, bool isRtl)> DirectionBlocks = new();
	public int Width;

	public Line ( List<Word> words, int width, bool? rtlHint = null ) {
		Words = words;
		Width = width;

		int amountInBlock = 0;
		bool? isRtl = rtlHint;
		int i = 0;

		void addBlock () {
			int j = i - amountInBlock - 1;
			while ( isRtl != true && j >= 0 && words[j].IsWhitespace ) {
				amountInBlock++;
				DirectionBlocks[^1] = (DirectionBlocks[^1].count - 1, DirectionBlocks[^1].isRtl);
				j--;
			}

			DirectionBlocks.Add( (amountInBlock, isRtl ?? false) );
		}

		for ( ; i < words.Count; i++ ) {
			var word = words[i];

			if ( word.IsWhitespace || TextLayout.IsEscapes.IsMatch( word.Value ) ) amountInBlock++;
			else {
				bool isWordRtl = TextLayout.RtlRegex.IsMatch( word.Value );
				if ( isRtl == null ) isRtl = isWordRtl;
				else if ( isRtl != isWordRtl ) {
					addBlock();
					isRtl = isWordRtl;
					amountInBlock = 0;
				}

				amountInBlock++;
			}
		}

		if ( amountInBlock != 0 ) addBlock();
	}

	public void Deconstruct ( out List<Word> words, out int width ) {
		words = Words;
		width = Width;
	}
}

public struct Word {
	public string Value;
	public bool IsVisible;
	public bool IsWhitespace;
	public int Width;

	public Word ( string value, bool isVisible, bool isWhitespace ) {
		Value = value;
		IsVisible = isVisible;
		IsWhitespace = isWhitespace;
		Width = isVisible ? value.Length - value.Count( x => x == '\u0001' ) * 2 : 0;
	}

	public void Deconstruct ( out string word, out bool visible, out bool whitespace ) {
		word = Value;
		visible = IsVisible;
		whitespace = IsWhitespace;
	}
}