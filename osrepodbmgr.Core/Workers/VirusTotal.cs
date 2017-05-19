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
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors.LZMA;
using VirusTotalNET;
using VirusTotalNET.Objects;
using VirusTotalNET.Results;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        static VirusTotal vTotal;

        public static bool TestVirusTotal(string key)
        {
            VirusTotal vt;
            FileReport report = null;

            try
            {
                Task.Run(async () =>
                {
                    vt = new VirusTotal(key);
                    report = await vt.GetFileReport("b82758fc5f737a58078d3c60e2798a70d895443a86aa39adf52dec70e98c2bed");
                }).Wait();
            }
            catch(Exception ex)
            {
                if(Failed != null)
                    Failed(ex.InnerException.Message);
                return false;
            }

            return report != null && report.MD5 == "0bf60adb1435639a42b490e7e80d25c7";
        }

        public static bool InitVirusTotal(string key)
        {
            VirusTotal vt = null;
            FileReport report = null;

            try
            {
                Task.Run(async () =>
                {
                    vt = new VirusTotal(key);
                    report = await vt.GetFileReport("b82758fc5f737a58078d3c60e2798a70d895443a86aa39adf52dec70e98c2bed");
                }).Wait();
            }
            catch(Exception ex)
            {
                if(Failed != null)
                    Failed(ex.InnerException.Message);
                return false;
            }

            if(report != null && report.MD5 == "0bf60adb1435639a42b490e7e80d25c7")
            {
                vTotal = vt;
                Context.virusTotalEnabled = true;
                return true;
            }
            return false;
        }

        public static void VirusTotalFileFromRepo(DBFile file)
        {
            try
            {
                if(!Context.virusTotalEnabled)
                {
                    if(Failed != null)
                        Failed("VirusTotal is not usable");
                    return;
                }

                if(vTotal == null)
                {
                    if(Failed != null)
                        Failed("VirusTotal is not initalized");
                }

                FileReport fResult = null;

                if(UpdateProgress != null)
                    UpdateProgress("Requesting existing report to VirusTotal", null, 0, 0);

#if DEBUG
                stopwatch.Restart();
#endif
                Task.Run(async () =>
                {
                    fResult = await vTotal.GetFileReport(file.Sha256);
                }).Wait();
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.VirusTotalFileFromRepo({0}): VirusTotal took {1} seconds to answer for SHA256 request", file, stopwatch.Elapsed.TotalSeconds);
#endif

                if(fResult.ResponseCode == VirusTotalNET.ResponseCodes.ReportResponseCode.Error)
                {
                    if(Failed != null)
                        Failed(fResult.VerboseMsg);
                    return;
                }

                if(fResult.ResponseCode != VirusTotalNET.ResponseCodes.ReportResponseCode.StillQueued)
                {
                    if(fResult.ResponseCode == VirusTotalNET.ResponseCodes.ReportResponseCode.Present)
                    {
                        if(fResult.Positives > 0)
                        {
                            file.HasVirus = true;
                            if(fResult.Scans != null)
                            {
                                foreach(KeyValuePair<string, ScanEngine> engine in fResult.Scans)
                                {
                                    if(engine.Value.Detected)
                                    {
                                        file.Virus = engine.Value.Result;
                                        file.VirusTotalTime = engine.Value.Update;
                                        dbCore.DBOps.UpdateFile(file);

                                        if(ScanFinished != null)
                                            ScanFinished(file);

                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If no scan has been done, mark as false.
                            // If a positive has already existed don't overwrite it.
                            file.HasVirus = false;
                            file.Virus = null;
                            file.VirusTotalTime = DateTime.UtcNow;

                            dbCore.DBOps.UpdateFile(file);

                            if(ScanFinished != null)
                                ScanFinished(file);

                            return;
                        }
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

                    if(UpdateProgress != null)
                        UpdateProgress("Uncompressing file...", null, 0, 0);

                    FileStream inFs = new FileStream(repoPath, FileMode.Open, FileAccess.Read);
                    Stream zStream = null;

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

                    ScanResult sResult = null;

#if DEBUG
                    stopwatch.Restart();
#endif
                    // Cannot use zStream directly, VirusTotal.NET requests the size *sigh*
                    string tmpFile = Path.Combine(Settings.Current.TemporaryFolder, Path.GetTempFileName());
                    FileStream outFs = new FileStream(tmpFile, FileMode.Create, FileAccess.ReadWrite);
                    zStream.CopyTo(outFs);
                    zStream.Close();
                    outFs.Seek(0, SeekOrigin.Begin);
#if DEBUG
                    stopwatch.Stop();
                    Console.WriteLine("Core.VirusTotalFileFromRepo({0}): Uncompressing took {1} seconds", file, stopwatch.Elapsed.TotalSeconds);
#endif

                    if(UpdateProgress != null)
                        UpdateProgress("Uploading file to VirusTotal...", null, 0, 0);

#if DEBUG
                    stopwatch.Restart();
#endif
                    Task.Run(async () =>
                    {
                        sResult = await vTotal.ScanFile(outFs, file.Sha256); // Keep filename private, sorry!
                    }).Wait();
#if DEBUG
                    stopwatch.Stop();
                    Console.WriteLine("Core.VirusTotalFileFromRepo({0}): Upload to VirusTotal took {1} seconds", file, stopwatch.Elapsed.TotalSeconds);
#endif
                    outFs.Close();

                    File.Delete(tmpFile);

                    if(sResult == null || sResult.ResponseCode == VirusTotalNET.ResponseCodes.ScanResponseCode.Error)
                    {
                        if(sResult == null)
                        {
                            if(Failed != null)
                                Failed("Cannot send file to VirusTotal");
                        }
                        else
                            Failed(sResult.VerboseMsg);

                        return;
                    }

                    // Seems that we are faster than them, getting a lot of "not queued" responses...
                    Thread.Sleep(2500);

                    Task.Run(async () =>
                    {
                        fResult = await vTotal.GetFileReport(file.Sha256);
                    }).Wait();
                }

                if(UpdateProgress != null)
                    UpdateProgress("Waiting for VirusTotal analysis...", null, 0, 0);

#if DEBUG
                stopwatch.Restart();
#endif
                int counter = 0;
                while(fResult.ResponseCode == VirusTotalNET.ResponseCodes.ReportResponseCode.StillQueued)
                {
                    // Timeout...
                    if(counter == 10)
                        break;
                    
                    // Wait 15 seconds so we fall in the 4 requests/minute
                    Thread.Sleep(15000);

                    Task.Run(async () =>
                    {
                        fResult = await vTotal.GetFileReport(file.Sha256);
                    }).Wait();

                    counter++;
                }
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.VirusTotalFileFromRepo({0}): VirusTotal took {1} seconds to do the analysis", file, stopwatch.Elapsed.TotalSeconds);
#endif

                if(fResult.ResponseCode != VirusTotalNET.ResponseCodes.ReportResponseCode.Present)
                {
                    if(Failed != null)
                        Failed(fResult.VerboseMsg);
                    return;
                }

                if(fResult.Positives > 0)
                {
                    file.HasVirus = true;
                    if(fResult.Scans != null)
                    {
                        foreach(KeyValuePair<string, ScanEngine> engine in fResult.Scans)
                        {
                            if(engine.Value.Detected)
                            {
                                file.Virus = engine.Value.Result;
                                file.VirusTotalTime = engine.Value.Update;
                                dbCore.DBOps.UpdateFile(file);

                                if(ScanFinished != null)
                                    ScanFinished(file);

                                return;
                            }
                        }
                    }
                }
                else
                {
                    // If no scan has been done, mark as false.
                    // If a positive has already existed don't overwrite it.
                    file.HasVirus = false;
                    file.Virus = null;
                    file.VirusTotalTime = DateTime.UtcNow;

                    dbCore.DBOps.UpdateFile(file);

                    if(ScanFinished != null)
                        ScanFinished(file);

                    return;
                }
            }
            catch(Exception ex)
            {
                if(Failed != null)
                    Failed(string.Format("Exception {0} when calling VirusTotal", ex.InnerException.Message));
            }
        }
    }
}
