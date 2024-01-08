using System;
using System.Windows.Forms;

namespace DBReportPluginWithDialog
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        public bool ShowMe(ref bool includeGains, ref bool includeAirflow, ref bool includePolygon)
        {

            bool result = ShowDialog() == DialogResult.OK;

            if (result)
            {
                includeGains = cbGains.Checked;
                includeAirflow = cbAirflow.Checked;
                includePolygon = cbPolygons.Checked;
            }

            return result;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void cbPolygons_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
