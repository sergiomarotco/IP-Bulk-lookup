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
        /// Требуемая мажорная версия .NET Desktop Runtime (соответствует TargetFramework net10.0).
        /// </summary>
        private const int RequiredNetMajor = 10;

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

                // Проверка версии запущенного .NET Desktop Runtime. Если приложение сумело
                // запуститься (например, на более мажорном рантайме через roll-forward), но
                // версия ниже требуемой - показываем форму со списком требуемых компонентов
                // вместо запуска основного окна.
                if (!CheckNetRuntime())
                {
                    Application.Run(new RequirementsForm(RequiredNetMajor, Environment.Version));
                    return;
                }

                Application.Run(new Form1());
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString());
                File.AppendAllText("EventLog.txt", "Program.cs Общая ошибка: " + ee.ToString() + Environment.NewLine);
            }
        }

        /// <summary>
        /// Проверяет, что версия запущенного .NET Desktop Runtime не ниже требуемой.
        /// </summary>
        /// <returns>True, если установленная версия подходящая; иначе false.</returns>
        private static bool CheckNetRuntime()
        {
            try
            {
                return Environment.Version.Major >= RequiredNetMajor;
            }
            catch
            {
                // Если проверить версию не удалось, считаем окружение подходящим и
                // продолжаем обычный запуск, чтобы не блокировать работу приложения.
                return true;
            }
        }
    }
}
