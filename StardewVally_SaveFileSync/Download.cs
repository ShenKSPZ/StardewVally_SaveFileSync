using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace StardewVally_SaveFileSync
{
    public partial class Download : Form
    {
        bool isReplaceLocal = false;
        public Download(bool isReplaceLocally)
        {
            isReplaceLocal = isReplaceLocally;
            InitializeComponent();
        }

        void GetSaveFileListSuccess()
        {
            comboBox1.Items.Clear();
            if(saveFileList.Length > 0)
            {
                comboBox1.Items.AddRange(saveFileList);
                label1.Text = "第一步，请选择要下载的云端存档：";
                comboBox1.Enabled = true;
                int index = -1;
                for (int i = 0; i < comboBox1.Items.Count; i++)
                {
                    if (comboBox1.Items[i].ToString() == GlobalConfig.current.SaveFileNameForAutoUpload)
                    {
                        index = i;
                        break;
                    }
                }
                comboBox1.SelectedIndex = index;
                isGetListSuccess = true;
                NextBtn.Enabled = true;
                NextBtn.Text = "下载";
            }
            else
            {
                label1.Text = "云端还没有任何存档哦";
            }
        }

        void GetSaveFileListFail()
        {
            progressBar1.Value = progressBar1.Minimum;
            progressBar1.Visible = false;
            linkLabel2.Visible = true;
            label1.Text = "从云端获取列表失败了";
            NextBtn.Visible = true;
            NextBtn.Enabled = true;
            NextBtn.Text = "再次尝试获取";
        }

        string[] saveFileList = new string[0];
        private void Download_Load(object sender, EventArgs e)
        {
            Size = new Size(Size.Width, 152);
            if(isReplaceLocal)
            {
                linkLabel2_LinkClicked(this, null);
            }
            else
            {
                HttpHelper.HttpGetSaveFileList((succ, array) =>
                {
                    saveFileList = array;
                    if (succ)
                        InvokeAtThis(GetSaveFileListSuccess);
                    else
                        InvokeAtThis(GetSaveFileListFail);
                });
            }
        }

        void InvokeAtThis(MethodInvoker mi)
        {
            this.BeginInvoke(mi);
        }

        bool isGetListSuccess = false;
        private void NextBtn_Click(object sender, EventArgs e)
        {
            if(isGetListSuccess)
            {
                comboBox1.Enabled = false;
                NextBtn.Enabled = false;
                NextBtn.Visible = false;
                progressBar1.Visible = true;
                progressBar1.Value = progressBar1.Minimum;
                timer1.Enabled = true;
                linkLabel2.Visible = false;
                string fileName = comboBox1.SelectedItem.ToString();
                HttpHelper.HttpGetSaveFileByName(fileName, (succ, content) =>
                {
                    if(succ)
                    {
                        File.WriteAllText(Path.Combine(Environment.CurrentDirectory, fileName + "_FromServer"), content);
                        InvokeAtThis(OnDownloadSaveFile);
                    }
                    else
                    {
                        InvokeAtThis(OnDownloadSaveFileFailure);
                    }
                });
            }
            else
            {
                NextBtn.Enabled = false;
                HttpHelper.HttpGetSaveFileList((succ, array) =>
                {
                    saveFileList = array;
                    if (succ)
                    {
                        InvokeAtThis(GetSaveFileListSuccess);
                    }
                    else
                    {
                        InvokeAtThis(GetSaveFileListFail);
                    }

                });
            }
        }

        void OnDownloadSaveFile()
        {
            var content = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, comboBox1.SelectedItem.ToString() + "_FromServer"));
            SaveFileInfo info = SaveFileOperation.ReadSaveFileFromXML(content, false);
            infoLabel.Text = "下载的存档的信息：" + "\r\n" + "\r\n";
            infoLabel.Text += "农场名字：" + info.FarmName + "\r\n";
            infoLabel.Text += "农场时间：" + info.GameDate + "\r\n";
            infoLabel.Text += "游玩时长：" + info.PlayTime + "\r\n";
            infoLabel.Text += "游戏中的玩家有：\r\n";
            for (int i = 0; i < info.Players.Length; i++)
            {
                infoLabel.Text += "    " + (i + 1).ToString() + ". " + info.Players[i] + "\r\n";
            }
            comboBox2.Items.Clear();
            comboBox2.Items.AddRange(info.Players);
            comboBox2.Enabled = true;
            for (int i = 0; i < comboBox2.Items.Count; i++)
            {
                if(comboBox2.Items[i].ToString() == GlobalConfig.current.SaveFileDefaultCharacter)
                {
                    comboBox2.SelectedIndex = i;
                    break;
                }
                else if(i == comboBox2.Items.Count - 1)
                {
                    GlobalConfig.current.SaveFileDefaultCharacter = "";
                    GlobalConfig.SaveConfig();
                }
            }
            Size = new Size(Size.Width, 486);
            progressBar1.Value = progressBar1.Maximum;
            timer1.Enabled = false;
        }

        void OnDownloadSaveFileFailure()
        {
            timer1.Enabled = false;
            progressBar1.Value = progressBar1.Minimum;
            progressBar1.Visible = false;
            linkLabel2.Visible = true;
            label1.Text = "从云端下载存档失败了";
            NextBtn.Visible = true;
            NextBtn.Enabled = true;
            NextBtn.Text = "再次下载";
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            GlobalConfig.current.SaveFileDefaultCharacter = comboBox2.SelectedItem.ToString();
            GlobalConfig.SaveConfig();
            if(comboBox2.SelectedItem.ToString() != string.Empty)
            {
                ReplaceBtn.Enabled = true;
            }
        }

        private void ReplaceBtn_Click(object sender, EventArgs e)
        {
            string saveFileName = comboBox1.SelectedItem.ToString();
            //先备份存档文件
            SaveFileOperation.BackupSaveFileToPath(saveFileName,
                Path.Combine(Environment.CurrentDirectory, "Backup", saveFileName + "_" + SaveFileOperation.GetTimeStamp()));

            //再将下载的存档文件移入存档文件夹
            if(!isReplaceLocal)
                SaveFileOperation.MoveSaveFileToSaveFolder(saveFileName, Path.Combine(Environment.CurrentDirectory, comboBox1.SelectedItem.ToString() + "_FromServer"));

            //当选择的角色并非主角色时，进行替换
            if(comboBox2.SelectedIndex > 0)
            {
                bool success = SaveFileOperation.SwapCharacter(
                    saveFileName: comboBox1.SelectedItem.ToString(), 
                    farmhandName: comboBox2.SelectedItem.ToString());

                if(success)
                {
                    ReplaceBtn.Enabled = false;
                    ReplaceBtn.Text = "存档实装成功";
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                }
                else
                {
                    ReplaceBtn.Text = "再次尝试实装";
                }
            }
            else
            {
                ReplaceBtn.Enabled = false;
                ReplaceBtn.Text = "存档实装成功";
                comboBox1.Enabled = false;
                comboBox2.Enabled = false;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Path.Combine(Environment.CurrentDirectory, "Backup"));
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Text = "为本地存档替换角色";
            isReplaceLocal = true;
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SaveFileOperation.GetAllSaveFileName());
            comboBox1.Enabled = true;
            label1.Text = "第一步，请选择一个本地存档";
            linkLabel2.Visible = false;
            NextBtn.Visible = false;
            linkLabel1.Visible = false;
            ReplaceBtn.Text = "替换角色";
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(isReplaceLocal)
            {
                var content = File.ReadAllText(SaveFileOperation.GetSaveFilePathByName(comboBox1.SelectedItem.ToString()));
                SaveFileInfo info = SaveFileOperation.ReadSaveFileFromXML(content, false);
                infoLabel.Text = "本地的存档的信息：" + "\r\n" + "\r\n";
                infoLabel.Text += "农场名字：" + info.FarmName + "\r\n";
                infoLabel.Text += "农场时间：" + info.GameDate + "\r\n";
                infoLabel.Text += "游玩时长：" + info.PlayTime + "\r\n";
                infoLabel.Text += "游戏中的玩家有：\r\n";
                for (int i = 0; i < info.Players.Length; i++)
                {
                    infoLabel.Text += "    " + (i + 1).ToString() + ". " + info.Players[i] + "\r\n";
                }
                comboBox2.Items.Clear();
                comboBox2.Items.AddRange(info.Players);
                comboBox2.Enabled = true;
                for (int i = 0; i < comboBox2.Items.Count; i++)
                {
                    if (comboBox2.Items[i].ToString() == GlobalConfig.current.SaveFileDefaultCharacter)
                    {
                        comboBox2.SelectedIndex = i;
                        break;
                    }
                    else if (i == comboBox2.Items.Count - 1)
                    {
                        GlobalConfig.current.SaveFileDefaultCharacter = "";
                        GlobalConfig.SaveConfig();
                    }
                }
                Size = new Size(Size.Width, 486);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (progressBar1.Value < progressBar1.Maximum)
                progressBar1.Value++;
        }
    }
}
