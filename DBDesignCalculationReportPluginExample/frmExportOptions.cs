using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesignReportPlugin
{
    public partial class ExportOptions : Form
    {
        public ExportOptions()
        {
            InitializeComponent();
        }
        public Boolean ShowMe(ref Boolean includeNonCoincidentBuilding, ref Boolean includeNonCoincidentCoolingSystem, ref Boolean includeCoincidentDefBuilding,
                              ref Boolean includeCoincidentDefCoolingSystem, ref Boolean includeCoincidentTotalCalcBuilding, ref Boolean includeCoincidentTotalCalcCoolingSystem)
        {
            Boolean result = ShowDialog() == DialogResult.OK;
            if (result)
            {
                includeNonCoincidentBuilding = ((cbNoncoincidentTCM.SelectedIndex == 0 || cbNoncoincidentTCM.SelectedIndex == 2) && rbtnNoncoincident.Checked) || rbAll.Checked;
                includeNonCoincidentCoolingSystem = ((cbNoncoincidentTCM.SelectedIndex == 1 || cbNoncoincidentTCM.SelectedIndex == 2) && rbtnNoncoincident.Checked) || rbAll.Checked;
                includeCoincidentDefBuilding = ((cbCoincidentCDM.SelectedIndex == 0 || cbCoincidentCDM.SelectedIndex == 2) && rbCoincident.Checked) || rbAll.Checked;
                includeCoincidentDefCoolingSystem = ((cbCoincidentCDM.SelectedIndex == 1 || cbCoincidentCDM.SelectedIndex == 2) && rbCoincident.Checked) || rbAll.Checked;
                includeCoincidentTotalCalcBuilding = ((cbCoincidentTCM.SelectedIndex == 0 || cbCoincidentTCM.SelectedIndex == 2) && rbCoincident.Checked) || rbAll.Checked;
                includeCoincidentTotalCalcCoolingSystem = ((cbCoincidentTCM.SelectedIndex == 1 || cbCoincidentTCM.SelectedIndex == 2) && rbCoincident.Checked) || rbAll.Checked;
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
    }
}
