//
//  Author:
//    Natalia Portillo claunia@claunia.com
//
//  Copyright (c) 2017, © Claunia.com
//
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        static DBCore dbCore;

        static int zipCounter;
        static string zipCurrentEntryName;

        static string stringify(byte[] hash)
        {
            StringBuilder hashOutput = new StringBuilder();

            for(int i = 0; i < hash.Length; i++)
            {
                hashOutput.Append(hash[i].ToString("x2"));
            }

            return hashOutput.ToString();
        }


        public static void CheckUnar()
        {
            if(string.IsNullOrWhiteSpace(Settings.Current.UnArchiverPath))
            {
                if(Failed != null)
                    Failed("unar path is not set.");
                return;
            }

            string unarFolder = Path.GetDirectoryName(Settings.Current.UnArchiverPath);
            string extension = Path.GetExtension(Settings.Current.UnArchiverPath);
            string unarfilename = Path.GetFileNameWithoutExtension(Settings.Current.UnArchiverPath);
            string lsarfilename = unarfilename.Replace("unar", "lsar");
            string unarPath = Path.Combine(unarFolder, unarfilename + extension);
            string lsarPath = Path.Combine(unarFolder, lsarfilename + extension);

            if(!File.Exists(unarPath))
            {
                if(Failed != null)
                    Failed(string.Format("Cannot find unar executable at {0}.", unarPath));
                return;
            }

            if(!File.Exists(lsarPath))
            {
                if(Failed != null)
                    Failed("Cannot find unar executable.");
                return;
            }

            string unarOut, lsarOut;

            try
            {
                Process unarProcess = new Process();
                unarProcess.StartInfo.FileName = unarPath;
                unarProcess.StartInfo.CreateNoWindow = true;
                unarProcess.StartInfo.RedirectStandardOutput = true;
                unarProcess.StartInfo.UseShellExecute = false;
                unarProcess.Start();
                unarProcess.WaitForExit();
                unarOut = unarProcess.StandardOutput.ReadToEnd();
            }
            catch
            {
                if(Failed != null)
                    Failed("Cannot run unar.");
                return;
            }

            try
            {
                Process lsarProcess = new Process();
                lsarProcess.StartInfo.FileName = lsarPath;
                lsarProcess.StartInfo.CreateNoWindow = true;
                lsarProcess.StartInfo.RedirectStandardOutput = true;
                lsarProcess.StartInfo.UseShellExecute = false;
                lsarProcess.Start();
                lsarProcess.WaitForExit();
                lsarOut = lsarProcess.StandardOutput.ReadToEnd();
            }
            catch
            {
                if(Failed != null)
                    Failed("Cannot run lsar.");
                return;
            }

            if(!unarOut.StartsWith("unar ", StringComparison.CurrentCulture))
            {
                if(Failed != null)
                    Failed("Not the correct unar executable");
                return;
            }

            if(!lsarOut.StartsWith("lsar ", StringComparison.CurrentCulture))
            {
                if(Failed != null)
                    Failed("Not the correct unar executable");
                return;
            }

            Process versionProcess = new Process();
            versionProcess.StartInfo.FileName = unarPath;
            versionProcess.StartInfo.CreateNoWindow = true;
            versionProcess.StartInfo.RedirectStandardOutput = true;
            versionProcess.StartInfo.UseShellExecute = false;
            versionProcess.StartInfo.Arguments = "-v";
            versionProcess.Start();
            versionProcess.WaitForExit();

            if(FinishedWithText != null)
                FinishedWithText(versionProcess.StandardOutput.ReadToEnd().TrimEnd(new char[] { '\n' }));
        }
    }
}
