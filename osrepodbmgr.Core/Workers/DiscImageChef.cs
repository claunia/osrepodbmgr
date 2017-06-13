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
using DiscImageChef.Decoders.PCMCIA;
using DiscImageChef.Filesystems;
using DiscImageChef.Filters;
using DiscImageChef.ImagePlugins;
using DiscImageChef.PartPlugins;
using Schemas;

namespace osrepodbmgr.Core
{
    public static partial class Workers
    {
        public static void AddMedia()
        {
            if(string.IsNullOrWhiteSpace(Context.selectedFile))
            {
                if(Failed != null)
                    Failed("There is no file set");
                return;
            }

            string filesPath;

            if(!string.IsNullOrEmpty(Context.tmpFolder) && Directory.Exists(Context.tmpFolder))
                filesPath = Context.tmpFolder;
            else
                filesPath = Context.path;

            string selectedFile = Path.Combine(filesPath, Context.selectedFile);

            if(!File.Exists(selectedFile))
            {
                if(Failed != null)
                    Failed("Selected file does not exist");
                return;
            }

            CICMMetadataType sidecar = new CICMMetadataType();
            PluginBase plugins = new PluginBase();
            plugins.RegisterAllPlugins();
            ImagePlugin _imageFormat;

            long maxProgress = 4;
            long currentProgress = 0;

            FiltersList filtersList = new FiltersList();

            if(UpdateProgress != null)
                UpdateProgress(null, "Detecting image filter", 1, maxProgress);

            Filter inputFilter = filtersList.GetFilter(selectedFile);


            if(inputFilter == null)
            {
                if(Failed != null)
                    Failed("Cannot open specified file.");
                return;
            }

            try
            {
#if DEBUG
                stopwatch.Restart();
#endif
                if(UpdateProgress != null)
                    UpdateProgress(null, "Detecting image format", 2, maxProgress);
                _imageFormat = ImageFormat.Detect(inputFilter);
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.AddMedia(): Took {0} seconds to detect image format", stopwatch.Elapsed.TotalSeconds);
#endif

                if(_imageFormat == null)
                {
                    if(Failed != null)
                        Failed("Image format not identified, not proceeding with analysis.");
                    return;
                }

                try
                {
                    if(!_imageFormat.OpenImage(inputFilter))
                    {
                        if(Failed != null)
                            Failed("Unable to open image format\n" +
                                   "No error given");
                        return;
                    }
                }
                catch(Exception ex)
                {
                    if(Failed != null)
                        Failed(string.Format("Unable to open image format\n" +
                                             "Error: {0}", ex.Message));
#if DEBUG
                    Console.WriteLine("Exception {0}\n{1}", ex.Message, ex.InnerException);
#endif
                    return;
                }

                FileInfo fi = new FileInfo(selectedFile);
                FileStream fs = new FileStream(selectedFile, FileMode.Open, FileAccess.Read);

                Checksum imgChkWorker = new Checksum();

                if(UpdateProgress != null)
                    UpdateProgress(null, "Hashing image file", 3, maxProgress);

#if DEBUG
                stopwatch.Restart();
#endif
                byte[] data;
                long position = 0;
                while(position < (fi.Length - 524288))
                {
                    data = new byte[524288];
                    fs.Read(data, 0, 524288);

                    if(UpdateProgress2 != null)
                        UpdateProgress2(null, string.Format("{0} of {1} bytes", position, fi.Length), position, fi.Length);

                    imgChkWorker.Update(data);

                    position += 524288;
                }

                data = new byte[fi.Length - position];
                fs.Read(data, 0, (int)(fi.Length - position));

                if(UpdateProgress2 != null)
                    UpdateProgress2(null, string.Format("{0} of {1} bytes", position, fi.Length), position, fi.Length);

                imgChkWorker.Update(data);

                fs.Close();
#if DEBUG
                stopwatch.Stop();
                Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash image file", stopwatch.Elapsed.TotalSeconds);
#endif

                List<ChecksumType> imgChecksums = imgChkWorker.End();

                if(UpdateProgress2 != null)
                    UpdateProgress2(null, null, 0, 0);

                switch(_imageFormat.ImageInfo.xmlMediaType)
                {
                    case XmlMediaType.OpticalDisc:
                        {
                            maxProgress = 4 + _imageFormat.ImageInfo.readableMediaTags.Count + _imageFormat.GetTracks().Count;

                            if(UpdateProgress != null)
                                UpdateProgress(null, "Hashing image file", 3, maxProgress);

                            sidecar.OpticalDisc = new OpticalDiscType[1];
                            sidecar.OpticalDisc[0] = new OpticalDiscType();
                            sidecar.OpticalDisc[0].Checksums = imgChecksums.ToArray();
                            sidecar.OpticalDisc[0].Image = new ImageType();
                            sidecar.OpticalDisc[0].Image.format = _imageFormat.GetImageFormat();
                            sidecar.OpticalDisc[0].Image.offset = 0;
                            sidecar.OpticalDisc[0].Image.offsetSpecified = true;
                            sidecar.OpticalDisc[0].Image.Value = Path.GetFileName(selectedFile);
                            sidecar.OpticalDisc[0].Size = fi.Length;
                            sidecar.OpticalDisc[0].Sequence = new SequenceType();
                            if(_imageFormat.GetMediaSequence() != 0 && _imageFormat.GetLastDiskSequence() != 0)
                            {
                                sidecar.OpticalDisc[0].Sequence.MediaSequence = _imageFormat.GetMediaSequence();
                                sidecar.OpticalDisc[0].Sequence.TotalMedia = _imageFormat.GetMediaSequence();
                            }
                            else
                            {
                                sidecar.OpticalDisc[0].Sequence.MediaSequence = 1;
                                sidecar.OpticalDisc[0].Sequence.TotalMedia = 1;
                            }
                            sidecar.OpticalDisc[0].Sequence.MediaTitle = _imageFormat.GetImageName();

                            MediaType dskType = _imageFormat.ImageInfo.mediaType;

                            currentProgress = 3;

#if DEBUG
                            stopwatch.Restart();
#endif
                            foreach(MediaTagType tagType in _imageFormat.ImageInfo.readableMediaTags)
                            {
                                currentProgress++;
                                if(UpdateProgress != null)
                                    UpdateProgress(null, string.Format("Hashing file containing {0}", tagType), currentProgress, maxProgress);

                                switch(tagType)
                                {
                                    case MediaTagType.CD_ATIP:
                                        sidecar.OpticalDisc[0].ATIP = new DumpType();
                                        sidecar.OpticalDisc[0].ATIP.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.CD_ATIP)).ToArray();
                                        sidecar.OpticalDisc[0].ATIP.Size = _imageFormat.ReadDiskTag(MediaTagType.CD_ATIP).Length;
                                        DiscImageChef.Decoders.CD.ATIP.CDATIP? atip = DiscImageChef.Decoders.CD.ATIP.Decode(_imageFormat.ReadDiskTag(MediaTagType.CD_ATIP));
                                        if(atip.HasValue)
                                        {
                                            if(atip.Value.DDCD)
                                                dskType = atip.Value.DiscType ? MediaType.DDCDRW : MediaType.DDCDR;
                                            else
                                                dskType = atip.Value.DiscType ? MediaType.CDRW : MediaType.CDR;
                                        }
                                        break;
                                    case MediaTagType.DVD_BCA:
                                        sidecar.OpticalDisc[0].BCA = new DumpType();
                                        sidecar.OpticalDisc[0].BCA.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.DVD_BCA)).ToArray();
                                        sidecar.OpticalDisc[0].BCA.Size = _imageFormat.ReadDiskTag(MediaTagType.DVD_BCA).Length;
                                        break;
                                    case MediaTagType.BD_BCA:
                                        sidecar.OpticalDisc[0].BCA = new DumpType();
                                        sidecar.OpticalDisc[0].BCA.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.BD_BCA)).ToArray();
                                        sidecar.OpticalDisc[0].BCA.Size = _imageFormat.ReadDiskTag(MediaTagType.BD_BCA).Length;
                                        break;
                                    case MediaTagType.DVD_CMI:
                                        sidecar.OpticalDisc[0].CMI = new DumpType();
                                        DiscImageChef.Decoders.DVD.CSS_CPRM.LeadInCopyright? cmi = DiscImageChef.Decoders.DVD.CSS_CPRM.DecodeLeadInCopyright(_imageFormat.ReadDiskTag(MediaTagType.DVD_CMI));
                                        if(cmi.HasValue)
                                        {
                                            switch(cmi.Value.CopyrightType)
                                            {
                                                case DiscImageChef.Decoders.DVD.CopyrightType.AACS:
                                                    sidecar.OpticalDisc[0].CopyProtection = "AACS";
                                                    break;
                                                case DiscImageChef.Decoders.DVD.CopyrightType.CSS:
                                                    sidecar.OpticalDisc[0].CopyProtection = "CSS";
                                                    break;
                                                case DiscImageChef.Decoders.DVD.CopyrightType.CPRM:
                                                    sidecar.OpticalDisc[0].CopyProtection = "CPRM";
                                                    break;
                                            }
                                        }
                                        sidecar.OpticalDisc[0].CMI.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.DVD_CMI)).ToArray();
                                        sidecar.OpticalDisc[0].CMI.Size = _imageFormat.ReadDiskTag(MediaTagType.DVD_CMI).Length;
                                        break;
                                    case MediaTagType.DVD_DMI:
                                        sidecar.OpticalDisc[0].DMI = new DumpType();
                                        sidecar.OpticalDisc[0].DMI.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.DVD_DMI)).ToArray();
                                        sidecar.OpticalDisc[0].DMI.Size = _imageFormat.ReadDiskTag(MediaTagType.DVD_DMI).Length;
                                        if(DiscImageChef.Decoders.Xbox.DMI.IsXbox(_imageFormat.ReadDiskTag(MediaTagType.DVD_DMI)))
                                        {
                                            dskType = MediaType.XGD;
                                            sidecar.OpticalDisc[0].Dimensions = new DimensionsType();
                                            sidecar.OpticalDisc[0].Dimensions.Diameter = 120;
                                        }
                                        else if(DiscImageChef.Decoders.Xbox.DMI.IsXbox360(_imageFormat.ReadDiskTag(MediaTagType.DVD_DMI)))
                                        {
                                            dskType = MediaType.XGD2;
                                            sidecar.OpticalDisc[0].Dimensions = new DimensionsType();
                                            sidecar.OpticalDisc[0].Dimensions.Diameter = 120;
                                        }
                                        break;
                                    case MediaTagType.DVD_PFI:
                                        sidecar.OpticalDisc[0].PFI = new DumpType();
                                        sidecar.OpticalDisc[0].PFI.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.DVD_PFI)).ToArray();
                                        sidecar.OpticalDisc[0].PFI.Size = _imageFormat.ReadDiskTag(MediaTagType.DVD_PFI).Length;
                                        DiscImageChef.Decoders.DVD.PFI.PhysicalFormatInformation? pfi = DiscImageChef.Decoders.DVD.PFI.Decode(_imageFormat.ReadDiskTag(MediaTagType.DVD_PFI));
                                        if(pfi.HasValue)
                                        {
                                            if(dskType != MediaType.XGD &&
                                                dskType != MediaType.XGD2 &&
                                                dskType != MediaType.XGD3)
                                            {
                                                switch(pfi.Value.DiskCategory)
                                                {
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.DVDPR:
                                                        dskType = MediaType.DVDPR;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.DVDPRDL:
                                                        dskType = MediaType.DVDPRDL;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.DVDPRW:
                                                        dskType = MediaType.DVDPRW;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.DVDPRWDL:
                                                        dskType = MediaType.DVDPRWDL;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.DVDR:
                                                        dskType = MediaType.DVDR;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.DVDRAM:
                                                        dskType = MediaType.DVDRAM;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.DVDROM:
                                                        dskType = MediaType.DVDROM;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.DVDRW:
                                                        dskType = MediaType.DVDRW;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.HDDVDR:
                                                        dskType = MediaType.HDDVDR;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.HDDVDRAM:
                                                        dskType = MediaType.HDDVDRAM;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.HDDVDROM:
                                                        dskType = MediaType.HDDVDROM;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.HDDVDRW:
                                                        dskType = MediaType.HDDVDRW;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.Nintendo:
                                                        dskType = MediaType.GOD;
                                                        break;
                                                    case DiscImageChef.Decoders.DVD.DiskCategory.UMD:
                                                        dskType = MediaType.UMD;
                                                        break;
                                                }

                                                if(dskType == MediaType.DVDR && pfi.Value.PartVersion == 6)
                                                    dskType = MediaType.DVDRDL;
                                                if(dskType == MediaType.DVDRW && pfi.Value.PartVersion == 3)
                                                    dskType = MediaType.DVDRWDL;
                                                if(dskType == MediaType.GOD && pfi.Value.DiscSize == DiscImageChef.Decoders.DVD.DVDSize.OneTwenty)
                                                    dskType = MediaType.WOD;

                                                sidecar.OpticalDisc[0].Dimensions = new DimensionsType();
                                                if(dskType == MediaType.UMD)
                                                    sidecar.OpticalDisc[0].Dimensions.Diameter = 60;
                                                else if(pfi.Value.DiscSize == DiscImageChef.Decoders.DVD.DVDSize.Eighty)
                                                    sidecar.OpticalDisc[0].Dimensions.Diameter = 80;
                                                else if(pfi.Value.DiscSize == DiscImageChef.Decoders.DVD.DVDSize.OneTwenty)
                                                    sidecar.OpticalDisc[0].Dimensions.Diameter = 120;
                                            }
                                        }
                                        break;
                                    case MediaTagType.CD_PMA:
                                        sidecar.OpticalDisc[0].PMA = new DumpType();
                                        sidecar.OpticalDisc[0].PMA.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.CD_PMA)).ToArray();
                                        sidecar.OpticalDisc[0].PMA.Size = _imageFormat.ReadDiskTag(MediaTagType.CD_PMA).Length;
                                        break;
                                }
                            }
#if DEBUG
                            stopwatch.Stop();
                            Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash media tags", stopwatch.Elapsed.TotalSeconds);
#endif

                            try
                            {
                                List<Session> sessions = _imageFormat.GetSessions();
                                sidecar.OpticalDisc[0].Sessions = sessions != null ? sessions.Count : 1;
                            }
                            catch
                            {
                                sidecar.OpticalDisc[0].Sessions = 1;
                            }

                            List<Track> tracks = _imageFormat.GetTracks();
                            List<Schemas.TrackType> trksLst = null;
                            if(tracks != null)
                            {
                                sidecar.OpticalDisc[0].Tracks = new int[1];
                                sidecar.OpticalDisc[0].Tracks[0] = tracks.Count;
                                trksLst = new List<Schemas.TrackType>();
                            }

                            foreach(Track trk in tracks)
                            {
                                currentProgress++;
                                if(UpdateProgress != null)
                                    UpdateProgress(null, string.Format("Hashing track {0}", trk.TrackSequence), currentProgress, maxProgress);

                                Schemas.TrackType xmlTrk = new Schemas.TrackType();
                                switch(trk.TrackType)
                                {
                                    case DiscImageChef.ImagePlugins.TrackType.Audio:
                                        xmlTrk.TrackType1 = TrackTypeTrackType.audio;
                                        break;
                                    case DiscImageChef.ImagePlugins.TrackType.CDMode2Form2:
                                        xmlTrk.TrackType1 = TrackTypeTrackType.m2f2;
                                        break;
                                    case DiscImageChef.ImagePlugins.TrackType.CDMode2Formless:
                                        xmlTrk.TrackType1 = TrackTypeTrackType.mode2;
                                        break;
                                    case DiscImageChef.ImagePlugins.TrackType.CDMode2Form1:
                                        xmlTrk.TrackType1 = TrackTypeTrackType.m2f1;
                                        break;
                                    case DiscImageChef.ImagePlugins.TrackType.CDMode1:
                                        xmlTrk.TrackType1 = TrackTypeTrackType.mode1;
                                        break;
                                    case DiscImageChef.ImagePlugins.TrackType.Data:
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
                                xmlTrk.Sequence = new TrackSequenceType();
                                xmlTrk.Sequence.Session = trk.TrackSession;
                                xmlTrk.Sequence.TrackNumber = (int)trk.TrackSequence;
                                xmlTrk.StartSector = (long)trk.TrackStartSector;
                                xmlTrk.EndSector = (long)trk.TrackEndSector;

                                if(trk.Indexes != null && trk.Indexes.ContainsKey(0))
                                {
                                    ulong idx0;
                                    if(trk.Indexes.TryGetValue(0, out idx0))
                                        xmlTrk.StartSector = (long)idx0;
                                }

                                if(sidecar.OpticalDisc[0].DiscType == "CD" ||
                                    sidecar.OpticalDisc[0].DiscType == "GD")
                                {
                                    xmlTrk.StartMSF = LbaToMsf(xmlTrk.StartSector);
                                    xmlTrk.EndMSF = LbaToMsf(xmlTrk.EndSector);
                                }
                                else if(sidecar.OpticalDisc[0].DiscType == "DDCD")
                                {
                                    xmlTrk.StartMSF = DdcdLbaToMsf(xmlTrk.StartSector);
                                    xmlTrk.EndMSF = DdcdLbaToMsf(xmlTrk.EndSector);
                                }

                                xmlTrk.Image = new ImageType();
                                xmlTrk.Image.Value = Path.GetFileName(trk.TrackFile);
                                if(trk.TrackFileOffset > 0)
                                {
                                    xmlTrk.Image.offset = (long)trk.TrackFileOffset;
                                    xmlTrk.Image.offsetSpecified = true;
                                }

                                xmlTrk.Image.format = trk.TrackFileType;
                                xmlTrk.Size = (xmlTrk.EndSector - xmlTrk.StartSector + 1) * trk.TrackRawBytesPerSector;
                                xmlTrk.BytesPerSector = trk.TrackBytesPerSector;

                                uint sectorsToRead = 512;

                                Checksum trkChkWorker = new Checksum();

                                ulong sectors = (ulong)(xmlTrk.EndSector - xmlTrk.StartSector + 1);
                                ulong doneSectors = 0;

#if DEBUG
                                stopwatch.Restart();
#endif
                                while(doneSectors < sectors)
                                {
                                    byte[] sector;

                                    if((sectors - doneSectors) >= sectorsToRead)
                                    {
                                        sector = _imageFormat.ReadSectorsLong(doneSectors, sectorsToRead, (uint)xmlTrk.Sequence.TrackNumber);
                                        if(UpdateProgress2 != null)
                                            UpdateProgress2(null, string.Format("Sector {0} of {1}", doneSectors, sectors), (long)doneSectors, (long)sectors);
                                        doneSectors += sectorsToRead;
                                    }
                                    else
                                    {
                                        sector = _imageFormat.ReadSectorsLong(doneSectors, (uint)(sectors - doneSectors), (uint)xmlTrk.Sequence.TrackNumber);
                                        if(UpdateProgress2 != null)
                                            UpdateProgress2(null, string.Format("Sector {0} of {1}", doneSectors, sectors), (long)doneSectors, (long)sectors);
                                        doneSectors += (sectors - doneSectors);
                                    }

                                    trkChkWorker.Update(sector);
                                }

                                List<ChecksumType> trkChecksums = trkChkWorker.End();
#if DEBUG
                                stopwatch.Stop();
                                Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash track {1}", stopwatch.Elapsed.TotalSeconds, trk.TrackSequence);
#endif

                                xmlTrk.Checksums = trkChecksums.ToArray();

                                if(UpdateProgress2 != null)
                                    UpdateProgress2(null, null, 0, 0);

                                if(trk.TrackSubchannelType != TrackSubchannelType.None)
                                {
                                    if(UpdateProgress != null)
                                        UpdateProgress(null, string.Format("Hashing subchannel of track {0}", trk.TrackSequence), currentProgress, maxProgress);

                                    xmlTrk.SubChannel = new SubChannelType();
                                    xmlTrk.SubChannel.Image = new ImageType();
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
                                        xmlTrk.SubChannel.Image.offset = (long)trk.TrackSubchannelOffset;
                                        xmlTrk.SubChannel.Image.offsetSpecified = true;
                                    }
                                    xmlTrk.SubChannel.Image.Value = trk.TrackSubchannelFile;

                                    // TODO: Packed subchannel has different size?
                                    xmlTrk.SubChannel.Size = (xmlTrk.EndSector - xmlTrk.StartSector + 1) * 96;

                                    Checksum subChkWorker = new Checksum();

                                    sectors = (ulong)(xmlTrk.EndSector - xmlTrk.StartSector + 1);
                                    doneSectors = 0;

#if DEBUG
                                    stopwatch.Restart();
#endif
                                    while(doneSectors < sectors)
                                    {
                                        byte[] sector;

                                        if((sectors - doneSectors) >= sectorsToRead)
                                        {
                                            sector = _imageFormat.ReadSectorsTag(doneSectors, sectorsToRead, (uint)xmlTrk.Sequence.TrackNumber, SectorTagType.CDSectorSubchannel);
                                            if(UpdateProgress2 != null)
                                                UpdateProgress2(null, string.Format("Sector {0} of {1}", doneSectors, sectors), position, fi.Length);
                                            doneSectors += sectorsToRead;
                                        }
                                        else
                                        {
                                            sector = _imageFormat.ReadSectorsTag(doneSectors, (uint)(sectors - doneSectors), (uint)xmlTrk.Sequence.TrackNumber, SectorTagType.CDSectorSubchannel);
                                            if(UpdateProgress2 != null)
                                                UpdateProgress2(null, string.Format("Sector {0} of {1}", doneSectors, sectors), position, fi.Length);
                                            doneSectors += (sectors - doneSectors);
                                        }

                                        subChkWorker.Update(sector);
                                    }

                                    List<ChecksumType> subChecksums = subChkWorker.End();

                                    xmlTrk.SubChannel.Checksums = subChecksums.ToArray();
#if DEBUG
                                    stopwatch.Stop();
                                    Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash subchannel of track {1}", stopwatch.Elapsed.TotalSeconds, trk.TrackSequence);
#endif

                                    if(UpdateProgress2 != null)
                                        UpdateProgress2(null, null, 0, 0);
                                }

                                if(UpdateProgress != null)
                                    UpdateProgress(null, "Checking filesystems", maxProgress - 1, maxProgress);

#if DEBUG
                                stopwatch.Restart();
#endif
                                List<Partition> partitions = new List<Partition>();

                                foreach(PartPlugin _partplugin in plugins.PartPluginsList.Values)
                                {
                                    List<Partition> _partitions;

                                    if(_partplugin.GetInformation(_imageFormat, out _partitions))
                                        partitions.AddRange(_partitions);
                                }

                                xmlTrk.FileSystemInformation = new PartitionType[1];
                                if(partitions.Count > 0)
                                {
                                    xmlTrk.FileSystemInformation = new PartitionType[partitions.Count];
                                    for(int i = 0; i < partitions.Count; i++)
                                    {
                                        xmlTrk.FileSystemInformation[i] = new PartitionType();
                                        xmlTrk.FileSystemInformation[i].Description = partitions[i].PartitionDescription;
                                        xmlTrk.FileSystemInformation[i].EndSector = (int)(partitions[i].PartitionStartSector + partitions[i].PartitionSectors - 1);
                                        xmlTrk.FileSystemInformation[i].Name = partitions[i].PartitionName;
                                        xmlTrk.FileSystemInformation[i].Sequence = (int)partitions[i].PartitionSequence;
                                        xmlTrk.FileSystemInformation[i].StartSector = (int)partitions[i].PartitionStartSector;
                                        xmlTrk.FileSystemInformation[i].Type = partitions[i].PartitionType;

                                        List<FileSystemType> lstFs = new List<FileSystemType>();

                                        foreach(Filesystem _plugin in plugins.PluginsList.Values)
                                        {
                                            try
                                            {
                                                if(_plugin.Identify(_imageFormat, partitions[i].PartitionStartSector, partitions[i].PartitionStartSector + partitions[i].PartitionSectors - 1))
                                                {
                                                    string foo;
                                                    _plugin.GetInformation(_imageFormat, partitions[i].PartitionStartSector, partitions[i].PartitionStartSector + partitions[i].PartitionSectors - 1, out foo);
                                                    lstFs.Add(_plugin.XmlFSType);

                                                    if(_plugin.XmlFSType.Type == "Opera")
                                                        dskType = MediaType.ThreeDO;
                                                    if(_plugin.XmlFSType.Type == "PC Engine filesystem")
                                                        dskType = MediaType.SuperCDROM2;
                                                    if(_plugin.XmlFSType.Type == "Nintendo Wii filesystem")
                                                        dskType = MediaType.WOD;
                                                    if(_plugin.XmlFSType.Type == "Nintendo Gamecube filesystem")
                                                        dskType = MediaType.GOD;
                                                }
                                            }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                                            catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                                            {
                                                //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                                            }
                                        }

                                        if(lstFs.Count > 0)
                                            xmlTrk.FileSystemInformation[i].FileSystems = lstFs.ToArray();
                                    }
                                }
                                else
                                {
                                    xmlTrk.FileSystemInformation[0] = new PartitionType();
                                    xmlTrk.FileSystemInformation[0].EndSector = (int)xmlTrk.EndSector;
                                    xmlTrk.FileSystemInformation[0].StartSector = (int)xmlTrk.StartSector;

                                    List<FileSystemType> lstFs = new List<FileSystemType>();

                                    foreach(Filesystem _plugin in plugins.PluginsList.Values)
                                    {
                                        try
                                        {
                                            if(_plugin.Identify(_imageFormat, (ulong)xmlTrk.StartSector, (ulong)xmlTrk.EndSector))
                                            {
                                                string foo;
                                                _plugin.GetInformation(_imageFormat, (ulong)xmlTrk.StartSector, (ulong)xmlTrk.EndSector, out foo);
                                                lstFs.Add(_plugin.XmlFSType);

                                                if(_plugin.XmlFSType.Type == "Opera")
                                                    dskType = MediaType.ThreeDO;
                                                if(_plugin.XmlFSType.Type == "PC Engine filesystem")
                                                    dskType = MediaType.SuperCDROM2;
                                                if(_plugin.XmlFSType.Type == "Nintendo Wii filesystem")
                                                    dskType = MediaType.WOD;
                                                if(_plugin.XmlFSType.Type == "Nintendo Gamecube filesystem")
                                                    dskType = MediaType.GOD;
                                            }
                                        }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                                        catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                                        {
                                            //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                                        }
                                    }

                                    if(lstFs.Count > 0)
                                        xmlTrk.FileSystemInformation[0].FileSystems = lstFs.ToArray();
                                }
#if DEBUG
                                stopwatch.Stop();
                                Console.WriteLine("Core.AddMedia(): Took {0} seconds to check all filesystems on track {1}", stopwatch.Elapsed.TotalSeconds, trk.TrackSequence);
#endif

                                trksLst.Add(xmlTrk);
                            }

                            if(UpdateProgress != null)
                                UpdateProgress(null, "Finishing", maxProgress, maxProgress);

                            if(trksLst != null)
                                sidecar.OpticalDisc[0].Track = trksLst.ToArray();

                            string dscType, dscSubType;
                            DiscImageChef.Metadata.MediaType.MediaTypeToString(dskType, out dscType, out dscSubType);
                            sidecar.OpticalDisc[0].DiscType = dscType;
                            sidecar.OpticalDisc[0].DiscSubType = dscSubType;

                            if(!string.IsNullOrEmpty(_imageFormat.ImageInfo.driveManufacturer) ||
                               !string.IsNullOrEmpty(_imageFormat.ImageInfo.driveModel) ||
                               !string.IsNullOrEmpty(_imageFormat.ImageInfo.driveFirmwareRevision) ||
                               !string.IsNullOrEmpty(_imageFormat.ImageInfo.driveSerialNumber))
                            {
                                sidecar.OpticalDisc[0].DumpHardwareArray = new DumpHardwareType[1];
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Extents = new ExtentType[0];
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Extents[0].Start = 0;
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Extents[0].End = (int)_imageFormat.ImageInfo.sectors;
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Manufacturer = _imageFormat.ImageInfo.driveManufacturer;
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Model = _imageFormat.ImageInfo.driveModel;
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Firmware = _imageFormat.ImageInfo.driveFirmwareRevision;
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Serial = _imageFormat.ImageInfo.driveSerialNumber;
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Software = new SoftwareType();
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Software.Name = _imageFormat.GetImageApplication();
                                sidecar.OpticalDisc[0].DumpHardwareArray[0].Software.Version = _imageFormat.GetImageApplicationVersion();
                            }

                            Context.workingDisc = sidecar.OpticalDisc[0];
                            if(Finished != null)
                                Finished();
                            return;
                        }
                    case XmlMediaType.BlockMedia:
                        {
                            maxProgress = 3 + _imageFormat.ImageInfo.readableMediaTags.Count;
                            if(UpdateProgress != null)
                                UpdateProgress(null, "Hashing image file", 3, maxProgress);

                            sidecar.BlockMedia = new BlockMediaType[1];
                            sidecar.BlockMedia[0] = new BlockMediaType();
                            sidecar.BlockMedia[0].Checksums = imgChecksums.ToArray();
                            sidecar.BlockMedia[0].Image = new ImageType();
                            sidecar.BlockMedia[0].Image.format = _imageFormat.GetImageFormat();
                            sidecar.BlockMedia[0].Image.offset = 0;
                            sidecar.BlockMedia[0].Image.offsetSpecified = true;
                            sidecar.BlockMedia[0].Image.Value = Path.GetFileName(selectedFile);
                            sidecar.BlockMedia[0].Size = fi.Length;
                            sidecar.BlockMedia[0].Sequence = new SequenceType();
                            if(_imageFormat.GetMediaSequence() != 0 && _imageFormat.GetLastDiskSequence() != 0)
                            {
                                sidecar.BlockMedia[0].Sequence.MediaSequence = _imageFormat.GetMediaSequence();
                                sidecar.BlockMedia[0].Sequence.TotalMedia = _imageFormat.GetMediaSequence();
                            }
                            else
                            {
                                sidecar.BlockMedia[0].Sequence.MediaSequence = 1;
                                sidecar.BlockMedia[0].Sequence.TotalMedia = 1;
                            }
                            sidecar.BlockMedia[0].Sequence.MediaTitle = _imageFormat.GetImageName();

                            currentProgress = 3;

#if DEBUG
                            stopwatch.Restart();
#endif
                            foreach(MediaTagType tagType in _imageFormat.ImageInfo.readableMediaTags)
                            {
                                currentProgress++;
                                if(UpdateProgress != null)
                                    UpdateProgress(null, string.Format("Hashing file containing {0}", tagType), currentProgress, maxProgress);

                                switch(tagType)
                                {
                                    case MediaTagType.ATAPI_IDENTIFY:
                                        sidecar.BlockMedia[0].ATA = new ATAType();
                                        sidecar.BlockMedia[0].ATA.Identify = new DumpType();
                                        sidecar.BlockMedia[0].ATA.Identify.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.ATAPI_IDENTIFY)).ToArray();
                                        sidecar.BlockMedia[0].ATA.Identify.Size = _imageFormat.ReadDiskTag(MediaTagType.ATAPI_IDENTIFY).Length;
                                        break;
                                    case MediaTagType.ATA_IDENTIFY:
                                        sidecar.BlockMedia[0].ATA = new ATAType();
                                        sidecar.BlockMedia[0].ATA.Identify = new DumpType();
                                        sidecar.BlockMedia[0].ATA.Identify.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.ATA_IDENTIFY)).ToArray();
                                        sidecar.BlockMedia[0].ATA.Identify.Size = _imageFormat.ReadDiskTag(MediaTagType.ATA_IDENTIFY).Length;
                                        break;
                                    case MediaTagType.PCMCIA_CIS:
                                        byte[] cis = _imageFormat.ReadDiskTag(MediaTagType.PCMCIA_CIS);
                                        sidecar.BlockMedia[0].PCMCIA = new PCMCIAType();
                                        sidecar.BlockMedia[0].PCMCIA.CIS = new DumpType();
                                        sidecar.BlockMedia[0].PCMCIA.CIS.Checksums = Checksum.GetChecksums(cis).ToArray();
                                        sidecar.BlockMedia[0].PCMCIA.CIS.Size = cis.Length;
                                        DiscImageChef.Decoders.PCMCIA.Tuple[] tuples = CIS.GetTuples(cis);
                                        if(tuples != null)
                                        {
                                            foreach(DiscImageChef.Decoders.PCMCIA.Tuple tuple in tuples)
                                            {
                                                if(tuple.Code == TupleCodes.CISTPL_MANFID)
                                                {
                                                    ManufacturerIdentificationTuple manfid = CIS.DecodeManufacturerIdentificationTuple(tuple);

                                                    if(manfid != null)
                                                    {
                                                        sidecar.BlockMedia[0].PCMCIA.ManufacturerCode = manfid.ManufacturerID;
                                                        sidecar.BlockMedia[0].PCMCIA.CardCode = manfid.CardID;
                                                        sidecar.BlockMedia[0].PCMCIA.ManufacturerCodeSpecified = true;
                                                        sidecar.BlockMedia[0].PCMCIA.CardCodeSpecified = true;
                                                    }
                                                }
                                                else if(tuple.Code == TupleCodes.CISTPL_VERS_1)
                                                {
                                                    Level1VersionTuple vers = CIS.DecodeLevel1VersionTuple(tuple);

                                                    if(vers != null)
                                                    {
                                                        sidecar.BlockMedia[0].PCMCIA.Manufacturer = vers.Manufacturer;
                                                        sidecar.BlockMedia[0].PCMCIA.ProductName = vers.Product;
                                                        sidecar.BlockMedia[0].PCMCIA.Compliance = string.Format("{0}.{1}", vers.MajorVersion, vers.MinorVersion);
                                                        sidecar.BlockMedia[0].PCMCIA.AdditionalInformation = vers.AdditionalInformation;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case MediaTagType.SCSI_INQUIRY:
                                        sidecar.BlockMedia[0].SCSI = new SCSIType();
                                        sidecar.BlockMedia[0].SCSI.Inquiry = new DumpType();
                                        sidecar.BlockMedia[0].SCSI.Inquiry.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.SCSI_INQUIRY)).ToArray();
                                        sidecar.BlockMedia[0].SCSI.Inquiry.Size = _imageFormat.ReadDiskTag(MediaTagType.SCSI_INQUIRY).Length;
                                        break;
                                    case MediaTagType.SD_CID:
                                        if(sidecar.BlockMedia[0].SecureDigital == null)
                                            sidecar.BlockMedia[0].SecureDigital = new SecureDigitalType();
                                        sidecar.BlockMedia[0].SecureDigital.CID = new DumpType();
                                        sidecar.BlockMedia[0].SecureDigital.CID.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.SD_CID)).ToArray();
                                        sidecar.BlockMedia[0].SecureDigital.CID.Size = _imageFormat.ReadDiskTag(MediaTagType.SD_CID).Length;
                                        break;
                                    case MediaTagType.SD_CSD:
                                        if(sidecar.BlockMedia[0].SecureDigital == null)
                                            sidecar.BlockMedia[0].SecureDigital = new SecureDigitalType();
                                        sidecar.BlockMedia[0].SecureDigital.CSD = new DumpType();
                                        sidecar.BlockMedia[0].SecureDigital.CSD.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.SD_CSD)).ToArray();
                                        sidecar.BlockMedia[0].SecureDigital.CSD.Size = _imageFormat.ReadDiskTag(MediaTagType.SD_CSD).Length;
                                        break;
                                    case MediaTagType.SD_ExtendedCSD:
                                        if(sidecar.BlockMedia[0].SecureDigital == null)
                                            sidecar.BlockMedia[0].SecureDigital = new SecureDigitalType();
                                        sidecar.BlockMedia[0].SecureDigital.ExtendedCSD = new DumpType();
                                        sidecar.BlockMedia[0].SecureDigital.ExtendedCSD.Checksums = Checksum.GetChecksums(_imageFormat.ReadDiskTag(MediaTagType.SD_ExtendedCSD)).ToArray();
                                        sidecar.BlockMedia[0].SecureDigital.ExtendedCSD.Size = _imageFormat.ReadDiskTag(MediaTagType.SD_ExtendedCSD).Length;
                                        break;
                                }
                            }
#if DEBUG
                            stopwatch.Stop();
                            Console.WriteLine("Core.AddMedia(): Took {0} seconds to hash media tags", stopwatch.Elapsed.TotalSeconds);
#endif

                            string dskType, dskSubType;
                            DiscImageChef.Metadata.MediaType.MediaTypeToString(_imageFormat.ImageInfo.mediaType, out dskType, out dskSubType);
                            sidecar.BlockMedia[0].DiskType = dskType;
                            sidecar.BlockMedia[0].DiskSubType = dskSubType;

                            sidecar.BlockMedia[0].Dimensions = DiscImageChef.Metadata.Dimensions.DimensionsFromMediaType(_imageFormat.ImageInfo.mediaType);

                            sidecar.BlockMedia[0].LogicalBlocks = (long)_imageFormat.GetSectors();
                            sidecar.BlockMedia[0].LogicalBlockSize = (int)_imageFormat.GetSectorSize();
                            // TODO: Detect it
                            sidecar.BlockMedia[0].PhysicalBlockSize = (int)_imageFormat.GetSectorSize();

                            if(UpdateProgress != null)
                                UpdateProgress(null, "Checking filesystems", maxProgress - 1, maxProgress);

#if DEBUG
                            stopwatch.Restart();
#endif
                            List<Partition> partitions = new List<Partition>();

                            foreach(PartPlugin _partplugin in plugins.PartPluginsList.Values)
                            {
                                List<Partition> _partitions;

                                if(_partplugin.GetInformation(_imageFormat, out _partitions))
                                {
                                    partitions = _partitions;
                                    break;
                                }
                            }

                            sidecar.BlockMedia[0].FileSystemInformation = new PartitionType[1];
                            if(partitions.Count > 0)
                            {
                                sidecar.BlockMedia[0].FileSystemInformation = new PartitionType[partitions.Count];
                                for(int i = 0; i < partitions.Count; i++)
                                {
                                    sidecar.BlockMedia[0].FileSystemInformation[i] = new PartitionType();
                                    sidecar.BlockMedia[0].FileSystemInformation[i].Description = partitions[i].PartitionDescription;
                                    sidecar.BlockMedia[0].FileSystemInformation[i].EndSector = (int)(partitions[i].PartitionStartSector + partitions[i].PartitionSectors - 1);
                                    sidecar.BlockMedia[0].FileSystemInformation[i].Name = partitions[i].PartitionName;
                                    sidecar.BlockMedia[0].FileSystemInformation[i].Sequence = (int)partitions[i].PartitionSequence;
                                    sidecar.BlockMedia[0].FileSystemInformation[i].StartSector = (int)partitions[i].PartitionStartSector;
                                    sidecar.BlockMedia[0].FileSystemInformation[i].Type = partitions[i].PartitionType;

                                    List<FileSystemType> lstFs = new List<FileSystemType>();

                                    foreach(Filesystem _plugin in plugins.PluginsList.Values)
                                    {
                                        try
                                        {
                                            if(_plugin.Identify(_imageFormat, partitions[i].PartitionStartSector, partitions[i].PartitionStartSector + partitions[i].PartitionSectors - 1))
                                            {
                                                string foo;
                                                _plugin.GetInformation(_imageFormat, partitions[i].PartitionStartSector, partitions[i].PartitionStartSector + partitions[i].PartitionSectors - 1, out foo);
                                                lstFs.Add(_plugin.XmlFSType);
                                            }
                                        }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                                        catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                                        {
                                            //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                                        }
                                    }

                                    if(lstFs.Count > 0)
                                        sidecar.BlockMedia[0].FileSystemInformation[i].FileSystems = lstFs.ToArray();
                                }
                            }
                            else
                            {
                                sidecar.BlockMedia[0].FileSystemInformation[0] = new PartitionType();
                                sidecar.BlockMedia[0].FileSystemInformation[0].StartSector = 0;
                                sidecar.BlockMedia[0].FileSystemInformation[0].EndSector = (int)(_imageFormat.GetSectors() - 1);

                                List<FileSystemType> lstFs = new List<FileSystemType>();

                                foreach(Filesystem _plugin in plugins.PluginsList.Values)
                                {
                                    try
                                    {
                                        if(_plugin.Identify(_imageFormat, 0, _imageFormat.GetSectors() - 1))
                                        {
                                            string foo;
                                            _plugin.GetInformation(_imageFormat, 0, _imageFormat.GetSectors() - 1, out foo);
                                            lstFs.Add(_plugin.XmlFSType);
                                        }
                                    }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                                    catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                                    {
                                        //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                                    }
                                }

                                if(lstFs.Count > 0)
                                    sidecar.BlockMedia[0].FileSystemInformation[0].FileSystems = lstFs.ToArray();
                            }
#if DEBUG
                            stopwatch.Stop();
                            Console.WriteLine("Core.AddMedia(): Took {0} seconds to check all filesystems", stopwatch.Elapsed.TotalSeconds);
#endif

                            // TODO: Implement support for getting CHS
                            if(UpdateProgress != null)
                                UpdateProgress(null, "Finishing", maxProgress, maxProgress);
                            Context.workingDisk = sidecar.BlockMedia[0];
                            if(Finished != null)
                                Finished();
                            return;
                        }
                    case XmlMediaType.LinearMedia:
                        {
                            if(Failed != null)
                                Failed("Linear media not yet supported.");
                            return;
                        }
                    case XmlMediaType.AudioMedia:
                        {
                            if(Failed != null)
                                Failed("Audio media not yet supported.");
                            return;
                        }

                }

                if(Failed != null)
                    Failed("Should've not arrived here.");
                return;
            }
            catch(Exception ex)
            {
                if(Failed != null)
                    Failed(string.Format("Error reading file: {0}\n{1}", ex.Message, ex.StackTrace));
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
                m = (lba + 150) / (75 * 60);
                lba -= m * (75 * 60);
                s = (lba + 150) / 75;
                lba -= s * 75;
                f = lba + 150;
            }
            else
            {
                m = (lba + 450150) / (75 * 60);
                lba -= m * (75 * 60);
                s = (lba + 450150) / 75;
                lba -= s * 75;
                f = lba + 450150;
            }

            return string.Format("{0}:{1:D2}:{2:D2}", m, s, f);
        }

        static string DdcdLbaToMsf(long lba)
        {
            long h, m, s, f;
            if(lba >= -150)
            {
                h = (lba + 150) / (75 * 60 * 60);
                lba -= h * (75 * 60 * 60);
                m = (lba + 150) / (75 * 60);
                lba -= m * (75 * 60);
                s = (lba + 150) / 75;
                lba -= s * 75;
                f = lba + 150;
            }
            else
            {
                h = (lba + 450150 * 2) / (75 * 60 * 60);
                lba -= h * (75 * 60 * 60);
                m = (lba + 450150 * 2) / (75 * 60);
                lba -= m * (75 * 60);
                s = (lba + 450150 * 2) / 75;
                lba -= s * 75;
                f = lba + 450150 * 2;
            }

            return string.Format("{3}:{0:D2}:{1:D2}:{2:D2}", m, s, f, h);
        }
    }
}
