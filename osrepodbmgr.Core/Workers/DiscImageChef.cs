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
using DiscImageChef.CommonTypes;
using DiscImageChef.Decoders.CD;
using DiscImageChef.Decoders.DVD;
using DiscImageChef.Decoders.PCMCIA;
using DiscImageChef.DiscImages;
using DiscImageChef.Filesystems;
using DiscImageChef.Filters;
using DiscImageChef.Metadata;
using DiscImageChef.Partitions;
using Schemas;
using DMI = DiscImageChef.Decoders.Xbox.DMI;
using MediaType = DiscImageChef.CommonTypes.MediaType;
using Session = DiscImageChef.DiscImages.Session;
using TrackType = Schemas.TrackType;
using Tuple = DiscImageChef.Decoders.PCMCIA.Tuple;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        public static void AddMedia()
        {
            if(string.IsNullOrWhiteSpace(Context.SelectedFile))
            {
                Failed?.Invoke("There is no file set");
                return;
            }

            string filesPath;

            if(!string.IsNullOrEmpty(Context.TmpFolder) && Directory.Exists(Context.TmpFolder))
                filesPath  = Context.TmpFolder;
            else filesPath = Context.Path;

            string selectedFile = Path.Combine(filesPath, Context.SelectedFile);

            if(!File.Exists(selectedFile))
            {
                Failed?.Invoke("Selected file does not exist");
                return;
            }

            CICMMetadataType sidecar = new CICMMetadataType();
            PluginBase       plugins = new PluginBase();

            long maxProgress = 4;

            FiltersList filtersList = new FiltersList();

            UpdateProgress?.Invoke(null, "Detecting image filter", 1, maxProgress);

            IFilter inputFilter = filtersList.GetFilter(selectedFile);

            if(inputFilter == null)
            {
                Failed?.Invoke("Cannot open specified file.");
                return;
            }

            try
            {
                #if DEBUG
                stopwatch.Restart();
                #endif
                UpdateProgress?.Invoke(null, "Detecting image format", 2, maxProgress);
                IMediaImage imageFormat = ImageFormat.Detect(inputFilter);
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.AddMedia(): Took {0} seconds to detect image format",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                if(imageFormat == null)
                {
                    Failed?.Invoke("Image format not identified, not proceeding with analysis.");
                    return;
                }

                try
                {
                    if(!imageFormat.Open(inputFilter))
                    {
                        Failed?.Invoke("Unable to open image format\n" + "No error given");
                        return;
                    }
                }
                catch(Exception ex)
                {
                    Failed?.Invoke("Unable to open image format\n" + $"Error: {ex.Message}");
                    #if DEBUG
                    Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                    #endif
                    return;
                }

                FileInfo   fi = new FileInfo(selectedFile);
                FileStream fs = new FileStream(selectedFile, FileMode.Open, FileAccess.Read);

                Checksum imgChkWorker = new Checksum();

                UpdateProgress?.Invoke(null, "Hashing image file", 3, maxProgress);

                #if DEBUG
                stopwatch.Restart();
                #endif
                byte[] data;
                long   position = 0;
                while(position < fi.Length - 524288)
                {
                    data = new byte[524288];
                    fs.Read(data, 0, 524288);

                    UpdateProgress2?.Invoke(null, $"{position} of {fi.Length} bytes", position, fi.Length);

                    imgChkWorker.Update(data);

                    position += 524288;
                }

                data = new byte[fi.Length        - position];
                fs.Read(data, 0, (int)(fi.Length - position));

                UpdateProgress2?.Invoke(null, $"{position} of {fi.Length} bytes", position, fi.Length);

                imgChkWorker.Update(data);

                fs.Close();
                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash image file",
                                  stopwatch.Elapsed.TotalSeconds);
                #endif

                List<ChecksumType> imgChecksums = imgChkWorker.End();

                UpdateProgress2?.Invoke(null, null, 0, 0);

                long currentProgress = 0;
                switch(imageFormat.Info.XmlMediaType)
                {
                    case XmlMediaType.OpticalDisc:
                    {
                        maxProgress = 4 + imageFormat.Info.ReadableMediaTags.Count + imageFormat.Tracks.Count;

                        UpdateProgress?.Invoke(null, "Hashing image file", 3, maxProgress);

                        sidecar.OpticalDisc    = new OpticalDiscType[1];
                        sidecar.OpticalDisc[0] = new OpticalDiscType
                        {
                            Checksums = imgChecksums.ToArray(),
                            Image     = new ImageType
                            {
                                format          = imageFormat.Format,
                                offset          = 0,
                                offsetSpecified = true,
                                Value           = Path.GetFileName(selectedFile)
                            },
                            Size     = fi.Length,
                            Sequence = new SequenceType {MediaTitle = imageFormat.Info.MediaTitle}
                        };
                        if(imageFormat.Info.MediaSequence != 0 && imageFormat.Info.LastMediaSequence != 0)
                        {
                            sidecar.OpticalDisc[0].Sequence.MediaSequence = imageFormat.Info.MediaSequence;
                            sidecar.OpticalDisc[0].Sequence.TotalMedia    = imageFormat.Info.LastMediaSequence;
                        }
                        else
                        {
                            sidecar.OpticalDisc[0].Sequence.MediaSequence = 1;
                            sidecar.OpticalDisc[0].Sequence.TotalMedia    = 1;
                        }

                        MediaType dskType = imageFormat.Info.MediaType;

                        currentProgress = 3;

                        #if DEBUG
                        stopwatch.Restart();
                        #endif
                        foreach(MediaTagType tagType in imageFormat.Info.ReadableMediaTags)
                        {
                            currentProgress++;
                            UpdateProgress?.Invoke(null, $"Hashing file containing {tagType}", currentProgress,
                                                   maxProgress);

                            switch(tagType)
                            {
                                case MediaTagType.CD_ATIP:
                                    sidecar.OpticalDisc[0].ATIP           = new DumpType();
                                    sidecar.OpticalDisc[0].ATIP.Checksums = Checksum
                                                                           .GetChecksums(imageFormat
                                                                                            .ReadDiskTag(MediaTagType
                                                                                                            .CD_ATIP))
                                                                           .ToArray();
                                    sidecar.OpticalDisc[0].ATIP.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.CD_ATIP).Length;
                                    ATIP.CDATIP? atip = ATIP.Decode(imageFormat.ReadDiskTag(MediaTagType.CD_ATIP));
                                    if(atip.HasValue)
                                        if(atip.Value.DDCD)
                                            dskType = atip.Value.DiscType ? MediaType.DDCDRW : MediaType.DDCDR;
                                        else
                                            dskType = atip.Value.DiscType ? MediaType.CDRW : MediaType.CDR;
                                    break;
                                case MediaTagType.DVD_BCA:
                                    sidecar.OpticalDisc[0].BCA           = new DumpType();
                                    sidecar.OpticalDisc[0].BCA.Checksums = Checksum
                                                                          .GetChecksums(imageFormat
                                                                                           .ReadDiskTag(MediaTagType
                                                                                                           .DVD_BCA))
                                                                          .ToArray();
                                    sidecar.OpticalDisc[0].BCA.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.DVD_BCA).Length;
                                    break;
                                case MediaTagType.BD_BCA:
                                    sidecar.OpticalDisc[0].BCA           = new DumpType();
                                    sidecar.OpticalDisc[0].BCA.Checksums = Checksum
                                                                          .GetChecksums(imageFormat
                                                                                           .ReadDiskTag(MediaTagType
                                                                                                           .BD_BCA))
                                                                          .ToArray();
                                    sidecar.OpticalDisc[0].BCA.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.BD_BCA).Length;
                                    break;
                                case MediaTagType.DVD_CMI:
                                    sidecar.OpticalDisc[0].CMI    = new DumpType();
                                    CSS_CPRM.LeadInCopyright? cmi =
                                        CSS_CPRM.DecodeLeadInCopyright(imageFormat.ReadDiskTag(MediaTagType.DVD_CMI));
                                    if(cmi.HasValue)
                                        switch(cmi.Value.CopyrightType)
                                        {
                                            case CopyrightType.AACS:
                                                sidecar.OpticalDisc[0].CopyProtection = "AACS";
                                                break;
                                            case CopyrightType.CSS:
                                                sidecar.OpticalDisc[0].CopyProtection = "CSS";
                                                break;
                                            case CopyrightType.CPRM:
                                                sidecar.OpticalDisc[0].CopyProtection = "CPRM";
                                                break;
                                        }
                                    sidecar.OpticalDisc[0].CMI.Checksums = Checksum
                                                                          .GetChecksums(imageFormat
                                                                                           .ReadDiskTag(MediaTagType
                                                                                                           .DVD_CMI))
                                                                          .ToArray();
                                    sidecar.OpticalDisc[0].CMI.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.DVD_CMI).Length;
                                    break;
                                case MediaTagType.DVD_DMI:
                                    sidecar.OpticalDisc[0].DMI           = new DumpType();
                                    sidecar.OpticalDisc[0].DMI.Checksums = Checksum
                                                                          .GetChecksums(imageFormat
                                                                                           .ReadDiskTag(MediaTagType
                                                                                                           .DVD_DMI))
                                                                          .ToArray();
                                    sidecar.OpticalDisc[0].DMI.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.DVD_DMI).Length;
                                    if(DMI.IsXbox(imageFormat.ReadDiskTag(MediaTagType.DVD_DMI)))
                                    {
                                        dskType                           = MediaType.XGD;
                                        sidecar.OpticalDisc[0].Dimensions = new DimensionsType {Diameter = 120};
                                    }
                                    else if(DMI.IsXbox360(imageFormat.ReadDiskTag(MediaTagType.DVD_DMI)))
                                    {
                                        dskType                           = MediaType.XGD2;
                                        sidecar.OpticalDisc[0].Dimensions = new DimensionsType {Diameter = 120};
                                    }

                                    break;
                                case MediaTagType.DVD_PFI:
                                    sidecar.OpticalDisc[0].PFI           = new DumpType();
                                    sidecar.OpticalDisc[0].PFI.Checksums = Checksum
                                                                          .GetChecksums(imageFormat
                                                                                           .ReadDiskTag(MediaTagType
                                                                                                           .DVD_PFI))
                                                                          .ToArray();
                                    sidecar.OpticalDisc[0].PFI.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.DVD_PFI).Length;
                                    PFI.PhysicalFormatInformation? pfi =
                                        PFI.Decode(imageFormat.ReadDiskTag(MediaTagType.DVD_PFI));
                                    if(pfi.HasValue)
                                        if(dskType != MediaType.XGD && dskType != MediaType.XGD2 &&
                                           dskType != MediaType.XGD3)
                                        {
                                            switch(pfi.Value.DiskCategory)
                                            {
                                                case DiskCategory.DVDPR:
                                                    dskType = MediaType.DVDPR;
                                                    break;
                                                case DiskCategory.DVDPRDL:
                                                    dskType = MediaType.DVDPRDL;
                                                    break;
                                                case DiskCategory.DVDPRW:
                                                    dskType = MediaType.DVDPRW;
                                                    break;
                                                case DiskCategory.DVDPRWDL:
                                                    dskType = MediaType.DVDPRWDL;
                                                    break;
                                                case DiskCategory.DVDR:
                                                    dskType = MediaType.DVDR;
                                                    break;
                                                case DiskCategory.DVDRAM:
                                                    dskType = MediaType.DVDRAM;
                                                    break;
                                                case DiskCategory.DVDROM:
                                                    dskType = MediaType.DVDROM;
                                                    break;
                                                case DiskCategory.DVDRW:
                                                    dskType = MediaType.DVDRW;
                                                    break;
                                                case DiskCategory.HDDVDR:
                                                    dskType = MediaType.HDDVDR;
                                                    break;
                                                case DiskCategory.HDDVDRAM:
                                                    dskType = MediaType.HDDVDRAM;
                                                    break;
                                                case DiskCategory.HDDVDROM:
                                                    dskType = MediaType.HDDVDROM;
                                                    break;
                                                case DiskCategory.HDDVDRW:
                                                    dskType = MediaType.HDDVDRW;
                                                    break;
                                                case DiskCategory.Nintendo:
                                                    dskType = MediaType.GOD;
                                                    break;
                                                case DiskCategory.UMD:
                                                    dskType = MediaType.UMD;
                                                    break;
                                            }

                                            if(dskType == MediaType.DVDR && pfi.Value.PartVersion == 6)
                                                dskType = MediaType.DVDRDL;
                                            if(dskType == MediaType.DVDRW && pfi.Value.PartVersion == 3)
                                                dskType = MediaType.DVDRWDL;
                                            if(dskType == MediaType.GOD && pfi.Value.DiscSize == DVDSize.OneTwenty)
                                                dskType = MediaType.WOD;

                                            sidecar.OpticalDisc[0].Dimensions = new DimensionsType();
                                            if(dskType == MediaType.UMD)
                                                sidecar.OpticalDisc[0].Dimensions.Diameter = 60;
                                            else if(pfi.Value.DiscSize == DVDSize.Eighty)
                                                sidecar.OpticalDisc[0].Dimensions.Diameter = 80;
                                            else if(pfi.Value.DiscSize == DVDSize.OneTwenty)
                                                sidecar.OpticalDisc[0].Dimensions.Diameter = 120;
                                        }

                                    break;
                                case MediaTagType.CD_PMA:
                                    sidecar.OpticalDisc[0].PMA           = new DumpType();
                                    sidecar.OpticalDisc[0].PMA.Checksums = Checksum
                                                                          .GetChecksums(imageFormat
                                                                                           .ReadDiskTag(MediaTagType
                                                                                                           .CD_PMA))
                                                                          .ToArray();
                                    sidecar.OpticalDisc[0].PMA.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.CD_PMA).Length;
                                    break;
                            }
                        }
                        #if DEBUG
                        stopwatch.Stop();
                        Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash media tags",
                                          stopwatch.Elapsed.TotalSeconds);
                        #endif

                        try
                        {
                            List<Session> sessions          = imageFormat.Sessions;
                            sidecar.OpticalDisc[0].Sessions = sessions?.Count ?? 1;
                        }
                        catch { sidecar.OpticalDisc[0].Sessions = 1; }

                        List<Track>     tracks  = imageFormat.Tracks;
                        List<TrackType> trksLst = null;
                        if(tracks != null)
                        {
                            sidecar.OpticalDisc[0].Tracks    = new int[1];
                            sidecar.OpticalDisc[0].Tracks[0] = tracks.Count;
                            trksLst                          = new List<TrackType>();
                        }

                        foreach(Track trk in tracks)
                        {
                            currentProgress++;

                            TrackType xmlTrk = new TrackType();
                            switch(trk.TrackType)
                            {
                                case DiscImageChef.DiscImages.TrackType.Audio:
                                    xmlTrk.TrackType1 = TrackTypeTrackType.audio;
                                    break;
                                case DiscImageChef.DiscImages.TrackType.CdMode2Form2:
                                    xmlTrk.TrackType1 = TrackTypeTrackType.m2f2;
                                    break;
                                case DiscImageChef.DiscImages.TrackType.CdMode2Formless:
                                    xmlTrk.TrackType1 = TrackTypeTrackType.mode2;
                                    break;
                                case DiscImageChef.DiscImages.TrackType.CdMode2Form1:
                                    xmlTrk.TrackType1 = TrackTypeTrackType.m2f1;
                                    break;
                                case DiscImageChef.DiscImages.TrackType.CdMode1:
                                    xmlTrk.TrackType1 = TrackTypeTrackType.mode1;
                                    break;
                                case DiscImageChef.DiscImages.TrackType.Data:
                                    switch(sidecar.OpticalDisc[0].DiscType)
                                    {
                                        case "BD":
                                            xmlTrk.TrackType1 = TrackTypeTrackType.bluray;
                                            break;
                                        case "DDCD":
                                            xmlTrk.TrackType1 = TrackTypeTrackType.ddcd;
                                            break;
                                        case "DVD":
                                            xmlTrk.TrackType1 = TrackTypeTrackType.dvd;
                                            break;
                                        case "HD DVD":
                                            xmlTrk.TrackType1 = TrackTypeTrackType.hddvd;
                                            break;
                                        default:
                                            xmlTrk.TrackType1 = TrackTypeTrackType.mode1;
                                            break;
                                    }

                                    break;
                            }

                            xmlTrk.Sequence = new TrackSequenceType
                            {
                                Session     = trk.TrackSession,
                                TrackNumber = (int)trk.TrackSequence
                            };
                            xmlTrk.StartSector = (long)trk.TrackStartSector;
                            xmlTrk.EndSector   = (long)trk.TrackEndSector;

                            if(trk.Indexes != null && trk.Indexes.ContainsKey(0))
                                if(trk.Indexes.TryGetValue(0, out ulong idx0))
                                    xmlTrk.StartSector = (long)idx0;

                            switch(sidecar.OpticalDisc[0].DiscType)
                            {
                                case "CD":
                                case "GD":
                                    xmlTrk.StartMSF = LbaToMsf(xmlTrk.StartSector);
                                    xmlTrk.EndMSF   = LbaToMsf(xmlTrk.EndSector);
                                    break;
                                case "DDCD":
                                    xmlTrk.StartMSF = DdcdLbaToMsf(xmlTrk.StartSector);
                                    xmlTrk.EndMSF   = DdcdLbaToMsf(xmlTrk.EndSector);
                                    break;
                            }

                            xmlTrk.Image = new ImageType {Value = Path.GetFileName(trk.TrackFile)};
                            if(trk.TrackFileOffset > 0)
                            {
                                xmlTrk.Image.offset          = (long)trk.TrackFileOffset;
                                xmlTrk.Image.offsetSpecified = true;
                            }

                            xmlTrk.Image.format = trk.TrackFileType;
                            xmlTrk.Size         =
                                (xmlTrk.EndSector - xmlTrk.StartSector + 1) * trk.TrackRawBytesPerSector;
                            xmlTrk.BytesPerSector = trk.TrackBytesPerSector;

                            const uint SECTORS_TO_READ = 512;
                            ulong      sectors         = (ulong)(xmlTrk.EndSector - xmlTrk.StartSector + 1);
                            ulong      doneSectors     = 0;

                            // If there is only one track, and it's the same as the image file (e.g. ".iso" files), don't re-checksum.
                            if(imageFormat.Id == new Guid("12345678-AAAA-BBBB-CCCC-123456789000"))
                                xmlTrk.Checksums = sidecar.OpticalDisc[0].Checksums;
                            else
                            {
                                UpdateProgress?.Invoke(null, $"Hashing track {trk.TrackSequence}", currentProgress,
                                                       maxProgress);

                                Checksum trkChkWorker = new Checksum();

                                #if DEBUG
                                stopwatch.Restart();
                                #endif
                                while(doneSectors < sectors)
                                {
                                    byte[] sector;

                                    if(sectors - doneSectors >= SECTORS_TO_READ)
                                    {
                                        sector = imageFormat.ReadSectorsLong(doneSectors, SECTORS_TO_READ,
                                                                             (uint)xmlTrk.Sequence.TrackNumber);
                                        UpdateProgress2?.Invoke(null, $"Sector {doneSectors} of {sectors}",
                                                                (long)doneSectors, (long)sectors);
                                        doneSectors += SECTORS_TO_READ;
                                    }
                                    else
                                    {
                                        sector = imageFormat.ReadSectorsLong(doneSectors, (uint)(sectors - doneSectors),
                                                                             (uint)xmlTrk.Sequence.TrackNumber);
                                        UpdateProgress2?.Invoke(null, $"Sector {doneSectors} of {sectors}",
                                                                (long)doneSectors, (long)sectors);
                                        doneSectors += sectors - doneSectors;
                                    }

                                    trkChkWorker.Update(sector);
                                }

                                List<ChecksumType> trkChecksums = trkChkWorker.End();
                                #if DEBUG
                                stopwatch.Stop();
                                Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash track {1}",
                                                  stopwatch.Elapsed.TotalSeconds, trk.TrackSequence);
                                #endif

                                xmlTrk.Checksums = trkChecksums.ToArray();
                            }

                            UpdateProgress2?.Invoke(null, null, 0, 0);

                            if(trk.TrackSubchannelType != TrackSubchannelType.None)
                            {
                                UpdateProgress?.Invoke(null, $"Hashing subchannel of track {trk.TrackSequence}",
                                                       currentProgress, maxProgress);

                                xmlTrk.SubChannel = new SubChannelType {Image = new ImageType()};
                                switch(trk.TrackSubchannelType)
                                {
                                    case TrackSubchannelType.Packed:
                                    case TrackSubchannelType.PackedInterleaved:
                                        xmlTrk.SubChannel.Image.format = "rw";
                                        break;
                                    case TrackSubchannelType.Raw:
                                    case TrackSubchannelType.RawInterleaved:
                                        xmlTrk.SubChannel.Image.format = "rw_raw";
                                        break;
                                    case TrackSubchannelType.Q16:
                                    case TrackSubchannelType.Q16Interleaved:
                                        xmlTrk.SubChannel.Image.format = "q16";
                                        break;
                                }

                                if(trk.TrackFileOffset > 0)
                                {
                                    xmlTrk.SubChannel.Image.offset          = (long)trk.TrackSubchannelOffset;
                                    xmlTrk.SubChannel.Image.offsetSpecified = true;
                                }

                                xmlTrk.SubChannel.Image.Value = trk.TrackSubchannelFile;

                                // TODO: Packed subchannel has different size?
                                xmlTrk.SubChannel.Size = (xmlTrk.EndSector - xmlTrk.StartSector + 1) * 96;

                                Checksum subChkWorker = new Checksum();

                                sectors     = (ulong)(xmlTrk.EndSector - xmlTrk.StartSector + 1);
                                doneSectors = 0;

                                #if DEBUG
                                stopwatch.Restart();
                                #endif
                                while(doneSectors < sectors)
                                {
                                    byte[] sector;

                                    if(sectors - doneSectors >= SECTORS_TO_READ)
                                    {
                                        sector = imageFormat.ReadSectorsTag(doneSectors, SECTORS_TO_READ,
                                                                            (uint)xmlTrk.Sequence.TrackNumber,
                                                                            SectorTagType.CdSectorSubchannel);
                                        UpdateProgress2?.Invoke(null, $"Sector {doneSectors} of {sectors}", position,
                                                                fi.Length);
                                        doneSectors += SECTORS_TO_READ;
                                    }
                                    else
                                    {
                                        sector = imageFormat.ReadSectorsTag(doneSectors, (uint)(sectors - doneSectors),
                                                                            (uint)xmlTrk.Sequence.TrackNumber,
                                                                            SectorTagType.CdSectorSubchannel);
                                        UpdateProgress2?.Invoke(null, $"Sector {doneSectors} of {sectors}", position,
                                                                fi.Length);
                                        doneSectors += sectors - doneSectors;
                                    }

                                    subChkWorker.Update(sector);
                                }

                                List<ChecksumType> subChecksums = subChkWorker.End();

                                xmlTrk.SubChannel.Checksums = subChecksums.ToArray();
                                #if DEBUG
                                stopwatch.Stop();
                                Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash subchannel of track {1}",
                                                  stopwatch.Elapsed.TotalSeconds, trk.TrackSequence);
                                #endif

                                UpdateProgress2?.Invoke(null, null, 0, 0);
                            }

                            UpdateProgress?.Invoke(null, "Checking filesystems", maxProgress - 1, maxProgress);

                            #if DEBUG
                            stopwatch.Restart();
                            #endif
                            List<Partition> partitions = new List<Partition>();

                            foreach(IPartition partplugin in plugins.PartPluginsList.Values)
                                if(partplugin.GetInformation(imageFormat, out List<Partition> _partitions, 0)
                                ) // TODO: Subpartitions
                                    partitions.AddRange(_partitions);

                            xmlTrk.FileSystemInformation = new PartitionType[1];
                            if(partitions.Count > 0)
                            {
                                xmlTrk.FileSystemInformation = new PartitionType[partitions.Count];
                                for(int i = 0; i < partitions.Count; i++)
                                {
                                    xmlTrk.FileSystemInformation[i] = new PartitionType
                                    {
                                        Description = partitions[i].Description,
                                        EndSector   = (int)partitions[i].End,
                                        Name        = partitions[i].Name,
                                        Sequence    = (int)partitions[i].Sequence,
                                        StartSector = (int)partitions[i].Start,
                                        Type        = partitions[i].Type
                                    };

                                    List<FileSystemType> lstFs = new List<FileSystemType>();

                                    foreach(IFilesystem plugin in plugins.PluginsList.Values)
                                        try
                                        {
                                            if(!plugin.Identify(imageFormat, partitions[i])) continue;

                                            plugin.GetInformation(imageFormat, partitions[i], out _, null);
                                            lstFs.Add(plugin.XmlFsType);

                                            switch(plugin.XmlFsType.Type)
                                            {
                                                case "Opera":
                                                    dskType = MediaType.ThreeDO;
                                                    break;
                                                case "PC Engine filesystem":
                                                    dskType = MediaType.SuperCDROM2;
                                                    break;
                                                case "Nintendo Wii filesystem":
                                                    dskType = MediaType.WOD;
                                                    break;
                                                case "Nintendo Gamecube filesystem":
                                                    dskType = MediaType.GOD;
                                                    break;
                                            }
                                        }
                                        #pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                                        catch
                                            #pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                                        {
                                            //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                                        }

                                    if(lstFs.Count > 0) xmlTrk.FileSystemInformation[i].FileSystems = lstFs.ToArray();
                                }
                            }
                            else
                            {
                                xmlTrk.FileSystemInformation[0] = new PartitionType
                                {
                                    EndSector   = (int)xmlTrk.EndSector,
                                    StartSector = (int)xmlTrk.StartSector
                                };

                                Partition xmlPart = new Partition
                                {
                                    Start  = (ulong)xmlTrk.StartSector,
                                    Length = (ulong)(xmlTrk.EndSector - xmlTrk.StartSector + 1)
                                };

                                List<FileSystemType> lstFs = new List<FileSystemType>();

                                foreach(IFilesystem _plugin in plugins.PluginsList.Values)
                                    try
                                    {
                                        if(!_plugin.Identify(imageFormat, xmlPart)) continue;

                                        _plugin.GetInformation(imageFormat, xmlPart, out _, null);
                                        lstFs.Add(_plugin.XmlFsType);

                                        switch(_plugin.XmlFsType.Type)
                                        {
                                            case "Opera":
                                                dskType = MediaType.ThreeDO;
                                                break;
                                            case "PC Engine filesystem":
                                                dskType = MediaType.SuperCDROM2;
                                                break;
                                            case "Nintendo Wii filesystem":
                                                dskType = MediaType.WOD;
                                                break;
                                            case "Nintendo Gamecube filesystem":
                                                dskType = MediaType.GOD;
                                                break;
                                        }
                                    }
                                    #pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                                    catch
                                        #pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                                    {
                                        //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                                    }

                                if(lstFs.Count > 0) xmlTrk.FileSystemInformation[0].FileSystems = lstFs.ToArray();
                            }
                            #if DEBUG
                            stopwatch.Stop();
                            Console.WriteLine("Core.AddMedia(): Took {0} seconds to check all filesystems on track {1}",
                                              stopwatch.Elapsed.TotalSeconds, trk.TrackSequence);
                            #endif

                            trksLst.Add(xmlTrk);
                        }

                        UpdateProgress?.Invoke(null, "Finishing", maxProgress, maxProgress);

                        if(trksLst != null) sidecar.OpticalDisc[0].Track = trksLst.ToArray();

                        DiscImageChef.Metadata.MediaType.MediaTypeToString(dskType, out string dscType,
                                                                           out string dscSubType);
                        sidecar.OpticalDisc[0].DiscType    = dscType;
                        sidecar.OpticalDisc[0].DiscSubType = dscSubType;

                        if(!string.IsNullOrEmpty(imageFormat.Info.DriveManufacturer)     ||
                           !string.IsNullOrEmpty(imageFormat.Info.DriveModel)            ||
                           !string.IsNullOrEmpty(imageFormat.Info.DriveFirmwareRevision) ||
                           !string.IsNullOrEmpty(imageFormat.Info.DriveSerialNumber))
                        {
                            sidecar.OpticalDisc[0].DumpHardwareArray                     = new DumpHardwareType[1];
                            sidecar.OpticalDisc[0].DumpHardwareArray[0].Extents          = new ExtentType[0];
                            sidecar.OpticalDisc[0].DumpHardwareArray[0].Extents[0].Start = 0;
                            sidecar.OpticalDisc[0].DumpHardwareArray[0].Extents[0].End   = imageFormat.Info.Sectors;
                            sidecar.OpticalDisc[0].DumpHardwareArray[0].Manufacturer     =
                                imageFormat.Info.DriveManufacturer;
                            sidecar.OpticalDisc[0].DumpHardwareArray[0].Model    = imageFormat.Info.DriveModel;
                            sidecar.OpticalDisc[0].DumpHardwareArray[0].Firmware =
                                imageFormat.Info.DriveFirmwareRevision;
                            sidecar.OpticalDisc[0].DumpHardwareArray[0].Serial   = imageFormat.Info.DriveSerialNumber;
                            sidecar.OpticalDisc[0].DumpHardwareArray[0].Software = new SoftwareType
                            {
                                Name    = imageFormat.Info.Application,
                                Version = imageFormat.Info.ApplicationVersion
                            };
                        }

                        Context.WorkingDisc = sidecar.OpticalDisc[0];
                        Finished?.Invoke();
                        return;
                    }
                    case XmlMediaType.BlockMedia:
                    {
                        maxProgress = 3 + imageFormat.Info.ReadableMediaTags.Count;
                        UpdateProgress?.Invoke(null, "Hashing image file", 3, maxProgress);

                        sidecar.BlockMedia    = new BlockMediaType[1];
                        sidecar.BlockMedia[0] = new BlockMediaType
                        {
                            Checksums = imgChecksums.ToArray(),
                            Image     = new ImageType
                            {
                                format          = imageFormat.Format,
                                offset          = 0,
                                offsetSpecified = true,
                                Value           = Path.GetFileName(selectedFile)
                            },
                            Size     = fi.Length,
                            Sequence = new SequenceType {MediaTitle = imageFormat.Info.MediaTitle}
                        };
                        if(imageFormat.Info.MediaSequence != 0 && imageFormat.Info.LastMediaSequence != 0)
                        {
                            sidecar.BlockMedia[0].Sequence.MediaSequence = imageFormat.Info.MediaSequence;
                            sidecar.BlockMedia[0].Sequence.TotalMedia    = imageFormat.Info.LastMediaSequence;
                        }
                        else
                        {
                            sidecar.BlockMedia[0].Sequence.MediaSequence = 1;
                            sidecar.BlockMedia[0].Sequence.TotalMedia    = 1;
                        }

                        currentProgress = 3;

                        #if DEBUG
                        stopwatch.Restart();
                        #endif
                        foreach(MediaTagType tagType in imageFormat.Info.ReadableMediaTags)
                        {
                            currentProgress++;
                            UpdateProgress?.Invoke(null, $"Hashing file containing {tagType}", currentProgress,
                                                   maxProgress);

                            switch(tagType)
                            {
                                case MediaTagType.ATAPI_IDENTIFY:
                                    sidecar.BlockMedia[0].ATA                    = new ATAType();
                                    sidecar.BlockMedia[0].ATA.Identify           = new DumpType();
                                    sidecar.BlockMedia[0].ATA.Identify.Checksums = Checksum
                                                                                  .GetChecksums(imageFormat
                                                                                                   .ReadDiskTag(MediaTagType
                                                                                                                   .ATAPI_IDENTIFY))
                                                                                  .ToArray();
                                    sidecar.BlockMedia[0].ATA.Identify.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.ATAPI_IDENTIFY).Length;
                                    break;
                                case MediaTagType.ATA_IDENTIFY:
                                    sidecar.BlockMedia[0].ATA                    = new ATAType();
                                    sidecar.BlockMedia[0].ATA.Identify           = new DumpType();
                                    sidecar.BlockMedia[0].ATA.Identify.Checksums = Checksum
                                                                                  .GetChecksums(imageFormat
                                                                                                   .ReadDiskTag(MediaTagType
                                                                                                                   .ATA_IDENTIFY))
                                                                                  .ToArray();
                                    sidecar.BlockMedia[0].ATA.Identify.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.ATA_IDENTIFY).Length;
                                    break;
                                case MediaTagType.PCMCIA_CIS:
                                    byte[] cis =
                                        imageFormat.ReadDiskTag(MediaTagType.PCMCIA_CIS);
                                    sidecar.BlockMedia[0].PCMCIA               = new PCMCIAType();
                                    sidecar.BlockMedia[0].PCMCIA.CIS           = new DumpType();
                                    sidecar.BlockMedia[0].PCMCIA.CIS.Checksums = Checksum.GetChecksums(cis).ToArray();
                                    sidecar.BlockMedia[0].PCMCIA.CIS.Size      = cis.Length;
                                    Tuple[] tuples                             = CIS.GetTuples(cis);
                                    if(tuples != null)
                                        foreach(Tuple tuple in tuples)
                                            if(tuple.Code == TupleCodes.CISTPL_MANFID)
                                            {
                                                ManufacturerIdentificationTuple manfid =
                                                    CIS.DecodeManufacturerIdentificationTuple(tuple);

                                                if(manfid != null)
                                                {
                                                    sidecar.BlockMedia[0].PCMCIA.ManufacturerCode =
                                                        manfid.ManufacturerID;
                                                    sidecar.BlockMedia[0].PCMCIA.CardCode =
                                                        manfid.CardID;
                                                    sidecar.BlockMedia[0].PCMCIA.ManufacturerCodeSpecified = true;
                                                    sidecar.BlockMedia[0].PCMCIA.CardCodeSpecified         = true;
                                                }
                                            }
                                            else if(tuple.Code == TupleCodes.CISTPL_VERS_1)
                                            {
                                                Level1VersionTuple vers = CIS.DecodeLevel1VersionTuple(tuple);

                                                if(vers != null)
                                                {
                                                    sidecar.BlockMedia[0].PCMCIA.Manufacturer = vers.Manufacturer;
                                                    sidecar.BlockMedia[0].PCMCIA.ProductName  = vers.Product;
                                                    sidecar.BlockMedia[0].PCMCIA.Compliance   =
                                                        $"{vers.MajorVersion}.{vers.MinorVersion}";
                                                    sidecar.BlockMedia[0].PCMCIA.AdditionalInformation =
                                                        vers.AdditionalInformation;
                                                }
                                            }

                                    break;
                                case MediaTagType.SCSI_INQUIRY:
                                    sidecar.BlockMedia[0].SCSI                   = new SCSIType();
                                    sidecar.BlockMedia[0].SCSI.Inquiry           = new DumpType();
                                    sidecar.BlockMedia[0].SCSI.Inquiry.Checksums = Checksum
                                                                                  .GetChecksums(imageFormat
                                                                                                   .ReadDiskTag(MediaTagType
                                                                                                                   .SCSI_INQUIRY))
                                                                                  .ToArray();
                                    sidecar.BlockMedia[0].SCSI.Inquiry.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.SCSI_INQUIRY).Length;
                                    break;
                                case MediaTagType.SD_CID:
                                    if(sidecar.BlockMedia[0].SecureDigital == null)
                                        sidecar.BlockMedia[0].SecureDigital           = new SecureDigitalType();
                                    sidecar.BlockMedia[0].SecureDigital.CID           = new DumpType();
                                    sidecar.BlockMedia[0].SecureDigital.CID.Checksums = Checksum
                                                                                       .GetChecksums(imageFormat
                                                                                                        .ReadDiskTag(MediaTagType
                                                                                                                        .SD_CID))
                                                                                       .ToArray();
                                    sidecar.BlockMedia[0].SecureDigital.CID.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.SD_CID).Length;
                                    break;
                                case MediaTagType.SD_CSD:
                                    if(sidecar.BlockMedia[0].SecureDigital == null)
                                        sidecar.BlockMedia[0].SecureDigital           = new SecureDigitalType();
                                    sidecar.BlockMedia[0].SecureDigital.CSD           = new DumpType();
                                    sidecar.BlockMedia[0].SecureDigital.CSD.Checksums = Checksum
                                                                                       .GetChecksums(imageFormat
                                                                                                        .ReadDiskTag(MediaTagType
                                                                                                                        .SD_CSD))
                                                                                       .ToArray();
                                    sidecar.BlockMedia[0].SecureDigital.CSD.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.SD_CSD).Length;
                                    break;
                                case MediaTagType.MMC_ExtendedCSD:
                                    if(sidecar.BlockMedia[0].SecureDigital == null)
                                        sidecar.BlockMedia[0].SecureDigital =
                                            new SecureDigitalType();
                                    sidecar.BlockMedia[0].MultiMediaCard.ExtendedCSD           = new DumpType();
                                    sidecar.BlockMedia[0].MultiMediaCard.ExtendedCSD.Checksums = Checksum
                                                                                                .GetChecksums(imageFormat
                                                                                                                 .ReadDiskTag(MediaTagType
                                                                                                                                 .MMC_ExtendedCSD))
                                                                                                .ToArray();
                                    sidecar.BlockMedia[0].MultiMediaCard.ExtendedCSD.Size =
                                        imageFormat.ReadDiskTag(MediaTagType.MMC_ExtendedCSD).Length;
                                    break;
                            }
                        }
                        #if DEBUG
                        stopwatch.Stop();
                        Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash media tags",
                                          stopwatch.Elapsed.TotalSeconds);
                        #endif

                        // If there is only one track, and it's the same as the image file (e.g. ".iso" files), don't re-checksum.
                        if(imageFormat.Id == new Guid("12345678-AAAA-BBBB-CCCC-123456789000"))
                            sidecar.BlockMedia[0].ContentChecksums = sidecar.BlockMedia[0].Checksums;
                        else
                        {
                            const uint SECTORS_TO_READ = 512;
                            ulong      sectors         = imageFormat.Info.Sectors;
                            ulong      doneSectors     = 0;

                            UpdateProgress?.Invoke(null, "Hashing media contents", currentProgress, maxProgress);

                            Checksum cntChkWorker = new Checksum();

                            #if DEBUG
                            stopwatch.Restart();
                            #endif
                            while(doneSectors < sectors)
                            {
                                byte[] sector;

                                if(sectors - doneSectors >= SECTORS_TO_READ)
                                {
                                    sector = imageFormat.ReadSectors(doneSectors, SECTORS_TO_READ);
                                    UpdateProgress2?.Invoke(null, $"Sector {doneSectors} of {sectors}",
                                                            (long)doneSectors, (long)sectors);
                                    doneSectors += SECTORS_TO_READ;
                                }
                                else
                                {
                                    sector = imageFormat.ReadSectors(doneSectors, (uint)(sectors - doneSectors));
                                    UpdateProgress2?.Invoke(null, $"Sector {doneSectors} of {sectors}",
                                                            (long)doneSectors, (long)sectors);
                                    doneSectors += sectors - doneSectors;
                                }

                                cntChkWorker.Update(sector);
                            }

                            List<ChecksumType> cntChecksums = cntChkWorker.End();
                            #if DEBUG
                            stopwatch.Stop();
                            Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash media contents",
                                              stopwatch.Elapsed.TotalSeconds);
                            #endif

                            sidecar.BlockMedia[0].ContentChecksums = cntChecksums.ToArray();
                        }

                        DiscImageChef.Metadata.MediaType.MediaTypeToString(imageFormat.Info.MediaType,
                                                                           out string dskType, out string dskSubType);
                        sidecar.BlockMedia[0].DiskType    = dskType;
                        sidecar.BlockMedia[0].DiskSubType = dskSubType;

                        sidecar.BlockMedia[0].Dimensions =
                            Dimensions.DimensionsFromMediaType(imageFormat.Info.MediaType);

                        sidecar.BlockMedia[0].LogicalBlocks    = (long)imageFormat.Info.Sectors;
                        sidecar.BlockMedia[0].LogicalBlockSize = (int)imageFormat.Info.SectorSize;
                        // TODO: Detect it
                        sidecar.BlockMedia[0].PhysicalBlockSize = (int)imageFormat.Info.SectorSize;

                        UpdateProgress?.Invoke(null, "Checking filesystems", maxProgress - 1, maxProgress);

                        #if DEBUG
                        stopwatch.Restart();
                        #endif
                        List<Partition> partitions = new List<Partition>();

                        foreach(IPartition partplugin in plugins.PartPluginsList.Values)
                        {
                            if(!partplugin.GetInformation(imageFormat, out partitions, 0)) continue;

                            break;
                        }

                        sidecar.BlockMedia[0].FileSystemInformation = new PartitionType[1];
                        if(partitions.Count > 0)
                        {
                            sidecar.BlockMedia[0].FileSystemInformation = new PartitionType[partitions.Count];
                            for(int i = 0; i < partitions.Count; i++)
                            {
                                sidecar.BlockMedia[0].FileSystemInformation[i] = new PartitionType
                                {
                                    Description = partitions[i].Description,
                                    EndSector   = (int)partitions[i].End,
                                    Name        = partitions[i].Name,
                                    Sequence    = (int)partitions[i].Sequence,
                                    StartSector = (int)partitions[i].Start,
                                    Type        = partitions[i].Type
                                };

                                List<FileSystemType> lstFs = new List<FileSystemType>();

                                foreach(IFilesystem plugin in plugins.PluginsList.Values)
                                    try
                                    {
                                        if(!plugin.Identify(imageFormat, partitions[i])) continue;

                                        plugin.GetInformation(imageFormat, partitions[i], out _, null);
                                        lstFs.Add(plugin.XmlFsType);
                                    }
                                    #pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                                    catch
                                        #pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                                    {
                                        //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                                    }

                                if(lstFs.Count > 0)
                                    sidecar.BlockMedia[0].FileSystemInformation[i].FileSystems = lstFs.ToArray();
                            }
                        }
                        else
                        {
                            sidecar.BlockMedia[0].FileSystemInformation[0] = new PartitionType
                            {
                                StartSector = 0,
                                EndSector   = (int)(imageFormat.Info.Sectors - 1)
                            };

                            Partition wholePart = new Partition {Start = 0, Length = imageFormat.Info.Sectors};

                            List<FileSystemType> lstFs = new List<FileSystemType>();

                            foreach(IFilesystem _plugin in plugins.PluginsList.Values)
                                try
                                {
                                    if(!_plugin.Identify(imageFormat, wholePart)) continue;

                                    _plugin.GetInformation(imageFormat, wholePart, out _, null);
                                    lstFs.Add(_plugin.XmlFsType);
                                }
                                #pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                                catch
                                    #pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                                {
                                    //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                                }

                            if(lstFs.Count > 0)
                                sidecar.BlockMedia[0].FileSystemInformation[0].FileSystems = lstFs.ToArray();
                        }
                        #if DEBUG
                        stopwatch.Stop();
                        Console.WriteLine("Core.AddMedia(): Took {0} seconds to check all filesystems",
                                          stopwatch.Elapsed.TotalSeconds);
                        #endif

                        // TODO: Implement support for getting CHS
                        UpdateProgress?.Invoke(null, "Finishing", maxProgress, maxProgress);
                        Context.WorkingDisk = sidecar.BlockMedia[0];
                        Finished?.Invoke();
                        return;
                    }
                    case XmlMediaType.LinearMedia:
                    {
                        Failed?.Invoke("Linear media not yet supported.");
                        return;
                    }
                    case XmlMediaType.AudioMedia:
                    {
                        Failed?.Invoke("Audio media not yet supported.");
                        return;
                    }
                }

                Failed?.Invoke("Should've not arrived here.");
            }
            catch(Exception ex)
            {
                Failed?.Invoke($"Error reading file: {ex.Message}\n{ex.StackTrace}");
                #if DEBUG
                Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
                #endif
            }
        }

        static string LbaToMsf(long lba)
        {
            long m, s, f;
            if(lba >= -150)
            {
                m   =  (lba + 150) / (75 * 60);
                lba -= m           * (75 * 60);
                s   =  (lba + 150) / 75;
                lba -= s           * 75;
                f   =  lba + 150;
            }
            else
            {
                m   =  (lba + 450150) / (75 * 60);
                lba -= m              * (75 * 60);
                s   =  (lba + 450150) / 75;
                lba -= s              * 75;
                f   =  lba + 450150;
            }

            return $"{m}:{s:D2}:{f:D2}";
        }

        static string DdcdLbaToMsf(long lba)
        {
            long h, m, s, f;
            if(lba >= -150)
            {
                h   =  (lba + 150) / (75 * 60 * 60);
                lba -= h           * (75 * 60 * 60);
                m   =  (lba + 150) / (75 * 60);
                lba -= m           * (75 * 60);
                s   =  (lba + 150) / 75;
                lba -= s           * 75;
                f   =  lba + 150;
            }
            else
            {
                h   =  (lba + 450150 * 2)  / (75 * 60 * 60);
                lba -= h             * (75 * 60  * 60);
                m   =  (lba + 450150 * 2)  / (75 * 60);
                lba -= m             * (75 * 60);
                s   =  (lba + 450150 * 2)  / 75;
                lba -= s             * 75;
                f   =  lba + 450150  * 2;
            }

            return string.Format("{3}:{0:D2}:{1:D2}:{2:D2}", m, s, f, h);
        }
    }
}