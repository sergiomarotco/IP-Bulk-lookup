﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
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
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }

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
                    if (tags.Add(item.Text))
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

        private delegate void InsertIntoListDelegate(ListViewItem item);

        /// <summary>
        /// Добавляет элемент в listView2.
        /// </summary>
        /// <param name="item">Добавляемый элемент в listView2.</param>
        public void InsertIntoList(ListViewItem item)
        {
            try
            {
                if (InvokeRequired) Invoke(new InsertIntoListDelegate(InsertIntoList), item);
                else listView2.Items.Add(item);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString()); /* File.AppendAllText("EventLog.txt", "InsertIntoList. Общая ошибка: " + ee.ToString() + Environment.NewLine);*/
            }
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

        /*private void Progress_change(int step)
        {
            lock (locker)
            {
                progress += step;

                if (progress > 0) Invoke((ThreadStart)delegate { progresslabel.Text = progress.ToString(); });
                else Invoke((ThreadStart)delegate { progresslabel.Text = ""; });
            }
        }*/

        /// <summary>
        /// Lookup (Dns.GetHostEntry).
        /// <param name="ip">IP-адрес.</param>
        private async void GetHostEntryAsync(object ip)
        {
            string ip0 = string.Empty;
            try
            {
                string[] y = new string[2]; // bool lookuped = false;
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
                            y[1] = Dns.GetHostEntry(ip0).HostName;
                            y[0] = ip0;
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

                    // Thread.Sleep(10);
                    await Task.Factory.StartNew(() => InsertIntoList(new ListViewItem(y))).ConfigureAwait(false);
                }
                catch
                { /*File.AppendAllText("EventLog.txt", "GetHostEntryAsync. Ошибка проверки IP: " + ip + Environment.NewLine); */
                }
            }
            catch
            { /*File.AppendAllText("EventLog.txt", "GetHostEntryAsync. Иная ошибка: " + ee.ToString() + Environment.NewLine);*/
            }

            // Progress_change(-1);
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://ru.icons8.com"); // открыть рекламную ссылку
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
        /// Возвращает количество хостов для заданной маски.
        /// </summary>
        /// <param name="mask">Маска сети.</param>
        /// <returns>Максимальное зачение IP-адресов.</returns>
        private int Get_IPs_count_for_mask(int mask)
        {
            int max;
            switch (mask)
            {
                case 24:
                    max = 256;
                    break;
                case 25:
                    max = 128;
                    break;
                case 26:
                    max = 64;
                    break;
                case 27:
                    max = 32;
                    break;
                case 28:
                    max = 16;
                    break;
                case 29:
                    max = 8;
                    break;
                case 30:
                    max = 4;
                    break;
                case 31:
                    max = 2;
                    break;
                case 32:
                    max = 1;
                    break;
                default:
                    max = 0;
                    break;
            }

            return max;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
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
                        contextMenuStrip1.Show(this, new Point(e.X, e.Y));
                    }

                    break;
            }
        }

        private void ОтменаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Hide();
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
                            lines[i] += listView2.SelectedItems[i].SubItems[0].Text + "," + listView2.SelectedItems[i].SubItems[1].Text;
                    }
                    else
                    {
                        for (int i = 0; i < listView2.Items.Count; i++)
                            lines[i] += listView2.Items[i].SubItems[0].Text + "," + listView2.Items[i].SubItems[1].Text;
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
        /// <param name="type">0 - скопировать IP+Name, 1 - только IP, 2 - только Имя.</param>
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
                                    lines += listView2.SelectedItems[i].SubItems[0].Text + ";" + listView2.SelectedItems[i].SubItems[1].Text + Environment.NewLine;
                                else lines += listView2.SelectedItems[i].SubItems[0].Text + ";" + listView2.SelectedItems[i].SubItems[1].Text;
                                break;
                            case 1: // скопировать IP
                                if (i != listView2.SelectedItems.Count - 1)
                                    lines += listView2.SelectedItems[i].SubItems[0].Text + Environment.NewLine;
                                else lines += listView2.SelectedItems[i].SubItems[0].Text;
                                break;
                            case 2: // скопировать Name
                                if (i != listView2.SelectedItems.Count - 1)
                                    lines += listView2.SelectedItems[i].SubItems[1].Text + Environment.NewLine;
                                else lines += listView2.SelectedItems[i].SubItems[1].Text;
                                break;
                            default: // скопировать IP+Name
                                if (i != listView2.SelectedItems.Count - 1)
                                    lines += listView2.SelectedItems[i].SubItems[0].Text + ";" + listView2.SelectedItems[i].SubItems[1].Text + Environment.NewLine;
                                else lines += listView2.SelectedItems[i].SubItems[0].Text + ";" + listView2.SelectedItems[i].SubItems[1].Text;
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
            try
            {
                Regex domain_pattern = new Regex(@"((?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)\.){1,}(?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)))");
                MatchCollection domain_finded = domain_pattern.Matches(lines);

                // Invoke((ThreadStart)delegate { progresslabel.Text = domain_finded.Count.ToString(); });
                // Progress_change(domain_finded.Count);
                for (int i = 0; i < domain_finded.Count; i++)
                {
                    string[] y = new string[2]; // bool lookuped = false;
                    y[1] = domain_finded[i].ToString();
                    try
                    {
                        string ss = domain_finded[i].ToString();
                        IPAddress[] addresses = Dns.GetHostAddresses(domain_finded[i].ToString());

                        // Progress_change(addresses.Length);
                        foreach (IPAddress address in addresses)
                        {
                            y[0] = address.ToString();
                            await Task.Factory.StartNew(() => InsertIntoList(new ListViewItem(y))).ConfigureAwait(false);
                        }

                        // Progress_change(-addresses.Length);
                    }
                    catch (Exception ee)
                    {
                        y[0] = ee.Message; // File.AppendAllText("EventLog.txt", "GET_DNS_NAME. Ошибка поиска IP: "+ domain_finded[i].ToString()+" текст ошибки: " + ee.ToString() + Environment.NewLine);
                    }

                    await Task.Factory.StartNew(() => InsertIntoList(new ListViewItem(y))).ConfigureAwait(false);
                }

                lines = Regex.Replace(lines, @"((?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)\.){1,}(?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)))", string.Empty); // вырезаем сайты и теперь ищем ip

                Regex ip_pattern = new Regex(@"\d+\.\d+\.\d+\.\d+\/\d+|\d+\.\d+\.\d+\.\d+");
                MatchCollection ip_finded = ip_pattern.Matches(lines);

                // Invoke((ThreadStart)delegate { progresslabel.Text = ip_finded.Count.ToString(); });
                try
                {
                    for (int i = 0; i < ip_finded.Count; i++)
                    {
                        string fgdf = ip_finded[i].ToString();
                        if (fgdf.Contains("/"))
                        {
                            string[] masksize = fgdf.Split('/');
                            string[] oktets = masksize[0].Split('.');
                            int masksize_int = Convert.ToInt32(masksize[1]);
                            if (masksize_int >= 24)
                            {
                                if (masksize_int <= 32)
                                {
                                    int ip_max = Get_IPs_count_for_mask(masksize_int);
                                    if (ip_max > 255) ip_max = 255;

                                    // Progress_change(ip_max);
                                    for (int j = 0; j < ip_max; j++)
                                    {
                                        Thread myThread = new Thread(new ParameterizedThreadStart(GetHostEntryAsync));
                                        myThread.Start(oktets[0] + "." + oktets[1] + "." + oktets[2] + "." + (Convert.ToInt32(oktets[3]) + j));

                                        // Thread.Sleep(10);
                                    }
                                }
                            }

                            if (masksize_int < 24)
                            {
                                MessageBox.Show("Маска меньше /24 не поддерживается, поддержи проект доработкой");
                            }
                        }
                        else
                        {
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
                Invoke(method: (ThreadStart)delegate { RemoveDuplicates_In_ListView(listView2); });
            }
            catch
            {
            } // удаляем дубликаты
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

        private void PictureBox4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Drag the file onto the form :)");
        }

        /// <summary>
        /// Открыть папку с программой.
        /// </summary>
        /// <param name="sender">.</param>
        /// <param name="e">..</param>
        private void PictureBox3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Directory.GetCurrentDirectory());
        }

        /// <summary>
        /// Переменная используемая при сортировке.
        /// </summary>
        private ListViewColumnSorter lvwColumnSorter;

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
            System.Diagnostics.Process.Start("https://github.com/sergiomarotco/IP-Bulk-lookup");
        }

        private void CopyIPToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyFromView(1);
        }

        private void CopyNameToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyFromView(2);
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
    }
}
