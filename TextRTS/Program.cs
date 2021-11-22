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
                return new TerainType(Constants.StaticDictionary.Water, new List<string>(), "#0000ff", ":water_wave:");

            return new TerainType(Constants.StaticDictionary.Mountain, new List<string> { "Flight" }, "#ffff00", ":mountain:");
        }

        private static Dictionary<string, Character> CharacterMap = new Dictionary<string, Character>()
        {
            { "PLAYER", new Character(new Position(1, 1),new CharacterSprite("#434300", ":robot:"), new List<CharacterUpgrade>()) }
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

                    GameViewState gameViewState = new GameViewState(map, xScreen, yScreen,
                        (short)0, (short)0, string.Empty,
                        GameInputEntryType.None, string.Empty, GameflowStep.AwaitingUserInput,
                        GameflowProcessingType.None, string.Empty);

                    while (true)
                    {
                        table.UpdateTableForMap(gameViewState); // I am not certain if there is a good way to make this stateless using spectre console.
                                                                // I am thinking this is best that can be done?
                        ctx.Refresh();

                        if (gameViewState.Exit)
                            return;

                        switch (gameViewState.GameflowStep)
                        {
                            case GameflowStep.AwaitingUserInput:
                                await Task.Delay(Constants.InputLoopDelay);
                                gameViewState = ReduceKeyDowns(gameViewState);
                                break;
                            case GameflowStep.Processing:
                                await Task.Delay(Constants.ProcessingLoopDelay);
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

        private static GameViewState ProcessMainMenuCommand(GameViewState previousGameViewState, ConsoleKeyInfo currentKeyDown)
        {
            return currentKeyDown.Key switch
            {
                ConsoleKey.LeftArrow => BuildNewGameViewState(0, -1, Constants.CannotMoveMap("left"), previousGameViewState.entryType),
                ConsoleKey.RightArrow => BuildNewGameViewState(0, +1, Constants.CannotMoveMap("right"), previousGameViewState.entryType),
                ConsoleKey.UpArrow => BuildNewGameViewState(-1, 0, Constants.CannotMoveMap("Up"), previousGameViewState.entryType),
                ConsoleKey.DownArrow => BuildNewGameViewState(1, 0, Constants.CannotMoveMap("down"), previousGameViewState.entryType),
                ConsoleKey.M => BuildNewGameViewState(0, 0, Constants.EnterMovementMessage, GameInputEntryType.MovementPosition),
                ConsoleKey.B => BuildNewGameViewState(0, 0, Constants.EnterThingToBuild, GameInputEntryType.ThingToBuild),
                _ => previousGameViewState
            };

            GameViewState BuildNewGameViewState(short yShift, short xShift, string alertMessage, GameInputEntryType gameInputEntryType)
                => previousGameViewState with
                {
                    viewPortYStart = (short)(previousGameViewState.viewPortYStart + yShift),
                    viewPortXStart = (short)(previousGameViewState.viewPortXStart + xShift),
                    alertMessage = alertMessage,
                    currentBuildingInput = String.Empty,
                    entryType = gameInputEntryType
                };
        }

        private static GameViewState
            tryToMovePlayerForInput(GameViewState gameView)
        {
            var parsedNewLocation = Position.ParseInputForNewLocation(gameView.currentBuildingInput);
            if (!parsedNewLocation.IsSuccess)
            {
                return gameView with { alertMessage = parsedNewLocation.AsFailure, currentBuildingInput = string.Empty };
            }

            var movePlayerNewMap = gameView.map.TryToMovePlayer(parsedNewLocation.AsSuccess);
            if (!movePlayerNewMap.IsSuccess)
            {
                return gameView with { alertMessage = movePlayerNewMap.AsFailure, currentBuildingInput = string.Empty };
            }

            (bool final, Map map) = movePlayerNewMap.AsSuccess;
            var entryType = GameInputEntryType.None;
            var alertMessage = "You have moved to a new exiciting location";

            var gameflowStep = GameflowStep.AwaitingUserInput;
            var gameflowProcessingValue = gameView.currentBuildingInput;
            var gameflowProcessingType = GameflowProcessingType.None;

            if (!final)
            {
                gameflowStep = GameflowStep.Processing;
                gameflowProcessingValue = gameView.currentBuildingInput;
                gameflowProcessingType = GameflowProcessingType.MovingUser;
            }

            return gameView with
            {
                alertMessage = alertMessage,
                currentBuildingInput = final ? string.Empty : gameflowProcessingValue,
                map = map,
                entryType = entryType,
                GameflowStep = gameflowStep,
                GameflowProcessingValue = gameflowProcessingValue,
                GameflowProcessingType = gameflowProcessingType
            };
        }

        public static Table UpdateTableForMap(this Table table, GameViewState gameViewModel)
        {
            (Map map, short xScreen, short yScreen, short viewPortXStart, short viewPortYStart, _, _, _, _, _, _)
                    = gameViewModel;

            table.Caption = BuildCaptionForViewState(gameViewModel);

            for (int x = 1; x <= xScreen; x++)
                table.Columns[x].Header = BuildColumnHeaderForXValue(x + viewPortXStart);

            var renderableMap = map.GetRenderableTable(viewPortXStart, viewPortYStart, xScreen, yScreen);

            if (renderableMap.IsFailure)
                return table;

            for (int y = 0; y < yScreen; y++)
            {
                table.UpdateCell(y, 0, BuildRowHeaderForYValue(renderableMap.AsSuccess.Rows[y].CurrentY + 1));

                for (int x = 0; x < xScreen; x++)
                    table.UpdateCell(y, x + 1, RenderCellContent(renderableMap, y, x));
            }

            return table;
        }

        private static TableTitle BuildCaptionForViewState(GameViewState gameViewModel)
        {
            string alertMessage = gameViewModel.alertMessage;

            return gameViewModel.GameflowStep switch
            {
                GameflowStep.AwaitingUserInput => GenerateAwaitingUserInputMessage(gameViewModel, alertMessage),
                GameflowStep.Processing => new TableTitle($"[green]:timer_clock:[/][red]{alertMessage}[/][green]:timer_clock:[/]"),
                _ => throw new NotImplementedException(),
            };

            static TableTitle GenerateAwaitingUserInputMessage(GameViewState gameViewModel, string alertMessage)
            {
                if (string.IsNullOrEmpty(alertMessage))
                    alertMessage = Constants.GenericUserDirections;
                return (gameViewModel.entryType == GameInputEntryType.None) ?
                    CaptionWithoutBuildingInput(alertMessage)
                    : CaptionWithBuildingInput(alertMessage, gameViewModel.currentBuildingInput);
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

        public record GameViewState(Map map,
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