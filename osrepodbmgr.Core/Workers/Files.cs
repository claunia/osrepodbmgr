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
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using DiscImageChef.Checksums;
using DiscImageChef.Interop;
using Newtonsoft.Json;
using Schemas;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors.LZMA;
using PlatformID = DiscImageChef.Interop.PlatformID;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        public static void FindFiles()
        {
            string filesPath;

            if(!string.IsNullOrEmpty(Context.TmpFolder) && Directory.Exists(Context.TmpFolder))
                filesPath  = Context.TmpFolder;
            else filesPath = Context.Path;

            if(string.IsNullOrEmpty(filesPath)) Failed?.Invoke("Path is null or empty");

            if(!Directory.Exists(filesPath)) Failed?.Invoke("Directory not found");

            try
            {
                #if DEBUG
                stopwatch.Restart();
                #endif
                Context.Files = IO.EnumerateFiles(filesPath, "*", SearchOption.AllDirectories, false, false);
                Context.Files.Sort();
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.FindFiles(): Took {0} seconds to find all files",
                                  stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
                #endif
                Context.Folders = IO.EnumerateDirectories(filesPath, "*", SearchOption.AllDirectories, false, false);
                Context.Folders.Sort();
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.FindFiles(): Took {0} seconds to find all folders",
                                  stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
                #endif
                Context.Symlinks = IO.EnumerateSymlinks(filesPath, "*", SearchOption.AllDirectories);
                Context.Symlinks.Sort();
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.FindFiles(): Took {0} seconds to find all symbolic links",
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

        public static void HashFiles()
        {
            try
            {
                Context.Hashes               = new Dictionary<string, DbOsFile>();
                Context.FoldersDict          = new Dictionary<string, DbFolder>();
                Context.SymlinksDict         = new Dictionary<string, string>();
                List<string> alreadyMetadata = new List<string>();
                bool         foundMetadata   = false;

                // For metadata
                List<ArchitecturesTypeArchitecture> architectures        = new List<ArchitecturesTypeArchitecture>();
                List<BarcodeType>                   barcodes             = new List<BarcodeType>();
                List<BlockMediaType>                disks                = new List<BlockMediaType>();
                List<string>                        categories           = new List<string>();
                List<string>                        keywords             = new List<string>();
                List<LanguagesTypeLanguage>         languages            = new List<LanguagesTypeLanguage>();
                List<OpticalDiscType>               discs                = new List<OpticalDiscType>();
                List<string>                        subcategories        = new List<string>();
                List<string>                        systems              = new List<string>();
                bool                                releaseDateSpecified = false;
                DateTime                            releaseDate          = DateTime.MinValue;
                CICMMetadataTypeReleaseType         releaseType          = CICMMetadataTypeReleaseType.Retail;
                bool                                releaseTypeSpecified = false;
                List<string>                        authors              = new List<string>();
                List<string>                        developers           = new List<string>();
                List<string>                        performers           = new List<string>();
                List<string>                        publishers           = new List<string>();
                string                              metadataName         = null;
                string                              metadataPartNo       = null;
                string                              metadataSerial       = null;
                string                              metadataVersion      = null;
                List<MagazineType>                  magazines            = new List<MagazineType>();
                List<BookType>                      books                = new List<BookType>();
                List<RequiredOperatingSystemType>   requiredOses         = new List<RequiredOperatingSystemType>();
                List<UserManualType>                usermanuals          = new List<UserManualType>();
                List<AdvertisementType>             adverts              = new List<AdvertisementType>();
                List<LinearMediaType>               linearmedias         = new List<LinearMediaType>();
                List<PCIType>                       pcis                 = new List<PCIType>();
                List<AudioMediaType>                audiomedias          = new List<AudioMediaType>();

                // End for metadata

                if((DetectOS.GetRealPlatformID() == PlatformID.WinCE        ||
                    DetectOS.GetRealPlatformID() == PlatformID.Win32S       ||
                    DetectOS.GetRealPlatformID() == PlatformID.Win32NT      ||
                    DetectOS.GetRealPlatformID() == PlatformID.Win32Windows ||
                    DetectOS.GetRealPlatformID() == PlatformID.WindowsPhone) && Context.Symlinks.Count > 0)
                {
                    Failed?.Invoke("Source contain unsupported symbolic links, not continuing.");
                    return;
                }

                #if DEBUG
                stopwatch.Restart();
                #endif
                long counter = 1;
                foreach(string file in Context.Files)
                {
                    // An already known metadata file, skip it
                    if(alreadyMetadata.Contains(file))
                    {
                        counter++;
                        continue;
                    }

                    switch(Path.GetExtension(file).ToLowerInvariant())
                    {
                        case ".xml":
                            FileStream        xrs = new FileStream(file, FileMode.Open, FileAccess.Read);
                            XmlReaderSettings xrt = new XmlReaderSettings {DtdProcessing = DtdProcessing.Ignore};

                            XmlReader     xr = XmlReader.Create(xrs, xrt);
                            XmlSerializer xs = new XmlSerializer(typeof(CICMMetadataType));
                            try
                            {
                                if(xs.CanDeserialize(xr))
                                {
                                    CICMMetadataType thisMetadata = (CICMMetadataType)xs.Deserialize(xr);
                                    if(thisMetadata.Architectures != null)
                                        architectures.AddRange(thisMetadata.Architectures);
                                    if(thisMetadata.Barcodes      != null) barcodes.AddRange(thisMetadata.Barcodes);
                                    if(thisMetadata.BlockMedia    != null) disks.AddRange(thisMetadata.BlockMedia);
                                    if(thisMetadata.Categories    != null) categories.AddRange(thisMetadata.Categories);
                                    if(thisMetadata.Keywords      != null) keywords.AddRange(thisMetadata.Keywords);
                                    if(thisMetadata.Languages     != null) languages.AddRange(thisMetadata.Languages);
                                    if(thisMetadata.OpticalDisc   != null) discs.AddRange(thisMetadata.OpticalDisc);
                                    if(thisMetadata.Subcategories != null)
                                        subcategories.AddRange(thisMetadata.Subcategories);
                                    if(thisMetadata.Systems   != null) systems.AddRange(thisMetadata.Systems);
                                    if(thisMetadata.Author    != null) authors.AddRange(thisMetadata.Author);
                                    if(thisMetadata.Developer != null) developers.AddRange(thisMetadata.Developer);
                                    if(thisMetadata.Performer != null) performers.AddRange(thisMetadata.Performer);
                                    if(thisMetadata.Publisher != null) publishers.AddRange(thisMetadata.Publisher);
                                    if(string.IsNullOrWhiteSpace(metadataName) &&
                                       !string.IsNullOrWhiteSpace(thisMetadata.Name)) metadataName = thisMetadata.Name;
                                    if(string.IsNullOrWhiteSpace(metadataPartNo) &&
                                       !string.IsNullOrWhiteSpace(thisMetadata.PartNumber))
                                        metadataPartNo = thisMetadata.PartNumber;
                                    if(string.IsNullOrWhiteSpace(metadataSerial) &&
                                       !string.IsNullOrWhiteSpace(thisMetadata.SerialNumber))
                                        metadataSerial = thisMetadata.SerialNumber;
                                    if(string.IsNullOrWhiteSpace(metadataVersion) &&
                                       !string.IsNullOrWhiteSpace(thisMetadata.Version))
                                        metadataVersion = thisMetadata.Version;
                                    if(thisMetadata.ReleaseDateSpecified)
                                        if(thisMetadata.ReleaseDate > releaseDate)
                                        {
                                            releaseDateSpecified = true;
                                            releaseDate          = thisMetadata.ReleaseDate;
                                        }

                                    if(thisMetadata.ReleaseTypeSpecified)
                                    {
                                        releaseTypeSpecified = true;
                                        releaseType          = thisMetadata.ReleaseType;
                                    }

                                    if(thisMetadata.Magazine != null)
                                        magazines.AddRange(thisMetadata.Magazine);
                                    if(thisMetadata.Book                     != null) books.AddRange(thisMetadata.Book);
                                    if(thisMetadata.RequiredOperatingSystems != null)
                                        requiredOses.AddRange(thisMetadata.RequiredOperatingSystems);
                                    if(thisMetadata.UserManual != null)
                                        usermanuals.AddRange(thisMetadata.UserManual);
                                    if(thisMetadata.Advertisement != null) adverts.AddRange(thisMetadata.Advertisement);
                                    if(thisMetadata.LinearMedia   != null)
                                        linearmedias.AddRange(thisMetadata.LinearMedia);
                                    if(thisMetadata.PCICard    != null) pcis.AddRange(thisMetadata.PCICard);
                                    if(thisMetadata.AudioMedia != null) audiomedias.AddRange(thisMetadata.AudioMedia);

                                    foundMetadata = true;

                                    string metadataFileWithoutExtension =
                                        Path.Combine(Path.GetDirectoryName(file),
                                                     Path.GetFileNameWithoutExtension(file));
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
                            }
                            catch(XmlException)
                            {
                                xr.Close();
                                xrs.Close();
                            }

                            break;
                        case ".json":
                            FileStream     jrs = new FileStream(file, FileMode.Open, FileAccess.Read);
                            TextReader     jr  = new StreamReader(jrs);
                            JsonSerializer js  = new JsonSerializer();

                            try
                            {
                                CICMMetadataType thisMetadata =
                                    (CICMMetadataType)js.Deserialize(jr, typeof(CICMMetadataType));
                                if(thisMetadata.Architectures != null)
                                    architectures.AddRange(thisMetadata.Architectures);
                                if(thisMetadata.Barcodes      != null) barcodes.AddRange(thisMetadata.Barcodes);
                                if(thisMetadata.BlockMedia    != null) disks.AddRange(thisMetadata.BlockMedia);
                                if(thisMetadata.Categories    != null) categories.AddRange(thisMetadata.Categories);
                                if(thisMetadata.Keywords      != null) keywords.AddRange(thisMetadata.Keywords);
                                if(thisMetadata.Languages     != null) languages.AddRange(thisMetadata.Languages);
                                if(thisMetadata.OpticalDisc   != null) discs.AddRange(thisMetadata.OpticalDisc);
                                if(thisMetadata.Subcategories != null)
                                    subcategories.AddRange(thisMetadata.Subcategories);
                                if(thisMetadata.Systems   != null) systems.AddRange(thisMetadata.Systems);
                                if(thisMetadata.Author    != null) authors.AddRange(thisMetadata.Author);
                                if(thisMetadata.Developer != null) developers.AddRange(thisMetadata.Developer);
                                if(thisMetadata.Performer != null) performers.AddRange(thisMetadata.Performer);
                                if(thisMetadata.Publisher != null) publishers.AddRange(thisMetadata.Publisher);
                                if(string.IsNullOrWhiteSpace(metadataName) &&
                                   !string.IsNullOrWhiteSpace(thisMetadata.Name)) metadataName = thisMetadata.Name;
                                if(string.IsNullOrWhiteSpace(metadataPartNo) &&
                                   !string.IsNullOrWhiteSpace(thisMetadata.PartNumber))
                                    metadataPartNo = thisMetadata.PartNumber;
                                if(string.IsNullOrWhiteSpace(metadataSerial) &&
                                   !string.IsNullOrWhiteSpace(thisMetadata.SerialNumber))
                                    metadataSerial = thisMetadata.SerialNumber;
                                if(string.IsNullOrWhiteSpace(metadataVersion) &&
                                   !string.IsNullOrWhiteSpace(thisMetadata.Version))
                                    metadataVersion = thisMetadata.Version;
                                if(thisMetadata.ReleaseDateSpecified)
                                    if(thisMetadata.ReleaseDate > releaseDate)
                                    {
                                        releaseDateSpecified = true;
                                        releaseDate          = thisMetadata.ReleaseDate;
                                    }

                                if(thisMetadata.ReleaseTypeSpecified)
                                {
                                    releaseTypeSpecified = true;
                                    releaseType          = thisMetadata.ReleaseType;
                                }

                                if(thisMetadata.Magazine != null)
                                    magazines.AddRange(thisMetadata.Magazine);
                                if(thisMetadata.Book                     != null) books.AddRange(thisMetadata.Book);
                                if(thisMetadata.RequiredOperatingSystems != null)
                                    requiredOses.AddRange(thisMetadata.RequiredOperatingSystems);
                                if(thisMetadata.UserManual    != null) usermanuals.AddRange(thisMetadata.UserManual);
                                if(thisMetadata.Advertisement != null) adverts.AddRange(thisMetadata.Advertisement);
                                if(thisMetadata.LinearMedia   != null) linearmedias.AddRange(thisMetadata.LinearMedia);
                                if(thisMetadata.PCICard       != null) pcis.AddRange(thisMetadata.PCICard);
                                if(thisMetadata.AudioMedia    != null) audiomedias.AddRange(thisMetadata.AudioMedia);

                                foundMetadata = true;

                                string metadataFileWithoutExtension =
                                    Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
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
                            catch(JsonException)
                            {
                                jr.Close();
                                jrs.Close();
                            }

                            break;
                    }

                    string   filesPath;
                    FileInfo fi = new FileInfo(file);

                    if(!string.IsNullOrEmpty(Context.TmpFolder) && Directory.Exists(Context.TmpFolder))
                        filesPath  = Context.TmpFolder;
                    else filesPath = Context.Path;

                    string relpath = file.Substring(filesPath.Length + 1);

                    UpdateProgress?.Invoke($"Hashing file {counter} of {Context.Files.Count}", null, counter,
                                           Context.Files.Count);
                    FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

                    byte[]        dataBuffer;
                    Sha256Context sha256Context = new Sha256Context();
                    sha256Context.Init();

                    if(fileStream.Length > BUFFER_SIZE)
                    {
                        long offset;
                        long remainder = fileStream.Length % BUFFER_SIZE;
                        for(offset = 0; offset < fileStream.Length - remainder; offset += (int)BUFFER_SIZE)
                        {
                            UpdateProgress2?.Invoke($"{offset / (double)fileStream.Length:P}", relpath, offset,
                                                    fileStream.Length);
                            dataBuffer = new byte[BUFFER_SIZE];
                            fileStream.Read(dataBuffer, 0, (int)BUFFER_SIZE);
                            sha256Context.Update(dataBuffer);
                        }

                        UpdateProgress2?.Invoke($"{offset / (double)fileStream.Length:P}", relpath, offset,
                                                fileStream.Length);
                        dataBuffer = new byte[remainder];
                        fileStream.Read(dataBuffer, 0, (int)remainder);
                        sha256Context.Update(dataBuffer);
                    }
                    else
                    {
                        UpdateProgress2?.Invoke($"{0 / (double)fileStream.Length:P}", relpath, 0, fileStream.Length);
                        dataBuffer = new byte[fileStream.Length];
                        fileStream.Read(dataBuffer, 0, (int)fileStream.Length);
                        sha256Context.Update(dataBuffer);
                    }

                    fileStream.Close();
                    string hash = Stringify(sha256Context.Final());

                    DbOsFile dbFile = new DbOsFile
                    {
                        Attributes        = fi.Attributes,
                        CreationTimeUtc   = fi.CreationTimeUtc,
                        LastAccessTimeUtc = fi.LastAccessTimeUtc,
                        LastWriteTimeUtc  = fi.LastWriteTimeUtc,
                        Length            = fi.Length,
                        Path              = relpath,
                        Sha256            = hash
                    };

                    // TODO: Add common cracker group names?
                    dbFile.Crack |= relpath.ToLowerInvariant().Contains("crack") || // Typical crack
                                    relpath.ToLowerInvariant().Contains("crack") || // Typical keygen
                                    relpath.ToLowerInvariant().Contains("[k]");

                    Context.Hashes.Add(relpath, dbFile);
                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.HashFiles(): Took {0} seconds to hash all files",
                                  stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
                #endif
                counter = 1;
                foreach(string folder in Context.Folders)
                {
                    string        filesPath;
                    DirectoryInfo di = new DirectoryInfo(folder);

                    if(!string.IsNullOrEmpty(Context.TmpFolder) && Directory.Exists(Context.TmpFolder))
                        filesPath  = Context.TmpFolder;
                    else filesPath = Context.Path;

                    string relpath = folder.Substring(filesPath.Length + 1);
                    UpdateProgress?.Invoke($"Checking folder {counter} of {Context.Folders.Count}", null, counter,
                                           Context.Folders.Count);

                    DbFolder dbFolder = new DbFolder
                    {
                        Attributes        = di.Attributes,
                        CreationTimeUtc   = di.CreationTimeUtc,
                        LastAccessTimeUtc = di.LastAccessTimeUtc,
                        LastWriteTimeUtc  = di.LastWriteTimeUtc,
                        Path              = relpath
                    };

                    Context.FoldersDict.Add(relpath, dbFolder);
                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.HashFiles(): Took {0} seconds to iterate all folders",
                                  stopwatch.Elapsed.TotalSeconds);
                stopwatch.Restart();
                #endif
                counter = 2;
                foreach(string symlink in Context.Symlinks)
                {
                    string filesPath;

                    if(!string.IsNullOrEmpty(Context.TmpFolder) && Directory.Exists(Context.TmpFolder))
                        filesPath  = Context.TmpFolder;
                    else filesPath = Context.Path;

                    string relpath = symlink.Substring(filesPath.Length + 1);

                    UpdateProgress?.Invoke($"Resolving symlink {counter} of {Context.Symlinks.Count}", null, counter,
                                           Context.Symlinks.Count);

                    string target = Symlinks.ReadLink(symlink);
                    if(target == null)
                    {
                        Failed?.Invoke($"Could not resolve symbolic link at {relpath}, not continuing.");
                        return;
                    }

                    Context.SymlinksDict.Add(relpath, target);
                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.HashFiles(): Took {0} seconds to resolve all symbolic links",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                if(foundMetadata)
                {
                    Context.Metadata                                           = new CICMMetadataType();
                    if(architectures.Count > 0) Context.Metadata.Architectures = architectures.Distinct().ToArray();
                    if(authors.Count       > 0) Context.Metadata.Author        = authors.Distinct().ToArray();
                    // TODO: Check for uniqueness
                    if(barcodes.Count   > 0) Context.Metadata.Barcodes   = barcodes.ToArray();
                    if(disks.Count      > 0) Context.Metadata.BlockMedia = disks.ToArray();
                    if(categories.Count > 0) Context.Metadata.Categories = categories.Distinct().ToArray();
                    if(developers.Count > 0) Context.Metadata.Developer  = developers.Distinct().ToArray();
                    if(keywords.Count   > 0) Context.Metadata.Keywords   = keywords.Distinct().ToArray();
                    if(languages.Count  > 0) Context.Metadata.Languages  = languages.Distinct().ToArray();
                    Context.Metadata.Name                                = metadataName;
                    if(discs.Count > 0) Context.Metadata.OpticalDisc     = discs.ToArray();
                    Context.Metadata.PartNumber                          = metadataPartNo;
                    if(performers.Count > 0) Context.Metadata.Performer  = performers.Distinct().ToArray();
                    if(publishers.Count > 0) Context.Metadata.Publisher  = publishers.Distinct().ToArray();
                    if(releaseDateSpecified)
                    {
                        Context.Metadata.ReleaseDate          = releaseDate;
                        Context.Metadata.ReleaseDateSpecified = true;
                    }

                    if(releaseTypeSpecified)
                    {
                        Context.Metadata.ReleaseType          = releaseType;
                        Context.Metadata.ReleaseTypeSpecified = true;
                    }

                    Context.Metadata.SerialNumber                              = metadataSerial;
                    if(subcategories.Count > 0) Context.Metadata.Subcategories = subcategories.Distinct().ToArray();
                    if(systems.Count       > 0) Context.Metadata.Systems       = systems.Distinct().ToArray();
                    Context.Metadata.Version                                   = metadataVersion;
                    Context.Metadata.Magazine                                  = magazines.ToArray();
                    Context.Metadata.Book                                      = books.ToArray();
                    Context.Metadata.RequiredOperatingSystems                  = requiredOses.ToArray();
                    Context.Metadata.UserManual                                = usermanuals.ToArray();
                    Context.Metadata.Advertisement                             = adverts.ToArray();
                    Context.Metadata.LinearMedia                               = linearmedias.ToArray();
                    Context.Metadata.PCICard                                   = pcis.ToArray();
                    Context.Metadata.AudioMedia                                = audiomedias.ToArray();

                    foreach(string metadataFile in alreadyMetadata) Context.Files.Remove(metadataFile);
                }
                else Context.Metadata = null;

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

        public static void RemoveTempFolder()
        {
            try
            {
                if(!Directory.Exists(Context.TmpFolder)) return;

                Directory.Delete(Context.TmpFolder, true);
                Finished?.Invoke();
            }
            catch(ThreadAbortException) { }
            catch(IOException)
            {
                // Could not delete temporary files, do not crash.
                Finished?.Invoke();
            }
            catch(Exception ex)
            {
                if(Debugger.IsAttached) throw;

                Failed?.Invoke($"Exception {ex.Message}\n{ex.InnerException}");
                #if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                #endif
            }
        }

        public static void CopyFile()
        {
            try
            {
                if(!File.Exists(Context.Path))
                {
                    Failed?.Invoke("Specified file cannot be found");
                    return;
                }

                if(string.IsNullOrWhiteSpace(Context.TmpFolder))
                {
                    Failed?.Invoke("Destination cannot be empty");
                    return;
                }

                if(Directory.Exists(Context.TmpFolder))
                {
                    Failed?.Invoke("Destination cannot be a folder");
                    return;
                }

                FileStream inFs  = new FileStream(Context.Path,      FileMode.Open,   FileAccess.Read);
                FileStream outFs = new FileStream(Context.TmpFolder, FileMode.Create, FileAccess.Write);

                #if DEBUG
                stopwatch.Restart();
                #endif
                byte[] buffer = new byte[BUFFER_SIZE];

                while(inFs.Position + BUFFER_SIZE <= inFs.Length)
                {
                    UpdateProgress?.Invoke("Copying file...", $"{inFs.Position} / {inFs.Length} bytes", inFs.Position,
                                           inFs.Length);

                    inFs.Read(buffer, 0, buffer.Length);
                    outFs.Write(buffer, 0, buffer.Length);
                }

                buffer = new byte[inFs.Length - inFs.Position];
                UpdateProgress?.Invoke("Copying file...", $"{inFs.Position} / {inFs.Length} bytes", inFs.Position,
                                       inFs.Length);

                inFs.Read(buffer, 0, buffer.Length);
                outFs.Write(buffer, 0, buffer.Length);

                inFs.Close();
                outFs.Close();
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.CopyFile(): Took {0} seconds to copy file", stopwatch.Elapsed.TotalSeconds);
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

        public static void SaveAs()
        {
            try
            {
                if(string.IsNullOrWhiteSpace(Context.Path))
                {
                    Failed?.Invoke("Destination cannot be empty");
                    return;
                }

                if(File.Exists(Context.Path))
                {
                    Failed?.Invoke("Destination cannot be a file");
                    return;
                }

                if(Context.DbInfo.Id == 0)
                {
                    Failed?.Invoke("Operating system must be set");
                    return;
                }

                bool symlinksSupported = DetectOS.GetRealPlatformID() != PlatformID.WinCE        &&
                                         DetectOS.GetRealPlatformID() != PlatformID.Win32S       &&
                                         DetectOS.GetRealPlatformID() != PlatformID.Win32NT      &&
                                         DetectOS.GetRealPlatformID() != PlatformID.Win32Windows &&
                                         DetectOS.GetRealPlatformID() != PlatformID.WindowsPhone;

                Dictionary<string, string> symlinks = new Dictionary<string, string>();

                UpdateProgress?.Invoke("", "Asking DB for files...", 1, 100);

                dbCore.DbOps.GetAllFilesInOs(out List<DbOsFile> files, Context.DbInfo.Id);

                UpdateProgress?.Invoke("", "Asking DB for folders...", 2, 100);

                dbCore.DbOps.GetAllFolders(out List<DbFolder> folders, Context.DbInfo.Id);

                UpdateProgress?.Invoke("", "Asking DB for symbolic links...", 3, 100);

                if(dbCore.DbOps.HasSymlinks(Context.DbInfo.Id))
                {
                    if(!symlinksSupported)
                    {
                        Failed?.Invoke("Symbolic links cannot be created on this platform.");
                        return;
                    }

                    dbCore.DbOps.GetAllSymlinks(out symlinks, Context.DbInfo.Id);
                }

                UpdateProgress?.Invoke("", "Creating folders...", 4, 100);

                #if DEBUG
                stopwatch.Restart();
                #endif
                long counter = 0;
                foreach(DbFolder folder in folders)
                {
                    UpdateProgress2?.Invoke("", folder.Path, counter, folders.Count);

                    DirectoryInfo di     = Directory.CreateDirectory(Path.Combine(Context.Path, folder.Path));
                    di.Attributes        = folder.Attributes;
                    di.CreationTimeUtc   = folder.CreationTimeUtc;
                    di.LastAccessTimeUtc = folder.LastAccessTimeUtc;
                    di.LastWriteTimeUtc  = folder.LastWriteTimeUtc;

                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.SaveAs(): Took {0} seconds to create all folders",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                UpdateProgress?.Invoke("", "Creating symbolic links...", 4, 100);

                #if DEBUG
                stopwatch.Restart();
                #endif
                counter = 0;
                foreach(KeyValuePair<string, string> kvp in symlinks)
                {
                    UpdateProgress2?.Invoke("", kvp.Key, counter, folders.Count);

                    Symlinks.Symlink(kvp.Value, kvp.Key);
                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.SaveAs(): Took {0} seconds to create all symbolic links",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                #if DEBUG
                stopwatch.Restart();
                #endif

                counter = 4;
                foreach(DbOsFile file in files)
                {
                    UpdateProgress?.Invoke("", $"Creating {file.Path}...", counter, 4 + files.Count);

                    Stream   zStream = null;
                    string   repoPath;
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
                    {
                        Failed?.Invoke($"Cannot find file with hash {file.Sha256} in the repository");
                        return;
                    }

                    FileStream inFs  = new FileStream(repoPath, FileMode.Open, FileAccess.Read);
                    FileStream outFs = new FileStream(Path.Combine(Context.Path, file.Path), FileMode.CreateNew,
                                                      FileAccess.Write);

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
                            zStream = new LzmaStream(properties, inFs);
                            break;
                        case AlgoEnum.LZip:
                            zStream = new LZipStream(inFs, CompressionMode.Decompress);
                            break;
                    }

                    byte[] buffer = new byte[BUFFER_SIZE];

                    while(outFs.Position + BUFFER_SIZE <= file.Length)
                    {
                        UpdateProgress2?.Invoke($"{outFs.Position / (double)file.Length:P}",
                                                $"{outFs.Position} / {file.Length} bytes", outFs.Position, file.Length);

                        zStream.Read(buffer, 0, buffer.Length);
                        outFs.Write(buffer, 0, buffer.Length);
                    }

                    buffer = new byte[file.Length - outFs.Position];
                    UpdateProgress2?.Invoke($"{outFs.Position / (double)file.Length:P}",
                                            $"{outFs.Position} / {file.Length} bytes", outFs.Position, file.Length);

                    zStream.Read(buffer, 0, buffer.Length);
                    outFs.Write(buffer, 0, buffer.Length);

                    UpdateProgress2?.Invoke($"{file.Length / (double)file.Length:P}", "Finishing...", inFs.Length,
                                            inFs.Length);

                    zStream.Close();
                    outFs.Close();

                    FileInfo fi = new FileInfo(Path.Combine(Context.Path, file.Path))
                    {
                        Attributes        = file.Attributes,
                        CreationTimeUtc   = file.CreationTimeUtc,
                        LastAccessTimeUtc = file.LastAccessTimeUtc,
                        LastWriteTimeUtc  = file.LastWriteTimeUtc
                    };

                    counter++;
                }
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.SaveAs(): Took {0} seconds to create all files",
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

        public static void CleanFiles()
        {
            ulong       count  = dbCore.DbOps.GetFilesCount();
            const ulong page   = 2500;
            ulong       offset = 0;

            List<DbFile> filesPage;
            List<DbFile> allFiles = new List<DbFile>();

            #if DEBUG
            stopwatch.Restart();
            #endif
            while(dbCore.DbOps.GetFiles(out filesPage, offset, page))
            {
                if(filesPage.Count == 0) break;

                UpdateProgress?.Invoke(null, $"Loaded file {offset} of {count}", (long)offset, (long)count);

                allFiles.AddRange(filesPage);

                offset += page;
            }
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.CleanFiles(): Took {0} seconds to get all files from the database",
                              stopwatch.Elapsed.TotalSeconds);
            #endif

            filesPage = null;

            UpdateProgress?.Invoke(null, "Getting OSes from the database", 0, 0);
            #if DEBUG
            stopwatch.Restart();
            #endif
            dbCore.DbOps.GetAllOSes(out List<DbEntry> oses);
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.CleanFiles(): Took {0} seconds to get OSes from database",
                              stopwatch.Elapsed.TotalSeconds);
            #endif

            List<string> orphanFiles = new List<string>();

            #if DEBUG
            stopwatch.Restart();
            Stopwatch stopwatch2 = new Stopwatch();
            #endif
            int counterF = 0;
            foreach(DbFile file in allFiles)
            {
                UpdateProgress?.Invoke(null, $"Checking file {counterF} of {allFiles.Count}", counterF, allFiles.Count);

                bool fileExists = false;
                int  counterO   = 0;
                #if DEBUG
                stopwatch2.Restart();
                #endif
                foreach(DbEntry os in oses)
                {
                    UpdateProgress2?.Invoke(null, $"Checking OS {counterO} of {oses.Count}", counterO, oses.Count);

                    if(dbCore.DbOps.ExistsFileInOs(file.Sha256, os.Id))
                    {
                        fileExists = true;
                        break;
                    }

                    counterO++;
                }
                #if DEBUG
                stopwatch2.Stop();
                Console.WriteLine("Core.CleanFiles(): Took {0} seconds to check file in all OSes",
                                  stopwatch2.Elapsed.TotalSeconds);
                #endif

                if(!fileExists) orphanFiles.Add(file.Sha256);

                counterF++;
            }
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.CleanFiles(): Took {0} seconds to check all files", stopwatch.Elapsed.TotalSeconds);
            #endif

            UpdateProgress2?.Invoke(null, null, 0, 0);

            #if DEBUG
            stopwatch.Restart();
            #endif
            counterF = 0;
            foreach(string hash in orphanFiles)
            {
                UpdateProgress?.Invoke(null, $"Deleting file {counterF} of {orphanFiles.Count} from database", counterF,
                                       orphanFiles.Count);

                dbCore.DbOps.DeleteFile(hash);
                counterF++;
            }
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.CleanFiles(): Took {0} seconds to remove all orphan files from database",
                              stopwatch.Elapsed.TotalSeconds);
            #endif

            UpdateProgress?.Invoke(null, "Listing files in repository", 0, 0);

            #if DEBUG
            stopwatch.Restart();
            #endif
            List<string> repoFiles =
                new List<string>(Directory.EnumerateFiles(Settings.Current.RepositoryPath, "*",
                                                          SearchOption.AllDirectories));
            repoFiles.Sort();
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.CleanFiles(): Took {0} seconds to find all files", stopwatch.Elapsed.TotalSeconds);
            stopwatch.Restart();
            #endif
            counterF                   = 0;
            List<string> filesToDelete = new List<string>();
            foreach(string file in repoFiles)
            {
                UpdateProgress?.Invoke(null, $"Checking file {counterF} of {repoFiles.Count} from repository", counterF,
                                       repoFiles.Count);

                // Allow database to be inside repo
                if(file == Settings.Current.DatabasePath) continue;

                if(Path.GetExtension(file)?.ToLowerInvariant() == ".xml" ||
                   Path.GetExtension(file)?.ToLowerInvariant() == ".json")
                {
                    if(!dbCore.DbOps.ExistsOs(Path.GetFileNameWithoutExtension(file))) filesToDelete.Add(file);
                }
                else if(!dbCore.DbOps.ExistsFile(Path.GetFileNameWithoutExtension(file)))
                    filesToDelete.Add(file);

                counterF++;
            }
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.CleanFiles(): Took {0} seconds to check all repository files",
                              stopwatch.Elapsed.TotalSeconds);
            stopwatch.Restart();
            #endif
            counterF = 0;
            foreach(string file in filesToDelete)
            {
                UpdateProgress?.Invoke(null, $"Deleting file {counterF} of {filesToDelete.Count} from repository",
                                       counterF, filesToDelete.Count);

                try { File.Delete(file); }
                #pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                catch
                    #pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                {
                    // Do not crash
                }

                counterF++;
            }
            #if DEBUG
            stopwatch.Stop();
            Console.WriteLine("Core.CleanFiles(): Took {0} seconds to delete all orphan files",
                              stopwatch.Elapsed.TotalSeconds);
            #endif

            Finished?.Invoke();
        }
    }
}