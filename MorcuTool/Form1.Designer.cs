namespace MorcuTool
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.isMSK = new System.Windows.Forms.RadioButton();
            this.isMSA = new System.Windows.Forms.RadioButton();
            this.isSkyHeroes = new System.Windows.Forms.RadioButton();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.convertModelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.utilityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.simsTPLToTPLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tPLToMSATPLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadVaultToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compressionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.decompressQFSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.morcubusModeBox = new System.Windows.Forms.CheckBox();
            this.FileTree = new System.Windows.Forms.TreeView();
            this.subfileContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exportSubfile = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.menuStrip1.SuspendLayout();
            this.subfileContextMenu.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // isMSK
            // 
            this.isMSK.AutoSize = true;
            this.isMSK.Location = new System.Drawing.Point(7, 22);
            this.isMSK.Margin = new System.Windows.Forms.Padding(4);
            this.isMSK.Name = "isMSK";
            this.isMSK.Size = new System.Drawing.Size(136, 21);
            this.isMSK.TabIndex = 3;
            this.isMSK.Text = "MySims Kingdom";
            this.isMSK.UseVisualStyleBackColor = true;
            // 
            // isMSA
            // 
            this.isMSA.AutoSize = true;
            this.isMSA.Checked = true;
            this.isMSA.Location = new System.Drawing.Point(7, 51);
            this.isMSA.Margin = new System.Windows.Forms.Padding(4);
            this.isMSA.Name = "isMSA";
            this.isMSA.Size = new System.Drawing.Size(125, 21);
            this.isMSA.TabIndex = 4;
            this.isMSA.TabStop = true;
            this.isMSA.Text = "MySims Agents";
            this.isMSA.UseVisualStyleBackColor = true;
            this.isMSA.CheckedChanged += new System.EventHandler(this.isMSA_CheckedChanged);
            // 
            // isSkyHeroes
            // 
            this.isSkyHeroes.AutoSize = true;
            this.isSkyHeroes.Location = new System.Drawing.Point(7, 80);
            this.isSkyHeroes.Margin = new System.Windows.Forms.Padding(4);
            this.isSkyHeroes.Name = "isSkyHeroes";
            this.isSkyHeroes.Size = new System.Drawing.Size(148, 21);
            this.isSkyHeroes.TabIndex = 6;
            this.isSkyHeroes.Text = "MySims Skyheroes";
            this.isSkyHeroes.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.utilityToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(855, 28);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.convertModelToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(46, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.openToolStripMenuItem.Text = "Open Package";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // convertModelToolStripMenuItem
            // 
            this.convertModelToolStripMenuItem.Name = "convertModelToolStripMenuItem";
            this.convertModelToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.convertModelToolStripMenuItem.Text = "Convert Model";
            this.convertModelToolStripMenuItem.Click += new System.EventHandler(this.convertModelToolStripMenuItem_Click);
            // 
            // utilityToolStripMenuItem
            // 
            this.utilityToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.simsTPLToTPLToolStripMenuItem,
            this.tPLToMSATPLToolStripMenuItem,
            this.loadVaultToolStripMenuItem,
            this.compressionToolStripMenuItem});
            this.utilityToolStripMenuItem.Name = "utilityToolStripMenuItem";
            this.utilityToolStripMenuItem.Size = new System.Drawing.Size(62, 24);
            this.utilityToolStripMenuItem.Text = "Utility";
            // 
            // simsTPLToTPLToolStripMenuItem
            // 
            this.simsTPLToTPLToolStripMenuItem.Name = "simsTPLToTPLToolStripMenuItem";
            this.simsTPLToTPLToolStripMenuItem.Size = new System.Drawing.Size(227, 26);
            this.simsTPLToTPLToolStripMenuItem.Text = "Raw Sims TPL to TPL";
            this.simsTPLToTPLToolStripMenuItem.Click += new System.EventHandler(this.simsTPLToTPLToolStripMenuItem_Click);
            // 
            // tPLToMSATPLToolStripMenuItem
            // 
            this.tPLToMSATPLToolStripMenuItem.Name = "tPLToMSATPLToolStripMenuItem";
            this.tPLToMSATPLToolStripMenuItem.Size = new System.Drawing.Size(227, 26);
            this.tPLToMSATPLToolStripMenuItem.Text = "TPL to MSA TPL";
            this.tPLToMSATPLToolStripMenuItem.Click += new System.EventHandler(this.tPLToMSATPLToolStripMenuItem_Click);
            // 
            // loadVaultToolStripMenuItem
            // 
            this.loadVaultToolStripMenuItem.Name = "loadVaultToolStripMenuItem";
            this.loadVaultToolStripMenuItem.Size = new System.Drawing.Size(227, 26);
            this.loadVaultToolStripMenuItem.Text = "Load Vault";
            this.loadVaultToolStripMenuItem.Click += new System.EventHandler(this.loadVaultToolStripMenuItem_Click);
            // 
            // compressionToolStripMenuItem
            // 
            this.compressionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.decompressQFSToolStripMenuItem});
            this.compressionToolStripMenuItem.Name = "compressionToolStripMenuItem";
            this.compressionToolStripMenuItem.Size = new System.Drawing.Size(227, 26);
            this.compressionToolStripMenuItem.Text = "Compression";
            // 
            // decompressQFSToolStripMenuItem
            // 
            this.decompressQFSToolStripMenuItem.Name = "decompressQFSToolStripMenuItem";
            this.decompressQFSToolStripMenuItem.Size = new System.Drawing.Size(204, 26);
            this.decompressQFSToolStripMenuItem.Text = "Decompress QFS";
            this.decompressQFSToolStripMenuItem.Click += new System.EventHandler(this.decompressQFSToolStripMenuItem_Click_1);
            // 
            // morcubusModeBox
            // 
            this.morcubusModeBox.AutoSize = true;
            this.morcubusModeBox.Location = new System.Drawing.Point(12, 441);
            this.morcubusModeBox.Name = "morcubusModeBox";
            this.morcubusModeBox.Size = new System.Drawing.Size(423, 21);
            this.morcubusModeBox.TabIndex = 9;
            this.morcubusModeBox.Text = "Morcubus Mode (Needed for some Skyheroes models to work)";
            this.morcubusModeBox.UseVisualStyleBackColor = true;
            // 
            // FileTree
            // 
            this.FileTree.Location = new System.Drawing.Point(13, 41);
            this.FileTree.Name = "FileTree";
            this.FileTree.Size = new System.Drawing.Size(602, 374);
            this.FileTree.TabIndex = 10;
            // 
            // subfileContextMenu
            // 
            this.subfileContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.subfileContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportSubfile});
            this.subfileContextMenu.Name = "subfileContextMenu";
            this.subfileContextMenu.Size = new System.Drawing.Size(122, 28);
            // 
            // exportSubfile
            // 
            this.exportSubfile.Name = "exportSubfile";
            this.exportSubfile.Size = new System.Drawing.Size(210, 24);
            this.exportSubfile.Text = "Export";
            this.exportSubfile.Click += new System.EventHandler(this.exportSubfile_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.isMSA);
            this.groupBox1.Controls.Add(this.isMSK);
            this.groupBox1.Controls.Add(this.isSkyHeroes);
            this.groupBox1.Location = new System.Drawing.Point(621, 351);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(222, 111);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "DEPRECATED (Do not use)";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(855, 474);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.FileTree);
            this.Controls.Add(this.morcubusModeBox);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "MorcuTool";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.subfileContextMenu.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.RadioButton isMSK;
        private System.Windows.Forms.RadioButton isMSA;
        private System.Windows.Forms.RadioButton isSkyHeroes;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem utilityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem simsTPLToTPLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tPLToMSATPLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadVaultToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem convertModelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compressionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem decompressQFSToolStripMenuItem;
        private System.Windows.Forms.CheckBox morcubusModeBox;
        private System.Windows.Forms.TreeView FileTree;
        private System.Windows.Forms.ContextMenuStrip subfileContextMenu;
        private System.Windows.Forms.ToolStripMenuItem exportSubfile;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}

