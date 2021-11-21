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
            var player = newChars[Constants.PlayerId];

            (bool isValidToMoveTo, InvalidMovementReason invalidMovementReason, string terrainName)
                = map.GetLocationMovabilityStatus(newPlayerPosition);

            if (!isValidToMoveTo)
                return invalidMovementReason switch
                {
                    InvalidMovementReason.ImpassableLocation => new Result<Map, string>.Failure("Location does not exist on map."),
                    InvalidMovementReason.LocationDoesNotExist => new Result<Map, string>.Failure($"You are not currently able to visit {terrainName} locations."),
                    InvalidMovementReason.SomeoneAlreadyThere => new Result<Map, string>.Failure("Unable to move there someone is already there? Maybe you should attack them."),
                    _ => throw new NotImplementedException(),
                };

            // So I can try in that direction.

            return TryPlayerStep(map, newPlayerPosition, newChars, player);
        }

        private static Result<Map, string> TryPlayerStep(Map map, Position newPlayerPosition, Dictionary<string, Character> newChars, Character player)
        {
            var (xMovement, yMovement) = (newPlayerPosition.X - player.Position.X, newPlayerPosition.Y - player.Position.Y);

            if (Math.Abs(xMovement) > 0)
            {
                var positionToTest = new Position((short)(player.Position.X + (1 * Math.Sign(xMovement))), player.Position.Y);
                (bool validStep, _, _) = map.GetLocationMovabilityStatus(newPlayerPosition);
                if (validStep)
                    return BuildNewMapWithStepedPlayer(map, positionToTest, newChars, player);
            }

            if (Math.Abs(yMovement) > 0)
            {
                var positionToTest = new Position(player.Position.X, (short)(player.Position.Y + (1 * Math.Sign(yMovement))));
                (bool validStep, _, _) = map.GetLocationMovabilityStatus(newPlayerPosition);
                if (validStep)
                    return BuildNewMapWithStepedPlayer(map, positionToTest, newChars, player);
            }

            return new Result<Map, string>.Failure("Unable to move in that direction.");
        }

        private static Result<Map, string> BuildNewMapWithStepedPlayer(Map map, Position newPlayerPosition, Dictionary<string, Character> newChars, Character player)
        {
            newChars[Constants.PlayerId] = player with { Position = newPlayerPosition };

            return new Result<Map, string>.Success(map with { Characters = newChars });
        }

        private static (bool isValidToMoveTo, InvalidMovementReason invalidMovementReason, string terrainName) GetLocationMovabilityStatus(this Map map, Position position)
        {

            var location = map.GetSquareForLocation(position.X, position.Y);
            var characters = map.GetCharactersForLocation(position.X, position.Y);

            if (location.IsFailure)
                return (false, InvalidMovementReason.LocationDoesNotExist, "NA");

            var terrain = location.AsSuccess.TerainType;

            if (!terrain.IsPassable)
                return (false, InvalidMovementReason.ImpassableLocation, terrain.Name);

            if (characters.IsSuccess && characters.AsSuccess.Any())
                return (false, InvalidMovementReason.SomeoneAlreadyThere, terrain.Name);

            return (true, InvalidMovementReason.None, terrain.Name);
        }

        private enum InvalidMovementReason
        {
            None = 0,
            LocationDoesNotExist = 1,
            ImpassableLocation = 2,
            SomeoneAlreadyThere = 3
        }
    }
}