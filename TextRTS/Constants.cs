using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextRTS
{
    public static class Constants
    {
        // Lookup Keys
        public const string PlayerId = "PLAYER";

        // User messages
        public const string GenericUserDirections = "Use the arrow keys to navigate the work. Press Esc to exit.";
        public const string CannotMoveMapMessage = "You have reached the end of map and I cannot move {0} anymore.";
        public const string EnterMovementMessage = "Enter movement to attempt to move to? `xx,yy`";

        // Not so constant constants
        public static string CannotMoveMap(string directionTryingToMoveTo) => string.Format(CannotMoveMapMessage, directionTryingToMoveTo);
    }
}
