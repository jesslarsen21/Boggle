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
        public LoadingGame()
        {
            InitializeComponent();
        }

        public string InfoText
        {
            get { return infoLabel.Text; }
            set { infoLabel.Text = value; }
        }

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

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;
        }
    }
}
