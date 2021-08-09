namespace MorcuTool
{
    partial class SavePackageForm
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
            this.MSKradiobutton = new System.Windows.Forms.RadioButton();
            this.MSAradiobutton = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.CreatePackageButton = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // MSKradiobutton
            // 
            this.MSKradiobutton.AutoSize = true;
            this.MSKradiobutton.Location = new System.Drawing.Point(16, 34);
            this.MSKradiobutton.Name = "MSKradiobutton";
            this.MSKradiobutton.Size = new System.Drawing.Size(214, 21);
            this.MSKradiobutton.TabIndex = 0;
            this.MSKradiobutton.TabStop = true;
            this.MSKradiobutton.Text = "2 (MySims, MySims Kingdom)";
            this.MSKradiobutton.UseVisualStyleBackColor = true;
            // 
            // MSAradiobutton
            // 
            this.MSAradiobutton.AutoSize = true;
            this.MSAradiobutton.Location = new System.Drawing.Point(16, 61);
            this.MSAradiobutton.Name = "MSAradiobutton";
            this.MSAradiobutton.Size = new System.Drawing.Size(147, 21);
            this.MSAradiobutton.TabIndex = 1;
            this.MSAradiobutton.TabStop = true;
            this.MSAradiobutton.Text = "3 (MySims Agents)";
            this.MSAradiobutton.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.MSKradiobutton);
            this.groupBox1.Controls.Add(this.MSAradiobutton);
            this.groupBox1.Location = new System.Drawing.Point(13, 53);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(260, 98);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Package version";
            // 
            // CreatePackageButton
            // 
            this.CreatePackageButton.Location = new System.Drawing.Point(62, 268);
            this.CreatePackageButton.Name = "CreatePackageButton";
            this.CreatePackageButton.Size = new System.Drawing.Size(143, 36);
            this.CreatePackageButton.TabIndex = 3;
            this.CreatePackageButton.Text = "Create Package";
            this.CreatePackageButton.UseVisualStyleBackColor = true;
            this.CreatePackageButton.Click += new System.EventHandler(this.CreatePackageButton_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButton3);
            this.groupBox2.Controls.Add(this.radioButton4);
            this.groupBox2.Location = new System.Drawing.Point(12, 157);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(260, 98);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Index version";
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Location = new System.Drawing.Point(16, 34);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(214, 21);
            this.radioButton3.TabIndex = 0;
            this.radioButton3.TabStop = true;
            this.radioButton3.Text = "2 (MySims, MySims Kingdom)";
            this.radioButton3.UseVisualStyleBackColor = true;
            // 
            // radioButton4
            // 
            this.radioButton4.AutoSize = true;
            this.radioButton4.Location = new System.Drawing.Point(16, 61);
            this.radioButton4.Name = "radioButton4";
            this.radioButton4.Size = new System.Drawing.Size(147, 21);
            this.radioButton4.TabIndex = 1;
            this.radioButton4.TabStop = true;
            this.radioButton4.Text = "3 (MySims Agents)";
            this.radioButton4.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(216, 17);
            this.label1.TabIndex = 5;
            this.label1.Text = "If unsure, leave as default values";
            // 
            // SavePackageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(285, 320);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.CreatePackageButton);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "SavePackageForm";
            this.Text = "Package Options";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton MSKradiobutton;
        private System.Windows.Forms.RadioButton MSAradiobutton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button CreatePackageButton;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton4;
        private System.Windows.Forms.Label label1;
    }
}