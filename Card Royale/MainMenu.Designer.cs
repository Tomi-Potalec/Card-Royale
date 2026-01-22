using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Card_Royale
{
    partial class MainMenu
    {
        private System.ComponentModel.IContainer components = null;
        private Panel panelMainMenu;
        private Panel scrollPanelGames;
        private Panel panelSolitaire;
        private Button btnExit;

        // Game buttons
        private Button btnSolitaire;
        private Button btnSpider;
        private Button btnKlondike;
        private Button btnFreeCell;
        private Button btnPyramid;
        private Button btnHearts;

        

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainMenu));
            this.panelMainMenu = new System.Windows.Forms.Panel();
            this.scrollPanelGames = new System.Windows.Forms.Panel();
            this.selectorBar = new System.Windows.Forms.Panel();
            this.btnSolitaire = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.panelSolitaire = new System.Windows.Forms.Panel();
            this.btnSpider = new System.Windows.Forms.Button();
            this.btnKlondike = new System.Windows.Forms.Button();
            this.btnFreeCell = new System.Windows.Forms.Button();
            this.btnPyramid = new System.Windows.Forms.Button();
            this.btnHearts = new System.Windows.Forms.Button();
            this.panelMainMenu.SuspendLayout();
            this.scrollPanelGames.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelMainMenu
            // 
            this.panelMainMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(81)))), ((int)(((byte)(44)))));
            this.panelMainMenu.Controls.Add(this.scrollPanelGames);
            this.panelMainMenu.Controls.Add(this.btnExit);
            this.panelMainMenu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMainMenu.Location = new System.Drawing.Point(0, 0);
            this.panelMainMenu.Name = "panelMainMenu";
            this.panelMainMenu.Size = new System.Drawing.Size(835, 805);
            this.panelMainMenu.TabIndex = 1;
            // 
            // scrollPanelGames
            // 
            this.scrollPanelGames.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scrollPanelGames.AutoScroll = true;
            this.scrollPanelGames.BackColor = System.Drawing.Color.Transparent;
            this.scrollPanelGames.Controls.Add(this.selectorBar);
            this.scrollPanelGames.Location = new System.Drawing.Point(0, 0);
            this.scrollPanelGames.Name = "scrollPanelGames";
            this.scrollPanelGames.Size = new System.Drawing.Size(1470, 200);
            this.scrollPanelGames.TabIndex = 0;
            // 
            // selectorBar
            // 
            this.selectorBar.BackColor = System.Drawing.Color.LightGreen;
            this.selectorBar.Location = new System.Drawing.Point(0, 23);
            this.selectorBar.Name = "selectorBar";
            this.selectorBar.Size = new System.Drawing.Size(150, 6);
            this.selectorBar.TabIndex = 0;
            // 
            // btnSolitaire
            // 
            this.btnSolitaire.Location = new System.Drawing.Point(0, 0);
            this.btnSolitaire.Name = "btnSolitaire";
            this.btnSolitaire.Size = new System.Drawing.Size(75, 23);
            this.btnSolitaire.TabIndex = 0;
            // 
            // btnExit
            // 
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.Location = new System.Drawing.Point(1335, 1445);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(120, 50);
            this.btnExit.TabIndex = 1;
            this.btnExit.UseVisualStyleBackColor = false;
            // 
            // panelSolitaire
            // 
            this.panelSolitaire.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(81)))), ((int)(((byte)(44)))));
            this.panelSolitaire.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSolitaire.Location = new System.Drawing.Point(0, 0);
            this.panelSolitaire.Name = "panelSolitaire";
            this.panelSolitaire.Size = new System.Drawing.Size(835, 805);
            this.panelSolitaire.TabIndex = 0;
            // 
            // btnSpider
            // 
            this.btnSpider.Location = new System.Drawing.Point(0, 0);
            this.btnSpider.Name = "btnSpider";
            this.btnSpider.Size = new System.Drawing.Size(75, 23);
            this.btnSpider.TabIndex = 0;
            // 
            // btnKlondike
            // 
            this.btnKlondike.Location = new System.Drawing.Point(0, 0);
            this.btnKlondike.Name = "btnKlondike";
            this.btnKlondike.Size = new System.Drawing.Size(75, 23);
            this.btnKlondike.TabIndex = 0;
            // 
            // btnFreeCell
            // 
            this.btnFreeCell.Location = new System.Drawing.Point(0, 0);
            this.btnFreeCell.Name = "btnFreeCell";
            this.btnFreeCell.Size = new System.Drawing.Size(75, 23);
            this.btnFreeCell.TabIndex = 0;
            // 
            // btnPyramid
            // 
            this.btnPyramid.Location = new System.Drawing.Point(0, 0);
            this.btnPyramid.Name = "btnPyramid";
            this.btnPyramid.Size = new System.Drawing.Size(75, 23);
            this.btnPyramid.TabIndex = 0;
            // 
            // btnHearts
            // 
            this.btnHearts.Location = new System.Drawing.Point(0, 0);
            this.btnHearts.Name = "btnHearts";
            this.btnHearts.Size = new System.Drawing.Size(75, 23);
            this.btnHearts.TabIndex = 0;
            // 
            // MainMenu
            // 
            this.ClientSize = new System.Drawing.Size(835, 805);
            this.Controls.Add(this.panelSolitaire);
            this.Controls.Add(this.panelMainMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainMenu";
            this.Text = "Card Royale";
            this.Load += new System.EventHandler(this.MainMenu_Load);
            this.panelMainMenu.ResumeLayout(false);
            this.scrollPanelGames.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        


    }
}
