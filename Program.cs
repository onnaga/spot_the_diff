using System;
using System.Windows.Forms;
using WinFormsApp_deff_Game;

namespace SpotTheDifferenceGUI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
