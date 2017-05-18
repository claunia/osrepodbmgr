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
using System.Diagnostics;
using System.IO;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        public static void GetAllOSes()
        {
            try
            {
                List<DBEntry> oses;
                dbCore.DBOps.GetAllOSes(out oses);

                if(AddOS != null)
                {
                    int counter = 0;
                    // TODO: Check file name and existence
                    foreach(DBEntry os in oses)
                    {
                        if(UpdateProgress != null)
                            UpdateProgress("Populating OSes table", string.Format("{0} {1}", os.developer, os.product), counter, oses.Count);
                        string destination = Path.Combine(Settings.Current.RepositoryPath, os.mdid[0].ToString(),
                                                          os.mdid[1].ToString(), os.mdid[2].ToString(), os.mdid[3].ToString(),
                                                          os.mdid[4].ToString(), os.mdid) + ".zip";

                        if(AddOS != null)
                            AddOS(os, File.Exists(destination), destination);

                        counter++;
                    }
                }

                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
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
                foreach(KeyValuePair<string, DBOSFile> kvp in Context.hashes)
                {
                    if(UpdateProgress != null)
                        UpdateProgress(null, "Checking files in database", counter, Context.hashes.Count);

                    if(AddFileForOS != null)
                        AddFileForOS(kvp.Key, kvp.Value.Sha256, dbCore.DBOps.ExistsFile(kvp.Value.Sha256), kvp.Value.Crack);

                    counter++;
                }

                if(UpdateProgress != null)
                    UpdateProgress(null, "Retrieving OSes from database", counter, Context.hashes.Count);
                List<DBEntry> oses;
                dbCore.DBOps.GetAllOSes(out oses);

                if(oses != null && oses.Count > 0)
                {
                    DBEntry[] osesArray = new DBEntry[oses.Count];
                    oses.CopyTo(osesArray);

                    long osCounter = 0;
                    foreach(DBEntry os in osesArray)
                    {
                        if(UpdateProgress != null)
                            UpdateProgress(null, string.Format("Check OS id {0}", os.id), osCounter, osesArray.Length);

                        counter = 0;
                        foreach(KeyValuePair<string, DBOSFile> kvp in Context.hashes)
                        {
                            if(UpdateProgress2 != null)
                                UpdateProgress2(null, string.Format("Checking for file {0}", kvp.Value.Path), counter, Context.hashes.Count);

                            if(!dbCore.DBOps.ExistsFileInOS(kvp.Value.Sha256, os.id))
                            {
                                if(oses.Contains(os))
                                    oses.Remove(os);

                                // If one file is missing, the rest don't matter
                                break;
                            }

                            counter++;
                        }

                        if(oses.Count == 0)
                            break; // No OSes left
                    }
                }

                if(AddOS != null)
                {
                    // TODO: Check file name and existence
                    foreach(DBEntry os in oses)
                    {
                        string destination = Path.Combine(Settings.Current.RepositoryPath, os.mdid[0].ToString(),
                                                          os.mdid[1].ToString(), os.mdid[2].ToString(), os.mdid[3].ToString(),
                                                          os.mdid[4].ToString(), os.mdid) + ".zip";

                        if(AddOS != null)
                            AddOS(os, File.Exists(destination), destination);
                    }
                }

                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
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
                foreach(KeyValuePair<string, DBOSFile> kvp in Context.hashes)
                {
                    if(UpdateProgress != null)
                        UpdateProgress(null, "Adding files to database", counter, Context.hashes.Count);

                    if(!dbCore.DBOps.ExistsFile(kvp.Value.Sha256))
                    {
                        DBFile file = new DBFile
                        {
                            Sha256 = kvp.Value.Sha256, ClamTime = null, Crack = kvp.Value.Crack,
                            Length = kvp.Value.Length, Virus = null, HasVirus = null, VirusTotalTime = null
                        };
                        dbCore.DBOps.AddFile(file);

                        if(AddFile != null)
                            AddFile(file);
                    }

                    counter++;
                }

                if(UpdateProgress != null)
                    UpdateProgress(null, "Adding OS information", counter, Context.hashes.Count);
                dbCore.DBOps.AddOS(Context.dbInfo, out Context.dbInfo.id);
                if(UpdateProgress != null)
                    UpdateProgress(null, "Creating OS table", counter, Context.hashes.Count);
                dbCore.DBOps.CreateTableForOS(Context.dbInfo.id);

                counter = 0;
                foreach(KeyValuePair<string, DBOSFile> kvp in Context.hashes)
                {
                    if(UpdateProgress != null)
                        UpdateProgress(null, "Adding files to OS in database", counter, Context.hashes.Count);

                    dbCore.DBOps.AddFileToOS(kvp.Value, Context.dbInfo.id);

                    counter++;
                }

                counter = 0;
                foreach(KeyValuePair<string, DBFolder> kvp in Context.foldersDict)
                {
                    if(UpdateProgress != null)
                        UpdateProgress(null, "Adding folders to OS in database", counter, Context.foldersDict.Count);

                    dbCore.DBOps.AddFolderToOS(kvp.Value, Context.dbInfo.id);

                    counter++;
                }

                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
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
                else
                {
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
                if(Debugger.IsAttached)
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

        public static void RemoveOS(long id, string mdid)
        {
            if(id == 0 || string.IsNullOrWhiteSpace(mdid))
                return;

            string destination = Path.Combine(Settings.Current.RepositoryPath, mdid[0].ToString(),
                                  mdid[1].ToString(), mdid[2].ToString(), mdid[3].ToString(),
                                  mdid[4].ToString(), mdid) + ".zip";

            if(File.Exists(destination))
                File.Delete(destination);

            dbCore.DBOps.RemoveOS(id);
        }

        public static void GetFilesFromDb()
        {
            try
            {
                ulong count = dbCore.DBOps.GetFilesCount();
                const ulong page = 2500;
                ulong offset = 0;

                List<DBFile> files;

                while(dbCore.DBOps.GetFiles(out files, offset, page))
                {
                    if(files.Count == 0)
                        break;

                    if(UpdateProgress != null)
                        UpdateProgress(null, string.Format("Loaded file {0} of {1}", offset, count), (long)offset, (long)count);

                    if(AddFiles != null)

                        AddFiles(files);

                    offset += page;
                }

                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
        }

        public static void ToggleCrack(string hash, bool crack)
        {
            try
            {
                dbCore.DBOps.ToggleCrack(hash, crack);

                if(Finished != null)
                    Finished();
            }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
        }

        public static DBFile GetDBFile(string hash)
        {
            return dbCore.DBOps.GetFile(hash);
        }
    }
}
