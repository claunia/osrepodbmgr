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
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using DiscImageChef.Checksums;
using Ionic.Zip;
using Newtonsoft.Json;
using Schemas;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.LZMA;
using System.Threading;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        // Sets a 128Kbyte buffer
        const long bufferSize = 131072;

        public delegate void UpdateProgressDelegate(string text, string inner, long current, long maximum);
        public delegate void UpdateProgress2Delegate(string text, string inner, long current, long maximum);
        public delegate void FailedDelegate(string text);
        public delegate void FinishedWithoutErrorDelegate();
        public delegate void FinishedWithTextDelegate(string text);
        public delegate void AddFileForOSDelegate(string filename, string hash, bool known);
        public delegate void AddOSDelegate(DBEntry os, bool existsInRepo, string pathInRepo);
        public delegate void AddFileDelegate(DBFile file);
public delegate void AddFilesDelegate(List<DBFile> file);

        public static event UpdateProgressDelegate UpdateProgress;
        public static event UpdateProgress2Delegate UpdateProgress2;
        public static event FailedDelegate Failed;
        public static event FinishedWithoutErrorDelegate Finished;
        public static event FinishedWithTextDelegate FinishedWithText;
        public static event AddFileForOSDelegate AddFileForOS;
        public static event AddOSDelegate AddOS;
public static event AddFileDelegate AddFile;
public static event AddFilesDelegate AddFiles;

        static DBCore dbCore;

        static int zipCounter;
        static string zipCurrentEntryName;

        public static void FindFiles()
        {
            string filesPath;

            if(!string.IsNullOrEmpty(Context.tmpFolder) && Directory.Exists(Context.tmpFolder))
                filesPath = Context.tmpFolder;
            else
                filesPath = Context.path;

            if(string.IsNullOrEmpty(filesPath))
            {
                if(Failed != null)
                    Failed("Path is null or empty");
            }

            if(!Directory.Exists(filesPath))
            {
                if(Failed != null)
                    Failed("Directory not found");
            }

            try
            {
                Context.files = new List<string>(Directory.EnumerateFiles(filesPath, "*", SearchOption.AllDirectories));
                Context.files.Sort();
                Context.folders = new List<string>(Directory.EnumerateDirectories(filesPath, "*", SearchOption.AllDirectories));
                Context.folders.Sort();
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

        public static void HashFiles()
        {
            try
            {
                Context.hashes = new Dictionary<string, DBOSFile>();
                Context.foldersDict = new Dictionary<string, DBFolder>();
                List<string> alreadyMetadata = new List<string>();
                bool foundMetadata = false;

                // For metadata
                List<ArchitecturesTypeArchitecture> architectures = new List<ArchitecturesTypeArchitecture>();
                List<BarcodeType> barcodes = new List<BarcodeType>();
                List<BlockMediaType> disks = new List<BlockMediaType>();
                List<string> categories = new List<string>();
                List<string> keywords = new List<string>();
                List<LanguagesTypeLanguage> languages = new List<LanguagesTypeLanguage>();
                List<OpticalDiscType> discs = new List<OpticalDiscType>();
                List<string> subcategories = new List<string>();
                List<string> systems = new List<string>();
                bool releaseDateSpecified = false;
                DateTime releaseDate = DateTime.MinValue;
                CICMMetadataTypeReleaseType releaseType = CICMMetadataTypeReleaseType.Retail;
                bool releaseTypeSpecified = false;
                List<string> authors = new List<string>();
                List<string> developers = new List<string>();
                List<string> performers = new List<string>();
                List<string> publishers = new List<string>();
                string metadataName = null;
                string metadataPartNo = null;
                string metadataSerial = null;
                string metadataVersion = null;

                // End for metadata

                long counter = 1;
                foreach(string file in Context.files)
                {
                    // An already known metadata file, skip it
                    if(alreadyMetadata.Contains(file))
                    {
                        counter++;
                        continue;
                    }

                    if(Path.GetExtension(file).ToLowerInvariant() == ".xml")
                    {
                        FileStream xrs = new FileStream(file, FileMode.Open, FileAccess.Read);
                        XmlReader xr = XmlReader.Create(xrs);
                        XmlSerializer xs = new XmlSerializer(typeof(CICMMetadataType));
                        if(xs.CanDeserialize(xr))
                        {
                            CICMMetadataType thisMetadata = (CICMMetadataType)xs.Deserialize(xr);
                            if(thisMetadata.Architectures != null)
                                architectures.AddRange(thisMetadata.Architectures);
                            if(thisMetadata.Barcodes != null)
                                barcodes.AddRange(thisMetadata.Barcodes);
                            if(thisMetadata.BlockMedia != null)
                                disks.AddRange(thisMetadata.BlockMedia);
                            if(thisMetadata.Categories != null)
                                categories.AddRange(thisMetadata.Categories);
                            if(thisMetadata.Keywords != null)
                                keywords.AddRange(thisMetadata.Keywords);
                            if(thisMetadata.Languages != null)
                                languages.AddRange(thisMetadata.Languages);
                            if(thisMetadata.OpticalDisc != null)
                                discs.AddRange(thisMetadata.OpticalDisc);
                            if(thisMetadata.Subcategories != null)
                                subcategories.AddRange(thisMetadata.Subcategories);
                            if(thisMetadata.Systems != null)
                                systems.AddRange(thisMetadata.Systems);
                            if(thisMetadata.Author != null)
                                authors.AddRange(thisMetadata.Author);
                            if(thisMetadata.Developer != null)
                                developers.AddRange(thisMetadata.Developer);
                            if(thisMetadata.Performer != null)
                                performers.AddRange(thisMetadata.Performer);
                            if(thisMetadata.Publisher != null)
                                publishers.AddRange(thisMetadata.Publisher);
                            if(string.IsNullOrWhiteSpace(metadataName) && !string.IsNullOrWhiteSpace(thisMetadata.Name))
                                metadataName = thisMetadata.Name;
                            if(string.IsNullOrWhiteSpace(metadataPartNo) && !string.IsNullOrWhiteSpace(thisMetadata.PartNumber))
                                metadataPartNo = thisMetadata.PartNumber;
                            if(string.IsNullOrWhiteSpace(metadataSerial) && !string.IsNullOrWhiteSpace(thisMetadata.SerialNumber))
                                metadataSerial = thisMetadata.SerialNumber;
                            if(string.IsNullOrWhiteSpace(metadataVersion) && !string.IsNullOrWhiteSpace(thisMetadata.Version))
                                metadataVersion = thisMetadata.Version;
                            if(thisMetadata.ReleaseDateSpecified)
                            {
                                if(thisMetadata.ReleaseDate > releaseDate)
                                {
                                    releaseDateSpecified = true;
                                    releaseDate = thisMetadata.ReleaseDate;
                                }
                            }
                            if(thisMetadata.ReleaseTypeSpecified)
                            {
                                releaseTypeSpecified = true;
                                releaseType = thisMetadata.ReleaseType;
                            }

                            foundMetadata = true;

                            string metadataFileWithoutExtension = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".xml");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".xmL");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".xMl");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".xML");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".Xml");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".XmL");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".XMl");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".XML");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".json");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jsoN");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jsOn");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jsON");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jSon");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jSoN");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jSOn");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jSON");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".Json");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JsoN");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JsOn");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JsON");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JSon");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JSoN");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JSOn");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JSON");

                            xr.Close();
                            xrs.Close();
                            continue;
                        }

                        xr.Close();
                        xrs.Close();
                    }
                    else if(Path.GetExtension(file).ToLowerInvariant() == ".json")
                    {
                        FileStream jrs = new FileStream(file, FileMode.Open, FileAccess.Read);
                        TextReader jr = new StreamReader(jrs);
                        JsonSerializer js = new JsonSerializer();

                        try
                        {
                            CICMMetadataType thisMetadata = (CICMMetadataType)js.Deserialize(jr, typeof(CICMMetadataType));
                            if(thisMetadata.Architectures != null)
                                architectures.AddRange(thisMetadata.Architectures);
                            if(thisMetadata.Barcodes != null)
                                barcodes.AddRange(thisMetadata.Barcodes);
                            if(thisMetadata.BlockMedia != null)
                                disks.AddRange(thisMetadata.BlockMedia);
                            if(thisMetadata.Categories != null)
                                categories.AddRange(thisMetadata.Categories);
                            if(thisMetadata.Keywords != null)
                                keywords.AddRange(thisMetadata.Keywords);
                            if(thisMetadata.Languages != null)
                                languages.AddRange(thisMetadata.Languages);
                            if(thisMetadata.OpticalDisc != null)
                                discs.AddRange(thisMetadata.OpticalDisc);
                            if(thisMetadata.Subcategories != null)
                                subcategories.AddRange(thisMetadata.Subcategories);
                            if(thisMetadata.Systems != null)
                                systems.AddRange(thisMetadata.Systems);
                            if(thisMetadata.Author != null)
                                authors.AddRange(thisMetadata.Author);
                            if(thisMetadata.Developer != null)
                                developers.AddRange(thisMetadata.Developer);
                            if(thisMetadata.Performer != null)
                                performers.AddRange(thisMetadata.Performer);
                            if(thisMetadata.Publisher != null)
                                publishers.AddRange(thisMetadata.Publisher);
                            if(string.IsNullOrWhiteSpace(metadataName) && !string.IsNullOrWhiteSpace(thisMetadata.Name))
                                metadataName = thisMetadata.Name;
                            if(string.IsNullOrWhiteSpace(metadataPartNo) && !string.IsNullOrWhiteSpace(thisMetadata.PartNumber))
                                metadataPartNo = thisMetadata.PartNumber;
                            if(string.IsNullOrWhiteSpace(metadataSerial) && !string.IsNullOrWhiteSpace(thisMetadata.SerialNumber))
                                metadataSerial = thisMetadata.SerialNumber;
                            if(string.IsNullOrWhiteSpace(metadataVersion) && !string.IsNullOrWhiteSpace(thisMetadata.Version))
                                metadataVersion = thisMetadata.Version;
                            if(thisMetadata.ReleaseDateSpecified)
                            {
                                if(thisMetadata.ReleaseDate > releaseDate)
                                {
                                    releaseDateSpecified = true;
                                    releaseDate = thisMetadata.ReleaseDate;
                                }
                            }
                            if(thisMetadata.ReleaseTypeSpecified)
                            {
                                releaseTypeSpecified = true;
                                releaseType = thisMetadata.ReleaseType;
                            }

                            foundMetadata = true;

                            string metadataFileWithoutExtension = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".xml");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".xmL");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".xMl");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".xML");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".Xml");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".XmL");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".XMl");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".XML");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".json");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jsoN");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jsOn");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jsON");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jSon");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jSoN");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jSOn");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".jSON");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".Json");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JsoN");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JsOn");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JsON");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JSon");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JSoN");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JSOn");
                            alreadyMetadata.Add(metadataFileWithoutExtension + ".JSON");

                            jr.Close();
                            jrs.Close();
                            continue;
                        }
                        catch
                        {
                            jr.Close();
                            jrs.Close();
                        }
                    }

                    string filesPath;
                    FileInfo fi = new FileInfo(file);

                    if(!string.IsNullOrEmpty(Context.tmpFolder) && Directory.Exists(Context.tmpFolder))
                        filesPath = Context.tmpFolder;
                    else
                        filesPath = Context.path;

                    string relpath = file.Substring(filesPath.Length + 1);
                    if(UpdateProgress != null)
                        UpdateProgress(string.Format("Hashing file {0} of {1}", counter, Context.files.Count), null, counter, Context.files.Count);
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
                    else
                    {
                        if(UpdateProgress2 != null)
                            UpdateProgress2(string.Format("{0:P}", 0 / (double)fileStream.Length), relpath, 0, fileStream.Length);
                        dataBuffer = new byte[fileStream.Length];
                        fileStream.Read(dataBuffer, 0, (int)fileStream.Length);
                        sha256Context.Update(dataBuffer);
                    }

                    fileStream.Close();
                    string hash = stringify(sha256Context.Final());

                    DBOSFile dbFile = new DBOSFile();
                    dbFile.Attributes = fi.Attributes;
                    dbFile.CreationTimeUtc = fi.CreationTimeUtc;
                    dbFile.LastAccessTimeUtc = fi.LastAccessTimeUtc;
                    dbFile.LastWriteTimeUtc = fi.LastWriteTimeUtc;
                    dbFile.Length = fi.Length;
                    dbFile.Path = relpath;
                    dbFile.Sha256 = hash;

                    Context.hashes.Add(relpath, dbFile);
                    counter++;
                }

                counter = 1;
                foreach(string folder in Context.folders)
                {

                    string filesPath;
                    DirectoryInfo di = new DirectoryInfo(folder);

                    if(!string.IsNullOrEmpty(Context.tmpFolder) && Directory.Exists(Context.tmpFolder))
                        filesPath = Context.tmpFolder;
                    else
                        filesPath = Context.path;

                    string relpath = folder.Substring(filesPath.Length + 1);
                    if(UpdateProgress != null)
                        UpdateProgress(string.Format("Checking folder {0} of {1}", counter, Context.folders.Count), null, counter, Context.folders.Count);

                    DBFolder dbFolder = new DBFolder();
                    dbFolder.Attributes = di.Attributes;
                    dbFolder.CreationTimeUtc = di.CreationTimeUtc;
                    dbFolder.LastAccessTimeUtc = di.LastAccessTimeUtc;
                    dbFolder.LastWriteTimeUtc = di.LastWriteTimeUtc;
                    dbFolder.Path = relpath;

                    Context.foldersDict.Add(relpath, dbFolder);
                    counter++;
                }

                if(foundMetadata)
                {
                    Context.metadata = new CICMMetadataType();
                    if(architectures.Count > 0)
                        Context.metadata.Architectures = architectures.Distinct().ToArray();
                    if(authors.Count > 0)
                        Context.metadata.Author = authors.Distinct().ToArray();
                    // TODO: Check for uniqueness
                    if(barcodes.Count > 0)
                        Context.metadata.Barcodes = barcodes.ToArray();
                    if(disks.Count > 0)
                        Context.metadata.BlockMedia = disks.ToArray();
                    if(categories.Count > 0)
                        Context.metadata.Categories = categories.Distinct().ToArray();
                    if(developers.Count > 0)
                        Context.metadata.Developer = developers.Distinct().ToArray();
                    if(keywords.Count > 0)
                        Context.metadata.Keywords = keywords.Distinct().ToArray();
                    if(languages.Count > 0)
                        Context.metadata.Languages = languages.Distinct().ToArray();
                    Context.metadata.Name = metadataName;
                    if(discs.Count > 0)
                        Context.metadata.OpticalDisc = discs.ToArray();
                    Context.metadata.PartNumber = metadataPartNo;
                    if(performers.Count > 0)
                        Context.metadata.Performer = performers.Distinct().ToArray();
                    if(publishers.Count > 0)
                        Context.metadata.Publisher = publishers.Distinct().ToArray();
                    if(releaseDateSpecified)
                    {
                        Context.metadata.ReleaseDate = releaseDate;
                        Context.metadata.ReleaseDateSpecified = true;
                    }
                    if(releaseTypeSpecified)
                    {
                        Context.metadata.ReleaseType = releaseType;
                        Context.metadata.ReleaseTypeSpecified = true;
                    }
                    Context.metadata.SerialNumber = metadataSerial;
                    if(subcategories.Count > 0)
                        Context.metadata.Subcategories = subcategories.Distinct().ToArray();
                    if(systems.Count > 0)
                        Context.metadata.Systems = systems.Distinct().ToArray();
                    Context.metadata.Version = metadataVersion;

                    foreach(string metadataFile in alreadyMetadata)
                        Context.files.Remove(metadataFile);
                }
                else
                    Context.metadata = null;
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
                        AddFileForOS(kvp.Key, kvp.Value.Sha256, dbCore.DBOps.ExistsFile(kvp.Value.Sha256));

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
                            Sha256 = kvp.Value.Sha256, ClamTime = null, Crack = false,
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

        public static void CompressFiles()
        {
            try
            {
                if(string.IsNullOrWhiteSpace(Context.dbInfo.developer))
                {
                    if(Failed != null)
                        Failed("Developer cannot be empty");
                    return;
                }

                if(string.IsNullOrWhiteSpace(Context.dbInfo.product))
                {
                    if(Failed != null)
                        Failed("Product cannot be empty");
                    return;
                }

                if(string.IsNullOrWhiteSpace(Context.dbInfo.version))
                {
                    if(Failed != null)
                        Failed("Version cannot be empty");
                    return;
                }

                string destinationFolder = "";
                destinationFolder = Path.Combine(destinationFolder, Context.dbInfo.developer);
                destinationFolder = Path.Combine(destinationFolder, Context.dbInfo.product);
                destinationFolder = Path.Combine(destinationFolder, Context.dbInfo.version);
                if(!string.IsNullOrWhiteSpace(Context.dbInfo.languages))
                {
                    destinationFolder = Path.Combine(destinationFolder, Context.dbInfo.languages);
                }
                if(!string.IsNullOrWhiteSpace(Context.dbInfo.architecture))
                {
                    destinationFolder = Path.Combine(destinationFolder, Context.dbInfo.architecture);
                }
                if(Context.dbInfo.oem)
                {
                    destinationFolder = Path.Combine(destinationFolder, "oem");
                }
                if(!string.IsNullOrWhiteSpace(Context.dbInfo.machine))
                {
                    destinationFolder = Path.Combine(destinationFolder, "for " + Context.dbInfo.machine);
                }

                string destinationFile = "";
                if(!string.IsNullOrWhiteSpace(Context.dbInfo.format))
                    destinationFile += "[" + Context.dbInfo.format + "]";
                if(Context.dbInfo.files)
                {
                    if(destinationFile != "")
                        destinationFile += "_";
                    destinationFile += "files";
                }
                if(Context.dbInfo.netinstall)
                {
                    if(destinationFile != "")
                        destinationFile += "_";
                    destinationFile += "netinstall";
                }
                if(Context.dbInfo.source)
                {
                    if(destinationFile != "")
                        destinationFile += "_";
                    destinationFile += "source";
                }
                if(Context.dbInfo.update)
                {
                    if(destinationFile != "")
                        destinationFile += "_";
                    destinationFile += "update";
                }
                if(Context.dbInfo.upgrade)
                {
                    if(destinationFile != "")
                        destinationFile += "_";
                    destinationFile += "upgrade";
                }
                if(!string.IsNullOrWhiteSpace(Context.dbInfo.description))
                {
                    if(destinationFile != "")
                        destinationFile += "_";
                    destinationFile += Context.dbInfo.description;
                }
                else if(destinationFile == "")
                {
                    destinationFile = "archive";
                }

                string destination = Path.Combine(destinationFolder, destinationFile) + ".zip";

                MD5Context md5 = new MD5Context();
                md5.Init();
                byte[] tmp;
                string mdid = md5.Data(Encoding.UTF8.GetBytes(destination), out tmp);
                Console.WriteLine("MDID: {0}", mdid);

                if(dbCore.DBOps.ExistsOS(mdid))
                {
                    if(File.Exists(destination))
                    {
                        if(Failed != null)
                            Failed("OS already exists.");
                        return;
                    }

                    if(Failed != null)
                        Failed("OS already exists in the database but not in the repository, check for inconsistencies.");
                    return;
                }

                if(File.Exists(destination))
                {
                    if(Failed != null)
                        Failed("OS already exists in the repository but not in the database, check for inconsistencies.");
                    return;
                }

                Context.dbInfo.mdid = mdid;

                string filesPath;

                if(!string.IsNullOrEmpty(Context.tmpFolder) && Directory.Exists(Context.tmpFolder))
                    filesPath = Context.tmpFolder;
                else
                    filesPath = Context.path;

                int counter = 0;
                string extension = null;

                switch(Settings.Current.CompressionAlgorithm)
                {
                    case AlgoEnum.GZip:
                        extension = ".gz";
                        break;
                    case AlgoEnum.BZip2:
                        extension = ".bz2";
                        break;
                    case AlgoEnum.LZMA:
                        extension = ".lzma";
                        break;
                }

                foreach(KeyValuePair<string, DBOSFile> file in Context.hashes)
                {
                    if(UpdateProgress != null)
                        UpdateProgress("Compressing...", file.Value.Path, counter, Context.hashes.Count);

                    destinationFolder = Path.Combine(Settings.Current.RepositoryPath, file.Value.Sha256[0].ToString(), file.Value.Sha256[1].ToString(), file.Value.Sha256[2].ToString(), file.Value.Sha256[3].ToString(), file.Value.Sha256[4].ToString());
                    Directory.CreateDirectory(destinationFolder);

                    destinationFile = Path.Combine(destinationFolder, file.Value.Sha256 + extension);

                    if(!File.Exists(destinationFile))
                    {
                        FileStream inFs = new FileStream(Path.Combine(filesPath, file.Value.Path), FileMode.Open, FileAccess.Read);
                        FileStream outFs = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write);
                        Stream zStream = null;

                        switch(Settings.Current.CompressionAlgorithm)
                        {
                            case AlgoEnum.GZip:
                                zStream = new GZipStream(outFs, SharpCompress.Compressors.CompressionMode.Compress, CompressionLevel.BestCompression);
                                break;
                            case AlgoEnum.BZip2:
                                zStream = new BZip2Stream(outFs, SharpCompress.Compressors.CompressionMode.Compress);
                                break;
                            case AlgoEnum.LZMA:
                                zStream = new LzmaStream(new LzmaEncoderProperties(), false, outFs);
                                outFs.Write(((LzmaStream)zStream).Properties, 0, ((LzmaStream)zStream).Properties.Length);
                                outFs.Write(BitConverter.GetBytes(inFs.Length), 0, 8);
                                break;
                        }

                        byte[] buffer = new byte[bufferSize];

                        while((inFs.Position + bufferSize) <= inFs.Length)
                        {
                            if(UpdateProgress2 != null)
                                UpdateProgress2(string.Format("{0:P}", inFs.Position / (double)inFs.Length),
                                                string.Format("{0} / {1} bytes", inFs.Position, inFs.Length),
                                                inFs.Position, inFs.Length);

                            inFs.Read(buffer, 0, buffer.Length);
                            zStream.Write(buffer, 0, buffer.Length);
                        }

                        buffer = new byte[inFs.Length - inFs.Position];
                        if(UpdateProgress2 != null)
                            UpdateProgress2(string.Format("{0:P}", inFs.Position / (double)inFs.Length),
                                            string.Format("{0} / {1} bytes", inFs.Position, inFs.Length),
                                            inFs.Position, inFs.Length);

                        inFs.Read(buffer, 0, buffer.Length);
                        zStream.Write(buffer, 0, buffer.Length);

                        if(UpdateProgress2 != null)
                            UpdateProgress2(string.Format("{0:P}", inFs.Length / (double)inFs.Length),
                                            "Finishing...", inFs.Length, inFs.Length);

                        inFs.Close();
                        zStream.Close();
                    }

                    counter++;
                }

                if(Context.metadata != null)
                {
                    MemoryStream xms = new MemoryStream();
                    XmlSerializer xs = new XmlSerializer(typeof(CICMMetadataType));
                    xs.Serialize(xms, Context.metadata);
                    xms.Position = 0;

                    JsonSerializer js = new JsonSerializer();
                    js.Formatting = Newtonsoft.Json.Formatting.Indented;
                    js.NullValueHandling = NullValueHandling.Ignore;
                    MemoryStream jms = new MemoryStream();
                    StreamWriter sw = new StreamWriter(jms, Encoding.UTF8, 1048576, true);
                    js.Serialize(sw, Context.metadata, typeof(CICMMetadataType));
                    sw.Close();
                    jms.Position = 0;

                    destinationFolder = Path.Combine(Settings.Current.RepositoryPath, "metadata", mdid[0].ToString(), mdid[1].ToString(),
                                                     mdid[2].ToString(), mdid[3].ToString(), mdid[4].ToString());
                    Directory.CreateDirectory(destinationFolder);

                    FileStream xfs = new FileStream(Path.Combine(destinationFolder, mdid + ".xml"), FileMode.CreateNew, FileAccess.Write);
                    xms.CopyTo(xfs);
                    xfs.Close();
                    FileStream jfs = new FileStream(Path.Combine(destinationFolder, mdid + ".json"), FileMode.CreateNew, FileAccess.Write);
                    jms.CopyTo(jfs);
                    jfs.Close();

                    xms.Position = 0;
                    jms.Position = 0;
                }

                if(FinishedWithText != null)
                    FinishedWithText(string.Format("Correctly added operating system with MDID {0}", mdid));
            }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
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

        public static void OpenArchive()
        {
            if(!Context.unarUsable)
            {
                if(Failed != null)
                    Failed("The UnArchiver is not correctly installed");
                return;
            }

            if(!File.Exists(Context.path))
            {
                if(Failed != null)
                    Failed("Specified file cannot be found");
                return;
            }

            try
            {
                string unarFolder = Path.GetDirectoryName(Settings.Current.UnArchiverPath);
                string extension = Path.GetExtension(Settings.Current.UnArchiverPath);
                string unarfilename = Path.GetFileNameWithoutExtension(Settings.Current.UnArchiverPath);
                string lsarfilename = unarfilename.Replace("unar", "lsar");
                string lsarPath = Path.Combine(unarFolder, lsarfilename + extension);

                Process lsarProcess = new Process();
                lsarProcess.StartInfo.FileName = lsarPath;
                lsarProcess.StartInfo.CreateNoWindow = true;
                lsarProcess.StartInfo.RedirectStandardOutput = true;
                lsarProcess.StartInfo.UseShellExecute = false;
                lsarProcess.StartInfo.Arguments = string.Format("-j \"\"\"{0}\"\"\"", Context.path);
                lsarProcess.Start();
                string lsarOutput = lsarProcess.StandardOutput.ReadToEnd();
                lsarProcess.WaitForExit();

                long counter = 0;
                string format = null;
                JsonTextReader jsReader = new JsonTextReader(new StringReader(lsarOutput));
                while(jsReader.Read())
                {
                    if(jsReader.TokenType == JsonToken.PropertyName && jsReader.Value != null && jsReader.Value.ToString() == "XADFileName")
                        counter++;
                    else if(jsReader.TokenType == JsonToken.PropertyName && jsReader.Value != null && jsReader.Value.ToString() == "lsarFormatName")
                    {
                        jsReader.Read();
                        if(jsReader.TokenType == JsonToken.String && jsReader.Value != null)
                            format = jsReader.Value.ToString();
                    }
                }

                Context.unzipWithUnAr = false;
                Context.archiveFormat = format;
                Context.noFilesInArchive = counter;

                if(string.IsNullOrEmpty(format))
                {
                    if(Failed != null)
                        Failed("File not recognized as an archive");
                    return;
                }

                if(counter == 0)
                {
                    if(Failed != null)
                        Failed("Archive contains no files");
                    return;
                }

                if(Context.archiveFormat == "Zip")
                {
                    Context.unzipWithUnAr = false;

                    if(Context.usableDotNetZip)
                    {
                        ZipFile zf = ZipFile.Read(Context.path, new ReadOptions { Encoding = Encoding.UTF8 });
                        foreach(ZipEntry ze in zf)
                        {
                            // ZIP created with Mac OS X, need to be extracted with The UnArchiver to get correct ResourceFork structure
                            if(ze.FileName.StartsWith("__MACOSX", StringComparison.CurrentCulture))
                            {
                                Context.unzipWithUnAr = true;
                                break;
                            }
                        }
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

        public static void ExtractArchive()
        {
            if(!File.Exists(Context.path))
            {
                if(Failed != null)
                    Failed("Specified file cannot be found");
                return;
            }

            if(!Directory.Exists(Settings.Current.TemporaryFolder))
            {
                if(Failed != null)
                    Failed("Temporary folder cannot be found");
                return;
            }

            string tmpFolder;

            if(Context.userExtracting)
                tmpFolder = Context.tmpFolder;
            else
                tmpFolder = Path.Combine(Settings.Current.TemporaryFolder, Path.GetRandomFileName());

            try
            {
                Directory.CreateDirectory(tmpFolder);

                Context.tmpFolder = tmpFolder;
            }
            catch(Exception)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed("Cannot create temporary folder");
            }

            try
            {
                // If it's a ZIP file not created by Mac OS X, use DotNetZip to uncompress (unar freaks out or corrupts certain ZIP features)
                if(Context.archiveFormat == "Zip" && !Context.unzipWithUnAr && Context.usableDotNetZip)
                {
                    try
                    {
                        ZipFile zf = ZipFile.Read(Context.path, new ReadOptions { Encoding = Encoding.UTF8 });
                        zf.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                        zf.ExtractProgress += Zf_ExtractProgress;
                        zipCounter = 0;
                        zipCurrentEntryName = "";
                        zf.ExtractAll(tmpFolder);
                        return;
                    }
                    catch(ThreadAbortException)
                    {
                        return;
                    }
                    catch(Exception ex)
                    {
                        if(Debugger.IsAttached)
                            throw;
                        if(Failed != null)
                            Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
                    }
                }
                else
                {
                    if(!Context.unarUsable)
                    {
                        if(Failed != null)
                            Failed("The UnArchiver is not correctly installed");
                        return;
                    }

                    Context.unarProcess = new Process();
                    Context.unarProcess.StartInfo.FileName = Settings.Current.UnArchiverPath;
                    Context.unarProcess.StartInfo.CreateNoWindow = true;
                    Context.unarProcess.StartInfo.RedirectStandardOutput = true;
                    Context.unarProcess.StartInfo.UseShellExecute = false;
                    Context.unarProcess.StartInfo.Arguments = string.Format("-o \"\"\"{0}\"\"\" -r -D -k hidden \"\"\"{1}\"\"\"", tmpFolder, Context.path);
                    long counter = 0;
                    Context.unarProcess.OutputDataReceived += (sender, e) =>
                    {
                        counter++;
                        if(UpdateProgress2 != null)
                            UpdateProgress2("", e.Data, counter, Context.noFilesInArchive);
                    };
                    Context.unarProcess.Start();
                    Context.unarProcess.BeginOutputReadLine();
                    Context.unarProcess.WaitForExit();
                    Context.unarProcess.Close();
                    Context.unarProcess = null;

                    if(Finished != null)
                        Finished();
                }
            }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
        }

        static void Zf_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if(e.CurrentEntry != null && e.CurrentEntry.FileName != zipCurrentEntryName)
            {
                zipCurrentEntryName = e.CurrentEntry.FileName;
                zipCounter++;
            }

            if(UpdateProgress != null && e.CurrentEntry != null && e.EntriesTotal > 0)
                UpdateProgress("Extracting...", e.CurrentEntry.FileName, zipCounter, e.EntriesTotal);
            if(UpdateProgress2 != null && e.TotalBytesToTransfer > 0)
                UpdateProgress2(string.Format("{0:P}", e.BytesTransferred / (double)e.TotalBytesToTransfer),
                                string.Format("{0} / {1}", e.BytesTransferred, e.TotalBytesToTransfer),
                                e.BytesTransferred, e.TotalBytesToTransfer);

            Console.WriteLine("{0}", e.EventType);
            if(e.EventType == ZipProgressEventType.Extracting_AfterExtractAll && Finished != null)
                Finished();
        }

        public static void RemoveTempFolder()
        {
            try
            {
                if(Directory.Exists(Context.tmpFolder))
                {
                    Directory.Delete(Context.tmpFolder, true);
                    if(Finished != null)
                        Finished();
                }
            }
            catch(System.IO.IOException)
            {
                // Could not delete temporary files, do not crash.
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

        public static void CopyFile()
        {
            try
            {
                if(!File.Exists(Context.path))
                {
                    if(Failed != null)
                        Failed("Specified file cannot be found");
                    return;
                }

                if(string.IsNullOrWhiteSpace(Context.tmpFolder))
                {
                    if(Failed != null)
                        Failed("Destination cannot be empty");
                    return;
                }

                if(Directory.Exists(Context.tmpFolder))
                {
                    if(Failed != null)
                        Failed("Destination cannot be a folder");
                    return;
                }

                FileStream inFs = new FileStream(Context.path, FileMode.Open, FileAccess.Read);
                FileStream outFs = new FileStream(Context.tmpFolder, FileMode.Create, FileAccess.Write);

                byte[] buffer = new byte[bufferSize];

                while((inFs.Position + bufferSize) <= inFs.Length)
                {
                    if(UpdateProgress != null)
                        UpdateProgress("Copying file...", string.Format("{0} / {1} bytes", inFs.Position, inFs.Length), inFs.Position, inFs.Length);

                    inFs.Read(buffer, 0, buffer.Length);
                    outFs.Write(buffer, 0, buffer.Length);
                }

                buffer = new byte[inFs.Length - inFs.Position];
                if(UpdateProgress != null)
                    UpdateProgress("Copying file...", string.Format("{0} / {1} bytes", inFs.Position, inFs.Length), inFs.Position, inFs.Length);

                inFs.Read(buffer, 0, buffer.Length);
                outFs.Write(buffer, 0, buffer.Length);

                inFs.Close();
                outFs.Close();

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

        public static void SaveAs()
        {
            try
            {
                if(string.IsNullOrWhiteSpace(Context.path))
                {
                    if(Failed != null)
                        Failed("Destination cannot be empty");
                    return;
                }

                if(File.Exists(Context.path))
                {
                    if(Failed != null)
                        Failed("Destination cannot be a file");
                    return;
                }

                if(Context.dbInfo.id == 0)
                {
                    if(Failed != null)
                        Failed("Operating system must be set");
                    return;
                }

                List<DBOSFile> files;
                List<DBFolder> folders;
                long counter;

                if(UpdateProgress != null)
                    UpdateProgress("", "Asking DB for files...", 1, 100);

                dbCore.DBOps.GetAllFilesInOS(out files, Context.dbInfo.id);

                if(UpdateProgress != null)
                    UpdateProgress("", "Asking DB for folders...", 2, 100);

                dbCore.DBOps.GetAllFolders(out folders, Context.dbInfo.id);

                if(UpdateProgress != null)
                    UpdateProgress("", "Creating folders...", 3, 100);

                counter = 0;
                foreach(DBFolder folder in folders)
                {
                    if(UpdateProgress2 != null)
                        UpdateProgress2("", folder.Path, counter, folders.Count);

                    DirectoryInfo di = Directory.CreateDirectory(Path.Combine(Context.path, folder.Path));
                    di.Attributes = folder.Attributes;
                    di.CreationTimeUtc = folder.CreationTimeUtc;
                    di.LastAccessTimeUtc = folder.LastAccessTimeUtc;
                    di.LastWriteTimeUtc = folder.LastWriteTimeUtc;

                    counter++;
                }

                counter = 3;
                foreach(DBOSFile file in files)
                {
                    if(UpdateProgress != null)
                        UpdateProgress("", string.Format("Creating {0}...", file.Path), counter, 3 + files.Count);

                    Stream zStream = null;
                    string repoPath;
                    AlgoEnum algorithm;

                    if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                                file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                                file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                                file.Sha256 + ".gz")))
                    {
                        repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                                file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                                file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                                file.Sha256 + ".gz");
                        algorithm = AlgoEnum.GZip;
                    }
                    else if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                                file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                                file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                                file.Sha256 + ".bz2")))
                    {
                        repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                                file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                                file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                                file.Sha256 + ".bz2");
                        algorithm = AlgoEnum.BZip2;
                    }
                    else if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                                file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                                file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                                file.Sha256 + ".lzma")))
                    {
                        repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                                file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                                file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                                file.Sha256 + ".lzma");
                        algorithm = AlgoEnum.LZMA;
                    }
                    else
                    {
                        if(Failed != null)
                            Failed(string.Format("Cannot find file with hash {0} in the repository", file.Sha256));
                        return;
                    }

                    FileStream inFs = new FileStream(repoPath, FileMode.Open, FileAccess.Read);
                    FileStream outFs = new FileStream(Path.Combine(Context.path, file.Path), FileMode.CreateNew, FileAccess.Write);

                    switch(algorithm)
                    {
                        case AlgoEnum.GZip:
                            zStream = new GZipStream(inFs, SharpCompress.Compressors.CompressionMode.Decompress);
                            break;
                        case AlgoEnum.BZip2:
                            zStream = new BZip2Stream(inFs, SharpCompress.Compressors.CompressionMode.Decompress);
                            break;
                        case AlgoEnum.LZMA:
                            byte[] properties = new byte[5];
                            inFs.Read(properties, 0, 5);
                            inFs.Seek(8, SeekOrigin.Current);
                            zStream = new LzmaStream(properties, inFs);
                            break;
                    }

                    byte[] buffer = new byte[bufferSize];

                    while((outFs.Position + bufferSize) <= file.Length)
                    {
                        if(UpdateProgress2 != null)
                            UpdateProgress2(string.Format("{0:P}", outFs.Position / (double)file.Length),
                                            string.Format("{0} / {1} bytes", outFs.Position, file.Length),
                                            outFs.Position, file.Length);

                        zStream.Read(buffer, 0, buffer.Length);
                        outFs.Write(buffer, 0, buffer.Length);
                    }

                    buffer = new byte[file.Length - outFs.Position];
                    if(UpdateProgress2 != null)
                        UpdateProgress2(string.Format("{0:P}", outFs.Position / (double)file.Length),
                                        string.Format("{0} / {1} bytes", outFs.Position, file.Length),
                                        outFs.Position, file.Length);

                    zStream.Read(buffer, 0, buffer.Length);
                    outFs.Write(buffer, 0, buffer.Length);

                    if(UpdateProgress2 != null)
                        UpdateProgress2(string.Format("{0:P}", file.Length / (double)file.Length),
                                        "Finishing...", inFs.Length, inFs.Length);

                    zStream.Close();
                    outFs.Close();

                    FileInfo fi = new FileInfo(Path.Combine(Context.path, file.Path));
                    fi.Attributes = file.Attributes;
                    fi.CreationTimeUtc = file.CreationTimeUtc;
                    fi.LastAccessTimeUtc = file.LastAccessTimeUtc;
                    fi.LastWriteTimeUtc = file.LastWriteTimeUtc;

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

        public static void CompressTo()
        {
            try
            {
                if(string.IsNullOrWhiteSpace(Context.path))
                {
                    if(Failed != null)
                        Failed("Destination cannot be empty");
                    return;
                }

                if(Directory.Exists(Context.path))
                {
                    if(Failed != null)
                        Failed("Destination cannot be a folder");
                    return;
                }

                if(Context.dbInfo.id == 0)
                {
                    if(Failed != null)
                        Failed("Operating system must be set");
                    return;
                }

                if(!Context.usableDotNetZip)
                {
                    if(Failed != null)
                        Failed("Cannot create ZIP files");
                    return;
                }

                ZipFile zf = new ZipFile(Context.path, Encoding.UTF8);
                zf.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zf.CompressionMethod = CompressionMethod.Deflate;
                zf.SaveProgress += Zf_SaveProgress;
                zf.EmitTimesInUnixFormatWhenSaving = true;
                zf.EmitTimesInWindowsFormatWhenSaving = true;
                zf.UseZip64WhenSaving = Zip64Option.AsNecessary;
                zf.SortEntriesBeforeSaving = true;
                List<DBOSFile> files;
                List<DBFolder> folders;
                long counter;

                if(UpdateProgress != null)
                    UpdateProgress("", "Asking DB for files...", 1, 100);

                dbCore.DBOps.GetAllFilesInOS(out files, Context.dbInfo.id);

                if(UpdateProgress != null)
                    UpdateProgress("", "Asking DB for folders...", 2, 100);

                dbCore.DBOps.GetAllFolders(out folders, Context.dbInfo.id);

                if(UpdateProgress != null)
                    UpdateProgress("", "Creating folders...", 3, 100);

                counter = 0;
                foreach(DBFolder folder in folders)
                {
                    if(UpdateProgress2 != null)
                        UpdateProgress2("", folder.Path, counter, folders.Count);

                    ZipEntry zd = zf.AddDirectoryByName(folder.Path);
                    zd.Attributes = folder.Attributes;
                    zd.CreationTime = folder.CreationTimeUtc;
                    zd.AccessedTime = folder.LastAccessTimeUtc;
                    zd.LastModified = folder.LastWriteTimeUtc;
                    zd.ModifiedTime = folder.LastWriteTimeUtc;

                    counter++;
                }

                counter = 3;
                Context.hashes = new Dictionary<string, DBOSFile>();
                foreach(DBOSFile file in files)
                {
                    if(UpdateProgress != null)
                        UpdateProgress("", string.Format("Adding {0}...", file.Path), counter, 3 + files.Count);

                    Context.hashes.Add(file.Path, file);

                    ZipEntry zi = zf.AddEntry(file.Path, Zf_HandleOpen, Zf_HandleClose);
                    zi.Attributes = file.Attributes;
                    zi.CreationTime = file.CreationTimeUtc;
                    zi.AccessedTime = file.LastAccessTimeUtc;
                    zi.LastModified = file.LastWriteTimeUtc;
                    zi.ModifiedTime = file.LastWriteTimeUtc;

                    counter++;
                }

                zipCounter = 0;
                zipCurrentEntryName = "";
                zf.Save();
            }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
            }
        }

        static Stream Zf_HandleOpen(string entryName)
        {
            DBOSFile file;
            if(!Context.hashes.TryGetValue(entryName, out file))
            {
                if(!Context.hashes.TryGetValue(entryName.Replace('/', '\\'), out file))
                    throw new ArgumentException("Cannot find requested zip entry in hashes dictionary");
            }

            // Special case for empty file, as it seems to crash when SharpCompress tries to unLZMA it.
            if(file.Length == 0)
                return new MemoryStream();

            Stream zStream = null;
            string repoPath;
            AlgoEnum algorithm;

            if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                        file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                        file.Sha256 + ".gz")))
            {
                repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                        file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                        file.Sha256 + ".gz");
                algorithm = AlgoEnum.GZip;
            }
            else if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                        file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                        file.Sha256 + ".bz2")))
            {
                repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                        file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                        file.Sha256 + ".bz2");
                algorithm = AlgoEnum.BZip2;
            }
            else if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                        file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                        file.Sha256 + ".lzma")))
            {
                repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                        file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                        file.Sha256 + ".lzma");
                algorithm = AlgoEnum.LZMA;
            }
            else
                throw new ArgumentException(string.Format("Cannot find file with hash {0} in the repository", file.Sha256));

            FileStream inFs = new FileStream(repoPath, FileMode.Open, FileAccess.Read);

            switch(algorithm)
            {
                case AlgoEnum.GZip:
                    zStream = new GZipStream(inFs, SharpCompress.Compressors.CompressionMode.Decompress);
                    break;
                case AlgoEnum.BZip2:
                    zStream = new BZip2Stream(inFs, SharpCompress.Compressors.CompressionMode.Decompress);
                    break;
                case AlgoEnum.LZMA:
                    byte[] properties = new byte[5];
                    inFs.Read(properties, 0, 5);
                    inFs.Seek(8, SeekOrigin.Current);
                    zStream = new LzmaStream(properties, inFs, inFs.Length - 13, file.Length);
                    break;
            }

            return zStream;
        }

        static void Zf_HandleClose(string entryName, Stream stream)
        {
            stream.Close();
        }

        static void Zf_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if(e.CurrentEntry != null && e.CurrentEntry.FileName != zipCurrentEntryName)
            {
                zipCurrentEntryName = e.CurrentEntry.FileName;
                zipCounter++;
            }

            if(UpdateProgress != null && e.CurrentEntry != null && e.EntriesTotal > 0)
                UpdateProgress("Compressing...", e.CurrentEntry.FileName, zipCounter, e.EntriesTotal);
            if(UpdateProgress2 != null && e.TotalBytesToTransfer > 0)
                UpdateProgress2(string.Format("{0:P}", e.BytesTransferred / (double)e.TotalBytesToTransfer),
                                string.Format("{0} / {1}", e.BytesTransferred, e.TotalBytesToTransfer),
                                e.BytesTransferred, e.TotalBytesToTransfer);

            if(e.EventType == ZipProgressEventType.Error_Saving && Failed != null)
                Failed("An error occurred creating ZIP file.");

            if(e.EventType == ZipProgressEventType.Saving_Completed && Finished != null)
                Finished();
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
    }
}
