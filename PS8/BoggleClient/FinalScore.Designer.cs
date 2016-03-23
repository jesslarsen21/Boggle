namespace BoggleClient
{
    partial class FinalScore
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.player1Name = new System.Windows.Forms.Label();
            this.player2Name = new System.Windows.Forms.Label();
            this.player1Words = new System.Windows.Forms.Label();
            this.player2Words = new System.Windows.Forms.Label();
            this.anotherGame = new System.Windows.Forms.Button();
            this.quitAll = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.player1Name, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.player2Name, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.player1Words, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.player2Words, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.anotherGame, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.quitAll, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(382, 553);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // player1Name
            // 
            this.player1Name.AutoSize = true;
            this.player1Name.Dock = System.Windows.Forms.DockStyle.Fill;
            this.player1Name.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.player1Name.Location = new System.Drawing.Point(3, 0);
            this.player1Name.Name = "player1Name";
            this.player1Name.Size = new System.Drawing.Size(185, 40);
            this.player1Name.TabIndex = 0;
            this.player1Name.Text = "label1";
            this.player1Name.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // player2Name
            // 
            this.player2Name.AutoSize = true;
            this.player2Name.Dock = System.Windows.Forms.DockStyle.Fill;
            this.player2Name.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.player2Name.Location = new System.Drawing.Point(194, 0);
            this.player2Name.Name = "player2Name";
            this.player2Name.Size = new System.Drawing.Size(185, 40);
            this.player2Name.TabIndex = 1;
            this.player2Name.Text = "label2";
            this.player2Name.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // player1Words
            // 
            this.player1Words.AutoSize = true;
            this.player1Words.Dock = System.Windows.Forms.DockStyle.Fill;
            this.player1Words.Location = new System.Drawing.Point(3, 40);
            this.player1Words.Name = "player1Words";
            this.player1Words.Size = new System.Drawing.Size(185, 473);
            this.player1Words.TabIndex = 2;
            this.player1Words.Text = "label1";
            // 
            // player2Words
            // 
            this.player2Words.AutoSize = true;
            this.player2Words.Dock = System.Windows.Forms.DockStyle.Fill;
            this.player2Words.Location = new System.Drawing.Point(194, 40);
            this.player2Words.Name = "player2Words";
            this.player2Words.Size = new System.Drawing.Size(185, 473);
            this.player2Words.TabIndex = 3;
            this.player2Words.Text = "label2";
            // 
            // anotherGame
            // 
            this.anotherGame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.anotherGame.Location = new System.Drawing.Point(3, 516);
            this.anotherGame.Name = "anotherGame";
            this.anotherGame.Size = new System.Drawing.Size(185, 34);
            this.anotherGame.TabIndex = 4;
            this.anotherGame.Text = "Another Game";
            this.anotherGame.UseVisualStyleBackColor = true;
            this.anotherGame.Click += new System.EventHandler(this.anotherGame_Click);
            // 
            // quitAll
            // 
            this.quitAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.quitAll.Location = new System.Drawing.Point(194, 516);
            this.quitAll.Name = "quitAll";
            this.quitAll.Size = new System.Drawing.Size(185, 34);
            this.quitAll.TabIndex = 5;
            this.quitAll.Text = "Quit";
            this.quitAll.UseVisualStyleBackColor = true;
            this.quitAll.Click += new System.EventHandler(this.quitAll_Click);
            // 
            // FinalScore
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(382, 553);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "FinalScore";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FinalScore";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label player1Name;
        private System.Windows.Forms.Label player2Name;
        private System.Windows.Forms.Label player1Words;
        private System.Windows.Forms.Label player2Words;
        private System.Windows.Forms.Button anotherGame;
        private System.Windows.Forms.Button quitAll;
    }
}