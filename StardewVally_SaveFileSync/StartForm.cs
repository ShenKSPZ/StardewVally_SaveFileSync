using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StardewVally_SaveFileSync
{
    public partial class StartForm : Form
    {
        bool ServerPingSuccess = false;
        bool isServerPinging = false;
        bool isGameRunning = false;

        public StartForm()
        {
            InitializeComponent();
        }

        void DownloadBtnCheck()
        {
            if (!isGameRunning && !isServerPinging)
            {
                button1.Enabled = true;
            }
            else
            {
                button1.Enabled = false;
            }
        }

        void UploadBtnCheck()
        {
            if (ServerPingSuccess && !isGameRunning)
            {
                button2.Enabled = true;
            }
            else
            {
                button2.Enabled = false;
            }
        }

        void OnStartPingServer()
        {
            CloudLabel.Visible = true;
            CloudLabel.Text = "正在连接云端";
            button1.Enabled = false;
            button1.Text = "从云端获取存档";
        }

        void OnServerPingFail()
        {
            CloudLabel.Visible = true;
            CloudLabel.Text = "无法连接至云端";
            button1.Enabled = true;
            button1.Text = "重试连接";
        }

        ulong cloudFileTimeStamp = 0;
        void OnServerPingSuccess()
        {
            ulong saveFileTimeStamp = SaveFileOperation.GetSaveFileTimeStamp(GlobalConfig.current.SaveFileNameForAutoUpload);
            if (cloudFileTimeStamp > saveFileTimeStamp)
            {
                CloudLabel.Text = "云端有更加新的存档哦！";
            }
            else
            {
                CloudLabel.Visible = false;
            }
            button1.Enabled = true;
            button1.Text = "从云端获取存档";
        }

        private void StartForm_Load(object sender, EventArgs e)
        {
            OnStartPingServer();
            TryPingServer();
        }

        void TryPingServer()
        {
            isServerPinging = true;
            HttpHelper.ServerPing((success, time) =>
            {
                cloudFileTimeStamp = time;
                isServerPinging = false;
                ServerPingSuccess = success;
                InvokeAtThis(UploadBtnCheck);
                InvokeAtThis(DownloadBtnCheck);
                if (ServerPingSuccess)
                    InvokeAtThis(OnServerPingSuccess);
                else
                    InvokeAtThis(OnServerPingFail);
            });
        }

        void InvokeAtThis(MethodInvoker mi)
        {
            this.BeginInvoke(mi);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(ServerPingSuccess)
            {
                Download download = new Download(false);
                PlaceFormAtCenter(download);
                download.ShowDialog();
            }
            else
            {
                OnStartPingServer();
                TryPingServer();
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Upload upload = new Upload(false);
            PlaceFormAtCenter(upload);
            upload.ShowDialog();
        }

        bool last_open = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            Process[] pc = Process.GetProcessesByName("Stardew Valley");
            isGameRunning = LocalLabel.Visible = pc.Length != 0;
            linkLabel2.Visible = !LocalLabel.Visible;
            if (isGameRunning != last_open)
            {
                last_open = isGameRunning;
                if (!isGameRunning && GlobalConfig.current.isAutoUploadSave && ServerPingSuccess)
                {
                    Upload upload = new Upload(true);
                    PlaceFormAtCenter(upload);
                    upload.ShowDialog();
                }
            }
            UploadBtnCheck();
            DownloadBtnCheck();
        }

        private void StartForm_Activated(object sender, EventArgs e)
        {
            if (GlobalConfig.current.SaveFileNameForAutoUpload != "")
                label1.Text = "记录的存档为：" + GlobalConfig.current.SaveFileNameForAutoUpload.Split('_')[0];
            else
                label1.Text = "";
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Download download = new Download(true);
            PlaceFormAtCenter(download);
            download.ShowDialog();
        }

        void PlaceFormAtCenter(Form otherForm)
        {
            Point center = new Point(Location.X + (Size.Width / 2), Location.Y + (Size.Height / 2));
            otherForm.Location = new Point(center.X - otherForm.Width / 2, center.Y - otherForm.Height / 2);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("本程序由Lawrence(aka 深空)开发。\r\n目前版本v1.0。\r\n没有经过大规模测试，也许会存在Bug，还请见谅。");
        }
    }
}
