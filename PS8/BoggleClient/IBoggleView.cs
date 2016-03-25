using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoggleClient
{
    /// <summary>
    /// Interface for the View and Controller to communicate. Makes unit testing easier.
    /// </summary>
    interface IBoggleView
    {
        /// <summary>
        /// Triggered when the user inputs the requisite information to join a new game.
        /// </summary>
        event Action<string, string, int> NewGameEvent;

        /// <summary>
        /// Triggered when the user enters a word.
        /// </summary>
        event Action<string> EnterWordEvent;

        /// <summary>
        /// Triggered when the user quits a pending or active game.
        /// </summary>
        event Action QuitGameEvent;

        /// <summary>
        /// Allows the Controller to close the GUI window.
        /// </summary>
        void DoClose();

        /// <summary>
        /// Displays the 16 tiles of the board.
        /// </summary>
        void ShowLetters(LinkedList<string> list);

        /// <summary>
        /// Displays a message box.
        /// </summary>
        string Message { set; }

        /// <summary>
        /// Allows access to the score label.
        /// </summary>
        void SetScore(string text);

        /// <summary>
        /// Allows access to the time label.
        /// </summary>
        void SetTime(string text);

        /// <summary>
        /// Displays a warning box and returns the result.
        /// </summary>
        DialogResult Warning(string message);

        /// <summary>
        /// Displays an error box and returns the result.
        /// </summary>
        DialogResult Error(string message);
    }
}
