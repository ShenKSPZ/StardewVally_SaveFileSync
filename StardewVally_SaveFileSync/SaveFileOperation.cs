using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace StardewVally_SaveFileSync
{
    public struct SaveFileInfo
    {
        public string PlayTime;
        public string GameDate;
        public string FarmName;
        public string[] Players;
    }

    public static class SaveFileOperation
    {
        public static string SaveFileFolder => Environment.GetEnvironmentVariable("systemdrive") + @"\Users\" + Environment.UserName + @"\AppData\Roaming\StardewValley\Saves\";

        public static bool CheckSaveFileFolderExist() { return Directory.Exists(SaveFileFolder); }

        public static string[] GetAllSaveFileName()
        {
            if(CheckSaveFileFolderExist())
            {
                string[] strs = Directory.GetDirectories(SaveFileFolder);
                string[] value = new string[strs.Length];
                for (int i = 0; i < strs.Length; i++)
                {
                    value[i] = Path.GetFileNameWithoutExtension(strs[i]);
                }

                return value;
            }
            else
            {
                return null;
            }
        }

        public static string GetSaveFilePathByName(string name)
        {
            return Path.Combine(SaveFileFolder, name, name);
        }

        /// <summary>
        /// 获取saveFileInfo文件的路径，该文件用于菜单界面的存档信息读取。
        /// </summary>
        /// <param name="name">存档文件名称</param>
        /// <returns></returns>
        public static string GetSaveGameInfoPathByName(string name)
        {
            return Path.Combine(SaveFileFolder, name, "SaveGameInfo");
        }

        public static SaveFileInfo ReadSaveFile(string saveFileName, bool keepEmptyPlayer = true)
        {
            SaveFileInfo saveFileInfo = new SaveFileInfo();
            string path = GetSaveFilePathByName(saveFileName);
            string content = File.ReadAllText(path);
            XmlDocument xmlData = new XmlDocument();
            xmlData.LoadXml(content);
            //找出存档游玩的日期
            XmlNode playerNode = xmlData.SelectSingleNode("SaveGame/player");
            string day = playerNode.SelectSingleNode("dayOfMonthForSaveGame").InnerText;
            string season = playerNode.SelectSingleNode("seasonForSaveGame").InnerText;
            string year = playerNode.SelectSingleNode("yearForSaveGame").InnerText;
            saveFileInfo.GameDate = "第" + year + "年 " + GetSeasonByIndex(int.Parse(season)) + " 第" + day + "天";

            //找出存档中的玩家
            string playerName = playerNode.SelectSingleNode("name").InnerText;
            XmlNodeList farmHandList = xmlData.SelectNodes("SaveGame/locations/GameLocation/buildings/Building/indoors/farmhand");
            if (keepEmptyPlayer)
            {
                saveFileInfo.Players = new string[farmHandList.Count + 1];
                saveFileInfo.Players[0] = playerName;
                for (int i = 1; i < saveFileInfo.Players.Length; i++)
                {
                    string farmHandName = farmHandList.Item(i - 1).SelectSingleNode("name").InnerText;
                    if (farmHandName == "")
                        farmHandName = "[空位]";
                    saveFileInfo.Players[i] = farmHandName;
                }
            }
            else
            {
                List<string> Players = new List<string>();
                Players.Add(playerName);
                for (int i = 0; i < farmHandList.Count; i++)
                {
                    string farmHandName = farmHandList[i].SelectSingleNode("name").InnerText;
                    if (farmHandName != "")
                        Players.Add(farmHandName);
                }
                saveFileInfo.Players = Players.ToArray();
            }

            //找出存档的游玩时长
            saveFileInfo.PlayTime = GetPlayTimeByInteger(int.Parse(playerNode.SelectSingleNode("millisecondsPlayed").InnerText));

            //找出农场的名字
            saveFileInfo.FarmName = playerNode.SelectSingleNode("farmName").InnerText;

            //string subNodes = AllChildNodeTreeView(xmlData);
            //XmlNode playerNode = xmlData.SelectSingleNode("SaveGame/player");
            //XmlNodeList list = playerNode.ChildNodes;
            //for (int i = 0; i < list.Count; i++)
            //{
            //    subNodes += list.Item(i).Name + "\r\n";
            //}
            return saveFileInfo;
        }

        public static SaveFileInfo ReadSaveFileFromXML(string xmlContent, bool keepEmptyPlayer = true)
        {
            SaveFileInfo saveFileInfo = new SaveFileInfo();
            XmlDocument xmlData = new XmlDocument();
            xmlData.LoadXml(xmlContent);
            //找出存档游玩的日期
            XmlNode playerNode = xmlData.SelectSingleNode("SaveGame/player");
            string day = playerNode.SelectSingleNode("dayOfMonthForSaveGame").InnerText;
            string season = playerNode.SelectSingleNode("seasonForSaveGame").InnerText;
            string year = playerNode.SelectSingleNode("yearForSaveGame").InnerText;
            saveFileInfo.GameDate = "第" + year + "年 " + GetSeasonByIndex(int.Parse(season)) + " 第" + day + "天";

            //找出存档中的玩家
            string playerName = playerNode.SelectSingleNode("name").InnerText;
            XmlNodeList farmHandList = xmlData.SelectNodes("SaveGame/locations/GameLocation/buildings/Building/indoors/farmhand");
            if (keepEmptyPlayer)
            {
                saveFileInfo.Players = new string[farmHandList.Count + 1];
                saveFileInfo.Players[0] = playerName;
                for (int i = 1; i < saveFileInfo.Players.Length; i++)
                {
                    string farmHandName = farmHandList.Item(i - 1).SelectSingleNode("name").InnerText;
                    if (farmHandName == "")
                        farmHandName = "[空位]";
                    saveFileInfo.Players[i] = farmHandName;
                }
            }
            else
            {
                List<string> Players = new List<string>();
                Players.Add(playerName);
                for (int i = 0; i < farmHandList.Count; i++)
                {
                    string farmHandName = farmHandList[i].SelectSingleNode("name").InnerText;
                    if (farmHandName != "")
                        Players.Add(farmHandName);
                }
                saveFileInfo.Players = Players.ToArray();
            }

            //找出存档的游玩时长
            saveFileInfo.PlayTime = GetPlayTimeByInteger(int.Parse(playerNode.SelectSingleNode("millisecondsPlayed").InnerText));

            //找出农场的名字
            saveFileInfo.FarmName = playerNode.SelectSingleNode("farmName").InnerText;

            //string subNodes = AllChildNodeTreeView(xmlData);
            //XmlNode playerNode = xmlData.SelectSingleNode("SaveGame/player");
            //XmlNodeList list = playerNode.ChildNodes;
            //for (int i = 0; i < list.Count; i++)
            //{
            //    subNodes += list.Item(i).Name + "\r\n";
            //}
            return saveFileInfo;
        }

        public static void BackupSaveFileToPath(string saveFileName, string backupPath)
        {
            string path = GetSaveFilePathByName(saveFileName);
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(backupPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                }
                File.Copy(path, backupPath, true);
            }
            catch (Exception e)
            {
                MessageBox.Show("备份本地存档过程中出错，错误为：\r\n" + e.Message);
            }
        }

        public static void MoveSaveFileToSaveFolder(string saveFileName, string saveFilePath)
        {
            try
            {
                File.Copy(saveFilePath, GetSaveFilePathByName(saveFileName), true);
            }
            catch (Exception e)
            {
                MessageBox.Show("覆盖本地存档过程中出错，错误为：\r\n" + e.Message);
            }
        }

        public static bool SwapCharacter(string saveFileName, string farmhandName)
        {
            try
            {
                string content = File.ReadAllText(GetSaveFilePathByName(saveFileName));
                XmlDocument xml = SwapCharacterByContent(content, farmhandName);
                if(xml != null)
                {
                    //保存替换完毕的存档文件
                    try
                    {
                        xml.Save(GetSaveFilePathByName(saveFileName));
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("尝试保存修改后的存档的过程中出现问题了" + e.Message);
                    }

                    //替换SaveGameInfo文件内的数据
                    try
                    {
                        content = File.ReadAllText(GetSaveGameInfoPathByName(saveFileName));
                        XmlDocument saveFileInfoXml = new XmlDocument();
                        saveFileInfoXml.LoadXml(content);
                        saveFileInfoXml.SelectSingleNode("Farmer").InnerXml = xml.SelectSingleNode("SaveGame/player").InnerXml;
                        saveFileInfoXml.Save(GetSaveGameInfoPathByName(saveFileName));
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show("替换SaveGameInfo的时候出现问题了，错误为：" + e.Message);
                    }
                    return true;
                }

                return false;
            }
            catch(Exception e)
            {
                MessageBox.Show("读取本地存档过程中遇到问题，错误为：" + e.Message);
            }
            return false;
        }

        public static XmlDocument SwapCharacterByContent(string XmlContent, string farmhandName)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(XmlContent);
            //找出主玩家节点
            XmlNode MainPlayerNode = xml.SelectSingleNode("SaveGame/player");

            //找出要进行替换的副玩家节点
            XmlNodeList FarmhandList = xml.SelectNodes("SaveGame/locations/GameLocation/buildings/Building/indoors/farmhand");
            XmlNode FarmHandNode = null;
            for (int i = 0; i < FarmhandList.Count; i++)
            {
                if(FarmhandList[i].SelectSingleNode("name").InnerText == farmhandName)
                {
                    FarmHandNode = FarmhandList[i];
                }
            }
            if (FarmHandNode != null)
            {
                //TODO: 替换角色
                try
                {
                    //替换二者的房屋等级。因为当副玩家变为主玩家时，其房子也会从联机玩家的房子变为主玩家的房子。
                    //不再是原本自己的房子之后，如果房子等级不一致的话，会导致房屋内的家具出现Bug。
                    SwapContentBetweenXmlNode(MainPlayerNode, FarmHandNode, "houseUpgradeLevel");

                    //同步二者的农场时间(年月季节那些)为主玩家的时间。
                    //不同步的话，时间会变为副玩家最后一次上线的时间，会发生时光倒流。
                    SyncContentBetweenXmlNode(MainPlayerNode, FarmHandNode, "dayOfMonthForSaveGame");
                    SyncContentBetweenXmlNode(MainPlayerNode, FarmHandNode, "seasonForSaveGame");
                    SyncContentBetweenXmlNode(MainPlayerNode, FarmHandNode, "yearForSaveGame");

                    //替换二者家的位置。因为主副玩家改变，玩家的房子已经发生了对调，
                    //所以也需要对调二者对于家的具体坐标的定位。
                    SwapContentBetweenXmlNode(MainPlayerNode, FarmHandNode, "homeLocation");

                    //替换二者收到过的邮件。用于解决许多需要收到特定邮件才能解锁的事件，比如开矿车和修桥梁。
                    SwapContentBetweenXmlNode(MainPlayerNode, FarmHandNode, "mailReceived");

                    //同步二者的游戏时长为主玩家的时长，不然存档时间会变为副玩家玩的时长，存档时间会变短。
                    SyncContentBetweenXmlNode(MainPlayerNode, FarmHandNode, "millisecondsPlayed");

                    //替换二者触发过的事件，避免一些只有主机才会触发的动画事件被重复触发。
                    SwapContentBetweenXmlNode(MainPlayerNode, FarmHandNode, "eventsSeen");

                    //彻底对调两个节点
                    SwapAllContentBetweemNode(MainPlayerNode, FarmHandNode);

                    return xml;
                }
                catch (Exception e)
                {
                    MessageBox.Show("在替换存档的过程中发生问题，错误为：" + e.Message);
                    return null;
                }
            }
            return null;
        }

        public static void SyncContentBetweenXmlNode(XmlNode A, XmlNode B, string NodePath)
        {
            B.SelectSingleNode(NodePath).InnerText = A.SelectSingleNode(NodePath).InnerText;
        }

        public static void SwapContentBetweenXmlNode(XmlNode A, XmlNode B, string NodePath)
        {
            string tempA = A.SelectSingleNode(NodePath).InnerText;
            A.SelectSingleNode(NodePath).InnerText = B.SelectSingleNode(NodePath).InnerText;
            B.SelectSingleNode(NodePath).InnerText = tempA;
        }

        public static void SwapAllContentBetweemNode(XmlNode aNode, XmlNode bNode)
        {
            string a_InnerText= aNode.InnerText;
            string a_InnerXML = aNode.InnerXml;

            aNode.InnerText = bNode.InnerText;
            aNode.InnerXml = bNode.InnerXml;
            bNode.InnerText = a_InnerText;
            bNode.InnerXml = a_InnerXML;
        }

        static int second, min, hour;
        static float secondRaw, minRaw, hourRaw;
        public static string GetPlayTimeByInteger(int miliSecond)
        {
            second = miliSecond / 1000;
            hourRaw = second / 3600f;
            hour = (int)Math.Floor(hourRaw);
            minRaw = (hourRaw - hour) * 60f;
            min = (int)Math.Floor(minRaw);
            secondRaw = (minRaw - min) * 60f;
            second = (int)Math.Floor(secondRaw);

            return hour + "小时" + min + "分" + second + "秒";
        }

        public static string GetSeasonByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return "春季";
                case 1:
                    return "夏季";
                case 2:
                    return "秋季";
                case 3:
                    return "冬季";
                default:
                    return "未知季节";
            }
        }

        public static ulong GetSaveFileTimeStampByPath(string saveFilePath)
        {
            try
            {
                FileInfo fi = new FileInfo(saveFilePath);
                string timeStamp = new DateTimeOffset(fi.LastWriteTimeUtc).ToUnixTimeMilliseconds().ToString();
                if (ulong.TryParse(timeStamp, out ulong result))
                    return result;
                else
                    return 0;
            }
            catch(Exception e)
            {
                MessageBox.Show("获取本地存档修改时间的时候发生问题，错误为" + e.Message);
                return 0;
            }
        }

        public static ulong GetSaveFileTimeStamp(string saveFileName)
        {
            try
            {
                string path = GetSaveFilePathByName(saveFileName);
                FileInfo fi = new FileInfo(path);
                string timeStamp = new DateTimeOffset(fi.LastWriteTimeUtc).ToUnixTimeMilliseconds().ToString();
                if (ulong.TryParse(timeStamp, out ulong result))
                    return result;
                else
                    return 0;
            }
            catch (Exception e)
            {
                MessageBox.Show("获取本地存档修改时间的时候发生问题，错误为" + e.Message);
                return 0;
            }
        }

        public static string GetTimeStamp()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
        }

        public static string AllChildNodeTreeView(XmlNode node, int level = 0)
        {
            string Tree = "";
            XmlNodeList list = node.ChildNodes;
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < level; j++)
                {
                    Tree += "    ";
                }
                Tree += list[i].Name + "\r\n";
                Tree += AllChildNodeTreeView(list[i], level + 1);
            }
            return Tree;
        }

        public static string FindChildNodeByName(XmlNode node, string name)
        {
            string CurrentPath = "";
            if(node.Name == name)
            {
                return node.Name;
            }
            
            XmlNodeList list = node.ChildNodes;
            if(list.Count != 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Name == name)
                        return list[i].Name;

                    string val = FindChildNodeByName(list[i], name);
                    if (val != "")
                    {
                        CurrentPath = val;
                        return list[i].Name + "/" + val;
                    }
                }
            }
            else
            {
                return "";
            }

            return CurrentPath;
        }
    }
}
