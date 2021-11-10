using Xunit;
using TextRTS.Domain;
using System.Linq;
using System.Collections.Generic;
using TextRTS.Domain.Extensions;

namespace TextRTS.Domain.Tests
{
    public class MapTests
    {

        public static IEnumerable<MapSquare> TestSquares(short totalX, short totalY) =>
                 Enumerable.Range(0, totalY)
                   .SelectMany(y => Enumerable.Range(0, totalX)
                               .Select(x =>
                                    new MapSquare((short)x, (short)y,
                                    new TerainType("Water", "#0000ff", "🌊"))));
        public static Map TestMap(short totalX, short totalY) => new Map(new List<MapSquare>(TestSquares(totalX, totalY)), new Dictionary<string, Character>());

        [Fact]
        public void Map_TotalX_Correct()
        {
            var tenByTwentyMap = TestMap(10, 20);
            Assert.Equal(10, tenByTwentyMap.TotalX);
        }

        [Fact]
        public void Map_TotalY_Correct()
        {
            var tenByTwentyMap = TestMap(10, 20);
            Assert.Equal(20, tenByTwentyMap.TotalY);
        }

        [Theory]
        [InlineData(111, 1111)]
        [InlineData(0, 1111)]
        [InlineData(11, 1111)]
        [InlineData(11, 11)]
        [InlineData(11, 50)]
        [InlineData(10, 0)]
        [InlineData(0, 20)]
        public void Map_GetSquareForLocation_NotFound_CorrectFailureMessage(short x, short y)
        {
            var tenByTwentyMap = TestMap(10, 20);
            var result = tenByTwentyMap.GetSquareForLocation(x, y);

            Assert.True(result.IsFailure);
            Assert.Equal($"Unable to locate a map tile @ ({x},{y}).", result.AsFailure);
        }


        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(9, 10)]
        [InlineData(5, 13)]
        public void Map_GetSquareForLocation_Found_SuccessReturned(short x, short y)
        {

            var tenByTwentyMap = TestMap(10, 20);
            var result = tenByTwentyMap.GetSquareForLocation(x, y);

            Assert.True(result.IsSuccess);
            Assert.Equal(x, result.AsSuccess.X);
            Assert.Equal(y, result.AsSuccess.Y);

        }

        [Theory]
        [InlineData(1, 1, 1, 1)]
        [InlineData(1, 1, 5, 5)]
        public void Map_GetRenderableTable_RenderedCorrectly(short offsetX, short offsetY, short xRenderScreen, short yRenderScreen)
        {

            var tenByTwentyMap = TestMap(10, 20);
            var result = tenByTwentyMap.GetRenderableTable(offsetX, offsetY, xRenderScreen, yRenderScreen);

            Assert.True(result.IsSuccess);
            Assert.Equal(yRenderScreen, result.AsSuccess.Rows.Count);
            Assert.All(result.AsSuccess.Rows, x => Assert.Equal(x.RenderableMapSquares.Count, xRenderScreen));
        }
    }
}