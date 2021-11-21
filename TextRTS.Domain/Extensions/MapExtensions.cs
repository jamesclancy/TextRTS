using TestRTS.Contracts;
using TestRTS.Contracts.Extensions;

namespace TextRTS.Domain.Extensions
{
    public static class MapExtensions
    {
        public static Result<IEnumerable<KeyValuePair<string, Character>>, string> GetCharactersForLocation(this Map map, short x, short y)
        {
            var record = map.Characters
                .Where(squareLocation => squareLocation.Value.Position.X == x && squareLocation.Value.Position.Y == y);

            return new Result<IEnumerable<KeyValuePair<string, Character>>, string>.Success(record);
        }

        public static Result<MapSquare, string> GetSquareForLocation(this Map map, short x, short y)
        {
            var record = map.Squares
                .FirstOrDefault(squareLocation => squareLocation.Position.X == x && squareLocation.Position.Y == y);

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

        public static Result<Map, string> TryToMovePlayer(this Map map, Position newPlayerPosition)
        {
            var newChars = map.Characters;

            var location = map.GetSquareForLocation(newPlayerPosition.X, newPlayerPosition.Y);
            var characters = map.GetCharactersForLocation(newPlayerPosition.X, newPlayerPosition.Y);

            if (location.IsFailure)
                return new Result<Map, string>.Failure("Location does not exist on map.");

            var terrain = location.AsSuccess.TerainType;

            if (!terrain.IsPassable)
            {
                return new Result<Map, string>.Failure($"You are not currently able to visit {terrain.Name} locations.");
            }

            if (characters.IsSuccess && characters.AsSuccess.Any())
            {
                // Somebody else is here? You wanna fight them
                return new Result<Map, string>.Failure("Unable to move there someone is already there? Maybe you should attack them.");
            }

            newChars[Constants.PlayerId] = newChars[Constants.PlayerId] with { Position = newPlayerPosition };

            return new Result<Map, string>.Success(map with { Characters = newChars });
        }
    }
}