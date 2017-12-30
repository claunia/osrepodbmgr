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
using System.Threading;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        public static void GetAllOSes()
        {
            try
            {
                #if DEBUG
                stopwatch.Restart();
                #endif
                dbCore.DbOps.GetAllOSes(out List<DbEntry> oses);
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.GetAllOSes(): Took {0} seconds to get OSes from database",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                if(AddOS != null)
                {
                    #if DEBUG
                    stopwatch.Restart();
                    #endif
                    int counter = 0;
                    // TODO: Check file name and existence
                    foreach(DbEntry os in oses)
                    {
                        UpdateProgress?.Invoke("Populating OSes table", $"{os.Developer} {os.Product}", counter,
                                               oses.Count);
                        string destination = Path.Combine(Settings.Current.RepositoryPath, os.Mdid[0].ToString(),
                                                          os.Mdid[1].ToString(), os.Mdid[2].ToString(),
                                                          os.Mdid[3].ToString(), os.Mdid[4].ToString(), os.Mdid) +
                                             ".zip";

                        AddOS?.Invoke(os);

                        counter++;
                    }
                    #if DEBUG
                    stopwatch.Stop();
                    Console.WriteLine("Core.GetAllOSes(): Took {0} seconds to add OSes to the GUI",
                                      stopwatch.Elapsed.TotalSeconds);
                    #endif
                }

                Finished?.Invoke();
            }
            catch(ThreadAbortException) { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached) throw;

                Failed?.Invoke($"Exception {ex.Message}\n{ex.InnerException}");
                #if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                #endif
            }
        }

        public static void CheckDbForFiles()
        {
            try
            {
                long counter = 0;
                #if DEBUG
                stopwatch.Restart();
                #endif
                Dictionary<string, DbOsFile> knownFiles = new Dictionary<string, DbOsFile>();

                bool unknownFile = false;

                foreach(KeyValuePair<string, DbOsFile> kvp in Context.Hashes)
                {
                    // Empty file with size zero
                    if(kvp.Value.Sha256 == "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")
                    {
                        AddFileForOS(kvp.Key, kvp.Value.Sha256, true, kvp.Value.Crack);
                        counter++;
                        continue;
                    }

                    UpdateProgress?.Invoke(null, "Checking files in database", counter, Context.Hashes.Count);

                    AddFileForOS?.Invoke(kvp.Key, kvp.Value.Sha256, dbCore.DbOps.ExistsFile(kvp.Value.Sha256),
                                         kvp.Value.Crack);

                    if(dbCore.DbOps.ExistsFile(kvp.Value.Sha256))
                    {
                        counter++;
                        knownFiles.Add(kvp.Key, kvp.Value);
                    }
                    else unknownFile = true;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.CheckDbForFiles(): Took {0} seconds to checks for file knowledge in the DB",
                                  stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
                #endif
                if(knownFiles.Count == 0 || unknownFile)
                {
                    Finished?.Invoke();
                    return;
                }

                UpdateProgress?.Invoke(null, "Retrieving OSes from database", counter, Context.Hashes.Count);
                dbCore.DbOps.GetAllOSes(out List<DbEntry> oses);
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.CheckDbForFiles(): Took {0} seconds get all OSes from DB",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                if(oses != null && oses.Count > 0)
                {
                    DbEntry[] osesArray = new DbEntry[oses.Count];
                    oses.CopyTo(osesArray);

                    long osCounter = 0;
                    #if DEBUG
                    stopwatch.Restart();
                    #endif

                    foreach(DbEntry os in osesArray)
                    {
                        UpdateProgress?.Invoke(null, $"Check OS id {os.Id}", osCounter, osesArray.Length);

                        counter = 0;
                        foreach(KeyValuePair<string, DbOsFile> kvp in knownFiles)
                        {
                            UpdateProgress2?.Invoke(null, $"Checking for file {kvp.Value.Path}", counter,
                                                    knownFiles.Count);

                            if(!dbCore.DbOps.ExistsFileInOs(kvp.Value.Sha256, os.Id))
                            {
                                if(oses.Contains(os)) oses.Remove(os);

                                // If one file is missing, the rest don't matter
                                break;
                            }

                            counter++;
                        }

                        if(oses.Count == 0) break; // No OSes left
                    }
                    #if DEBUG
                    stopwatch.Stop();
                    Console.WriteLine("Core.CheckDbForFiles(): Took {0} seconds correlate all files with all known OSes",
                                      stopwatch.Elapsed.TotalSeconds);
                    #endif
                }

                if(AddOS != null)
                    foreach(DbEntry os in oses)
                    {
                        string destination = Path.Combine(Settings.Current.RepositoryPath, os.Mdid[0].ToString(),
                                                          os.Mdid[1].ToString(), os.Mdid[2].ToString(),
                                                          os.Mdid[3].ToString(), os.Mdid[4].ToString(), os.Mdid) +
                                             ".zip";

                        AddOS?.Invoke(os);
                    }

                Finished?.Invoke();
            }
            catch(ThreadAbortException) { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached) throw;

                Failed?.Invoke($"Exception {ex.Message}\n{ex.InnerException}");
                #if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                #endif
            }
        }

        public static void AddFilesToDb()
        {
            try
            {
                long counter = 0;
                #if DEBUG
                stopwatch.Restart();
                #endif
                foreach(KeyValuePair<string, DbOsFile> kvp in Context.Hashes)
                {
                    UpdateProgress?.Invoke(null, "Adding files to database", counter, Context.Hashes.Count);

                    if(!dbCore.DbOps.ExistsFile(kvp.Value.Sha256))
                    {
                        DbFile file = new DbFile
                        {
                            Sha256         = kvp.Value.Sha256,
                            ClamTime       = null,
                            Crack          = kvp.Value.Crack,
                            Length         = kvp.Value.Length,
                            Virus          = null,
                            HasVirus       = null,
                            VirusTotalTime = null
                        };
                        dbCore.DbOps.AddFile(file);

                        AddFile?.Invoke(file);
                    }

                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.AddFilesToDb(): Took {0} seconds to add all files to the database",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                UpdateProgress?.Invoke(null, "Adding OS information", counter, Context.Hashes.Count);
                dbCore.DbOps.AddOs(Context.DbInfo, out Context.DbInfo.Id);
                UpdateProgress?.Invoke(null, "Creating OS table", counter, Context.Hashes.Count);
                dbCore.DbOps.CreateTableForOs(Context.DbInfo.Id);

                #if DEBUG
                stopwatch.Restart();
                #endif
                counter = 0;
                foreach(KeyValuePair<string, DbOsFile> kvp in Context.Hashes)
                {
                    UpdateProgress?.Invoke(null, "Adding files to OS in database", counter, Context.Hashes.Count);

                    dbCore.DbOps.AddFileToOs(kvp.Value, Context.DbInfo.Id);

                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.AddFilesToDb(): Took {0} seconds to add all files to the OS in the database",
                                  stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
                #endif
                counter = 0;
                foreach(KeyValuePair<string, DbFolder> kvp in Context.FoldersDict)
                {
                    UpdateProgress?.Invoke(null, "Adding folders to OS in database", counter,
                                           Context.FoldersDict.Count);

                    dbCore.DbOps.AddFolderToOs(kvp.Value, Context.DbInfo.Id);

                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.AddFilesToDb(): Took {0} seconds to add all folders to the database",
                                  stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
                #endif
                counter = 0;
                if(Context.SymlinksDict.Count > 0) dbCore.DbOps.CreateSymlinkTableForOs(Context.DbInfo.Id);

                foreach(KeyValuePair<string, string> kvp in Context.SymlinksDict)
                {
                    UpdateProgress?.Invoke(null, "Adding symbolic links to OS in database", counter,
                                           Context.SymlinksDict.Count);

                    dbCore.DbOps.AddSymlinkToOs(kvp.Key, kvp.Value, Context.DbInfo.Id);

                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.AddFilesToDb(): Took {0} seconds to add all symbolic links to the database",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                Finished?.Invoke();
            }
            catch(ThreadAbortException) { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached) throw;

                Failed?.Invoke($"Exception {ex.Message}\n{ex.InnerException}");
                #if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                #endif
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
                    Failed?.Invoke("No database file specified");
                    return;
                }

                dbCore = new SQLite();
                if(File.Exists(Settings.Current.DatabasePath))
                {
                    if(!dbCore.OpenDb(Settings.Current.DatabasePath, null, null, null))
                    {
                        Failed?.Invoke("Could not open database, correct file selected?");
                        dbCore = null;
                        return;
                    }
                }
                else
                {
                    if(!dbCore.CreateDb(Settings.Current.DatabasePath, null, null, null))
                    {
                        Failed?.Invoke("Could not create database, correct file selected?");
                        dbCore = null;
                        return;
                    }

                    if(!dbCore.OpenDb(Settings.Current.DatabasePath, null, null, null))
                    {
                        Failed?.Invoke("Could not open database, correct file selected?");
                        dbCore = null;
                        return;
                    }
                }

                Finished?.Invoke();
            }
            catch(ThreadAbortException) { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached) throw;

                Failed?.Invoke($"Exception {ex.Message}\n{ex.InnerException}");
                #if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                #endif
            }
        }

        public static void CloseDB()
        {
            dbCore?.CloseDb();
        }

        public static void RemoveOS(long id, string mdid)
        {
            if(id == 0 || string.IsNullOrWhiteSpace(mdid)) return;

            string destination = Path.Combine(Settings.Current.RepositoryPath, mdid[0].ToString(), mdid[1].ToString(),
                                              mdid[2].ToString(), mdid[3].ToString(), mdid[4].ToString(),
                                              mdid) + ".zip";

            if(File.Exists(destination)) File.Delete(destination);

            dbCore.DbOps.RemoveOs(id);
        }

        public static void GetFilesFromDb()
        {
            try
            {
                ulong       count  = dbCore.DbOps.GetFilesCount();
                const ulong PAGE   = 2500;
                ulong       offset = 0;

                #if DEBUG
                stopwatch.Restart();
                #endif
                while(dbCore.DbOps.GetFiles(out List<DbFile> files, offset, PAGE))
                {
                    if(files.Count == 0) break;

                    UpdateProgress?.Invoke(null, $"Loaded file {offset} of {count}", (long)offset, (long)count);

                    AddFiles?.Invoke(files);

                    offset += PAGE;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.GetFilesFromDb(): Took {0} seconds to get all files from the database",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                Finished?.Invoke();
            }
            catch(ThreadAbortException) { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached) throw;

                Failed?.Invoke($"Exception {ex.Message}\n{ex.InnerException}");
                #if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                #endif
            }
        }

        public static void ToggleCrack(string hash, bool crack)
        {
            try
            {
                dbCore.DbOps.ToggleCrack(hash, crack);

                Finished?.Invoke();
            }
            catch(ThreadAbortException) { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached) throw;

                Failed?.Invoke($"Exception {ex.Message}\n{ex.InnerException}");
                #if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                #endif
            }
        }

        public static DbFile GetDBFile(string hash)
        {
            return dbCore.DbOps.GetFile(hash);
        }
    }
}