namespace IP_Bulk_Lookup
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Форма, показываемая при запуске приложения на неподходящей (слишком старой) версии
    /// .NET Desktop Runtime. Выводит список требуемых компонентов и ссылку на скачивание.
    /// Форма собирается программно (без .Designer.cs), чтобы не зависеть от конструктора форм.
    /// </summary>
    internal sealed class RequirementsForm : Form
    {
        /// <summary>Страница загрузки .NET Desktop Runtime.</summary>
        private const string DownloadUrl = "https://dotnet.microsoft.com/download/dotnet";

        /// <summary>Страница релизов (версия с зависимостями внутри).</summary>
        private const string ReleaseUrl = "https://github.com/sergiomarotco/IP-Bulk-lookup/releases";

        /// <summary>Требуемая мажорная версия .NET Desktop Runtime.</summary>
        private readonly int requiredMajor;

        /// <summary>Фактическая версия запущенного рантайма.</summary>
        private readonly Version currentVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsForm"/> class.
        /// </summary>
        /// <param name="requiredMajor">Требуемая мажорная версия .NET Desktop Runtime.</param>
        /// <param name="currentVersion">Фактическая версия запущенного рантайма.</param>
        public RequirementsForm(int requiredMajor, Version currentVersion)
        {
            this.requiredMajor = requiredMajor;
            this.currentVersion = currentVersion;
            this.BuildUi();
        }

        /// <summary>
        /// Формирует элементы управления формы.
        /// </summary>
        private void BuildUi()
        {
            this.Text = "Требуемые компоненты";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;
            this.ClientSize = new Size(520, 400);
            try
            {
                this.Icon = Properties.Resources.Icon;
            }
            catch
            {
                // Если иконка недоступна - показываем форму без неё.
            }

            var headerLabel = new Label
            {
                AutoSize = false,
                Location = new Point(16, 14),
                Size = new Size(488, 56),
                Text = "Для запуска приложения требуется установленный .NET Desktop Runtime версии " +
                       this.requiredMajor + ".0 или новее.\n" +
                       "Текущая версия рантайма: " + this.currentVersion + "\n" +
                       "Установите требуемые компоненты из списка ниже:",
            };

            var listBox = new ListBox
            {
                Location = new Point(16, 84),
                Size = new Size(488, 150),
                IntegralHeight = false,
                Font = new Font(this.Font.FontFamily, 9.75f),
            };
            listBox.Items.Add("• Microsoft .NET Desktop Runtime " + this.requiredMajor + ".0 (x64)");
            listBox.Items.Add("• Windows 10 (версия 1607 или новее) либо Windows 11");
            listBox.Items.Add("   .NET Desktop Runtime " + this.requiredMajor + ".0 не поддерживается на Windows Server 2019 / Windows 8.1 и старше.");

            var downloadLink = new LinkLabel
            {
                AutoSize = false,
                Location = new Point(16, 248),
                Size = new Size(488, 23),
                Text = "Скачать .NET Desktop Runtime: " + DownloadUrl,
            };
            downloadLink.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(DownloadUrl) { UseShellExecute = true });
                }
                catch
                {
                    // Игнорируем ошибку открытия браузера.
                }
            };

            var releaseLink = new LinkLabel
            {
                AutoSize = false,
                Location = new Point(16, 284),
                Size = new Size(488, 40),
                Text = "Версия с зависимостями внутри: " + ReleaseUrl,
            };
            releaseLink.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(ReleaseUrl) { UseShellExecute = true });
                }
                catch
                {
                    // Игнорируем ошибку открытия браузера.
                }
            };

            var closeButton = new Button
            {
                Text = "Закрыть",
                Location = new Point(419, 340),
                Size = new Size(85, 28),
                DialogResult = DialogResult.OK,
            };

            this.Controls.Add(headerLabel);
            this.Controls.Add(listBox);
            this.Controls.Add(downloadLink);
            this.Controls.Add(releaseLink);
            this.Controls.Add(closeButton);
            this.AcceptButton = closeButton;
        }
    }
}
