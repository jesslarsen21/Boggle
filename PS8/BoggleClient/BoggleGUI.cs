using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BoggleClient
{
    public partial class BoggleGUI : Form, IBoggleView
    {
        // TODO Trigger each action in a new task, so that the GUI can remain responsive.

        // Contains an iterable list of the boggle letter labels.
        private LinkedList<object> letterLabels;
        delegate void SetTextCallback(string text);
        delegate void SetLabelsCallback(LinkedList<string> text);

        public event Action<string, string, int> NewGameEvent;
        public event Action<string> EnterWordEvent;
        public event Action QuitGameEvent;

        public BoggleGUI()
        {
            InitializeComponent();
            new Controller(this);
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
            CaptureGameInfo();
        }

        /// <summary>
        /// Displays a message dialog box.
        /// </summary>
        public string Message
        {
            set
            {
                MessageBox.Show(value, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Displays a warning dialog box.
        /// </summary>
        public DialogResult Warning(string message)
        {
            return MessageBox.Show(message, "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Displays a warning dialog box.
        /// </summary>
        public DialogResult Error(string message)
        {
            return MessageBox.Show(message, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Allows access to the time label for the controller class.
        /// </summary>
        public void SetTime(string text)
        {
            if (timeLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetTime);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                timeLabel.Text = text;
            }
        }

        /// <summary>
        /// Allows access to the score label for the controller class.
        /// </summary>
        public void SetScore(string text)
        {
            if (scoreLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetScore);
                this.Invoke(d, text);
            }
            else
            {
                scoreLabel.Text = text;
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
            if (label1.InvokeRequired)
            {
                SetLabelsCallback d = new SetLabelsCallback(ShowLetters);
                this.Invoke(d, list);
            }
            else
            {
                LinkedListNode<string> node = list.First;
                label1.Text = node.Value;
                node = node.Next;
                label2.Text = node.Value;
                node = node.Next;
                label3.Text = node.Value;
                node = node.Next;
                label4.Text = node.Value;
                node = node.Next;
                label5.Text = node.Value;
                node = node.Next;
                label6.Text = node.Value;
                node = node.Next;
                label7.Text = node.Value;
                node = node.Next;
                label8.Text = node.Value;
                node = node.Next;
                label9.Text = node.Value;
                node = node.Next;
                label10.Text = node.Value;
                node = node.Next;
                label11.Text = node.Value;
                node = node.Next;
                label12.Text = node.Value;
                node = node.Next;
                label13.Text = node.Value;
                node = node.Next;
                label14.Text = node.Value;
                node = node.Next;
                label15.Text = node.Value;
                node = node.Next;
                label16.Text = node.Value;
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
                textBox1.Text = "";
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

        private void helpButton_Click(object sender, EventArgs e)
        {
            string str = "Controls:\n"
                + "To enter a word, type it into the textbox and press the Enter key or click the Enter button.\n"
                + "To quit the current game, click the Quit button.\n"
                + "To close the window, click the OS-designated close button.\n\n"
                + "How to Play:\n"
                + "For more information, check out http://en.wikipedia.org/wiki/Boggle";
            MessageBox.Show(str, "Information.", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
