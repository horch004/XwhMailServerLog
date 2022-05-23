using ProgressBarSample;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace XwhMailServerLog
{
    public partial class Main : Form
    {
        //****************************************************************************************************
        private void textBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                buttonSearch_Click(sender, e);
        }

        //****************************************************************************************************
        private void buttonSearch_Click(object sender, EventArgs e)
        {
            if (searchWorker.IsBusy)
            {
                searchWorker.CancelAsync();
                buttonSearch.Text = "Canceling...";
                return;
            }

            if (!KnownLogType())
                return;

            string filter = textBoxSearch.Text.Trim();
            
            if (filter == "")
                if (MessageBox.Show("A empty search will return the whole log file.\r\nIf the log is big, it will take time and RAM.\r\n\r\nAre you sure you want to do this?", "Search...", MessageBoxButtons.YesNo) == DialogResult.No)
                    return;
                      
            progressBar1.Value = 0;
            textResult.Text = string.Empty;
            outputSB = new StringBuilder();
            TotalSessions = 0;
            ProgressText = "";

            searchWorker.RunWorkerAsync();
            buttonSearch.Text = "Cancel";
        }

  

        //****************************************************************************************************
        private void searchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string logFilePath = textBoxFile.Text.Trim();
            string filter = textBoxSearch.Text.Trim();
            bool useRegex = checkBoxRegex.Checked;

            FileInfo info = new FileInfo(logFilePath);
            long FileSize = info.Length;
            long readPosition = 0;
            int LastPercent = 0;


            var lines = File.ReadLines(logFilePath);
            int index = 0;

            //find all user sessions!
            List<string> sessions = new List<string>();
            Dictionary<string, StringBuilder> operations = new Dictionary<string, StringBuilder>();

            ProgressText = "Enumerating sessions: ...";
            foreach (string line in lines)
            {
                if ((searchWorker.CancellationPending == true))
                {
                    e.Cancel = true;
                    return;
                }

                index++;
                readPosition += line.Length;
                int Percent = (int)(readPosition * barMax / FileSize);
                if (Percent != LastPercent)
                {
                    searchWorker.ReportProgress(Percent);
                    LastPercent = Percent;
                }

                Match mSession = Regex.Match(line, @"^""(IMAPD|POP3D|SMTPD)""\s\d+\s(?<SESSION>\d+)");
                if (mSession.Success && line.Contains(filter))
                {
                    string session = mSession.Groups["SESSION"].ToString();
                    if (!sessions.Contains(session))
                    {
                        TotalSessions++;
                        ProgressText = $"Enumerating sessions: {TotalSessions}";
                        sessions.Add(session);
                        if (!operations.ContainsKey(session))
                        {
                            operations.Add(session, new StringBuilder());
                        }
                    }
                }
            }

            if (sessions.Count == 0)
            {
                outputSB.Clear();
                outputSB.Append("Nothing found!");
                return;
            }


            readPosition = 0;

            ProgressText = "Collect activity from found sessions: ...";
            foreach (string line in lines)
            {
                if ((searchWorker.CancellationPending == true))
                {
                    e.Cancel = true;
                    return;
                }

                index++;
                readPosition += line.Length;
                int Percent = (int)(readPosition * barMax / FileSize);
                if (Percent != LastPercent)
                {
                    searchWorker.ReportProgress(Percent);
                    LastPercent = Percent;
                }

                Match mSession = Regex.Match(line, @"^""(IMAPD|POP3D|SMTPD)""\s\d+\s(?<SESSION>\d+)");
                if (mSession.Success)
                {
                    string session = mSession.Groups["SESSION"].ToString();
                    if (sessions.Contains(session))
                    {
                        operations[session].Append(line + "\r\n");
                        TotalLines++;
                        ProgressText = $"Collect activity from found sessions: {TotalLines} lines";
                    }
                }
            }

            foreach (var session in operations)
            {
                outputSB.Append($"################################ {session.Key} ################################" + "\r\n");
                outputSB.Append(session.Value.ToString());
            }

            searchWorker.ReportProgress(barMax);
            ProgressText = "DONE";
        }

   
    }
}