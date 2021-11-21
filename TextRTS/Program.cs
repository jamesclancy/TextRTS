// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using Spectre.Console.Rendering;
using TestRTS.Contracts;
using TextRTS.Domain;
using TextRTS.Domain.Extensions;

namespace TextRTS
{
    public static class Program
    {
        private static IEnumerable<MapSquare> TestSquares(short totalX, short totalY) =>
         Enumerable.Range(0, totalY)
           .SelectMany(y => Enumerable.Range(0, totalX)
                       .Select(x =>
                            new MapSquare(new Position((short)x, (short)y), GenerateTerain((short)x, (short)y))));

        private static TerainType GenerateTerain(short x, short y)
        {
            if (x != y)
                return new TerainType("Water", true, "#0000ff", ":water_wave:");

            return new TerainType("Mountain", false, "#ffff00", ":mountain:");
        }

        private static Dictionary<string, Character> CharacterMap = new Dictionary<string, Character>()
        {
            { "PLAYER", new Character(new Position(1, 1),new CharacterSprite("#434300", ":robot:")) }
        };

        private static Map TestMap(short totalX, short totalY) => new Map(new List<MapSquare>(TestSquares(totalX, totalY)), CharacterMap);

        public static async Task Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8; // Hacky solution for emojis to work on windows. 

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

                    GameViewState gameViewState = new GameViewState(map, table, xScreen, yScreen,
                        (short)0, (short)0, string.Empty,
                        GameInputEntryType.None, string.Empty, GameflowStep.AwaitingUserInput,
                        GameflowProcessingType.None, string.Empty);

                    while (true)
                    {
                        UpdateTableForMap(gameViewState);
                        ctx.Refresh();

                        if (gameViewState.Exit)
                            return;

                        switch(gameViewState.GameflowStep)
                        {
                            case GameflowStep.AwaitingUserInput:
                                await Task.Delay(10);
                                gameViewState = ReduceKeyDowns(gameViewState);
                                break;
                            case GameflowStep.Processing:
                                await Task.Delay(100);
                                gameViewState = ProcessStep(gameViewState);
                                break;
                        }
                    }
                });
        }

        private static GameViewState ProcessStep(GameViewState gameViewState)
        {
            return gameViewState.GameflowProcessingType switch
            {
                GameflowProcessingType.MovingUser => tryToMovePlayerForInput(gameViewState),
                _ => throw new NotImplementedException(),
            };
        }

        public static GameViewState ReduceKeyDowns(GameViewState currentViewState)
        {
            ConsoleKeyInfo currentKeyDown = Console.ReadKey(true);

            if (currentViewState.Exit || currentKeyDown.Key == ConsoleKey.Escape)
                return currentViewState with { Exit = true };

            if (currentViewState.entryType == GameInputEntryType.None)
            {
                return ProcessMainMenuCommand(currentViewState, currentKeyDown);
            }
            else
            {
                if (currentKeyDown.Key == ConsoleKey.Enter)
                {
                    return currentViewState.entryType switch
                    {
                        GameInputEntryType.MovementPosition => tryToMovePlayerForInput(currentViewState),
                        _ => throw new NotImplementedException(),
                    };
                }
                else
                {
                    return currentViewState with { currentBuildingInput = $"{currentViewState.currentBuildingInput}{currentKeyDown.KeyChar}" };
                }
            }
        }

        private static GameViewState ProcessMainMenuCommand(GameViewState currentViewState, ConsoleKeyInfo currentKeyDown)
        {
            (Map map, Table table, short xScreen, short yScreen, short viewPortXStart, short viewPortYStart,
                string alertMessage, GameInputEntryType entryType, string currentBuildingInput,
            GameflowStep gameflowStep, GameflowProcessingType gameflowProcessingType, string gameflowProcessingValue
                ) = currentViewState;

            alertMessage = string.Empty;

            switch (currentKeyDown.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (viewPortXStart > 0)
                        viewPortXStart--;
                    else alertMessage = Constants.CannotMoveMap("left");
                    break;
                case ConsoleKey.RightArrow:
                    if (viewPortXStart < map.TotalX - xScreen)
                        viewPortXStart++;
                    else alertMessage = Constants.CannotMoveMap("right");
                    break;
                case ConsoleKey.UpArrow:
                    if (viewPortYStart > 0)
                        viewPortYStart--;
                    else alertMessage = Constants.CannotMoveMap("up");
                    break;
                case ConsoleKey.DownArrow:
                    if (viewPortYStart < map.TotalY - yScreen)
                        viewPortYStart++;
                    else alertMessage = Constants.CannotMoveMap("down");
                    break;
                case ConsoleKey.M:
                    currentBuildingInput = string.Empty;
                    entryType = GameInputEntryType.MovementPosition;
                    alertMessage = Constants.EnterMovementMessage;
                    break;
                case ConsoleKey.B:
                    currentBuildingInput = string.Empty;
                    entryType = GameInputEntryType.ThingToBuild;
                    alertMessage = Constants.EnterThingToBuild;
                    break;
                default:
                    break;
            }

            return new GameViewState(map, table, xScreen, yScreen, viewPortXStart, viewPortYStart, alertMessage,
                entryType, currentBuildingInput, gameflowStep, gameflowProcessingType, gameflowProcessingValue);
        }

        private static GameViewState
            tryToMovePlayerForInput(GameViewState gameView)
        {
            (Map map, Table table, short xScreen, short yScreen, short viewPortXStart, short viewPortYStart,
                string alertMessage, GameInputEntryType entryType, string currentBuildingInput,
            GameflowStep gameflowStep, GameflowProcessingType gameflowProcessingType, string gameflowProcessingValue
                ) = gameView;

            var parsedNewLocation = Position.ParseInputForNewLocation(currentBuildingInput);
            if (parsedNewLocation.IsSuccess)
            {
                var movePlayerNewMap = map.TryToMovePlayer(parsedNewLocation.AsSuccess);

                if (movePlayerNewMap.IsSuccess)
                {
                    (bool final, map) = movePlayerNewMap.AsSuccess;
                    currentBuildingInput = string.Empty;
                    entryType = GameInputEntryType.None;
                    alertMessage = "You have moved to a new exiciting location";

                    if (!final)
                    {
                        gameflowStep = GameflowStep.Processing;
                        gameflowProcessingValue = currentBuildingInput;
                        gameflowProcessingType = GameflowProcessingType.MovingUser;
                    } else
                    {
                        gameflowStep = GameflowStep.None;
                        gameflowProcessingValue = currentBuildingInput;
                        gameflowProcessingType = GameflowProcessingType.None;
                    }
                }
                else
                {

                    alertMessage = $"{movePlayerNewMap.AsFailure}. Please enter another location (XX,YY):";
                    currentBuildingInput = string.Empty;
                }
            }
            else
            {
                alertMessage = $"{parsedNewLocation.AsFailure}. Please enter another location (XX,YY):";
                currentBuildingInput = string.Empty;
            }

            return new GameViewState(map, table, xScreen, yScreen, viewPortXStart, viewPortYStart, alertMessage,
                entryType, currentBuildingInput, gameflowStep, gameflowProcessingType, gameflowProcessingValue);
        }

        public static void UpdateTableForMap(GameViewState gameViewModel)
        {
            (Map map, Table table, short xScreen, short yScreen, short viewPortXStart, short viewPortYStart,
                    string alertMessage, GameInputEntryType entryType, string currentBuildingInput,
                    GameflowStep gameflowStep, GameflowProcessingType gameflowProcessingType, string gameflowProcessingValue)
                    = gameViewModel;


            switch (gameflowStep)
            {
                case GameflowStep.AwaitingUserInput:
                    if (string.IsNullOrEmpty(alertMessage))
                        alertMessage = Constants.GenericUserDirections;

                    table.Caption = entryType == GameInputEntryType.None ?
                        CaptionWithoutBuildingInput(alertMessage)
                        : CaptionWithBuildingInput(alertMessage, currentBuildingInput);

                    break;
                case GameflowStep.Processing:

                    if (string.IsNullOrEmpty(alertMessage))
                        alertMessage = Constants.GenericUserDirections;

                    table.Caption = new TableTitle($":timer_clock:[red]{alertMessage}[/]:timer_clock:");
                    break;
            }


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

        private static IRenderable BuildColumnHeaderForXValue(int x) => new Markup($"[green]{x.ToString("00")}[/]");
        private static IRenderable BuildRowHeaderForYValue(int y) => new Markup($"[green]{y.ToString("00")}[/]");
        public static TableTitle CaptionWithoutBuildingInput(string alertMessage) => new TableTitle($"[red]{alertMessage}[/]");
        public static TableTitle CaptionWithBuildingInput(string alertMessage, string currentBuildingInput) => new TableTitle($"[red]{alertMessage}[/] : [bold yellow]{currentBuildingInput}[/]");

        public record GameViewState(Map map, Table table,
            short xScreen, short yScreen,
            short viewPortXStart, short viewPortYStart,
            string alertMessage,
            GameInputEntryType entryType, string currentBuildingInput,
            GameflowStep GameflowStep, GameflowProcessingType GameflowProcessingType, string GameflowProcessingValue)
        {
            public bool Exit { get; init; }
        }

        public enum GameflowStep
        {
            None = 0,
            AwaitingUserInput = 1,
            Processing = 2
        }

        public enum GameflowProcessingType
        {
            None = 0,
            MovingUser = 1,
            Building = 2
        }

        public enum GameInputEntryType
        {
            None = 0,
            MovementPosition = 1,
            ThingToBuild = 2
        }
    }
}