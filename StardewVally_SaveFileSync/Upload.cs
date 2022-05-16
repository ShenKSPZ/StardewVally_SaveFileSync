using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StardewVally_SaveFileSync
{
    public partial class Upload : Form
    {
        bool AutoUpload = false;
        public Upload(bool autoUpload)
        {
            AutoUpload = autoUpload;
            InitializeComponent();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox1.SelectedItem.ToString() != "")
            {
                SaveFileInfo info = SaveFileOperation.ReadSaveFile(comboBox1.SelectedItem.ToString(), false);
                infoLabel.Text = "农场名字：" + info.FarmName + "\r\n";
                infoLabel.Text += "农场时间：" + info.GameDate + "\r\n";
                infoLabel.Text += "游玩时长：" + info.PlayTime + "\r\n";
                infoLabel.Text += "游戏中的玩家有：\r\n";
                for (int i = 0; i < info.Players.Length; i++)
                {
                    infoLabel.Text += "    " + (i + 1).ToString() + ". " + info.Players[i] + "\r\n";
                }
                UploadBtn.Enabled = true;
                checkBox1.Visible = true;
                GlobalConfig.current.SaveFileNameForAutoUpload = comboBox1.SelectedItem.ToString();
                GlobalConfig.SaveConfig();
                checkBox1.Text = "之后都自动上传 " + comboBox1.SelectedItem.ToString();
                Size = new Size(Size.Width, 471);
            }
        }

        bool isUploadSuccess = false;
        bool isUploading = false;
        private void UploadBtn_Click(object sender, EventArgs e)
        {
            if (!isUploadSuccess && !isUploading)
            {
                progressBar1.Value = progressBar1.Minimum;
                isUploading = true;
                UploadBtn.Enabled = false;
                comboBox1.Enabled = false;
                checkBox1.Enabled = false;
                UploadBtn.Visible = false;
                progressBar1.Visible = true;
                label2.Text = "正在上传";
                timer2.Enabled = true;
                HttpHelper.HttpPostFile(
                    SaveFileOperation.GetSaveFilePathByName(comboBox1.SelectedItem.ToString()), (res) => {
                        if (res == "FileReceiveSuccess")
                        {
                            isUploadSuccess = true;
                            InvokeAtThis(UploadSuccess);
                        }
                        else
                        {
                            InvokeAtThis(UploadFail);
                        }
                        isUploading = false;
                    });
            }
            else
            {
                Close();
            }
        }

        void UploadSuccess()
        {
            progressBar1.Value = progressBar1.Maximum;
            if(!AutoUpload)
            {
                progressBar1.Visible = false;
                UploadBtn.Visible = true;
                UploadBtn.Enabled = true;
                UploadBtn.Text = "关闭";
            }
            else
            {
                timer1.Enabled = true;
            }
            label2.Text = AutoUpload ? "自动上传成功" : "上传成功";
        }

        void UploadFail()
        {
            timer2.Enabled = false;
            progressBar1.Visible = false;
            label2.Text = "上传失败";
            UploadBtn.Visible = true;
            UploadBtn.Enabled = true;
            UploadBtn.Text = "再次上传";
            comboBox1.Enabled = true;
            checkBox1.Enabled = true;
        }

        void InvokeAtThis(MethodInvoker mi)
        {
            this.BeginInvoke(mi);
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            GlobalConfig.current.isAutoUploadSave = checkBox1.Checked;
            GlobalConfig.current.SaveFileNameForAutoUpload = comboBox1.SelectedItem.ToString();
            GlobalConfig.SaveConfig();
        }

        private void Upload_Shown(object sender, EventArgs e)
        {
            if(AutoUpload)
            {
                TopMost = true;
                int index = -1;
                for (int i = 0; i < comboBox1.Items.Count; i++)
                {
                    if(comboBox1.Items[i].ToString() == GlobalConfig.current.SaveFileNameForAutoUpload)
                    {
                        index = i;
                        break;
                    }
                }
                if(index >= 0)
                {
                    comboBox1.SelectedIndex = index;
                    UploadBtn_Click(this, null);
                }
                else
                {
                    label2.Text = "找不到要自动上传的存档了";
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void Upload_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(isUploading)
            {
                e.Cancel = true;
            }
        }

        private void Upload_Load(object sender, EventArgs e)
        {
            string[] saveFile = SaveFileOperation.GetAllSaveFileName();
            comboBox1.Items.AddRange(saveFile);
            checkBox1.Visible = false;
            for (int i = 0; i < comboBox1.Items.Count; i++)
            {
                if(comboBox1.Items[i].ToString() == GlobalConfig.current.SaveFileNameForAutoUpload)
                {
                    comboBox1.SelectedIndex = i;
                    checkBox1.Visible = true;
                    break;
                }
            }
            if(comboBox1.SelectedIndex < 0)
                Size = new Size(Size.Width, 127);
            checkBox1.Checked = GlobalConfig.current.isAutoUploadSave;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (progressBar1.Value < progressBar1.Maximum)
                progressBar1.Value++;
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }
    }
}
