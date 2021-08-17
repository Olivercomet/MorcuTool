using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MorcuTool
{
    public partial class MSGtexteditor : Form
    {
        public MSGtexteditor()
        {
            InitializeComponent();
        }

        public MSG activeMSG;

        private void MSGtexteditor_Load(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1) {
                richTextBox1.Text = activeMSG.strings[listBox1.SelectedIndex];
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "MySims Agents DS message files (*.msg)|*.msg";

            if (openFileDialog1.ShowDialog() == DialogResult.OK) {

                activeMSG = new MSG(File.ReadAllBytes(openFileDialog1.FileName));
                LoadListBox();
            }
        }

        public void LoadListBox() {

            listBox1.Items.Clear();

            foreach (string s in activeMSG.strings) {
                listBox1.Items.Add(s);
            }

            listBox1.SelectedIndex = 0;
        }
    }
}
