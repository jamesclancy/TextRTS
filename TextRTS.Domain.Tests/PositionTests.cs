using Xunit;

namespace TextRTS.Domain.Tests
{
    public class PositionTests
    {
        [Theory]
        [InlineData("01,01", 0, 0)]
        [InlineData("02,01", 1, 0)]
        [InlineData("03,03", 2, 2)]
        [InlineData("04,01", 3, 0)]
        [InlineData("01,04", 0, 3)]
        [InlineData("10,01", 9, 0)]
        [InlineData("10,12", 9, 11)]
        [InlineData("01,99", 0, 98)]
        public void Position_ParseInputForNewLocation_SuccessOutput(string input, short x, short y)
        {
            var pos = Position.ParseInputForNewLocation(input);
            Assert.True(pos.IsSuccess);
            Assert.Equal(new Position(x, y), pos.AsSuccess);
        }

        [Theory]
        [InlineData("01,1")]
        [InlineData("02;01")]
        [InlineData("3,03")]
        [InlineData("O4,01")]
        [InlineData("01,4")]
        [InlineData("100,01")]
        [InlineData("10.12")]
        [InlineData("01,99.0")]
        public void Position_ParseInputForNewLocation_Fails(string input)
        {
            var pos = Position.ParseInputForNewLocation(input);
            Assert.True(pos.IsFailure);
            Assert.False(pos.IsSuccess);
        }
    }
}