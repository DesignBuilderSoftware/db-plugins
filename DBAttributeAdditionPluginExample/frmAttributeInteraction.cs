using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Attribute_Addition_Plugin;
using DB.Extensibility.Contracts;
using DB.Api;

namespace Attribute_Addition_Plugin
{
    public partial class frmAttributeInteraction : Form
    {
        public frmAttributeInteraction()
        {
            InitializeComponent();
        }

        public ExampleAddAttributePlugin RootPlugin;
        public Site ModelSite;
        public string AttributeName;

        public void ShowMe(ref Site referenceSite, string referenceName)
        {
            ModelSite = referenceSite;
            AttributeName = referenceName;
            ShowDialog();
        }

        private void frmAttributeInteraction_Load(object sender, EventArgs e)
        {
            RootPlugin = new ExampleAddAttributePlugin();
            lblExplain.Text = "This dialog interacts with "+ AttributeName + ", allowing you to retrieve and set the attribute's value";
            lblMessage.Text = "Please click a button";
        }

        private void btnGet_Click(object sender, EventArgs e)
        {
            
            lblMessage.Text = "Current Value = " + ModelSite.GetAttribute(AttributeName);
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            ModelSite.SetAttribute(AttributeName, txtNewValue.Text);
            lblMessage.Text = "Attribute updated";
        }
    }
}
