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
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using DiscImageChef.Checksums;
using Ionic.Zip;
using Newtonsoft.Json;
using Schemas;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors.LZMA;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
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

                Md5Context md5 = new Md5Context();
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
                    case AlgoEnum.LZip:
                        extension = ".lz";
                        break;
                }

                long totalSize = 0, currentSize = 0;
                foreach (KeyValuePair<string, DBOSFile> file in Context.hashes)
                    totalSize += file.Value.Length;

#if DEBUG
                    stopwatch.Restart();
#endif
                foreach(KeyValuePair<string, DBOSFile> file in Context.hashes)
                {
                    if(UpdateProgress != null)
                        UpdateProgress("Compressing...", file.Value.Path, currentSize, totalSize);

                    destinationFolder = Path.Combine(Settings.Current.RepositoryPath, file.Value.Sha256[0].ToString(), file.Value.Sha256[1].ToString(), file.Value.Sha256[2].ToString(), file.Value.Sha256[3].ToString(), file.Value.Sha256[4].ToString());
                    Directory.CreateDirectory(destinationFolder);

                    destinationFile = Path.Combine(destinationFolder, file.Value.Sha256 + extension);

                    if (!File.Exists(destinationFile))
                    {
                        FileStream inFs = new FileStream(Path.Combine(filesPath, file.Value.Path), FileMode.Open, FileAccess.Read);
                        FileStream outFs = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write);
                        Stream zStream = null;

                        switch (Settings.Current.CompressionAlgorithm)
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
                            case AlgoEnum.LZip:
                                zStream = new LZipStream(outFs, SharpCompress.Compressors.CompressionMode.Compress);
                                break;
                        }

                        byte[] buffer = new byte[bufferSize];

                        while ((inFs.Position + bufferSize) <= inFs.Length)
                        {
                            if (UpdateProgress2 != null)
                                UpdateProgress2(string.Format("{0:P}", inFs.Position / (double)inFs.Length),
                                                string.Format("{0} / {1} bytes", inFs.Position, inFs.Length),
                                                inFs.Position, inFs.Length);
                            if (UpdateProgress != null)
                                UpdateProgress("Compressing...", file.Value.Path, currentSize, totalSize);

                            inFs.Read(buffer, 0, buffer.Length);
                            zStream.Write(buffer, 0, buffer.Length);
                            currentSize += buffer.Length;
                        }

                        buffer = new byte[inFs.Length - inFs.Position];
                        if (UpdateProgress2 != null)
                            UpdateProgress2(string.Format("{0:P}", inFs.Position / (double)inFs.Length),
                                            string.Format("{0} / {1} bytes", inFs.Position, inFs.Length),
                                            inFs.Position, inFs.Length);
                        if (UpdateProgress != null)
                            UpdateProgress("Compressing...", file.Value.Path, currentSize, totalSize);

                        inFs.Read(buffer, 0, buffer.Length);
                        zStream.Write(buffer, 0, buffer.Length);
                        currentSize += buffer.Length;

                        if (UpdateProgress2 != null)
                            UpdateProgress2(string.Format("{0:P}", inFs.Length / (double)inFs.Length),
                                            "Finishing...", inFs.Length, inFs.Length);

                        inFs.Close();
                        zStream.Close();
                        outFs.Dispose();
                    }
                    else
                        currentSize += file.Value.Length;
                }
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.CompressFiles(): Took {0} seconds to compress files", stopwatch.Elapsed.TotalSeconds);
#endif

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
            catch(ThreadAbortException)
            { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
#if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
#endif
            }
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

#if DEBUG
                stopwatch.Restart();
#endif
                Process lsarProcess = new Process();
                lsarProcess.StartInfo.FileName = lsarPath;
                lsarProcess.StartInfo.CreateNoWindow = true;
                lsarProcess.StartInfo.RedirectStandardOutput = true;
                lsarProcess.StartInfo.UseShellExecute = false;
                lsarProcess.StartInfo.Arguments = string.Format("-j \"\"\"{0}\"\"\"", Context.path);
                lsarProcess.Start();
                string lsarOutput = lsarProcess.StandardOutput.ReadToEnd();
                lsarProcess.WaitForExit();
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.OpenArchive(): Took {0} seconds to list archive contents", stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
#endif
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
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.OpenArchive(): Took {0} seconds to process archive contents", stopwatch.Elapsed.TotalSeconds);
#endif

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
#if DEBUG
                        stopwatch.Restart();
#endif
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
#if DEBUG
                        stopwatch.Stop();
                        Console.WriteLine("Core.OpenArchive(): Took {0} seconds to navigate in search of Mac OS X metadata", stopwatch.Elapsed.TotalSeconds);
#endif
                    }
                }

                if(Finished != null)
                    Finished();
            }
            catch(ThreadAbortException)
            { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
#if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
#endif
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
            catch(ThreadAbortException)
            { }
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
#if DEBUG
                        stopwatch.Restart();
#endif
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
#if DEBUG
                        Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
#endif
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

#if DEBUG
                    stopwatch.Restart();
#endif
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
#if DEBUG
                    stopwatch.Stop();
                    Console.WriteLine("Core.ExtractArchive(): Took {0} seconds to extract archive contents using UnAr", stopwatch.Elapsed.TotalSeconds);
#endif

                    if(Finished != null)
                        Finished();
                }
            }
            catch(ThreadAbortException)
            { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
#if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
#endif
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

            if(e.EventType == ZipProgressEventType.Extracting_AfterExtractAll && Finished != null)
            {
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.Zf_ExtractProgress(): Took {0} seconds to extract archive contents using DotNetZip", stopwatch.Elapsed.TotalSeconds);
#endif
                Finished();
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

                if(dbCore.DBOps.HasSymlinks(Context.dbInfo.id))
                {
                    if(Failed != null)
                        Failed("Cannot create symbolic links on ZIP files");
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

#if DEBUG
                stopwatch.Restart();
#endif
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
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.CompressTo(): Took {0} seconds to add folders to ZIP", stopwatch.Elapsed.TotalSeconds);
#endif

                counter = 3;
                Context.hashes = new Dictionary<string, DBOSFile>();
#if DEBUG
                stopwatch.Restart();
#endif
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
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.CompressTo(): Took {0} seconds to add files to ZIP", stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
#endif
                zipCounter = 0;
                zipCurrentEntryName = "";
                zf.Save();
            }
            catch(ThreadAbortException)
            { }
            catch(Exception ex)
            {
                if(Debugger.IsAttached)
                    throw;
                if(Failed != null)
                    Failed(string.Format("Exception {0}\n{1}", ex.Message, ex.InnerException));
#if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
#endif
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
            else if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                        file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                        file.Sha256 + ".lz")))
            {
                repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                        file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                        file.Sha256 + ".lz");
                algorithm = AlgoEnum.LZip;
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
                case AlgoEnum.LZip:
                    zStream = new LZipStream(inFs, SharpCompress.Compressors.CompressionMode.Decompress);
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
            {
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.Zf_SaveProgress(): Took {0} seconds to compress files to ZIP", stopwatch.Elapsed.TotalSeconds);
#endif
                Finished();
            }
        }
    }
}
