using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MorcuTool
{
    public partial class SavePackageForm : Form
    {
        public SavePackageForm()
        {
            InitializeComponent();

            switch (global.activePackage.packageType)
                {
                case Package.PackageType.Kingdom:
                    MSKradiobutton.Checked = true;
                    break;

                case Package.PackageType.Agents:
                    MSAradiobutton.Checked = true;
                    break;
                }
        }

        private void CreatePackageButton_Click(object sender, EventArgs e)
        {
            if (MSKradiobutton.Checked)
                {
                global.activePackage.packageType = Package.PackageType.Kingdom;
                }
            else if (MSAradiobutton.Checked)
                {
                global.activePackage.packageType = Package.PackageType.Agents;
                }

            global.activePackage.RebuildPackage();
        }
    }
}
