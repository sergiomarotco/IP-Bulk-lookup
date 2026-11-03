using System;
using System.IO;
using System.Windows.Forms;

namespace IP_Bulk_Lookup
{
    /// <summary>
    /// Основной класс.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); // корректный рендеринг на мониторах с высоким DPI (Windows 10/11)
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString());
                File.AppendAllText("EventLog.txt", "Program.cs Общая ошибка: " + ee.ToString() + Environment.NewLine);
            }
        }
    }
}
