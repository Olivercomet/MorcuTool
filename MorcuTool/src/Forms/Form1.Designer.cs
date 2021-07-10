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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.savePackageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.packageRootContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exportAllContextMenuStripButton = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.findByHashButton = new System.Windows.Forms.Button();
            this.findByHashTextBox = new System.Windows.Forms.TextBox();
            this.vaultSearchButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.vaultSearchTextBox = new System.Windows.Forms.TextBox();
            this.hashLabel = new System.Windows.Forms.Label();
            this.backtrackToModel = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.subfileContextMenu.SuspendLayout();
            this.packageRootContextMenu.SuspendLayout();
            this.SuspendLayout();
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
            this.savePackageToolStripMenuItem,
            this.convertModelToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(46, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            this.openToolStripMenuItem.Text = "Open Package";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // savePackageToolStripMenuItem
            // 
            this.savePackageToolStripMenuItem.Name = "savePackageToolStripMenuItem";
            this.savePackageToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            this.savePackageToolStripMenuItem.Text = "Save Package";
            this.savePackageToolStripMenuItem.Click += new System.EventHandler(this.savePackageToolStripMenuItem_Click);
            // 
            // convertModelToolStripMenuItem
            // 
            this.convertModelToolStripMenuItem.Name = "convertModelToolStripMenuItem";
            this.convertModelToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
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
            this.exportSubfile,
            this.backtrackToModel});
            this.subfileContextMenu.Name = "subfileContextMenu";
            this.subfileContextMenu.Size = new System.Drawing.Size(211, 80);
            // 
            // exportSubfile
            // 
            this.exportSubfile.Name = "exportSubfile";
            this.exportSubfile.Size = new System.Drawing.Size(210, 24);
            this.exportSubfile.Text = "Export";
            this.exportSubfile.Click += new System.EventHandler(this.exportSubfile_Click);
            // 
            // packageRootContextMenu
            // 
            this.packageRootContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.packageRootContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportAllContextMenuStripButton});
            this.packageRootContextMenu.Name = "subfileContextMenu";
            this.packageRootContextMenu.Size = new System.Drawing.Size(144, 28);
            // 
            // exportAllContextMenuStripButton
            // 
            this.exportAllContextMenuStripButton.Name = "exportAllContextMenuStripButton";
            this.exportAllContextMenuStripButton.Size = new System.Drawing.Size(143, 24);
            this.exportAllContextMenuStripButton.Text = "Export All";
            this.exportAllContextMenuStripButton.Click += new System.EventHandler(this.exportAllContextMenuStripButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(650, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(193, 17);
            this.label1.TabIndex = 14;
            this.label1.Text = "Find file by hexadecimal hash";
            // 
            // findByHashButton
            // 
            this.findByHashButton.Location = new System.Drawing.Point(701, 89);
            this.findByHashButton.Name = "findByHashButton";
            this.findByHashButton.Size = new System.Drawing.Size(75, 23);
            this.findByHashButton.TabIndex = 15;
            this.findByHashButton.Text = "Find";
            this.findByHashButton.UseVisualStyleBackColor = true;
            this.findByHashButton.Click += new System.EventHandler(this.findByHashButton_Click);
            // 
            // findByHashTextBox
            // 
            this.findByHashTextBox.Location = new System.Drawing.Point(653, 61);
            this.findByHashTextBox.Name = "findByHashTextBox";
            this.findByHashTextBox.Size = new System.Drawing.Size(190, 22);
            this.findByHashTextBox.TabIndex = 13;
            // 
            // vaultSearchButton
            // 
            this.vaultSearchButton.Location = new System.Drawing.Point(701, 177);
            this.vaultSearchButton.Name = "vaultSearchButton";
            this.vaultSearchButton.Size = new System.Drawing.Size(75, 23);
            this.vaultSearchButton.TabIndex = 18;
            this.vaultSearchButton.Text = "Find";
            this.vaultSearchButton.UseVisualStyleBackColor = true;
            this.vaultSearchButton.Click += new System.EventHandler(this.vaultSearchButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(644, 129);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(211, 17);
            this.label2.TabIndex = 17;
            this.label2.Text = "Find vault entry by decimal hash";
            // 
            // vaultSearchTextBox
            // 
            this.vaultSearchTextBox.Location = new System.Drawing.Point(653, 149);
            this.vaultSearchTextBox.Name = "vaultSearchTextBox";
            this.vaultSearchTextBox.Size = new System.Drawing.Size(190, 22);
            this.vaultSearchTextBox.TabIndex = 16;
            // 
            // hashLabel
            // 
            this.hashLabel.AutoSize = true;
            this.hashLabel.Location = new System.Drawing.Point(650, 248);
            this.hashLabel.Name = "hashLabel";
            this.hashLabel.Size = new System.Drawing.Size(49, 17);
            this.hashLabel.TabIndex = 19;
            this.hashLabel.Text = "Hash: ";
            this.hashLabel.Click += new System.EventHandler(this.hashLabel_Click);
            // 
            // backtrackToModel
            // 
            this.backtrackToModel.Name = "backtrackToModel";
            this.backtrackToModel.Size = new System.Drawing.Size(210, 24);
            this.backtrackToModel.Text = "Backtrack to model";
            this.backtrackToModel.Click += new System.EventHandler(this.backtrackToModel_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(855, 474);
            this.Controls.Add(this.hashLabel);
            this.Controls.Add(this.vaultSearchButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.vaultSearchTextBox);
            this.Controls.Add(this.findByHashButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.findByHashTextBox);
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
            this.packageRootContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
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
        private System.Windows.Forms.ToolStripMenuItem savePackageToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip packageRootContextMenu;
        private System.Windows.Forms.ToolStripMenuItem exportAllContextMenuStripButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button findByHashButton;
        private System.Windows.Forms.TextBox findByHashTextBox;
        private System.Windows.Forms.Button vaultSearchButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox vaultSearchTextBox;
        private System.Windows.Forms.Label hashLabel;
        private System.Windows.Forms.ToolStripMenuItem backtrackToModel;
    }
}

