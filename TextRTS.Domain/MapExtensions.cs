namespace TextRTS.Domain
{
    public static class MapExtensions
    {
        public static Result<IEnumerable<KeyValuePair<string, Character>>, string> GetCharactersForLocation(this Map map, short x, short y)
        {
            var record = map.Characters
                .Where(squareLocation => squareLocation.Value.X == x && squareLocation.Value.Y == y);

            return new Result<IEnumerable<KeyValuePair<string, Character>>, string>.Success(record);
        }

        public static Result<MapSquare, string> GetSquareForLocation(this Map map, short x, short y)
        {
            var record = map.Squares
                .FirstOrDefault(squareLocation => squareLocation.X == x && squareLocation.Y == y);

            if (record == null)
                return new Result<MapSquare, string>.Failure($"Unable to locate a map tile @ ({x},{y}).");

            return new Result<MapSquare, string>.Success(record);
        }


        public static Result<RenderableMapTable, string> GetRenderableTable(this Map map, short offsetX, short offsetY, short xRenderScreen, short yRenderScreen)
        {

            var rows = new List<RenderableMapRow>();
            for (var y = offsetY; y < yRenderScreen + offsetY; y++)
            {
                var result = ExtractRow(map, offsetX, xRenderScreen, y);
                if (result.IsFailure)
                    return new Result<RenderableMapTable, string>.Failure($"Unable to generate specified renderable table. - {result.AsFailure}");
                rows.Add(result.AsSuccess);

            }

            return new Result<RenderableMapTable, string>.Success(new RenderableMapTable(rows, offsetX, offsetY, xRenderScreen, yRenderScreen, map.TotalX, map.TotalY));
        }

        private static Result<RenderableMapRow, string> ExtractRow(Map map, short offsetX, short xRenderScreen, short y)
        {
            var rowBuilder = new List<RenderableMapSquare>();

            for (var x = offsetX; x < xRenderScreen + offsetX; x++)
            {
                var charactersAtLocation = map.GetCharactersForLocation(x, y);
                Result<MapSquare, string> result = map.GetSquareForLocation(x, y);


                var (anyFailues, failureList) = ResultExtensions.GetAllFailures(charactersAtLocation, result);

                if (anyFailues)
                    return new Result<RenderableMapRow, string>.Failure($"Unable to locate a map row @ ({y}). - {string.Join(";", failureList)}");
                rowBuilder.Add(result.AsSuccess.ToRenderableMapSquare(charactersAtLocation.AsSuccess));
            }

            return new Result<RenderableMapRow, string>.Success(new RenderableMapRow(rowBuilder, y));
        }

        private static RenderableMapSquare ToRenderableMapSquare(this MapSquare mapSquare, IEnumerable<KeyValuePair<string, Character>> charactersOnSquare)
        {
            var (hexCode, characterSymbol) = GetHexAndSymbol(mapSquare, charactersOnSquare);
            return new RenderableMapSquare(mapSquare.X, mapSquare.Y, hexCode, characterSymbol);
        }

        private static (string hexCode, string characterSymbol) GetHexAndSymbol(MapSquare mapSquare, IEnumerable<KeyValuePair<string, Character>> charactersOnSquare)
        {
            string hexCode, characterSymbol = String.Empty;
            hexCode = mapSquare.TerainType.ColorHexCode;
            characterSymbol = mapSquare.TerainType.CharacterSymbol;
            switch (charactersOnSquare.Count())
            {
                case 0:
                    hexCode = mapSquare.TerainType.ColorHexCode;
                    characterSymbol = mapSquare.TerainType.CharacterSymbol;
                    break;
                case 1:
                    hexCode = charactersOnSquare.First().Value.CharacterSprite.ColorHexCode;
                    characterSymbol = charactersOnSquare.First().Value.CharacterSprite.CharacterSymbol;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return (hexCode, characterSymbol);
        }
    }
}