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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Gtk;

namespace osrepodbmgr
{
    static class MainClass
    {
        public static List<string> files;
        public static Dictionary<string, string> hashes;
        public static string path;
        public static DBEntry dbInfo;
        public static bool unarUsable;
        public delegate void UnarChangeStatusDelegate();
        public static event UnarChangeStatusDelegate UnarChangeStatus;
        public static string tmpFolder;
        public static long noFilesInArchive;
        public static string archiveFormat;
        public static Process unarProcess;
        public static bool copyArchive;

        public static void Main(string[] args)
        {
            Settings.LoadSettings();
            CheckUnar();
            Application.Init();
            MainWindow win = new MainWindow();
            win.Show();
            Application.Run();
        }

        public static void CheckUnar()
        {
            Core.Finished += CheckUnarFinished;
            Core.Failed += CheckUnarFailed;

            Thread thdCheckUnar = new Thread(Core.CheckUnar);
            thdCheckUnar.Start();
        }

        static void CheckUnarFinished()
        {
            unarUsable = true;
            if(UnarChangeStatus != null)
                UnarChangeStatus();
            Core.Finished -= CheckUnarFinished;
            Core.Failed -= CheckUnarFailed;
        }

        static void CheckUnarFailed(string text)
        {
            unarUsable = false;
            if(UnarChangeStatus != null)
                UnarChangeStatus();
            Core.Finished -= CheckUnarFinished;
            Core.Failed -= CheckUnarFailed;
        }
    }
}
