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
using nClam;
using System.Threading.Tasks;
using System.IO;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors.BZip2;
using System.Threading;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        static ClamClient clam;

        public static void InitClamd()
        {
            if(!Settings.Current.UseClamd || !Settings.Current.UseAntivirus)
            {
                Context.clamdVersion = null;
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
                    clam = new ClamClient(Settings.Current.ClamdHost, Settings.Current.ClamdPort);
                    Context.clamdVersion = await clam.GetVersionAsync();
                }
                catch(System.Net.Sockets.SocketException)
                {

                }
            }).Wait();
        }

        public static void ClamScanFileFromRepo(DBFile file)
        {
            try
            {
                if(Context.clamdVersion == null)
                {
                    if(Failed != null)
                        Failed("clamd is not usable");
                    return;
                }

                if(clam == null)
                {
                    if(Failed != null)
                        Failed("clamd is not initalized");
                }

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

                ClamScanResult result = null;
                Stream zStream = null;

                if(Settings.Current.ClamdIsLocal)
                {
                    // clamd supports gzip and bzip2 but not lzma
                    if(algorithm == AlgoEnum.LZMA)
                    {
                        string tmpFile = Path.Combine(Settings.Current.TemporaryFolder, Path.GetTempFileName());
                        FileStream outFs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write);
                        FileStream inFs = new FileStream(repoPath, FileMode.Open, FileAccess.Read);

                        byte[] properties = new byte[5];
                        inFs.Read(properties, 0, 5);
                        inFs.Seek(8, SeekOrigin.Current);
                        zStream = new LzmaStream(properties, inFs);

                        if(UpdateProgress != null)
                            UpdateProgress("Uncompressing file...", null, 0, 0);

                        zStream.CopyTo(outFs);
                        zStream.Close();
                        outFs.Close();

                        if(UpdateProgress != null)
                            UpdateProgress("Requesting local scan to clamd server...", null, 0, 0);

                        Task.Run(async () =>
                        {
                            result = await clam.ScanFileOnServerMultithreadedAsync(tmpFile);
                        }).Wait();

                        File.Delete(tmpFile);
                    }
                    else
                    {
                        if(UpdateProgress != null)
                            UpdateProgress("Requesting local scan to clamd server...", null, 0, 0);

                        Task.Run(async () =>
                        {
                            result = await clam.ScanFileOnServerMultithreadedAsync(repoPath);
                        }).Wait();
                    }
                }
                else
                {
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
                            zStream = new LzmaStream(properties, inFs);
                            break;
                    }

                    if(UpdateProgress != null)
                        UpdateProgress("Uploading file to clamd server...", null, 0, 0);

                    Task.Run(async () =>
                    {
                        result = await clam.SendAndScanFileAsync(zStream);
                    }).Wait();
                    zStream.Close();
                }

                if(result.InfectedFiles != null && result.InfectedFiles.Count > 0)
                {
                    file.HasVirus = true;
                    file.Virus = result.InfectedFiles[0].VirusName;
                }
                else if(file.HasVirus == null)
                {
                    // If no scan has been done, mark as false.
                    // If a positive has already existed don't overwrite it.
                    file.HasVirus = false;
                    file.Virus = null;
                }
                file.ClamTime = DateTime.UtcNow;

                dbCore.DBOps.UpdateFile(file);

                if(ScanFinished != null)
                    ScanFinished(file);

                return;
            }
            catch(Exception ex)
            {
                if(Failed != null)
                    Failed(string.Format("Exception {0} when calling clamd", ex.Message));
            }
        }
    }
}
