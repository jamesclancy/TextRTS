﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRTS.Contracts
{
    public static class Constants
    {
        // Lookup Keys
        public const string PlayerId = "PLAYER";

        // User messages
        public const string GenericUserDirections = "Arrow keys to navigate screen. M to move. Press Esc to exit.";
        public const string CannotMoveMapMessage = "You have reached the end of map and I cannot move {0} anymore.";
        public const string EnterMovementMessage = "Enter movement to attempt to move to? `xx,yy`";
        public const string EnterThingToBuild = "Enter what you want to build:";

        public const string LocationDoesNotExistDefaultTerainName = "NA";

        // Delays
        public const int InputLoopDelay = 10;
        public const int ProcessingLoopDelay = 100;

        // Not so constant constants
        public static string CannotMoveMap(string directionTryingToMoveTo) => string.Format(CannotMoveMapMessage, directionTryingToMoveTo);
    }
}