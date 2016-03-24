using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoggleClient
{
    interface IBoggleView
    {
        event Action<string, string, int> NewGameEvent;

        event Action<string> EnterWordEvent;

        event Action QuitGameEvent;

        void DoClose();

        void ShowLetters(LinkedList<string> list);

        string Message { set; }

        void SetScore(string text);

        void SetTime(string text);

        DialogResult Warning(string message);

        DialogResult Error(string message);
    }
}
