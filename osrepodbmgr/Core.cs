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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace osrepodbmgr
{
    public static class Core
    {
        // Sets a 128Kbyte buffer
        const long bufferSize = 131072;

        public delegate void UpdateProgressDelegate(string text, string inner, long current, long maximum);
        public delegate void UpdateProgress2Delegate(string text, string inner, long current, long maximum);
        public delegate void FailedDelegate(string text);
        public delegate void FinishedWithoutErrorDelegate();
        public delegate void AddEntryDelegate(string filename, string hash, bool known);

        public static event UpdateProgressDelegate UpdateProgress;
        public static event UpdateProgress2Delegate UpdateProgress2;
        public static event FailedDelegate Failed;
        public static event FinishedWithoutErrorDelegate Finished;
        public static event AddEntryDelegate AddEntry;

        static DBCore dbCore;

        public static void FindFiles()
        {
            if(string.IsNullOrEmpty(MainClass.path))
            {
                if(Failed != null)
                    Failed("Path is null or empty");
            }

            if(!Directory.Exists(MainClass.path))
            {
                if(Failed != null)
                    Failed("Directory not found");
            }

            try
            {
                MainClass.files = new List<string>(Directory.EnumerateFiles(MainClass.path, "*", SearchOption.AllDirectories));
                MainClass.files.Sort();
                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(System.Diagnostics.Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
        }

        public static void HashFiles()
        {
            try
            {
                MainClass.hashes = new Dictionary<string, string>();
                long counter = 1;
                foreach(string file in MainClass.files)
                {
                    string relpath = file.Substring(MainClass.path.Length + 1);
                    if(UpdateProgress != null)
                        UpdateProgress(string.Format("Hashing file {0} of {1}", counter, MainClass.files.Count), null, counter, MainClass.files.Count);
                    FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

                    byte[] dataBuffer = new byte[bufferSize];
                    SHA256Context sha256Context = new SHA256Context();
                    sha256Context.Init();

                    if(fileStream.Length > bufferSize)
                    {
                        long offset;
                        long remainder = fileStream.Length % bufferSize;
                        for(offset = 0; offset < (fileStream.Length - remainder); offset += (int)bufferSize)
                        {
                            if(UpdateProgress2 != null)
                                UpdateProgress2(string.Format("{0:P}", offset / (double)fileStream.Length), relpath, offset, fileStream.Length);
                            dataBuffer = new byte[bufferSize];
                            fileStream.Read(dataBuffer, 0, (int)bufferSize);
                            sha256Context.Update(dataBuffer);
                        }
                        if(UpdateProgress2 != null)
                            UpdateProgress2(string.Format("{0:P}", offset / (double)fileStream.Length), relpath, offset, fileStream.Length);
                        dataBuffer = new byte[remainder];
                        fileStream.Read(dataBuffer, 0, (int)remainder);
                        sha256Context.Update(dataBuffer);
                    }
                    else {
                        if(UpdateProgress2 != null)
                            UpdateProgress2(string.Format("{0:P}", 0 / (double)fileStream.Length), relpath, 0, fileStream.Length);
                        dataBuffer = new byte[fileStream.Length];
                        fileStream.Read(dataBuffer, 0, (int)fileStream.Length);
                        sha256Context.Update(dataBuffer);
                    }

                    fileStream.Close();
                    string hash = stringify(sha256Context.Final());
                    MainClass.hashes.Add(relpath, hash);
                    counter++;
                }
                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(System.Diagnostics.Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
        }

        public static void CheckDbForFiles()
        {
            try
            {
                long counter = 0;
                foreach(KeyValuePair<string, string> kvp in MainClass.hashes)
                {
                    if(UpdateProgress != null)
                        UpdateProgress(null, "Checking files in database", counter, MainClass.hashes.Count);

                    if(AddEntry != null)
                        AddEntry(kvp.Key, kvp.Value, dbCore.DBEntries.ExistsFile(kvp.Value));

                    counter++;
                }
                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(System.Diagnostics.Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
        }

        public static void AddFilesToDb()
        {
            try
            {
                long counter = 0;
                foreach(KeyValuePair<string, string> kvp in MainClass.hashes)
                {
                    if(UpdateProgress != null)
                        UpdateProgress(null, "Adding files to database", counter, MainClass.hashes.Count);

                    if(!dbCore.DBEntries.ExistsFile(kvp.Value))
                        dbCore.DBEntries.AddFile(kvp.Value);

                    counter++;
                }

                if(UpdateProgress != null)
                    UpdateProgress(null, "Adding OS information", counter, MainClass.hashes.Count);
                dbCore.DBEntries.AddOS(MainClass.dbInfo);

                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(System.Diagnostics.Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
        }

        static string stringify(byte[] hash)
        {
            StringBuilder hashOutput = new StringBuilder();

            for(int i = 0; i < hash.Length; i++)
            {
                hashOutput.Append(hash[i].ToString("x2"));
            }

            return hashOutput.ToString();
        }

        public static void InitDB()
        {
            CloseDB();
            dbCore = null;

            try
            {
                if(string.IsNullOrEmpty(Settings.Current.DatabasePath))
                {
                    if(Failed != null)
                        Failed("No database file specified");
                    return;
                }

                dbCore = new SQLite();
                if(File.Exists(Settings.Current.DatabasePath))
                {
                    if(!dbCore.OpenDB(Settings.Current.DatabasePath, null, null, null))
                    {
                        if(Failed != null)
                            Failed("Could not open database, correct file selected?");
                        dbCore = null;
                        return;
                    }
                }
                else {
                    if(!dbCore.CreateDB(Settings.Current.DatabasePath, null, null, null))
                    {
                        if(Failed != null)
                            Failed("Could not create database, correct file selected?");
                        dbCore = null;
                        return;
                    }
                    if(!dbCore.OpenDB(Settings.Current.DatabasePath, null, null, null))
                    {
                        if(Failed != null)
                            Failed("Could not open database, correct file selected?");
                        dbCore = null;
                        return;
                    }
                }
                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(System.Diagnostics.Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
        }

        public static void CloseDB()
        {
            if(dbCore != null)
                dbCore.CloseDB();
        }
    }
}
