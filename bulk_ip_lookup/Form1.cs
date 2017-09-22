using System;

using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
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
        int progress = 0;
        static bool CheckIp(string address)
        {
            try
            {
                string[] nums = address.Split('.');
                int useless;
                return nums.Length == 4 && nums.All(n => int.TryParse(n, out useless)) && nums.Select(int.Parse).All(n => n < 256);
            }
            catch { return false; }
        }
        private void Progress_change(int step)
        {
            lock (locker)
            {
                progress = progress + step;
            }
            if (progress != 0)
                Invoke((ThreadStart)delegate { progresslabel.Text = progress.ToString(); });
            else Invoke((ThreadStart)delegate { progresslabel.Text = ""; });
        }
        private async void GetHostEntryAsync(object IP)
        {
            //Progress_change(1);                    
            try
            {
                string[] y = new string[2];
                try
                {
                    y[0] = (string)IP;
                    try
                    {
                        string ip = (string)IP;
                        char[] trimcharachters = { ' ', '\t' };
                        ip = ip.Trim(trimcharachters);
                        if (CheckIp(ip))
                        {
                            y[1] = Dns.GetHostEntry(ip).HostName;
                            y[0] = ip;
                        }
                        else
                        {
                            y[1] = "incorrect IP";
                        }
                    }
                    catch (Exception ee)
                    {
                        y[1] = ee.Message;
                    }
                    await Task.Factory.StartNew(() => InsertIntoList(new ListViewItem(y)));
                }
                catch { }
                Progress_change(-1);
            }
            catch { }
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://ru.icons8.com");
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {            
            Icon = Properties.Resources.Icon;
            this.AllowDrop = true;
        }
        static object locker = new object();

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if(listView2.Items!=null)
                    if(listView2.Items.Count !=0)
                        listView2.Items.Clear();
                Array data1 = e.Data.GetData("FileName") as Array;
                //string filename = String.Empty;
                if (data1 != null)
                {
                    if ((data1.Length == 1) && (data1.GetValue(0) is String))
                    {
                        string filename = ((string[])data1)[0];
                        //string ext = Path.GetExtension(filename).ToLower();
                        string[] lines = File.ReadAllLines(filename);
                        try
                        {
                            progress = lines.Length;
                            Invoke((ThreadStart)delegate { progresslabel.Text = lines.Length.ToString(); });
                            for (int i = 0; i < lines.Length; i++)
                            {
                                Thread myThread = new Thread(new ParameterizedThreadStart(GetHostEntryAsync));
                                myThread.Start(lines[i]);
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

        private void ListView2_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (listView2.Items != null)
                    if (listView2.Items.Count != 0)
                    {
                        listView2.Items.Clear();
                        Array data1 = e.Data.GetData("FileName") as Array;
                        //string filename = String.Empty;
                        if (data1 != null)
                        {
                            if ((data1.Length == 1) && (data1.GetValue(0) is String))
                            {
                                string filename = ((string[])data1)[0];
                                //string ext = Path.GetExtension(filename).ToLower();
                                string[] lines = File.ReadAllLines(filename);
                                try
                                {
                                    for (int i = 0; i < lines.Length; i++)
                                    {
                                        Thread myThread = new Thread(new ParameterizedThreadStart(GetHostEntryAsync));
                                        myThread.Start(lines[i]);
                                    }
                                }
                                catch { }
                            }
                        }
                        listView2.Refresh();
                    }
            }
            catch { }
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
            try
            {
                string[] lines = new string[listView2.Items.Count];
                if (listView2.Items != null)
                    if (listView2.Items.Count != 0)
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
                        System.Diagnostics.Process.Start("explorer.exe", @"/select, " + filename);
                    }
            }
            catch { }
        }

        private void ListView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.Items != null)
                if (listView2.SelectedItems.Count != 0)
                {
                    string lines = string.Empty;
                    for (int i = 0; i < listView2.SelectedItems.Count; i++)
                    {
                        lines += listView2.SelectedItems[i].SubItems[0].Text + Environment.NewLine;
                    }
                    Clipboard.SetText(lines);
                }
        }

        private void СкопироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.Items != null)
                if (listView2.Items.Count != 0)
                {
                    string lines = string.Empty;
                    for (int i = 0; i < listView2.SelectedItems.Count; i++)
                    {
                        lines += listView2.SelectedItems[i].SubItems[0].Text + Environment.NewLine;
                    }
                    Clipboard.SetText(lines);
                }
        }

        private void PictureBox2_Click(object sender, EventArgs e)
        {
            if (listView2.Items != null)
                if (listView2.Items.Count != 0)
                    listView2.Items.Clear();
            string[] lines = Clipboard.GetText().Split('\n');
            try
            {
                progress = lines.Length;
                Invoke((ThreadStart)delegate { progresslabel.Text = lines.Length.ToString(); });
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].TrimEnd('\r');
                    lines[i] = lines[i].TrimEnd();
                    if (lines[i] != "")
                    {
                        Thread myThread = new Thread(new ParameterizedThreadStart(GetHostEntryAsync));
                         myThread.Start(lines[i]);
                    }
                    else Progress_change(-1);
                }
            }
            catch { }
        }

        private void PictureBox6_Click(object sender, EventArgs e)
        {
            if (listView2.Items != null)
                if (listView2.Items.Count != 0)
                {
                    string lines = string.Empty;
                    for (int i = 0; i < listView2.SelectedItems.Count; i++)
                    {
                        lines += listView2.SelectedItems[i].SubItems[0].Text + Environment.NewLine;
                    }
                    Clipboard.SetText(lines);
                }
        }

        private void PictureBox4_Click(object sender, EventArgs e)
        {

        }

        private void PictureBox3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Directory.GetCurrentDirectory());
        }
    }
}
