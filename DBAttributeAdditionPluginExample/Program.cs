using System;
using System.Windows.Forms;
using Attribute_Addition_Plugin;

namespace Attribute_Addition_Plugin
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmAttributeInteraction());
        }
    }
}
