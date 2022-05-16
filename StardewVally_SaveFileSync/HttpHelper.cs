using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StardewVally_SaveFileSync
{
    public static class HttpHelper
    {
        public static readonly string URI = GlobalConfig.current.URL;
        public static int timeOut = 5000;

        static Action<bool, string> GetSaveFileCallback;
        static string GetSaveFileName = "";
        static bool GettingSaveFileByName = false;
        public static void HttpGetSaveFileByName(string FileName, Action<bool, string> callback)
        {
            if(!GettingSaveFileByName)
            {
                GetSaveFileName = FileName;
                GetSaveFileCallback = callback;
                Task task = new Task(GetSaveFileByNameTask);
                task.Start();
            }
        }

        static void GetSaveFileByNameTask()
        {
            lock (GetSaveFileCallback)
            {
                try
                {
                    var webRequest = (HttpWebRequest)WebRequest.Create(URI + "StardewVally_GetCloundSaveFileByName/" + GetSaveFileName);
                    webRequest.Method = "GET";
                    webRequest.Timeout = timeOut;

                    var httpWebResponse = (HttpWebResponse)webRequest.GetResponse();

                    string responseContent = "";
                    using (var httpStreamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                    {
                        responseContent = httpStreamReader.ReadToEnd();
                    }
                    if(responseContent != "FindSaveFileFailure")
                    {
                        GetSaveFileCallback(true, responseContent);
                    }
                    else
                    {
                        GetSaveFileCallback(false, "");
                        MessageBox.Show("无法在云端找到存档文件");
                    }
                }
                catch (Exception e)
                {
                    GetSaveFileCallback(false, "");
                    MessageBox.Show(e.Message);
                    throw;
                }
            }
        }

        static Action<bool, string[]> SaveFileListCallback;
        static bool isGettingSaveFileList = false;
        public static void HttpGetSaveFileList(Action<bool, string[]> callback)
        {
            if(!isGettingSaveFileList)
            {
                SaveFileListCallback = callback;
                Task task = new Task(HttpGetSaveFileListTask);
                task.Start();
            }
        }

        static void HttpGetSaveFileListTask()
        {
            isGettingSaveFileList = true;
            lock (SaveFileListCallback)
            {
                try
                {
                    var webRequest = (HttpWebRequest)WebRequest.Create(URI + "StardewVally_GetCloundSaveFileList");

                    webRequest.Method = "GET";
                    webRequest.Timeout = timeOut;

                    var httpWebResponse = (HttpWebResponse)webRequest.GetResponse();

                    string responseContent = "";
                    using (var httpStreamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                    {
                        responseContent = httpStreamReader.ReadToEnd();
                    }

                    List<string> saveFilesList = responseContent.Split("/").ToList();
                    for (int i = saveFilesList.Count - 1; i >= 0; --i)
                    {
                        if(saveFilesList[i] == string.Empty)
                        {
                            saveFilesList.RemoveAt(i);
                        }
                    }
                    SaveFileListCallback(true, saveFilesList.ToArray());

                    isGettingSaveFileList = false;
                }
                catch (Exception e)
                {
                    SaveFileListCallback(false, null);
                    MessageBox.Show(e.Message);
                    isGettingSaveFileList = false;
                }
            }
        }

        /// <summary>
        /// HTTP文件下载
        /// </summary>
        /// <param name="fileName">要下载的文件名</param>
        /// <returns></returns>
        public static string HttpGetSaveFile(string fileName)
        {
            string data = "";
            string url = URI + "StardewVally_GetCloundSaveFile";
            var webRequest = (HttpWebRequest)WebRequest.Create(url);

            // 设置属性
            webRequest.Method = "GET";
            webRequest.Timeout = timeOut;

            var header = fileName;
            var headerbytes = Encoding.UTF8.GetBytes(header);

            var requestStream = webRequest.GetRequestStream();
            requestStream.Write(headerbytes, 0, headerbytes.Length);
            requestStream.Close();

            return data;
        }

        static bool Posting = false;
        static string filePath = "";
        static Action<string> postCallback;
        /// <summary>
        /// HTTP文件上传
        /// </summary>
        /// <param name="filePath">文件上传的本地路径</param>
        /// <returns></returns>
        public static void HttpPostFile(string filePath, Action<string> callback)
        {
            if(!Posting)
            {
                postCallback = callback;
                HttpHelper.filePath = filePath;
                Task task = new Task(HttpPostFileTask);
                task.Start();
            }
        }

        static void HttpPostFileTask()
        {
            Posting = true;
            lock(filePath)
            {
                lock(postCallback)
                {
                    FileStream fileStream = null;
                    try
                    {
                        string url = URI + "StardewVally_SaveFileUpload/" + SaveFileOperation.GetSaveFileTimeStampByPath(filePath);
                        string responseContent;
                        var memStream = new MemoryStream();
                        var webRequest = (HttpWebRequest)WebRequest.Create(url);
                        // 边界符
                        var boundary = "---------------" + DateTime.Now.Ticks.ToString("x");
                        // 边界符
                        //var beginBoundary = Encoding.ASCII.GetBytes("--" + boundary + "\r\n");
                        fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                        // 最后的结束符
                        //var endBoundary = Encoding.ASCII.GetBytes("--" + boundary + "--\r\n");

                        // 设置属性
                        webRequest.Method = "POST";
                        webRequest.Timeout = timeOut;
                        webRequest.ContentType = "multipart/form-data; boundary=" + boundary;

                        // 写入文件----------1.--------------
                        var header = "-------FileName = " + Path.GetFileNameWithoutExtension(filePath) + "-------";

                        var headerbytes = Encoding.UTF8.GetBytes(header);
                        memStream.Write(headerbytes, 0, headerbytes.Length);

                        var buffer = new byte[1024];
                        int bytesRead; // =0

                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            memStream.Write(buffer, 0, bytesRead);
                        }

                        // 写入最后的结束边界符------------ 3.-------------------------
                        //memStream.Write(endBoundary, 0, endBoundary.Length);

                        webRequest.ContentLength = memStream.Length;

                        var requestStream = webRequest.GetRequestStream();

                        memStream.Position = 0;
                        var tempBuffer = new byte[memStream.Length];
                        memStream.Read(tempBuffer, 0, tempBuffer.Length);
                        memStream.Close();

                        requestStream.Write(tempBuffer, 0, tempBuffer.Length);
                        requestStream.Close();

                        //响应 ------------------- 4.-----------------------------------
                        var httpWebResponse = (HttpWebResponse)webRequest.GetResponse();

                        using (var httpStreamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                        {
                            responseContent = httpStreamReader.ReadToEnd();
                        }

                        fileStream.Close();
                        httpWebResponse.Close();
                        webRequest.Abort();

                        postCallback(responseContent);
                        Posting = false;
                    }
                    catch (Exception e)
                    {
                        if(fileStream != null)
                            fileStream.Close();
                        postCallback(e.Message);
                        MessageBox.Show(e.Message);
                        Posting = false;
                    }
                }
            }
        }

        static bool Pinging = false;
        static Action<bool, ulong> PingingCallback;
        public static bool ServerPing(Action<bool, ulong> callback)
        {
            if (Pinging)
            {
                return false;
            }
            else
            {
                PingingCallback = callback;
                Task task = new Task(PingTask);
                task.Start();
                return true;
            }
        }

        static void PingTask()
        {
            lock(PingingCallback)
            {
                Pinging = true;

                try
                {
                    var webRequest = (HttpWebRequest)WebRequest.Create(URI + "StardewVally_ServerPing/" + GlobalConfig.current.SaveFileNameForAutoUpload);

                    webRequest.Method = "GET";
                    webRequest.Timeout = timeOut;

                    var httpWebResponse = (HttpWebResponse)webRequest.GetResponse();

                    string responseContent = "";
                    using (var httpStreamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                    {
                        responseContent = httpStreamReader.ReadToEnd();
                    }

                    string[] PingMsg = responseContent.Split("/");
                    if (PingMsg[0] == "Ping_Success")
                    {
                        if(ulong.TryParse(PingMsg[1], out ulong result))
                        {
                            PingingCallback(true, result);
                        }
                        else
                        {
                            PingingCallback(true, 0);
                        }
                    }
                    else
                    {
                        PingingCallback(false, 0);
                    }

                    Pinging = false;
                }
                catch (Exception e)
                {
                    PingingCallback(false, 0);
                    MessageBox.Show(e.Message);
                    Pinging = false;
                }

            }
        }
    }
}
