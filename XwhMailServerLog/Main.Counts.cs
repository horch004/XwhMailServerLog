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
        //****************************************************************************************************
        private void buttonSessionsCount_Click(object sender, EventArgs e)
        {
            if (countsWorker.IsBusy)
            {
                countsWorker.CancelAsync();
                buttonSessionsCount.Text = "Canceling...";
                return;
            }

            if (!KnownLogType())
                return;

            progressBar1.Value = 0;
            textResult.Text = string.Empty;
            outputSB = new StringBuilder();
            TotalSessions = 0;
            ProgressText = "";

            countsWorker.RunWorkerAsync();
            buttonSessionsCount.Text = "Cancel";
        }

        //****************************************************************************************************
        private void countsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string logFilePath = textBoxFile.Text.Trim();
            
            FileInfo info = new FileInfo(logFilePath);
            long FileSize = info.Length;
            long readPosition = 0;
            int LastPercent = 0;


            var lines = File.ReadLines(logFilePath);
            int index = 0;

            Dictionary<string, int> users = new Dictionary<string, int>();

            ProgressText = "Enumerating sessions: ...";
            foreach (string line in lines)
            {
                if ((countsWorker.CancellationPending == true))
                {
                    e.Cancel = true;
                    return;
                }

                index++;
                readPosition += line.Length;
                int Percent = (int)(readPosition * barMax / FileSize);
                if (Percent != LastPercent)
                {
                    countsWorker.ReportProgress(Percent);
                    LastPercent = Percent;
                }

                Match mSession = Regex.Match(line, @"^(""IMAPD"".*?RECEIVED:\s(?<USER>[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@.*?)\s|""POP3D"".*?RECEIVED:\sUSER\s(?<USER>.*?@.*?)"")");
                if (mSession.Success)
                {
                    TotalSessions++;
                    ProgressText = $"Enumerating sessions: {TotalSessions}";

                    string user = mSession.Groups["USER"].ToString().ToLower();
                    if (!users.ContainsKey(user))
                        users.Add(user, 0);
                    else
                        users[user]++;
                }
            }

            if (users.Count == 0)
            {
                outputSB.Clear();
                outputSB.Append("Nothing found!");
                return;
            }

            outputSB.Append($"{users.Count.ToString().PadLeft(10, ' ')} : Total users found\r\n");
            outputSB.Append($"{TotalSessions.ToString().PadLeft(10, ' ')} : Total sessions found\r\n");
            outputSB.Append($"\r\n");

            foreach (var user in users.OrderByDescending(o => o.Value))
            {
                outputSB.Append($"{user.Value.ToString().PadLeft(10, ' ')} : {user.Key}\r\n");
            }

            countsWorker.ReportProgress(barMax);
            ProgressText = "DONE";
        }

    }
}