//"SOURCE" ThreadID SessionID "Date Time" "RemoteIP" "SENT/RECEIVED: Conversation text" 

using ProgressBarSample;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace XwhMailServerLog
{
    public partial class Main : Form
    {
        StringBuilder outputSB = null;
        string CurrentVersion = "";
        int barMax = 500;
        int TotalSessions = 0;
        int TotalLines = 0;
        bool IsClosing = false;
        ProgressBarDisplayMode ProgressMode = ProgressBarDisplayMode.CustomText;
        string ProgressText = "";
        string selectedLogType = "";
        
        //****************************************************************************************************
        public Main()
        {
            InitializeComponent();
            CurrentVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(
               System.Reflection.Assembly.GetAssembly(typeof(Main)).Location).FileVersion.ToString();
            Text = $"XwhMailServerLog v{CurrentVersion}";
        }

        //****************************************************************************************************
        private void Main_Load(object sender, EventArgs e)
        {
            progressBar1.Step = 1;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = barMax;
            progressBar1.Value = 0;

            textBoxFile.Text = string.Empty;
            textBoxSearch.Text = string.Empty;

#if DEBUG
            //textBoxFile.Text = @"\\Mac\Home\Desktop\hMailServerLogs\hmailserver_SMTP_2022-05-18.log";
            //textBoxFile.Text = @"\\Mac\Home\Desktop\hMailServerLogs\hmailserver_POP3_2022-05-18.log";
            textBoxFile.Text = @"\\Mac\Home\Desktop\hMailServerLogs\hmailserver_IMAP_2022-05-18.log";
            textBoxSearch.Text = @"info@";
#endif
        }

        //****************************************************************************************************
        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.Multiselect = false;
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "Log files (*.log)|*.log";
            openFileDialog1.ShowDialog();
        }

        //****************************************************************************************************
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            textBoxFile.Text = openFileDialog1.FileName;
            KnownLogType();
        }

        //****************************************************************************************************
        private bool KnownLogType()
        {
            string logFilePath = textBoxFile.Text.Trim();

            if (!File.Exists(logFilePath))
            {
                MessageBox.Show("Selected file not found");
                return false;
            }

            FileInfo info = new FileInfo(logFilePath);
            var lines = File.ReadLines(logFilePath);
            string line = lines.First();
            Match mtype = Regex.Match(line, @"^""(?<TYPE>IMAPD|POP3D|SMTPD)"".*");
            selectedLogType = mtype.Groups["TYPE"].ToString();
            if (selectedLogType == "") selectedLogType = "UNKNOWN";
            labelLogStatus.Text = $"Log file of {selectedLogType} service, {GetFileSize(info.Length)} in size";

            if (selectedLogType == "UNKNOWN")
            {
                MessageBox.Show("Log type is UNKNOWN. Please choose a log file of type IMAP, POP, SMTP");
                return false;
            }

            return true;
        }

        //****************************************************************************************************
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (searchWorker.IsBusy)
            {
                searchWorker.CancelAsync();
                buttonSearch.Text = "Canceling...";
                e.Cancel = true;
                IsClosing = true;
            }
        }

        //****************************************************************************************************
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.paypal.me/maxsnts");
        }

        //****************************************************************************************************
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            progressBar1.VisualMode = ProgressMode;
            progressBar1.CustomText = ProgressText;
        }

        //****************************************************************************************************
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
            {
                textResult.Text = "Canceled!";
                progressBar1.Value = 0;
                if (IsClosing)
                    Close();
            }
            else if (!(e.Error == null))
            {
                textResult.Text = ("Error: " + e.Error.Message);
            }

            else
            {
                textResult.Text = outputSB.ToString();
            }

            buttonSearch.Text = "Search";
            buttonSessionsCount.Text = "Count sessions per user (IMAP or POP)";
            textResult.Show();
        }

        //****************************************************************************************************
        private void timerUI_Tick(object sender, EventArgs e)
        {
            if (searchWorker.IsBusy || countsWorker.IsBusy)
                buttonBrowse.Enabled = false;
            else
                buttonBrowse.Enabled = true;

            buttonSessionsCount.Enabled = !searchWorker.IsBusy;
            buttonSearch.Enabled = !countsWorker.IsBusy;
        }

        //****************************************************************************************************
        private string GetFileSize(double byteCount)
        {
            long m = 1024;
            double m4 = Math.Pow(m, 4);
            double m3 = Math.Pow(m, 3);
            double m2 = Math.Pow(m, 2);
            string size = "0 Bytes";
            if (byteCount >= m4)
                size = String.Format("{0:0.00}", byteCount / m4) + " TB";
            else if (byteCount >= m3)
                size = String.Format("{0:0.00}", byteCount / m3) + " GB";
            else if (byteCount >= m2)
                size = String.Format("{0:0.00}", byteCount / m2) + " MB";
            else if (byteCount >= m)
                size = String.Format("{0:0.00}", byteCount / m) + " KB";
            else if (byteCount < m)
                size = String.Format("{0:0}", byteCount) + " B";

            return size;
        }
    }
}
