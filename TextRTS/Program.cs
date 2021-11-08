// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using Spectre.Console.Rendering;
using TextRTS.Domain;




namespace TextRTS
{
    public static class Program
    {
        private static IEnumerable<MapSquare> TestSquares(short totalX, short totalY) =>
         Enumerable.Range(0, totalY)
           .SelectMany(y => Enumerable.Range(0, totalX)
                       .Select(x =>
                            new MapSquare((short)x, (short)y,
                            new TerainType("Water", "#0000ff", "~~"))));
        private static Map TestMap(short totalX, short totalY) => new Map(new List<MapSquare>(TestSquares(totalX, totalY)));

        public static async Task Main(string[] args)
        {

            var map = TestMap(100, 100);

            short xScreen = 20;
            short yScreen = 25;

            var table = new Table()
                    .ShowHeaders();

            table.AddColumn(new TableColumn($"[green][/]"));

            for (int x = 1; x <= xScreen; x++)
                table.AddColumn(new TableColumn("000") { Header = BuildColumnHeaderForXValue(x) });

            await AnsiConsole.Live(table)
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .Cropping(VerticalOverflowCropping.Bottom)
                .StartAsync(async ctx =>
                {
                    for (int y = 0; y < yScreen; y++)
                        table.AddEmptyRow();

                    GameViewState gameViewState = new GameViewState(map, table, xScreen, yScreen, (short)0, (short)0, string.Empty, false, string.Empty);

                    while (true)
                    {

                        UpdateTableForMap(gameViewState);
                        ctx.Refresh();
                        await Task.Delay(10);

                        gameViewState = ReduceKeyDowns(gameViewState);

                        if (gameViewState.Exit)
                            return;
                    }
                });
        }

        public static GameViewState ReduceKeyDowns(GameViewState currentViewState)
        {
            (Map map, Table table, short xScreen, short yScreen, short viewPortXStart, short viewPortYStart, string alertMessage, bool waitForLineReturn, string currentBuildingInput) = currentViewState;

            ConsoleKeyInfo currentKeyDown = Console.ReadKey(true);

            if (currentViewState.Exit || currentKeyDown.Key == ConsoleKey.Escape)
                return currentViewState with { Exit = true };

            if (!waitForLineReturn)
            {
                alertMessage = string.Empty;

                if (currentKeyDown.Key == ConsoleKey.LeftArrow)
                {
                    if (viewPortXStart > 0)
                        viewPortXStart--;
                    else alertMessage = "You have reached the end of map and I cannot move left anymore.";
                }
                if (currentKeyDown.Key == ConsoleKey.RightArrow)
                {
                    if (viewPortXStart < map.TotalX - xScreen)
                        viewPortXStart++;
                    else alertMessage = "You have reached the end of map and I cannot move right anymore.";
                }
                if (currentKeyDown.Key == ConsoleKey.UpArrow)
                {
                    if (viewPortYStart > 0)
                        viewPortYStart--;
                    else alertMessage = "You have reached the end of map and I cannot move up anymore.";
                }
                if (currentKeyDown.Key == ConsoleKey.DownArrow)
                {
                    if (viewPortYStart < map.TotalY - yScreen)
                        viewPortYStart++;
                    else alertMessage = "You have reached the end of map and I cannot move down anymore.";
                }
                if (currentKeyDown.Key == ConsoleKey.M)
                {
                    currentBuildingInput = string.Empty;
                    waitForLineReturn = true;

                    alertMessage = "Enter movement to attempt to move to? `xx,yy`:";
                }
            }
            else
            {
                if (currentKeyDown.Key == ConsoleKey.Enter)
                {
                    currentBuildingInput = string.Empty;
                    waitForLineReturn = false;
                }
                else
                {
                    currentBuildingInput = $"{currentBuildingInput}{currentKeyDown.KeyChar}";
                }
            }

            return new GameViewState(map, table, xScreen, yScreen, viewPortXStart, viewPortYStart, alertMessage, waitForLineReturn, currentBuildingInput);
        }


        public static void UpdateTableForMap(GameViewState gameViewModel)
        {
            (Map map, Table table, short xScreen, short yScreen, short viewPortXStart, short viewPortYStart, string alertMessage, bool waitForLineReturn, string currentBuildingInput) = gameViewModel;

            if (string.IsNullOrEmpty(alertMessage))
                alertMessage = "Use the arrow keys to navigate the work. Press Esc to exit.";

            table.Caption = waitForLineReturn ? new TableTitle($"[red]{alertMessage}[/] : [bold yellow]{currentBuildingInput}[/]") : new TableTitle($"[red]{alertMessage}[/]");

            for (int x = 1; x <= xScreen; x++)
                table.Columns[x].Header = BuildColumnHeaderForXValue(x + viewPortXStart);

            var renderableMap = map.GetRenderableTable(viewPortXStart, viewPortYStart, xScreen, yScreen);

            if (renderableMap.IsFailure)
                return;

            for (int y = 0; y < yScreen; y++)
            {
                table.UpdateCell(y, 0, BuildRowHeaderForYValue(renderableMap.AsSuccess.Rows[y].CurrentY + 1));

                for (int x = 0; x < xScreen; x++)
                    table.UpdateCell(y, x + 1, RenderCellContent(renderableMap, y, x));
            }
        }

        private static IRenderable RenderCellContent(Result<RenderableMapTable, string> renderableMap, int y, int x)
        {
            var cell = renderableMap.AsSuccess.Rows[y].RenderableMapSquares[x];

            return new Markup($"[#{cell.HexColor}]{cell.CharacterSymbol}[/]");
        }

        private static IRenderable BuildColumnHeaderForXValue(int x)
        {
            var formatedXValue = x.ToString("00");
            return new Markup($"[green]{formatedXValue}[/]");
        }

        private static IRenderable BuildRowHeaderForYValue(int y)
        {
            var formatedYValue = y.ToString("00");
            return new Markup($"[green]{formatedYValue}[/]");
        }

        public record GameViewState(Map map, Table table, short xScreen, short yScreen, short viewPortXStart, short viewPortYStart, string alertMessage, bool waitForLineReturn, string currentBuildingInput)
        {
            public bool Exit { get; init; }
        }
    }
}
