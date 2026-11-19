using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IP_Bulk_Lookup
{
    /// <summary>
    /// Основной класс.
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// Галка (хост отвечает на ping).
        /// </summary>
        private const string PingEmojiAlive = "\u2714\uFE0E";

        /// <summary>
        /// Emoji недоступности хоста (запрещающий знак) — хост не отвечает на ping и DNS записи нет.
        /// </summary>
        private const string PingEmojiDead = "\U0001F6AB";

        /// <summary>
        /// Предупреждение — DNS запись имеется, но хост не отвечает на ping.
        /// </summary>
        private const string PingEmojiWarning = "\u26A0\uFE0E";

        /// <summary>
        /// Флаг отмены выполняемого lookup. Устанавливается пунктом меню STOP LOOKUP;
        /// фоновые задачи проверяют его перед добавлением строк в таблицу.
        /// </summary>
        private volatile bool lookupCancelled;

        /// <summary>
        /// Число активных фоновых задач lookup (проверок IP). Пока оно больше нуля,
        /// Lookup не завершён и пункт STOP LOOKUP остаётся видимым.
        /// </summary>
        private int activeLookups;

        /// <summary>
        /// True, когда основной цикл выдачи задач lookup завершён (или отменён).
        /// В сумме с обнулением <see cref="activeLookups"/> означает полный конец lookup.
        /// </summary>
        private volatile bool lookupLoopFinished = true;

        /// <summary>
        /// Переменная используемая при сортировке.
        /// </summary>
        private ListViewColumnSorter lvwColumnSorter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }

        private delegate void InsertIntoListDelegate(ListViewItem item);

        /// <summary>
        /// Удаляет одинаковые элементы в listView.
        /// </summary>
        /// <param name="listView111">listView в котором удаляются дубли.</param>
        public void RemoveDuplicates_In_ListView(ListView listView111)
        {
            try
            {
                if (listView111 == null)
                {
                    throw new ArgumentNullException(nameof(listView111));
                }

                var tags = new HashSet<string>();
                var duplicates = new List<ListViewItem>();
                foreach (ListViewItem item in listView111.Items)
                {
                    if (tags.Add(item.SubItems[1].Text))
                    { // HashSet.Add() returns false if it already contains the key
                        continue;
                    }

                    duplicates.Add(item);
                }

                foreach (ListViewItem item in duplicates)
                    item.Remove();
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString()); // File.AppendAllText("EventLog.txt", "GetHostEntryAsync. Общая ошибка: " + ee.ToString() + Environment.NewLine);
            }
        }

        /// <summary>
        /// Добавляет элемент в listView2.
        /// </summary>
        /// <param name="item">Добавляемый элемент в listView2.</param>
        public void InsertIntoList(ListViewItem item)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new InsertIntoListDelegate(InsertIntoList), item);
                }
                else
                {
                    listView2.Items.Add(item);
                    AutoFitIpColumn();
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString()); /* File.AppendAllText("EventLog.txt", "InsertIntoList. Общая ошибка: " + ee.ToString() + Environment.NewLine);*/
            }
        }

        /// <summary>
        /// Отвечает ли хост на ping.
        /// </summary>
        /// <param name="ip">IP-адрес хоста.</param>
        /// <returns>True если получен ответ, false если нет.</returns>
        private static bool PingHost(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(ip, 1000);
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch
            { /* File.AppendAllText("EventLog.txt", "PingHost. Общая ошибка: " + ip + " " + ip + Environment.NewLine); */
                return false;
            }
        }

        /// <summary>
        /// Создает строку listView: [колонка статуса ping, IP, имя] со статусом ping.
        /// </summary>
        /// <param name="ip">IP-адрес (или текст ошибки).</param>
        /// <param name="name">DNS имя.</param>
        /// <param name="alive">Результат ping: true - ✔, false - 🚫 или ⚠, null - пусто.</param>
        /// <param name="dnsResolved">True если DNS запись для адреса получена (тогда при недоступности показывается ⚠ вместо 🚫).</param>
        /// <returns>Готовый ListViewItem.</returns>
        private static ListViewItem CreatePingRow(string ip, string name, bool? alive, bool dnsResolved = false)
        {
            string status = string.Empty;
            if (alive.HasValue)
            {
                status = alive.Value ? PingEmojiAlive : (dnsResolved ? PingEmojiWarning : PingEmojiDead);
            }

            return new ListViewItem(new string[] { status, ip, name });
        }

        /// <summary>
        /// Является ли текст IP адресом.
        /// </summary>
        /// <param name="address">Текст, который потенциально является IP-адресом.</param>
        /// <returns>Является ли IP-адресом.</returns>
        private static bool CheckIp(string address)
        {
            try
            {
                string[] nums = address.Split('.');
                return nums.Length == 4 && nums.All(n => int.TryParse(n, out int useless)) && nums.Select(int.Parse).All(n => n < 255);
            }
            catch
            { /*File.AppendAllText("EventLog.txt", "CheckIp. Общая ошибка: " + address.ToString() + Environment.NewLine);*/
                return false;
            }
        }

        /// <summary>
        /// Автоширина колонки с IP адресом: подгоняется под содержимое,
        /// но не шире максимально возможного IP адреса (255.255.255.255).
        /// </summary>
        private void AutoFitIpColumn()
        {
            try
            {
                if (listView2.Columns.Count < 2)
                {
                    return;
                }

                const int Padding = 8; // запас на границы ячейки
                int maxWidth = TextRenderer.MeasureText("255.255.255.255", listView2.Font).Width + Padding;
                int contentWidth = 0;
                foreach (ListViewItem row in listView2.Items)
                {
                    int w = TextRenderer.MeasureText(row.SubItems[1].Text, listView2.Font).Width + Padding;
                    if (w > contentWidth)
                    {
                        contentWidth = w;
                    }
                }

                int width = contentWidth < maxWidth ? contentWidth : maxWidth; // не шире максимально возможного IP адреса
                if (width > 20 && listView2.Columns[1].Width != width)
                {
                    listView2.Columns[1].Width = width;
                    ListView2_Resize(this, EventArgs.Empty); // пересчитать ширину колонки NAME
                }
            }
            catch
            { /* File.AppendAllText("EventLog.txt", "AutoFitIpColumn. Общая ошибка: " + Environment.NewLine); */
            }
        }

        /// <summary>
        /// Lookup (Dns.GetHostEntry).
        /// <param name="ip">IP-адрес.</param>
        private async void GetHostEntryAsync(object ip)
        {
            string ip0 = string.Empty;
            try
            {
                // Если пользователь нажал STOP LOOKUP - не выполняем проверку и не добавляем строку.
                if (this.lookupCancelled)
                {
                    return;
                }

                string[] y = new string[2];
                bool? alive = null;
                bool dnsResolved = false;
                try
                {
                    ip0 = (string)ip;
                    char[] trimcharachters = { ' ', '\t' };
                    ip0 = ip0.Trim(trimcharachters);
                    y[0] = (string)ip;
                    try
                    {
                        if (CheckIp(ip0))
                        {
                            y[0] = ip0;
                            alive = PingHost(ip0);
                            y[1] = Dns.GetHostEntry(ip0).HostName;
                            dnsResolved = !string.IsNullOrEmpty(y[1]);
                        }
                        else
                        {
                            y[1] = "incorrect IP";
                        }
                    }
                    catch (System.Net.Sockets.SocketException ee)
                    {
                        y[1] = ee.Message;

                        // File.AppendAllText("EventLog.txt", "Dns.GetHostEntry: " + ip + " текст ошибки: " + ee.Message.ToString() + Environment.NewLine);
                    }

                    ListViewItem row = CreatePingRow(y[0], y[1], alive, dnsResolved);
                    await Task.Factory.StartNew(() => InsertIntoList(row)).ConfigureAwait(false);
                }
                catch
                { /*File.AppendAllText("EventLog.txt", "GetHostEntryAsync. Ошибка проверки IP: " + ip + Environment.NewLine); */
                }
            }
            catch
            { /*File.AppendAllText("EventLog.txt", "GetHostEntryAsync. Иная ошибка: " + ee.ToString() + Environment.NewLine);*/
            }
            finally
            {
                // Уменьшаем счётчик активных фоновых задач; при полном завершении lookup скрываем STOP LOOKUP.
                this.FinishActiveLookup();
            }
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://ru.icons8.com") { UseShellExecute = true }); // открыть рекламную ссылку
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Icon = IP_Bulk_Lookup.Properties.Resources.Icon;
            this.AllowDrop = true;
            lvwColumnSorter = new ListViewColumnSorter();
            this.listView2.ListViewItemSorter = lvwColumnSorter;
            if (listView2.Items != null && listView2.Items.Count != 0)
            {
                listView2.Items.Clear();
            }
        }

        /// <summary>
        /// Возвращает количество адресов в подсети (2^(32-mask)) для масок /20-/32.
        /// </summary>
        /// <param name="mask">Маска сети (20-32).</param>
        /// <returns>Количество IP-адресов в подсети.</returns>
        private int Get_IPs_count_for_mask(int mask)
        {
            if (mask < 20 || mask > 32)
            {
                return 0;
            }

            return 1 << (32 - mask);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                // Разрешаем операцию перетаскивания (иначе Effect = None и курсор/drop неактивны).
                e.Effect = DragDropEffects.Copy;
                if (listView2.Items != null && listView2.Items.Count != 0) listView2.Items.Clear();
                if (e.Data.GetData("FileName") is Array data1)
                {
                    if ((data1.Length == 1) && (data1.GetValue(0) is string))
                    {
                        string filename = ((string[])data1)[0];
                        try
                        {
                            if (new FileInfo(filename).Length < 3000)
                                GET_DNS_NAME(File.ReadAllText(filename));
                        }
                        catch (Exception ee)
                        {
                            MessageBox.Show(ee.ToString());
                        }
                    }
                }

                listView2.Refresh();
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString()); /*File.AppendAllText("EventLog.txt", "Form1_DragEnter. Общая ошибка: " + ee.ToString() + Environment.NewLine);*/
            }
        }

        private void ListView2_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Right:
                    {
                        // Выделяем элемент под курсором, если он не входит в текущее
                        // выделение, иначе команды копирования (CopyFromView) работают
                        // над старым выделением или над пустым списком.
                        ListViewItem itemUnderCursor = listView2.HitTest(e.Location).Item;
                        if (itemUnderCursor != null && !itemUnderCursor.Selected)
                        {
                            listView2.SelectedItems.Clear();
                            itemUnderCursor.Selected = true;
                        }

                        // Позиционируем меню относительно listView2: координаты e.X/e.Y
                        // заданы в системе координат listView2. Показ относительно формы
                        // (this) смещал меню на ширину боковой панели (sidePanel, Dock=Left),
                        // из-за чего меню появлялось не в месте клика (под панелью) и его
                        // нельзя было использовать.
                        contextMenuStrip1.Show(listView2, e.Location);
                    }

                    break;
            }
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
            try
            {
                string[] lines = new string[listView2.Items.Count];
                if (listView2.Items != null && listView2.Items.Count != 0)
                {
                    if (listView2.SelectedItems.Count != 0)
                    {
                        for (int i = 0; i < listView2.SelectedItems.Count; i++)
                            lines[i] += listView2.SelectedItems[i].SubItems[0].Text + "," + listView2.SelectedItems[i].SubItems[1].Text + "," + listView2.SelectedItems[i].SubItems[2].Text;
                    }
                    else
                    {
                        for (int i = 0; i < listView2.Items.Count; i++)
                            lines[i] += listView2.Items[i].SubItems[0].Text + "," + listView2.Items[i].SubItems[1].Text + "," + listView2.Items[i].SubItems[2].Text;
                    }

                    DateTime d = DateTime.Now;
                    string filename = "nslookup " + d.Year + "." + d.Month + "." + d.Day + " " + d.Hour + "-" + d.Minute + "-" + d.Second + ".txt";
                    File.WriteAllLines(filename, lines);
                    if (checkBox2.Checked)
                        System.Diagnostics.Process.Start("explorer.exe", "/select, " + filename);
                    if (checkBox1.Checked)
                        System.Diagnostics.Process.Start("explorer.exe", "/open, " + filename);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString()); /*File.AppendAllText("EventLog.txt", "PictureBox1_Click. Общая ошибка: " + ee.ToString() + Environment.NewLine);*/
            }
        }

        /// <summary>
        /// Скопировать текст в буфер обмена.
        /// </summary>
        /// <param name="type">0 - скопировать IP+Name, 1 - только IP, 2 - только Имя, 3 - всю строку (Status,IP,Name).</param>
        private void CopyFromView(int type)
        {
            try
            {
                if (listView2.Items != null && listView2.Items.Count != 0)
                {
                    string lines = string.Empty;
                    for (int i = 0; i < listView2.SelectedItems.Count; i++)
                    {
                        switch (type)
                        {
                            case 0:
                                // скопировать IP+Name
                                if (i != listView2.SelectedItems.Count - 1)
                                    lines += listView2.SelectedItems[i].SubItems[1].Text + "," + listView2.SelectedItems[i].SubItems[2].Text + Environment.NewLine;
                                else lines += listView2.SelectedItems[i].SubItems[1].Text + "," + listView2.SelectedItems[i].SubItems[2].Text;
                                break;
                            case 1: // скопировать IP
                                if (i != listView2.SelectedItems.Count - 1)
                                    lines += listView2.SelectedItems[i].SubItems[1].Text + Environment.NewLine;
                                else lines += listView2.SelectedItems[i].SubItems[1].Text;
                                break;
                            case 2: // скопировать Name
                                if (i != listView2.SelectedItems.Count - 1)
                                    lines += listView2.SelectedItems[i].SubItems[2].Text + Environment.NewLine;
                                else lines += listView2.SelectedItems[i].SubItems[2].Text;
                                break;
                            case 3: // скопировать всю строку (Status,IP,Name)
                                if (i != listView2.SelectedItems.Count - 1)
                                    lines += listView2.SelectedItems[i].SubItems[0].Text + "," + listView2.SelectedItems[i].SubItems[1].Text + "," + listView2.SelectedItems[i].SubItems[2].Text + Environment.NewLine;
                                else lines += listView2.SelectedItems[i].SubItems[0].Text + "," + listView2.SelectedItems[i].SubItems[1].Text + "," + listView2.SelectedItems[i].SubItems[2].Text;
                                break;
                            default: // скопировать IP+Name
                                if (i != listView2.SelectedItems.Count - 1)
                                    lines += listView2.SelectedItems[i].SubItems[1].Text + "," + listView2.SelectedItems[i].SubItems[2].Text + Environment.NewLine;
                                else lines += listView2.SelectedItems[i].SubItems[1].Text + "," + listView2.SelectedItems[i].SubItems[2].Text;
                                break;
                        }
                    }

                    Clipboard.SetText(lines);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString()); /*File.AppendAllText("EventLog.txt", "CopyFromView. Общая ошибка: " + ee.ToString() + Environment.NewLine);*/
            }
        }

        private void СкопироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyFromView(0);
        }

        /// <summary>
        /// Основная функция поиска IP в тексте и получение DNS имени.
        /// </summary>
        private async void GET_DNS_NAME(string lines)
        {
            // Начинаем новый lookup: сбрасываем флаг отмены и завершённости цикла,
            // показываем пункт STOP LOOKUP. Счётчик активных задач намеренно не обнуляется:
            // между прогонами он равен нулю, а осиротевшие задачи отменённого прогона
            // безопасно уменьшают его через Interlocked (см. CheckLookupFinished).
            this.lookupLoopFinished = false;
            this.lookupCancelled = false;
            this.stopLookupToolStripMenuItem.Visible = true;
            try
            {
                Regex domain_pattern = new Regex(@"((?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)\.){1,}(?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)))");
                MatchCollection domain_finded = domain_pattern.Matches(lines);

                for (int i = 0; i < domain_finded.Count; i++)
                {
                    if (this.lookupCancelled)
                    {
                        break;
                    }

                    string[] y = new string[2];
                    y[1] = domain_finded[i].ToString();
                    try
                    {
                        IPAddress[] addresses = Dns.GetHostAddresses(domain_finded[i].ToString());

                        foreach (IPAddress address in addresses)
                        {
                            if (this.lookupCancelled)
                            {
                                break;
                            }

                            y[0] = address.ToString();

                            // DNS запись домена получена, поэтому недоступный по ping хост помечается предупреждением, а не запретом
                            ListViewItem addressRow = CreatePingRow(y[0], y[1], PingHost(y[0]), dnsResolved: true);
                            await Task.Factory.StartNew(() => InsertIntoList(addressRow)).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ee)
                    {
                        y[0] = ee.Message; // File.AppendAllText("EventLog.txt", "GET_DNS_NAME. Ошибка поиска IP: "+ domain_finded[i].ToString()+" текст ошибки: " + ee.ToString() + Environment.NewLine);
                    }

                    ListViewItem domainRow = CreatePingRow(y[0], y[1], null);
                    await Task.Factory.StartNew(() => InsertIntoList(domainRow)).ConfigureAwait(false);
                }

                lines = Regex.Replace(lines, @"((?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)\.){1,}(?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)))", string.Empty); // вырезаем сайты и теперь ищем ip

                Regex ip_pattern = new Regex(@"\d+\.\d+\.\d+\.\d+\/\d+|\d+\.\d+\.\d+\.\d+");
                MatchCollection ip_finded = ip_pattern.Matches(lines);

                try
                {
                    for (int i = 0; i < ip_finded.Count; i++)
                    {
                        if (this.lookupCancelled)
                        {
                            break;
                        }

                        string fgdf = ip_finded[i].ToString();
                        if (fgdf.Contains("/"))
                        {
                            string[] masksize = fgdf.Split('/');
                            string[] oktets = masksize[0].Split('.');
                            int masksize_int = Convert.ToInt32(masksize[1]);
                            if (masksize_int >= 20 && masksize_int <= 32)
                            {
                                // Считаем адрес сети (обнуляем хостовые биты) и перечисляем весь диапазон подсети
                                uint ipUint = ((uint)Convert.ToInt32(oktets[0]) << 24) |
                                              ((uint)Convert.ToInt32(oktets[1]) << 16) |
                                              ((uint)Convert.ToInt32(oktets[2]) << 8) |
                                              (uint)Convert.ToInt32(oktets[3]);
                                uint maskUint = 0xFFFFFFFF << (32 - masksize_int);
                                uint network = ipUint & maskUint;
                                int ipCount = Get_IPs_count_for_mask(masksize_int);

                                for (int j = 0; j < ipCount; j++)
                                {
                                    if (this.lookupCancelled)
                                    {
                                        break;
                                    }

                                    uint addr = network + (uint)j;
                                    string ipStr = ((addr >> 24) & 0xFF) + "." + ((addr >> 16) & 0xFF) + "." + ((addr >> 8) & 0xFF) + "." + (addr & 0xFF);

                                    // Учитываем активную фоновую проверку до её постановки в очередь.
                                    System.Threading.Interlocked.Increment(ref this.activeLookups);

                                    // Пул потоков, чтобы /20 (4096 адресов) не создал 4096 одновременных потоков
                                    ThreadPool.QueueUserWorkItem(new WaitCallback(GetHostEntryAsync), ipStr);
                                }
                            }

                            if (masksize_int < 20 || masksize_int > 32)
                            {
                                MessageBox.Show("Маски вне /20-/32 не поддерживаются, поддержи проект доработкой");
                            }
                        }
                        else
                        {
                            // Учитываем активную фоновую проверку до её запуска.
                            System.Threading.Interlocked.Increment(ref this.activeLookups);

                            Thread myThread = new Thread(new ParameterizedThreadStart(GetHostEntryAsync));
                            myThread.Start(fgdf);
                        }
                    }
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.ToString()); /*File.AppendAllText("EventLog.txt", "GET_DNS_NAME. Ошибка поиска IP: " + ee.ToString() + Environment.NewLine);*/
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString()); /*File.AppendAllText("EventLog.txt", "GET_DNS_NAME. Общая ошибка: " + ee.ToString() + Environment.NewLine);*/
            }

            try
            {
                Invoke((ThreadStart)(() => RemoveDuplicates_In_ListView(listView2)));
            }
            catch
            {
            } // удаляем дубликаты

            // Основной цикл выдачи задач завершён (или отменён). Скрытие кнопки происходит
            // только если активных фоновых проверек уже не осталось.
            this.lookupLoopFinished = true;
            this.CheckLookupFinished();
        }

        /// <summary>
        /// Завершает учёт одной активной фоновой проверки lookup и, при полном завершении,
        /// скрывает пункт STOP LOOKUP. Вызывается из <see cref="GetHostEntryAsync"/> (finally).
        /// </summary>
        private void FinishActiveLookup()
        {
            System.Threading.Interlocked.Decrement(ref this.activeLookups);
            this.CheckLookupFinished();
        }

        /// <summary>
        /// Если основной цикл завершён и активных фоновых проверок не осталось,
        /// скрывает пункт STOP LOOKUP (в потоке UI).
        /// </summary>
        private void CheckLookupFinished()
        {
            if (this.lookupLoopFinished && System.Threading.Volatile.Read(ref this.activeLookups) <= 0)
            {
                try
                {
                    this.BeginInvoke((System.Action)(() => this.stopLookupToolStripMenuItem.Visible = false));
                }
                catch
                {
                    // Если окно уже закрыто - скрывать нечего.
                }
            }
        }

        /// <summary>
        /// Запуск определения DNS имени по Ip-адресу.
        /// </summary>
        /// <param name="sender">.</param>
        /// <param name="e">..</param>
        private void PictureBox2_Click(object sender, EventArgs e)
        {
            if (listView2.Items != null && listView2.Items.Count != 0)
            {
                listView2.Items.Clear();
            }

            GET_DNS_NAME(Clipboard.GetText());
        }

        private void PictureBox6_Click(object sender, EventArgs e)
        {
            CopyFromView(0);
        }

        /// <summary>
        /// Открыть папку с программой.
        /// </summary>
        /// <param name="sender">.</param>
        /// <param name="e">..</param>
        private void PictureBox3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Directory.GetCurrentDirectory()) { UseShellExecute = true });
        }

        /// <summary>
        /// Отсортировать по Listview при нажатии на заголовок столба.
        /// </summary>
        /// <param name="sender">.</param>
        /// <param name="e">..</param>
        private void SortListView(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            listView2.Sort();
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/sergiomarotco/IP-Bulk-lookup") { UseShellExecute = true });
        }

        private void CopyIPToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyFromView(1);
        }

        private void CopyNameToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyFromView(2);
        }

        private void CopyAllToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyFromView(3);
        }

        /// <summary>
        /// Прерывание выполняемого lookup: выставляет флаг отмены и скрывает кнопку.
        /// </summary>
        /// <param name="sender">.</param>
        /// <param name="e">..</param>
        private void StopLookupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.lookupCancelled = true;
            this.stopLookupToolStripMenuItem.Visible = false;
        }

        private void Label6_Click(object sender, EventArgs e)
        {
            CopyFromView(0);
        }

        private void Label2_Click(object sender, EventArgs e)
        {
            if (listView2.Items != null && listView2.Items.Count != 0)
                listView2.Items.Clear();
            GET_DNS_NAME(Clipboard.GetText());
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            GET_DNS_NAME(Clipboard.GetText());
        }

        /// <summary>
        /// Растягивает последнюю колонку (NAME) на всю ширину таблицы при изменении размеров окна.
        /// </summary>
        private void ListView2_Resize(object sender, EventArgs e)
        {
            try
            {
                if (listView2.Columns.Count == 0)
                {
                    return;
                }

                int used = 0;
                for (int i = 0; i < listView2.Columns.Count - 1; i++)
                {
                    used += listView2.Columns[i].Width;
                }

                int nameWidth = listView2.ClientSize.Width - used;
                if (nameWidth > 50)
                {
                    listView2.Columns[listView2.Columns.Count - 1].Width = nameWidth;
                }
            }
            catch
            { /* File.AppendAllText("EventLog.txt", "ListView2_Resize. Общая ошибка: " + Environment.NewLine); */
            }
        }
    }
}
