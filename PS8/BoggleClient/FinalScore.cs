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
    public partial class FinalScore : Form
    {
        public FinalScore()
        {
            InitializeComponent();
        }

        public string Player1Name
        {
            set { player1Name.Text = value; }
        }

        public string Player2Name
        {
            set { player2Name.Text = value; }
        }

        public string Player1Words
        {
            set { player1Words.Text = value; }
        }

        public string Player2Words
        {
            set { player2Words.Text = value; }
        }

        private void anotherGame_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void quitAll_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
