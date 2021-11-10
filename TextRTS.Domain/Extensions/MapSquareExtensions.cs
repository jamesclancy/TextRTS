namespace TextRTS.Domain.Extensions
{
    public static class MapSquareExtensions
    {
        public static RenderableMapSquare ToRenderableMapSquare(this MapSquare mapSquare, IEnumerable<KeyValuePair<string, Character>> charactersOnSquare)
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