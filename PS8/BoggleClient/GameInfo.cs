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
    public partial class GameInfo : Form
    {
        public GameInfo()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Allows public read access to the domain text box.
        /// </summary>
        public string Domain
        {
            get { return domainTextBox.Text; }
        }

        /// <summary>
        /// Allows public read access to the nickname text box.
        /// </summary>
        public string Nickname
        {
            get { return nameTextBox.Text; }
        }

        /// <summary>
        /// Allows public read access to the game duration text box.
        /// </summary>
        public int Duration
        {
            get { return int.Parse(durationTextBox.Text); }
        }
    }
}
