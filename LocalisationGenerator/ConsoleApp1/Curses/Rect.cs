namespace LocalisationGenerator.Curses;

public struct Rect {
	public int X;
	public int Y;
	public int Width;
	public int Height;

	public bool Contains ( int x, int y ) {
		return x >= X && x < X + Width
			&& y >= Y && y < Y + Height;
	}

	public override bool Equals ( object? obj ) {
		return obj is Rect rect &&
				 X == rect.X &&
				 Y == rect.Y &&
				 Width == rect.Width &&
				 Height == rect.Height;
	}

	public override int GetHashCode () {
		return HashCode.Combine( X, Y, Width, Height );
	}

	public int Right => X + Width;
	public int Bottom => Y + Height;

	public static bool operator == ( Rect left, Rect right ) {
		return left.Equals( right );
	}

	public static bool operator != ( Rect left, Rect right ) {
		return !( left == right );
	}
}
