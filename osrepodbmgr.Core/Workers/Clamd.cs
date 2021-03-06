﻿//
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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using nClam;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors.LZMA;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        static ClamClient clam;

        public static void InitClamd()
        {
            if(!Settings.Current.UseClamd || !Settings.Current.UseAntivirus)
            {
                Context.ClamdVersion = null;
                return;
            }

            TestClamd();
        }

        public static void TestClamd()
        {
            Task.Run(async () =>
            {
                try
                {
                    clam                 = new ClamClient(Settings.Current.ClamdHost, Settings.Current.ClamdPort);
                    Context.ClamdVersion = await clam.GetVersionAsync();
                }
                catch(SocketException) { }
            }).Wait();
        }

        public static void ClamScanFileFromRepo(DbFile file)
        {
            try
            {
                if(Context.ClamdVersion == null)
                {
                    Failed?.Invoke("clamd is not usable");
                    return;
                }

                if(clam == null) Failed?.Invoke("clamd is not initalized");

                string   repoPath;
                AlgoEnum algorithm;

                if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                            file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                            file.Sha256[3].ToString(), file.Sha256[4].ToString(), file.Sha256 + ".gz")))
                {
                    repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                            file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                            file.Sha256[3].ToString(), file.Sha256[4].ToString(), file.Sha256 + ".gz");
                    algorithm = AlgoEnum.GZip;
                }
                else if(File.Exists(Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                                 file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                                 file.Sha256[3].ToString(), file.Sha256[4].ToString(),
                                                 file.Sha256 + ".bz2")))
                {
                    repoPath = Path.Combine(Settings.Current.RepositoryPath, file.Sha256[0].ToString(),
                                            file.Sha256[1].ToString(), file.Sha256[2].ToString(),
                                            file.Sha256[3].ToString(), file.Sha256[4].ToString(), file.Sha256 + ".bz2");
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
                                            file.Sha256[3].ToString(), file.Sha256[4].ToString(), file.Sha256 + ".lz");
                    algorithm = AlgoEnum.LZip;
                }
                else
                {
                    Failed?.Invoke($"Cannot find file with hash {file.Sha256} in the repository");
                    return;
                }

                ClamScanResult result  = null;
                Stream         zStream = null;

                if(Settings.Current.ClamdIsLocal)
                    if(algorithm == AlgoEnum.LZMA || algorithm == AlgoEnum.LZip)
                    {
                        string     tmpFile = Path.Combine(Settings.Current.TemporaryFolder, Path.GetTempFileName());
                        FileStream outFs   = new FileStream(tmpFile,  FileMode.Create, FileAccess.Write);
                        FileStream inFs    = new FileStream(repoPath, FileMode.Open,   FileAccess.Read);

                        if(algorithm == AlgoEnum.LZMA)
                        {
                            byte[] properties = new byte[5];
                            inFs.Read(properties, 0, 5);
                            inFs.Seek(8, SeekOrigin.Current);
                            zStream = new LzmaStream(properties, inFs, inFs.Length - 13, file.Length);
                        }
                        else zStream = new LZipStream(inFs, CompressionMode.Decompress);

                        UpdateProgress?.Invoke("Uncompressing file...", null, 0, 0);

                        #if DEBUG
                        stopwatch.Restart();
                        #endif
                        zStream.CopyTo(outFs);
                        zStream.Close();
                        outFs.Close();
                        #if DEBUG
                        stopwatch.Stop();
                        Console.WriteLine("Core.ClamScanFileFromRepo({0}): Uncompressing took {1} seconds", file,
                                          stopwatch.Elapsed.TotalSeconds);
                        #endif

                        UpdateProgress?.Invoke("Requesting local scan to clamd server...", null, 0, 0);

                        #if DEBUG
                        stopwatch.Restart();
                        #endif
                        Task.Run(async () => { result = await clam.ScanFileOnServerMultithreadedAsync(tmpFile); })
                            .Wait();
                        #if DEBUG
                        stopwatch.Stop();
                        Console.WriteLine("Core.ClamScanFileFromRepo({0}): Clamd took {1} seconds to scan", file,
                                          stopwatch.Elapsed.TotalSeconds);
                        #endif

                        File.Delete(tmpFile);
                    }
                    else
                    {
                        UpdateProgress?.Invoke("Requesting local scan to clamd server...", null, 0, 0);

                        #if DEBUG
                        stopwatch.Restart();
                        #endif
                        Task.Run(async () => { result = await clam.ScanFileOnServerMultithreadedAsync(repoPath); })
                            .Wait();
                        #if DEBUG
                        stopwatch.Stop();
                        Console.WriteLine("Core.ClamScanFileFromRepo({0}): Clamd took {1} seconds to scan", file,
                                          stopwatch.Elapsed.TotalSeconds);
                        #endif
                    }
                else
                {
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

                    UpdateProgress?.Invoke("Uploading file to clamd server...", null, 0, 0);

                    #if DEBUG
                    stopwatch.Restart();
                    #endif
                    Task.Run(async () => { result = await clam.SendAndScanFileAsync(zStream); }).Wait();
                    #if DEBUG
                    stopwatch.Stop();
                    Console.WriteLine("Core.ClamScanFileFromRepo({0}): Clamd took {1} seconds to scan", file,
                                      stopwatch.Elapsed.TotalSeconds);
                    #endif
                    zStream.Close();
                }

                if(result.InfectedFiles != null && result.InfectedFiles.Count > 0)
                {
                    file.HasVirus = true;
                    file.Virus    = result.InfectedFiles[0].VirusName;
                }
                else if(file.HasVirus == null)
                {
                    // If no scan has been done, mark as false.
                    // If a positive has already existed don't overwrite it.
                    file.HasVirus = false;
                    file.Virus    = null;
                }

                file.ClamTime = DateTime.UtcNow;

                dbCore.DbOps.UpdateFile(file);

                ScanFinished?.Invoke(file);
            }
            catch(ThreadAbortException) { }
            catch(Exception ex)
            {
                Failed?.Invoke($"Exception {ex.Message} when calling clamd");
                #if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                #endif
            }
        }

        public static void ClamScanAllFiles()
        {
            UpdateProgress2?.Invoke("Asking database for files", null, 0, 0);

            #if DEBUG
            stopwatch.Restart();
            #endif

            if(!dbCore.DbOps.GetNotAvFiles(out List<DbFile> files))
                Failed?.Invoke("Could not get files from database.");
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.ClamScanAllFiles(): Took {0} seconds to get files from database",
                              stopwatch.Elapsed.TotalSeconds);
            stopwatch.Restart();
            #endif
            int counter = 0;
            foreach(DbFile file in files)
            {
                UpdateProgress2?.Invoke($"Scanning file {counter} of {files.Count}", null, counter, files.Count);

                ClamScanFileFromRepo(file);

                counter++;
            }
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.ClamScanAllFiles(): Took {0} seconds scan all pending files",
                              stopwatch.Elapsed.TotalSeconds);
            #endif

            Finished?.Invoke();
        }
    }
}