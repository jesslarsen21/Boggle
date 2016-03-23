using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoggleClient
{
    public partial class BoggleGUI : Form, IBoggleView
    {
        // Contains an iterable list of the boggle letter labels.
        private LinkedList<object> letterLabels;

        public event Action<string, string, int> NewGameEvent;
        public event Action<string> EnterWordEvent;
        public event Action QuitGameEvent;

        public BoggleGUI()
        {
            InitializeComponent();
            CaptureGameInfo();
            letterLabels = new LinkedList<object>();
            letterLabels.AddLast(label1);
            letterLabels.AddLast(label2);
            letterLabels.AddLast(label3);
            letterLabels.AddLast(label4);
            letterLabels.AddLast(label5);
            letterLabels.AddLast(label6);
            letterLabels.AddLast(label7);
            letterLabels.AddLast(label8);
            letterLabels.AddLast(label9);
            letterLabels.AddLast(label10);
            letterLabels.AddLast(label11);
            letterLabels.AddLast(label12);
            letterLabels.AddLast(label13);
            letterLabels.AddLast(label14);
            letterLabels.AddLast(label15);
            letterLabels.AddLast(label16);
        }

        /// <summary>
        /// Displays a message dialog box.
        /// </summary>
        public string Message
        {
            set
            {
                MessageBox.Show(value, "Information.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Displays a warning dialog box.
        /// </summary>
        public DialogResult Warning(string message)
        {
            return MessageBox.Show(message, "Warning.", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Allows access to the score label for the controller class.
        /// </summary>
        public string Score
        {
            get { return scoreLabel.Text; }
            set { scoreLabel.Text = value; }
        }

        /// <summary>
        /// Displays a window at the end of the game with the score breakdown,
        /// and listing the words entered by both players.
        /// </summary>
        public string WordsGuessed
        {
            set
            {
                // TODO Create a popup window with the requisite information.
            }
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void DoClose()
        {
            Close();
        }

        /// <summary>
        /// Creates a popup window with text boxes for inputting the following strings:
        /// 1. Domain name of the Boggle server.
        /// 2. Player nickname.
        /// 3. Game duration (in seconds).
        /// </summary>
        public void CaptureGameInfo()
        {
            using (GameInfo form = new GameInfo())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (NewGameEvent != null)
                    {
                        NewGameEvent(form.Domain, form.Nickname, form.Duration);
                    }
                }
            }
        }

        /// <summary>
        /// Shows the randomly chosen letters in the corresponding labels.
        /// </summary>
        public void ShowLetters(LinkedList<string> list)
        {
            LinkedListNode<string> node = list.First;
            LinkedListNode<object> label = letterLabels.First;
            ((Label)label.Value).Text = node.Value;
            for (int j = 2; j <= 16; j++)
            {
                node = node.Next;
                label = label.Next;
                ((Label)label.Value).Text = node.Value;
            }
        }

        /// <summary>
        /// Handles the Enter button press event (also triggers if the Enter key is pressed).
        /// This launches when a word has been entered by the player.
        /// </summary>
        private void enterWord_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && EnterWordEvent != null)
            {
                EnterWordEvent(textBox1.Text);
            }
        }

        /// <summary>
        /// Handles the Quit button press event (also cancels a game creation event).
        /// </summary>
        private void quitGame_Click(object sender, EventArgs e)
        {
            if (QuitGameEvent != null)
            {
                QuitGameEvent();
            }
        }
    }
}
