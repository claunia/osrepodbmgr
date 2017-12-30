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
using SharpCompress.Compressors;
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
                if(string.IsNullOrWhiteSpace(Context.DbInfo.Developer))
                {
                    Failed?.Invoke("Developer cannot be empty");
                    return;
                }

                if(string.IsNullOrWhiteSpace(Context.DbInfo.Product))
                {
                    Failed?.Invoke("Product cannot be empty");
                    return;
                }

                if(string.IsNullOrWhiteSpace(Context.DbInfo.Version))
                {
                    Failed?.Invoke("Version cannot be empty");
                    return;
                }

                string destinationFolder = "";
                destinationFolder        = Path.Combine(destinationFolder, Context.DbInfo.Developer);
                destinationFolder        = Path.Combine(destinationFolder, Context.DbInfo.Product);
                destinationFolder        = Path.Combine(destinationFolder, Context.DbInfo.Version);
                if(!string.IsNullOrWhiteSpace(Context.DbInfo.Languages))
                    destinationFolder = Path.Combine(destinationFolder, Context.DbInfo.Languages);
                if(!string.IsNullOrWhiteSpace(Context.DbInfo.Architecture))
                    destinationFolder                    = Path.Combine(destinationFolder, Context.DbInfo.Architecture);
                if(Context.DbInfo.Oem) destinationFolder = Path.Combine(destinationFolder, "oem");
                if(!string.IsNullOrWhiteSpace(Context.DbInfo.Machine))
                    destinationFolder = Path.Combine(destinationFolder, "for " + Context.DbInfo.Machine);

                string destinationFile = "";
                if(!string.IsNullOrWhiteSpace(Context.DbInfo.Format))
                    destinationFile += "[" + Context.DbInfo.Format + "]";
                if(Context.DbInfo.Files)
                {
                    if(destinationFile != "") destinationFile += "_";
                    destinationFile                           += "files";
                }

                if(Context.DbInfo.Netinstall)
                {
                    if(destinationFile != "") destinationFile += "_";
                    destinationFile                           += "netinstall";
                }

                if(Context.DbInfo.Source)
                {
                    if(destinationFile != "") destinationFile += "_";
                    destinationFile                           += "source";
                }

                if(Context.DbInfo.Update)
                {
                    if(destinationFile != "") destinationFile += "_";
                    destinationFile                           += "update";
                }

                if(Context.DbInfo.Upgrade)
                {
                    if(destinationFile != "") destinationFile += "_";
                    destinationFile                           += "upgrade";
                }

                if(!string.IsNullOrWhiteSpace(Context.DbInfo.Description))
                {
                    if(destinationFile != "") destinationFile += "_";
                    destinationFile                           += Context.DbInfo.Description;
                }
                else if(destinationFile == "")
                    destinationFile = "archive";

                string destination = Path.Combine(destinationFolder, destinationFile) + ".zip";

                Md5Context md5 = new Md5Context();
                md5.Init();
                byte[] tmp;
                string mdid = md5.Data(Encoding.UTF8.GetBytes(destination), out tmp);
                Console.WriteLine("MDID: {0}", mdid);

                if(dbCore.DbOps.ExistsOs(mdid))
                {
                    if(File.Exists(destination))
                    {
                        Failed?.Invoke("OS already exists.");
                        return;
                    }

                    Failed?.Invoke("OS already exists in the database but not in the repository, check for inconsistencies.");
                    return;
                }

                if(File.Exists(destination))
                {
                    Failed?.Invoke("OS already exists in the repository but not in the database, check for inconsistencies.");
                    return;
                }

                Context.DbInfo.Mdid = mdid;

                string filesPath;

                if(!string.IsNullOrEmpty(Context.TmpFolder) && Directory.Exists(Context.TmpFolder))
                    filesPath  = Context.TmpFolder;
                else filesPath = Context.Path;

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

                long totalSize                                                           = 0, currentSize = 0;
                foreach(KeyValuePair<string, DbOsFile> file in Context.Hashes) totalSize += file.Value.Length;

                #if DEBUG
                stopwatch.Restart();
                #endif
                foreach(KeyValuePair<string, DbOsFile> file in Context.Hashes)
                {
                    UpdateProgress?.Invoke("Compressing...", file.Value.Path, currentSize, totalSize);

                    destinationFolder = Path.Combine(Settings.Current.RepositoryPath, file.Value.Sha256[0].ToString(),
                                                     file.Value.Sha256[1].ToString(), file.Value.Sha256[2].ToString(),
                                                     file.Value.Sha256[3].ToString(), file.Value.Sha256[4].ToString());
                    Directory.CreateDirectory(destinationFolder);

                    destinationFile = Path.Combine(destinationFolder, file.Value.Sha256 + extension);

                    if(!File.Exists(destinationFile))
                    {
                        FileStream inFs = new FileStream(Path.Combine(filesPath, file.Value.Path), FileMode.Open,
                                                         FileAccess.Read);
                        FileStream outFs   = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write);
                        Stream     zStream = null;

                        switch(Settings.Current.CompressionAlgorithm)
                        {
                            case AlgoEnum.GZip:
                                zStream = new GZipStream(outFs, CompressionMode.Compress,
                                                         CompressionLevel.BestCompression);
                                break;
                            case AlgoEnum.BZip2:
                                zStream = new BZip2Stream(outFs, CompressionMode.Compress);
                                break;
                            case AlgoEnum.LZMA:
                                zStream = new LzmaStream(new LzmaEncoderProperties(), false, outFs);
                                outFs.Write(((LzmaStream)zStream).Properties, 0,
                                            ((LzmaStream)zStream).Properties.Length);
                                outFs.Write(BitConverter.GetBytes(inFs.Length), 0, 8);
                                break;
                            case AlgoEnum.LZip:
                                zStream = new LZipStream(outFs, CompressionMode.Compress);
                                break;
                        }

                        byte[] buffer = new byte[BUFFER_SIZE];

                        while(inFs.Position + BUFFER_SIZE <= inFs.Length)
                        {
                            UpdateProgress2?.Invoke($"{inFs.Position / (double)inFs.Length:P}",
                                                    $"{inFs.Position} / {inFs.Length} bytes", inFs.Position,
                                                    inFs.Length);
                            UpdateProgress?.Invoke("Compressing...", file.Value.Path, currentSize, totalSize);

                            inFs.Read(buffer, 0, buffer.Length);
                            zStream.Write(buffer, 0, buffer.Length);
                            currentSize += buffer.Length;
                        }

                        buffer = new byte[inFs.Length - inFs.Position];
                        UpdateProgress2?.Invoke($"{inFs.Position / (double)inFs.Length:P}",
                                                $"{inFs.Position} / {inFs.Length} bytes", inFs.Position, inFs.Length);
                        UpdateProgress?.Invoke("Compressing...", file.Value.Path, currentSize, totalSize);

                        inFs.Read(buffer, 0, buffer.Length);
                        zStream.Write(buffer, 0, buffer.Length);
                        currentSize += buffer.Length;

                        UpdateProgress2?.Invoke($"{inFs.Length / (double)inFs.Length:P}", "Finishing...", inFs.Length,
                                                inFs.Length);

                        inFs.Close();
                        zStream.Close();
                        outFs.Dispose();
                    }
                    else currentSize += file.Value.Length;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.CompressFiles(): Took {0} seconds to compress files",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                if(Context.Metadata != null)
                {
                    MemoryStream  xms = new MemoryStream();
                    XmlSerializer xs  = new XmlSerializer(typeof(CICMMetadataType));
                    xs.Serialize(xms, Context.Metadata);
                    xms.Position = 0;

                    JsonSerializer js = new JsonSerializer
                    {
                        Formatting        = Formatting.Indented,
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    MemoryStream jms = new MemoryStream();
                    StreamWriter sw  = new StreamWriter(jms, Encoding.UTF8, 1048576, true);
                    js.Serialize(sw, Context.Metadata, typeof(CICMMetadataType));
                    sw.Close();
                    jms.Position = 0;

                    destinationFolder = Path.Combine(Settings.Current.RepositoryPath, "metadata", mdid[0].ToString(),
                                                     mdid[1].ToString(), mdid[2].ToString(), mdid[3].ToString(),
                                                     mdid[4].ToString());
                    Directory.CreateDirectory(destinationFolder);

                    FileStream xfs = new FileStream(Path.Combine(destinationFolder, mdid + ".xml"), FileMode.CreateNew,
                                                    FileAccess.Write);
                    xms.CopyTo(xfs);
                    xfs.Close();
                    FileStream jfs = new FileStream(Path.Combine(destinationFolder, mdid + ".json"), FileMode.CreateNew,
                                                    FileAccess.Write);
                    jms.CopyTo(jfs);
                    jfs.Close();

                    xms.Position = 0;
                    jms.Position = 0;
                }

                FinishedWithText?.Invoke($"Correctly added operating system with MDID {mdid}");
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

        public static void OpenArchive()
        {
            if(!Context.UnarUsable)
            {
                Failed?.Invoke("The UnArchiver is not correctly installed");
                return;
            }

            if(!File.Exists(Context.Path))
            {
                Failed?.Invoke("Specified file cannot be found");
                return;
            }

            try
            {
                string unarFolder   = Path.GetDirectoryName(Settings.Current.UnArchiverPath);
                string extension    = Path.GetExtension(Settings.Current.UnArchiverPath);
                string unarfilename = Path.GetFileNameWithoutExtension(Settings.Current.UnArchiverPath);
                string lsarfilename = unarfilename?.Replace("unar", "lsar");
                string lsarPath     = Path.Combine(unarFolder, lsarfilename + extension);

                #if DEBUG
                stopwatch.Restart();
                #endif
                Process lsarProcess = new Process
                {
                    StartInfo =
                    {
                        FileName               = lsarPath,
                        CreateNoWindow         = true,
                        RedirectStandardOutput = true,
                        UseShellExecute        = false,
                        Arguments              = $"-j \"\"\"{Context.Path}\"\"\""
                    }
                };
                lsarProcess.Start();
                string lsarOutput = lsarProcess.StandardOutput.ReadToEnd();
                lsarProcess.WaitForExit();
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.OpenArchive(): Took {0} seconds to list archive contents",
                                  stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
                #endif
                long           counter  = 0;
                string         format   = null;
                JsonTextReader jsReader = new JsonTextReader(new StringReader(lsarOutput));
                while(jsReader.Read())
                    switch(jsReader.TokenType)
                    {
                        case JsonToken.PropertyName when jsReader.Value            != null &&
                                                         jsReader.Value.ToString() == "XADFileName":
                            counter++;
                            break;
                        case JsonToken.PropertyName when jsReader.Value            != null &&
                                                         jsReader.Value.ToString() == "lsarFormatName":
                            jsReader.Read();
                            if(jsReader.TokenType == JsonToken.String && jsReader.Value != null)
                                format = jsReader.Value.ToString();
                            break;
                    }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.OpenArchive(): Took {0} seconds to process archive contents",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                Context.UnzipWithUnAr    = false;
                Context.ArchiveFormat    = format;
                Context.NoFilesInArchive = counter;

                if(string.IsNullOrEmpty(format))
                {
                    Failed?.Invoke("File not recognized as an archive");
                    return;
                }

                if(counter == 0)
                {
                    Failed?.Invoke("Archive contains no files");
                    return;
                }

                if(Context.ArchiveFormat == "Zip")
                {
                    Context.UnzipWithUnAr = false;

                    if(Context.UsableDotNetZip)
                    {
                        #if DEBUG
                        stopwatch.Restart();
                        #endif
                        ZipFile zf = ZipFile.Read(Context.Path, new ReadOptions {Encoding = Encoding.UTF8});
                        foreach(ZipEntry ze in zf)
                        {
                            // ZIP created with Mac OS X, need to be extracted with The UnArchiver to get correct ResourceFork structure
                            if(!ze.FileName.StartsWith("__MACOSX", StringComparison.CurrentCulture)) continue;

                            Context.UnzipWithUnAr = true;
                            break;
                        }
                        #if DEBUG
                        stopwatch.Stop();
                        Console.WriteLine("Core.OpenArchive(): Took {0} seconds to navigate in search of Mac OS X metadata",
                                          stopwatch.Elapsed.TotalSeconds);
                        #endif
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

        public static void ExtractArchive()
        {
            if(!File.Exists(Context.Path))
            {
                Failed?.Invoke("Specified file cannot be found");
                return;
            }

            if(!Directory.Exists(Settings.Current.TemporaryFolder))
            {
                Failed?.Invoke("Temporary folder cannot be found");
                return;
            }

            string tmpFolder = Context.UserExtracting
                                   ? Context.TmpFolder
                                   : Path.Combine(Settings.Current.TemporaryFolder, Path.GetRandomFileName());

            try
            {
                Directory.CreateDirectory(tmpFolder);

                Context.TmpFolder = tmpFolder;
            }
            catch(ThreadAbortException) { }
            catch(Exception)
            {
                if(Debugger.IsAttached) throw;

                Failed?.Invoke("Cannot create temporary folder");
            }

            try
            {
                // If it's a ZIP file not created by Mac OS X, use DotNetZip to uncompress (unar freaks out or corrupts certain ZIP features)
                if(Context.ArchiveFormat == "Zip" && !Context.UnzipWithUnAr && Context.UsableDotNetZip)
                    try
                    {
                        #if DEBUG
                        stopwatch.Restart();
                        #endif
                        ZipFile zf             = ZipFile.Read(Context.Path, new ReadOptions {Encoding = Encoding.UTF8});
                        zf.ExtractExistingFile =  ExtractExistingFileAction.OverwriteSilently;
                        zf.ExtractProgress     += Zf_ExtractProgress;
                        zipCounter             =  0;
                        zipCurrentEntryName    =  "";
                        zf.ExtractAll(tmpFolder);
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
                else
                {
                    if(!Context.UnarUsable)
                    {
                        Failed?.Invoke("The UnArchiver is not correctly installed");
                        return;
                    }

                    #if DEBUG
                    stopwatch.Restart();
                    #endif
                    Context.UnarProcess = new Process
                    {
                        StartInfo =
                        {
                            FileName               = Settings.Current.UnArchiverPath,
                            CreateNoWindow         = true,
                            RedirectStandardOutput = true,
                            UseShellExecute        = false,
                            Arguments              =
                                $"-o \"\"\"{tmpFolder}\"\"\" -r -D -k hidden \"\"\"{Context.Path}\"\"\""
                        }
                    };
                    long counter                           = 0;
                    Context.UnarProcess.OutputDataReceived += (sender, e) =>
                    {
                        counter++;
                        UpdateProgress2?.Invoke("", e.Data, counter, Context.NoFilesInArchive);
                    };
                    Context.UnarProcess.Start();
                    Context.UnarProcess.BeginOutputReadLine();
                    Context.UnarProcess.WaitForExit();
                    Context.UnarProcess.Close();
                    Context.UnarProcess = null;
                    #if DEBUG
                    stopwatch.Stop();
                    Console.WriteLine("Core.ExtractArchive(): Took {0} seconds to extract archive contents using UnAr",
                                      stopwatch.Elapsed.TotalSeconds);
                    #endif

                    Finished?.Invoke();
                }
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
                UpdateProgress2($"{e.BytesTransferred / (double)e.TotalBytesToTransfer:P}",
                                $"{e.BytesTransferred} / {e.TotalBytesToTransfer}", e.BytesTransferred,
                                e.TotalBytesToTransfer);

            if(e.EventType != ZipProgressEventType.Extracting_AfterExtractAll || Finished == null) return;
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.Zf_ExtractProgress(): Took {0} seconds to extract archive contents using DotNetZip",
                              stopwatch.Elapsed.TotalSeconds);
            #endif
            Finished();
        }

        public static void CompressTo()
        {
            try
            {
                if(string.IsNullOrWhiteSpace(Context.Path))
                {
                    Failed?.Invoke("Destination cannot be empty");
                    return;
                }

                if(Directory.Exists(Context.Path))
                {
                    Failed?.Invoke("Destination cannot be a folder");
                    return;
                }

                if(Context.DbInfo.Id == 0)
                {
                    Failed?.Invoke("Operating system must be set");
                    return;
                }

                if(dbCore.DbOps.HasSymlinks(Context.DbInfo.Id))
                {
                    Failed?.Invoke("Cannot create symbolic links on ZIP files");
                    return;
                }

                if(!Context.UsableDotNetZip)
                {
                    Failed?.Invoke("Cannot create ZIP files");
                    return;
                }

                ZipFile zf = new ZipFile(Context.Path, Encoding.UTF8)
                {
                    CompressionLevel                   = Ionic.Zlib.CompressionLevel.BestCompression,
                    CompressionMethod                  = CompressionMethod.Deflate,
                    EmitTimesInUnixFormatWhenSaving    = true,
                    EmitTimesInWindowsFormatWhenSaving = true,
                    UseZip64WhenSaving                 = Zip64Option.AsNecessary,
                    SortEntriesBeforeSaving            = true
                };
                zf.SaveProgress += Zf_SaveProgress;

                UpdateProgress?.Invoke("", "Asking DB for files...", 1, 100);

                dbCore.DbOps.GetAllFilesInOs(out List<DbOsFile> files, Context.DbInfo.Id);

                UpdateProgress?.Invoke("", "Asking DB for folders...", 2, 100);

                dbCore.DbOps.GetAllFolders(out List<DbFolder> folders, Context.DbInfo.Id);

                UpdateProgress?.Invoke("", "Creating folders...", 3, 100);

                #if DEBUG
                stopwatch.Restart();
                #endif
                long counter = 0;
                foreach(DbFolder folder in folders)
                {
                    UpdateProgress2?.Invoke("", folder.Path, counter, folders.Count);

                    ZipEntry zd     = zf.AddDirectoryByName(folder.Path);
                    zd.Attributes   = folder.Attributes;
                    zd.CreationTime = folder.CreationTimeUtc;
                    zd.AccessedTime = folder.LastAccessTimeUtc;
                    zd.LastModified = folder.LastWriteTimeUtc;
                    zd.ModifiedTime = folder.LastWriteTimeUtc;

                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.CompressTo(): Took {0} seconds to add folders to ZIP",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                counter        = 3;
                Context.Hashes = new Dictionary<string, DbOsFile>();
                #if DEBUG
                stopwatch.Restart();
                #endif
                foreach(DbOsFile file in files)
                {
                    UpdateProgress?.Invoke("", $"Adding {file.Path}...", counter, 3 + files.Count);

                    Context.Hashes.Add(file.Path, file);

                    ZipEntry zi     = zf.AddEntry(file.Path, Zf_HandleOpen, Zf_HandleClose);
                    zi.Attributes   = file.Attributes;
                    zi.CreationTime = file.CreationTimeUtc;
                    zi.AccessedTime = file.LastAccessTimeUtc;
                    zi.LastModified = file.LastWriteTimeUtc;
                    zi.ModifiedTime = file.LastWriteTimeUtc;

                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.CompressTo(): Took {0} seconds to add files to ZIP",
                                  stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
                #endif
                zipCounter          = 0;
                zipCurrentEntryName = "";
                zf.Save();
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

        static Stream Zf_HandleOpen(string entryName)
        {
            DbOsFile file;
            if(!Context.Hashes.TryGetValue(entryName,                        out file))
                if(!Context.Hashes.TryGetValue(entryName.Replace('/', '\\'), out file))
                    throw new ArgumentException("Cannot find requested zip entry in hashes dictionary");

            // Special case for empty file, as it seems to crash when SharpCompress tries to unLZMA it.
            if(file.Length == 0) return new MemoryStream();

            Stream   zStream = null;
            string   repoPath;
            AlgoEnum algorithm;

            if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(), file.Sha256[3].ToString(),
                                        file.Sha256[4].ToString(), file.Sha256 + ".gz")))
            {
                repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(), file.Sha256[3].ToString(),
                                        file.Sha256[4].ToString(), file.Sha256 + ".gz");
                algorithm = AlgoEnum.GZip;
            }
            else if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                             file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                             file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                             file.Sha256 + ".bz2")))
            {
                repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(), file.Sha256[3].ToString(),
                                        file.Sha256[4].ToString(), file.Sha256 + ".bz2");
                algorithm = AlgoEnum.BZip2;
            }
            else if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                             file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                             file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                             file.Sha256 + ".lzma")))
            {
                repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(), file.Sha256[3].ToString(),
                                        file.Sha256[4].ToString(), file.Sha256 + ".lzma");
                algorithm = AlgoEnum.LZMA;
            }
            else if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                             file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                             file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                             file.Sha256 + ".lz")))
            {
                repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                        file.Sha256[1].ToString(), file.Sha256[2].ToString(), file.Sha256[3].ToString(),
                                        file.Sha256[4].ToString(), file.Sha256 + ".lz");
                algorithm = AlgoEnum.LZip;
            }
            else
                throw new ArgumentException($"Cannot find file with hash {file.Sha256} in the repository");

            FileStream inFs = new FileStream(repoPath, FileMode.Open, FileAccess.Read);

            switch(algorithm)
            {
                case AlgoEnum.GZip:
                    zStream = new GZipStream(inFs, CompressionMode.Decompress);
                    break;
                case AlgoEnum.BZip2:
                    zStream = new BZip2Stream(inFs, CompressionMode.Decompress);
                    break;
                case AlgoEnum.LZMA:
                    byte[] properties = new byte[5];
                    inFs.Read(properties, 0, 5);
                    inFs.Seek(8, SeekOrigin.Current);
                    zStream = new LzmaStream(properties, inFs, inFs.Length - 13, file.Length);
                    break;
                case AlgoEnum.LZip:
                    zStream = new LZipStream(inFs, CompressionMode.Decompress);
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
                UpdateProgress2($"{e.BytesTransferred / (double)e.TotalBytesToTransfer:P}",
                                $"{e.BytesTransferred} / {e.TotalBytesToTransfer}", e.BytesTransferred,
                                e.TotalBytesToTransfer);

            switch(e.EventType)
            {
                case ZipProgressEventType.Error_Saving:
                    Failed?.Invoke("An error occurred creating ZIP file.");
                    break;
                case ZipProgressEventType.Saving_Completed when Finished != null:
                    #if DEBUG
                    stopwatch.Stop();
                    Console.WriteLine("Core.Zf_SaveProgress(): Took {0} seconds to compress files to ZIP",
                                      stopwatch.Elapsed.TotalSeconds);
                    #endif
                    Finished();
                    break;
            }
        }
    }
}