namespace TextRTS.Domain
{
    public record MapSquare(short X, short Y, TerainType TerainType);

    public record TerainType(string Name, string ColorHexCode, string CharacterSymbol);

    public record RenderableMapRow(List<RenderableMapSquare> RenderableMapSquares, short CurrentY);

    public record RenderableMapTable(List<RenderableMapRow> Rows, short CurrentXOffset, short CurrenyYOffset, short XRenderScreen, short YRenderScreen, short TotalX, short TotalY);

    public record RenderableMapSquare(short X, short Y, string HexColor, string CharacterSymbol);

    public record Map(ICollection<MapSquare> Squares)
    {
        public short TotalX { get { return (short)(Squares.Max(s => s.X) + (short)1); } }

        public short TotalY { get { return (short)(Squares.Max(s => s.Y) + (short)1); } }

    }
}