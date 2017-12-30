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
using Schemas;

namespace osrepodbmgr.Core
{
    public static class Context
    {
        public delegate void UnarChangeStatusDelegate();

        public static       List<string>                 Files;
        public static       List<string>                 Folders;
        public static       List<string>                 Symlinks;
        public static       Dictionary<string, DbOsFile> Hashes;
        public static       Dictionary<string, DbFolder> FoldersDict;
        public static       Dictionary<string, string>   SymlinksDict;
        public static       string                       Path;
        public static       DbEntry                      DbInfo;
        public static       bool                         UnarUsable;
        public static       string                       TmpFolder;
        public static       long                         NoFilesInArchive;
        public static       string                       ArchiveFormat;
        public static       Process                      UnarProcess;
        public static       bool                         UnzipWithUnAr;
        public static       string                       SelectedFile;
        public static       OpticalDiscType              WorkingDisc;
        public static       BlockMediaType               WorkingDisk;
        public static       CICMMetadataType             Metadata;
        public static       bool                         UserExtracting;
        public static       bool                         UsableDotNetZip;
        public static       string                       ClamdVersion;
        public static       bool                         VirusTotalEnabled;
        public static event UnarChangeStatusDelegate     UnarChangeStatus;

        public static void CheckUnar()
        {
            Workers.FinishedWithText += CheckUnarFinished;
            Workers.Failed           += CheckUnarFailed;

            Thread thdCheckUnar = new Thread(Workers.CheckUnar);
            thdCheckUnar.Start();
        }

        static void CheckUnarFinished(string text)
        {
            UnarUsable = true;
            UnarChangeStatus?.Invoke();
            Workers.FinishedWithText -= CheckUnarFinished;
            Workers.Failed           -= CheckUnarFailed;
        }

        static void CheckUnarFailed(string text)
        {
            UnarUsable = false;
            UnarChangeStatus?.Invoke();
            Workers.FinishedWithText -= CheckUnarFinished;
            Workers.Failed           -= CheckUnarFailed;
        }
    }
}