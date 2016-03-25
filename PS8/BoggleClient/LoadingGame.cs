using System;
using System.Windows.Forms;

namespace BoggleClient
{
    /// <summary>
    /// A custom window that can be remotely closed for allowing the user to cancel
    /// the game loading process.
    /// </summary>
    public partial class LoadingGame : Form
    {
        /// <summary>
        /// Instantiates a new LoadingGame window.
        /// </summary>
        public LoadingGame()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Allows access to the informative text label in the window.
        /// </summary>
        public string InfoText
        {
            get { return infoLabel.Text; }
            set { infoLabel.Text = value; }
        }

        /// <summary>
        /// Allows the Controller to remotely close this window.
        /// </summary>
        public void DoClose()
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                  {
                      Close();
                  });
            }
        }

        /// <summary>
        /// Handles the Abort button click event.
        /// </summary>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;
        }
    }
}
