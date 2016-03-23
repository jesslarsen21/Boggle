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

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Abort;
        }
    }
}
