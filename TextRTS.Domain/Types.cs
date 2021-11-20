﻿using TestRTS.Contracts;

namespace TextRTS.Domain
{
    public record Position(short X, short Y)
    {
        public static Result<Position, string> ParseInputForNewLocation(string input)
        {
            if (input.Length == 5 && input[2] == ',')
            {
                var xParseable = short.TryParse(input.Substring(0, 2), out short xPos);
                var yParseable = short.TryParse(input.Substring(3, 2), out short yPos);

                if (xParseable && yParseable && xPos >= 1 && yPos >= 1)
                    return new Result<Position, string>.Success(new Position((short)(xPos - (short)1), (short)(yPos - (short)1)));
            }

            return new Result<Position, string>.Failure($"Input {input} not in XX,YY format.");
        }
    }

    public record MapSquare(Position Position, TerainType TerainType);

    public record TerainType(string Name, bool IsPassable, string ColorHexCode, string CharacterSymbol);

    public record RenderableMapRow(List<RenderableMapSquare> RenderableMapSquares, short CurrentY);

    public record RenderableMapTable(List<RenderableMapRow> Rows, short CurrentXOffset, short CurrenyYOffset, short XRenderScreen, short YRenderScreen, short TotalX, short TotalY);

    public record RenderableMapSquare(Position Position, string HexColor, string CharacterSymbol);

    public record Map(ICollection<MapSquare> Squares, Dictionary<string, Character> Characters)
    {
        public short TotalX { get { return (short)(Squares.Max(s => s.Position.X) + (short)1); } }

        public short TotalY { get { return (short)(Squares.Max(s => s.Position.Y) + (short)1); } }
    }

    public abstract record MoveableCharacter(Position Position);

    public record CharacterSprite(string ColorHexCode, string CharacterSymbol);

    public record Character(Position Position, CharacterSprite CharacterSprite) : MoveableCharacter(Position);
}