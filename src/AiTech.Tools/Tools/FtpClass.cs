﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace AiTech.Tools
{
    public class cFTPEventHandlerArgs : EventArgs
    {
        public string FileName;
        public long TotalBytes;
        public long CompletedBytes;
    }


    public class FtpClass
    {
        private readonly NetworkCredential _Credential;
        private FileSystemWatcher _FolderWatcher;
        private readonly string _FTPServer;
        private readonly ICollection<string> ListOfFiles = new List<string>();

        /// <summary>
        /// Use in MonitorFolder. FTP Destination Path
        /// </summary>
        private string _FTPDefaultPath;

        public string LastError { get; set; }
        public event EventHandler<cFTPEventHandlerArgs> Progress;
        public event EventHandler<cFTPEventHandlerArgs> Completed;

        public FtpClass(string ServerIP, NetworkCredential credential)
        {
            _FTPServer = ServerIP;
            _Credential = credential;

        }


        public bool UploadFile(string filePath, string FTPAddress)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            FtpWebRequest requestFTP = (FtpWebRequest)WebRequest.Create(new Uri(new Uri("ftp://" + _FTPServer), FTPAddress + fileInfo.Name));
            // Credentials
            requestFTP.Credentials = _Credential;
            requestFTP.KeepAlive = false;
            requestFTP.Method = WebRequestMethods.Ftp.UploadFile;
            requestFTP.UseBinary = true;
            requestFTP.ContentLength = fileInfo.Length;

            // The buffer size is set to 2kb, breaking down into 2kb and uploading
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            long BytesUploaded = 0;
            try
            {
                FileStream fs = fileInfo.OpenRead();

                Stream strm = requestFTP.GetRequestStream();

                // Read from the file stream 2kb at a time
                var contentLen = fs.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                    BytesUploaded += contentLen;
                    OnProgress(new cFTPEventHandlerArgs() { TotalBytes = requestFTP.ContentLength, CompletedBytes = BytesUploaded });
                }
                // Close the file stream and the Request Stream
                strm.Close();
                fs.Close();

                OnCompleted(new cFTPEventHandlerArgs() { FileName = fileInfo.Name });
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public Boolean DownloadFile(string filePath, string fileName, string FTPAddress, string username, string password)
        {
            try
            {
                FileStream outputStream = new FileStream(filePath + "\\" + fileName, FileMode.Create);
                var requestFTP = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + FTPAddress + "/" + fileName));
                requestFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                requestFTP.UseBinary = true;
                requestFTP.Credentials = _Credential;
                var response = (FtpWebResponse)requestFTP.GetResponse();
                var ftpStream = response.GetResponseStream();
                var bufferSize = 2048;
                byte[] buffer = new byte[bufferSize];

                if (ftpStream != null)
                {
                    var readCount = ftpStream.Read(buffer, 0, bufferSize);
                    while (readCount > 0)
                    {
                        outputStream.Write(buffer, 0, readCount);
                        readCount = ftpStream.Read(buffer, 0, bufferSize);
                    }
                }

                if (ftpStream != null) ftpStream.Close();
                outputStream.Close();
                response.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        protected virtual void OnProgress(cFTPEventHandlerArgs e)
        {
            Progress?.Invoke(this, e);
        }

        protected virtual void OnCompleted(cFTPEventHandlerArgs e)
        {
            Completed?.Invoke(this, e);
        }

        /// <summary>
        /// Watch Folder for any created file. then Automatically Uploads it.
        /// </summary>
        /// <param name="FolderPath">Folder to Watch</param>
        /// <param name="FTPServerPath">FTP Path to where the file will be uploaded</param>
        public void MonitorFolder(string FolderPath, string FTPServerPath)
        {
            _FolderWatcher = new FileSystemWatcher(FolderPath, "*.jpg")
            {
                EnableRaisingEvents = true,
                //SynchronizingObject = (ISynchronizeInvoke) this,
                NotifyFilter = NotifyFilters.FileName
            };
            _FolderWatcher.Created += _FolderWatcher_Created;
            _FolderWatcher.Changed += _FolderWatcher_Changed;
            _FTPDefaultPath = FTPServerPath;
        }

        private void _FolderWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            //Console.WriteLine(e.Name);

            //var filename = ListOfFiles.FirstOrDefault(f => f == e.FullPath) ;
            //if (String.IsNullOrEmpty(filename))
            //{
            //    UploadFile(e.FullPath, _FTPDefaultPath);
            //}
        }

        private void _FolderWatcher_Created(object sender, FileSystemEventArgs e)
        {
            //throw new NotImplementedException();
            Console.WriteLine(e.Name);

            while (true)
            {
                try
                {
                    FileInfo info = new FileInfo(e.FullPath);
                    var stream = File.Open(e.FullPath, FileMode.Open);
                    stream.Close();
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(200);
                }
            }

            UploadFile(e.FullPath, _FTPDefaultPath);
        }
    }
}

