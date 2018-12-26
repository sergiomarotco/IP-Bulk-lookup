using System;
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

namespace IP_Bulk_lookup
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private delegate void InsertIntoListDelegate(ListViewItem item);

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
                }
            }
            catch { }
        }

        private int progress = 0;
        private static bool CheckIp(string address)
        {
            try
            {
                string[] nums = address.Split('.');
                return nums.Length == 4 && nums.All(n => int.TryParse(n, out int useless)) && nums.Select(int.Parse).All(n => n < 256);
            }
            catch { return false; }
        }

        private void Progress_change(int step)
        {
            lock (locker)
            {
                progress += step;

                if (progress > 0)
                    Invoke((ThreadStart)delegate { progresslabel.Text = progress.ToString(); });
                else Invoke((ThreadStart)delegate { progresslabel.Text = ""; });
            }
        }
        /// <summary>
        /// Lookup
        /// </summary>
        /// <param name="IP"></param>
        private async void GetHostEntryAsync(object IP)
        {
            try
            {
                string[] y = new string[2]; //bool lookuped = false;
                try
                {
                    string ip = (string)IP;
                    char[] trimcharachters = { ' ', '\t' };
                    ip = ip.Trim(trimcharachters);
                    y[0] = (string)IP;
                    try
                    {
                        if (CheckIp(ip))
                        {
                            y[1] = Dns.GetHostEntry(ip).HostName;
                            y[0] = ip;
                            //lookuped = true;
                        }
                        else
                        {
                            y[1] = "incorrect IP";
                           // y[1] = Dns.GetHostAddressesAsync(ip).ToString();
                           // y[0] = ip;
                        }
                    }
                    catch (Exception ee)
                    {
                        y[1] = ee.Message;
                    }
                    await Task.Factory.StartNew(() => InsertIntoList(new ListViewItem(y))).ConfigureAwait(false);
                }
                catch { }
                Progress_change(-1);
            }
            catch { }
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://ru.icons8.com");//открыть рекламную ссылку
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            Icon = Properties.Resources.Icon;
            this.AllowDrop = true;
            lvwColumnSorter = new ListViewColumnSorter();
            this.listView2.ListViewItemSorter = lvwColumnSorter;
            if (listView2.Items != null && listView2.Items.Count != 0)
                listView2.Items.Clear();
            GET_DNS_NAME(Clipboard.GetText());
        }

        private static readonly object locker = new object();
        /// <summary>
        /// Возвращает количество хостов для заданной маски
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        private int Get_IPs_count_for_mask(int mask)
        {
            int count = 0;
            switch (mask)
            {
                case 24:
                    count = 256;
                    break;
                case 25:
                    count = 128;
                    break;
                case 26:
                    count = 64;
                    break;
                case 27:
                    count = 32;
                    break;
                case 28:
                    count = 16;
                    break;
                case 29:
                    count = 8;
                    break;
                case 30:
                    count = 4;
                    break;
                case 31:
                    count = 2;
                    break;
                case 32:
                    count = 1;
                    break;
                default:
                    count = 0;
                    break;
            }
            return count;
        }
        
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if (listView2.Items != null && listView2.Items.Count != 0)
                    listView2.Items.Clear();
                if (e.Data.GetData("FileName") is Array data1)
                {
                    if ((data1.Length == 1) && (data1.GetValue(0) is String))
                    {
                        string filename = ((string[])data1)[0];
                        try
                        {
                            if (new FileInfo(filename).Length < 3000)
                            {
                                GET_DNS_NAME(File.ReadAllText(filename));
                            }
                        }
                        catch { }
                    }
                }
                listView2.Refresh();
            }
            catch { }
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

        private void PictureBox5_Click(object sender, EventArgs e)
        {
            //listView2.select
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
                        {
                            lines[i] += listView2.SelectedItems[i].SubItems[0].Text + "," + listView2.SelectedItems[i].SubItems[1].Text;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < listView2.Items.Count; i++)
                        {
                            lines[i] += listView2.Items[i].SubItems[0].Text + "," + listView2.Items[i].SubItems[1].Text;
                        }
                    }
                    DateTime D = DateTime.Now;
                    string filename = "nslookup " + D.Year + "." + D.Month + "." + D.Day + " " + D.Hour + "-" + D.Minute + "-" + D.Second + ".txt";
                    File.WriteAllLines(filename, lines);
                    System.Diagnostics.Process.Start("explorer.exe", "/select, " + filename);
                }
            }
            catch { }
        }

        /// <summary>
        /// Скопировать текст в буфер обмена
        /// </summary>
        private void CopyFromView()
        {
            try
            {
                if (listView2.Items != null && listView2.Items.Count != 0)
                {
                    string lines = string.Empty;
                    for (int i = 0; i < listView2.SelectedItems.Count; i++)
                    {
                        lines += listView2.SelectedItems[i].SubItems[0].Text + Environment.NewLine;
                    }
                    Clipboard.SetText(lines);
                }
            }
            catch { }
        }

        private void СкопироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyFromView();
        }

        /// <summary>
        /// Основная функция поиска IP в тексте и получение DNS имени
        /// </summary>
        private async void GET_DNS_NAME(string lines)
        {
            try
            {
                Regex domain_pattern = new Regex(@"((?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)\.){1,}(?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)))");
                MatchCollection domain_finded = domain_pattern.Matches(lines);
                Invoke((ThreadStart)delegate { progresslabel.Text = domain_finded.Count.ToString(); });
                Progress_change(domain_finded.Count);
                for (int i = 0; i < domain_finded.Count; i++)
                {
                    string[] y = new string[2]; //bool lookuped = false;
                    y[1] = domain_finded[i].ToString();
                    try
                    {
                        IPAddress[] addresses = Dns.GetHostAddresses(domain_finded[i].ToString());
                        Progress_change(addresses.Length);
                        foreach (IPAddress address in addresses)
                        {
                            y[0] = address.ToString();
                            await Task.Factory.StartNew(() => InsertIntoList(new ListViewItem(y))).ConfigureAwait(false);
                            Progress_change(-1);
                        }                        
                    }
                    catch (Exception ee)
                    {
                        y[0] = ee.Message;
                    }                    
                    await Task.Factory.StartNew(() => InsertIntoList(new ListViewItem(y))).ConfigureAwait(false);
                }
                lines = Regex.Replace(lines, @"((?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)\.){1,}(?:(?![0-9-])[a-zA-Z0-9-]{1,63}(?<!-)))", "");//вырезаем сайты и теперь ищем ip
                
                Regex ip_pattern = new Regex(@"\d+\.\d+\.\d+\.\d+\/\d+|\d+\.\d+\.\d+\.\d+");
                MatchCollection ip_finded = ip_pattern.Matches(lines);
                Invoke((ThreadStart)delegate { progresslabel.Text = ip_finded.Count.ToString(); });
                
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
                                Progress_change(ip_max);
                                for (int j = 0; j < ip_max; j++)
                                {
                                    Thread myThread = new Thread(new ParameterizedThreadStart(GetHostEntryAsync));
                                    myThread.Start(oktets[0] + "." + oktets[1] + "." + oktets[2] + "." + (Convert.ToInt32(oktets[3]) + j));
                                    Progress_change(-1);
                                }
                            }
                        }
                    }
                    else
                    {
                        Thread myThread = new Thread(new ParameterizedThreadStart(GetHostEntryAsync));
                        myThread.Start(fgdf);
                    }
                }            
            }
            catch (Exception ee){
                MessageBox.Show(ee.ToString());
            }
            try { Invoke((ThreadStart)delegate { RemoveDuplicates_In_ListView(listView2); }); } catch { }//удаляем дубликаты
        }
        /// <summary>
        /// Удаляет одинаковые элементы в listView
        /// </summary>
        /// <param name="ListView"></param>
        public void RemoveDuplicates_In_ListView(ListView ListView)
        {
            try
            {
                if (ListView == null)
                {
                    throw new ArgumentNullException(nameof(ListView));
                }

                var tags = new HashSet<string>();
                var duplicates = new List<ListViewItem>();
                foreach (ListViewItem item in ListView.Items)
                {
                    if (!tags.Add(item.Text))// HashSet.Add() returns false if it already contains the key.
                    {
                        duplicates.Add(item);
                    }
                }

                foreach (ListViewItem item in duplicates)
                    item.Remove();
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString());
            }
        }
        private void PictureBox2_Click(object sender, EventArgs e)
        {
            if (listView2.Items != null && listView2.Items.Count != 0)
                listView2.Items.Clear();
            GET_DNS_NAME(Clipboard.GetText());
        }

        private void PictureBox6_Click(object sender, EventArgs e)
        {
            CopyFromView();
        }

        private void PictureBox4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Drag the file onto the form :)");
        }

        /// <summary>
        /// Открыть папку с программой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Directory.GetCurrentDirectory());
        }

        private ListViewColumnSorter lvwColumnSorter;

        /// <summary>
        /// Отсортировать по Listview при нажатии на заголовок столба
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortListView(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                if (lvwColumnSorter.Order == SortOrder.Ascending) lvwColumnSorter.Order = SortOrder.Descending;
                else lvwColumnSorter.Order = SortOrder.Ascending;
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
    }
}
