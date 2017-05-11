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
using Gtk;
using Schemas;

namespace osrepodbmgr
{
    public partial class dlgBlockMedia : Dialog
    {
        public BlockMediaType Metadata;

        TreeIter partitionIter;
        TreeIter filesystemIter;
        TreeIter dumpHwIter;

        ListStore lstAta;
        ListStore lstPCIConfiguration;
        ListStore lstPCIOptionROM;
        ListStore lstPCMCIACIS;
        ListStore lstCID;
        ListStore lstCSD;
        ListStore lstECSD;
        ListStore lstInquiry;
        ListStore lstModeSense;
        ListStore lstModeSense10;
        ListStore lstLogSense;
        ListStore lstEVPDs;
        ListStore lstUSBDescriptors;
        ListStore lstMAM;
        ListStore lstTracks;
        ListStore lstPartitions;
        ListStore lstDumpHw;
        ListStore lstAdditionalInformation;

        bool editingPartition;
        bool editingDumpHw;

        // Non-editable fields
        ChecksumType[] checksums;
        BlockSizeType[] variableBlockSize;
        TapePartitionType[] tapeInformation;
        ScansType scans;

        public dlgBlockMedia()
        {
            Build();

            #region Set partitions table
            lstPartitions = new ListStore(typeof(int), typeof(int), typeof(int), typeof(string), typeof(string), typeof(string), typeof(ListStore));

            CellRendererText partSequenceCell = new CellRendererText();
            CellRendererText partStartCell = new CellRendererText();
            CellRendererText partEndCell = new CellRendererText();
            CellRendererText partTypeCell = new CellRendererText();
            CellRendererText partNameCell = new CellRendererText();
            CellRendererText partDescriptionCell = new CellRendererText();

            TreeViewColumn partSequenceColumn = new TreeViewColumn("Sequence", partSequenceCell, "text", 0);
            TreeViewColumn partStartColumn = new TreeViewColumn("Start", partStartCell, "text", 1);
            TreeViewColumn partEndColumn = new TreeViewColumn("End", partEndCell, "text", 2);
            TreeViewColumn partTypeColumn = new TreeViewColumn("Type", partTypeCell, "text", 3);
            TreeViewColumn partNameColumn = new TreeViewColumn("Name", partNameCell, "text", 4);
            TreeViewColumn partDescriptionColumn = new TreeViewColumn("Description", partDescriptionCell, "text", 5);

            treePartitions.Model = lstPartitions;

            treePartitions.AppendColumn(partSequenceColumn);
            treePartitions.AppendColumn(partStartColumn);
            treePartitions.AppendColumn(partEndColumn);
            treePartitions.AppendColumn(partTypeColumn);
            treePartitions.AppendColumn(partNameColumn);
            treePartitions.AppendColumn(partDescriptionColumn);

            treePartitions.Selection.Mode = SelectionMode.Single;
            #endregion Set partitions table

            #region Set filesystems table
            CellRendererText fsTypeCell = new CellRendererText();
            CellRendererText fsNameCell = new CellRendererText();
            TreeViewColumn fsTypeColumn = new TreeViewColumn("Type", fsTypeCell, "text", 0);
            TreeViewColumn fsNameColumn = new TreeViewColumn("Name", fsNameCell, "text", 1);
            treeFilesystems.AppendColumn(fsTypeColumn);
            treeFilesystems.AppendColumn(fsNameColumn);
            treeFilesystems.Selection.Mode = SelectionMode.Single;
            #endregion Set filesystems table

            #region Set dump hardware table
            lstDumpHw = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(ListStore));

            CellRendererText hwManufacturerCell = new CellRendererText();
            CellRendererText hwModelCell = new CellRendererText();
            CellRendererText hwRevisionCell = new CellRendererText();
            CellRendererText hwFirmwareCell = new CellRendererText();
            CellRendererText hwSerialCell = new CellRendererText();
            CellRendererText swNameCell = new CellRendererText();
            CellRendererText swVersionCell = new CellRendererText();
            CellRendererText swOSCell = new CellRendererText();

            TreeViewColumn hwManufacturerColumn = new TreeViewColumn("Manufacturer", hwManufacturerCell, "text", 0);
            TreeViewColumn hwModelColumn = new TreeViewColumn("Model", hwModelCell, "text", 1);
            TreeViewColumn hwRevisionColumn = new TreeViewColumn("Revision", hwRevisionCell, "text", 2);
            TreeViewColumn hwFirmwareColumn = new TreeViewColumn("Firmware", hwFirmwareCell, "text", 3);
            TreeViewColumn hwSerialColumn = new TreeViewColumn("Serial", hwSerialCell, "text", 4);
            TreeViewColumn swNameColumn = new TreeViewColumn("Software", swNameCell, "text", 5);
            TreeViewColumn swVersionColumn = new TreeViewColumn("Version", swVersionCell, "text", 6);
            TreeViewColumn swOSColumn = new TreeViewColumn("Operating system", swOSCell, "text", 7);

            treeDumpHardware.Model = lstDumpHw;

            treeDumpHardware.AppendColumn(hwManufacturerColumn);
            treeDumpHardware.AppendColumn(hwModelColumn);
            treeDumpHardware.AppendColumn(hwRevisionColumn);
            treeDumpHardware.AppendColumn(hwFirmwareColumn);
            treeDumpHardware.AppendColumn(hwSerialColumn);
            treeDumpHardware.AppendColumn(swNameColumn);
            treeDumpHardware.AppendColumn(swVersionColumn);
            treeDumpHardware.AppendColumn(swOSColumn);

            treeDumpHardware.Selection.Mode = SelectionMode.Single;

            CellRendererText extentStartCell = new CellRendererText();
            CellRendererText extentEndCell = new CellRendererText();
            TreeViewColumn extentStartColumn = new TreeViewColumn("Start", extentStartCell, "text", 0);
            TreeViewColumn extentEndColumn = new TreeViewColumn("End", extentEndCell, "text", 1);
            treeExtents.AppendColumn(extentStartColumn);
            treeExtents.AppendColumn(extentEndColumn);
            treeExtents.Selection.Mode = SelectionMode.Single;
            #endregion Set dump hardware table

            CellRendererText fileCell = new CellRendererText();
            CellRendererText sizeCell = new CellRendererText();
            TreeViewColumn fileColumn = new TreeViewColumn("File", fileCell, "text", 0);
            TreeViewColumn sizeColumn = new TreeViewColumn("Size", sizeCell, "text", 1);

            #region Set ATA IDENTIFY table
            lstAta = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeATA.Model = lstAta;
            treeATA.AppendColumn(fileColumn);
            treeATA.AppendColumn(sizeColumn);
            #endregion Set ATA IDENTIFY table

            #region Set PCI configuration table
            lstPCIConfiguration = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeATA.Model = lstPCIConfiguration;
            treeATA.AppendColumn(fileColumn);
            treeATA.AppendColumn(sizeColumn);
            #endregion Set PCI configuration table

            #region Set PCMCIA CIS table
            lstPCMCIACIS = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeCIS.Model = lstPCMCIACIS;
            treeCIS.AppendColumn(fileColumn);
            treeCIS.AppendColumn(sizeColumn);
            #endregion Set PCI option ROM table

            #region Set CID table
            lstCID = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeCID.Model = lstCID;
            treeCID.AppendColumn(fileColumn);
            treeCID.AppendColumn(sizeColumn);
            #endregion Set CID table

            #region Set CSD table
            lstCSD = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeCSD.Model = lstCSD;
            treeCSD.AppendColumn(fileColumn);
            treeCSD.AppendColumn(sizeColumn);
            #endregion Set CSD table

            #region Set Extended CSD table
            lstECSD = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeECSD.Model = lstECSD;
            treeECSD.AppendColumn(fileColumn);
            treeECSD.AppendColumn(sizeColumn);
            #endregion Set Extended CSD table

            #region Set SCSI INQUIRY table
            lstInquiry = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeInquiry.Model = lstInquiry;
            treeInquiry.AppendColumn(fileColumn);
            treeInquiry.AppendColumn(sizeColumn);
            #endregion Set SCSI INQUIRY table

            #region Set SCSI MODE SENSE table
            lstModeSense = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeModeSense.Model = lstModeSense;
            treeModeSense.AppendColumn(fileColumn);
            treeModeSense.AppendColumn(sizeColumn);
            #endregion Set SCSI MODE SENSE table

            #region Set SCSI MODE SENSE (10) table
            lstModeSense10 = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeModeSense10.Model = lstModeSense10;
            treeModeSense10.AppendColumn(fileColumn);
            treeModeSense10.AppendColumn(sizeColumn);
            #endregion Set SCSI MODE SENSE (10) table

            #region Set SCSI LOG SENSE table
            lstLogSense = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeLogSense.Model = lstLogSense;
            treeLogSense.AppendColumn(fileColumn);
            treeLogSense.AppendColumn(sizeColumn);
            #endregion Set SCSI MODE SENSE (10) table

            #region Set SCSI EVPDs table
            lstEVPDs = new ListStore(typeof(int), typeof(string), typeof(int), typeof(int), typeof(ChecksumType[]));

            CellRendererText evpdPageCell = new CellRendererText();
            CellRendererText evpdFileCell = new CellRendererText();
            CellRendererText evpdSizeCell = new CellRendererText();

            TreeViewColumn evpdPageColumn = new TreeViewColumn("Page", evpdPageCell, "text", 0);
            TreeViewColumn evpdFileColumn = new TreeViewColumn("File", evpdFileCell, "text", 1);
            TreeViewColumn evpdSizecolumn = new TreeViewColumn("Size", evpdSizeCell, "text", 2);

            treeEVPDs.Model = lstEVPDs;

            treeEVPDs.AppendColumn(evpdPageColumn);
            treeEVPDs.AppendColumn(evpdFileColumn);
            treeEVPDs.AppendColumn(evpdSizecolumn);

            treeEVPDs.Selection.Mode = SelectionMode.Single;
            #endregion Set SCSI EVPDs table

            #region Set USB descriptors table
            lstUSBDescriptors = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeDescriptors.Model = lstUSBDescriptors;
            treeDescriptors.AppendColumn(fileColumn);
            treeDescriptors.AppendColumn(sizeColumn);
            #endregion Set USB descriptors table

            #region Set MAM table
            lstMAM = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeMAM.Model = lstMAM;
            treeMAM.AppendColumn(fileColumn);
            treeMAM.AppendColumn(sizeColumn);
            #endregion Set MAM table

            #region Set Option ROM table
            lstPCIOptionROM = new ListStore(typeof(string), typeof(int), typeof(int), typeof(LinearMediaType));

            CellRendererText romFileCell = new CellRendererText();
            CellRendererText romOffsetCell = new CellRendererText();
            CellRendererText romSizeCell = new CellRendererText();

            TreeViewColumn romFileColumn = new TreeViewColumn("File", romFileCell, "text", 0);
            TreeViewColumn romOffsetColumn = new TreeViewColumn("Offset", romOffsetCell, "text", 1);
            TreeViewColumn romSizeColumn = new TreeViewColumn("Size", romSizeCell, "text", 2);

            treeOptionROM.Model = lstPCIOptionROM;

            treeOptionROM.AppendColumn(romFileColumn);
            treeOptionROM.AppendColumn(romOffsetColumn);
            treeOptionROM.AppendColumn(romSizeColumn);

            treeOptionROM.Selection.Mode = SelectionMode.Single;
            #endregion Set Option ROM table

            #region Set tracks table
            lstTracks = new ListStore(typeof(string), typeof(int), typeof(int), typeof(string), typeof(int), typeof(int), typeof(int), typeof(int),
                                      typeof(int), typeof(int), typeof(string), typeof(ChecksumType[]));

            CellRendererText trkFileCell = new CellRendererText();
            CellRendererText trkOffsetCell = new CellRendererText();
            CellRendererText trkSizeCell = new CellRendererText();
            CellRendererText trkImgFormatCell = new CellRendererText();
            CellRendererText trkHeadCell = new CellRendererText();
            CellRendererText trkCylCell = new CellRendererText();
            CellRendererText trkStartCell = new CellRendererText();
            CellRendererText trkEndCell = new CellRendererText();
            CellRendererText trkSectorsCell = new CellRendererText();
            CellRendererText trkBpsCell = new CellRendererText();
            CellRendererText trkFormatCell = new CellRendererText();

            TreeViewColumn trkFileColumn = new TreeViewColumn("File", trkFileCell, "text", 0);
            TreeViewColumn trkOffsetColumn = new TreeViewColumn("Offset", trkOffsetCell, "text", 1);
            TreeViewColumn trkSizeColumn = new TreeViewColumn("Size", trkSizeCell, "text", 2);
            TreeViewColumn trkImgFormatColumn = new TreeViewColumn("Image format", trkImgFormatCell, "text", 3);
            TreeViewColumn trkHeadColumn = new TreeViewColumn("Head", trkHeadCell, "text", 4);
            TreeViewColumn trkCylColumn = new TreeViewColumn("Cylinder", trkCylCell, "text", 5);
            TreeViewColumn trkStartColumn = new TreeViewColumn("Start", trkStartCell, "text", 6);
            TreeViewColumn trkEndColumn = new TreeViewColumn("End", trkEndCell, "text", 7);
            TreeViewColumn trkSectorsColumn = new TreeViewColumn("Sectors", trkSectorsCell, "text", 8);
            TreeViewColumn trkBpsColumn = new TreeViewColumn("Bytes per sector", trkBpsCell, "text", 9);
            TreeViewColumn trkFormatColumn = new TreeViewColumn("Track format", trkFormatCell, "text", 10);

            treeTracks.Model = lstTracks;

            treeTracks.AppendColumn(trkFileColumn);
            treeTracks.AppendColumn(trkOffsetColumn);
            treeTracks.AppendColumn(trkSizeColumn);
            treeTracks.AppendColumn(trkImgFormatColumn);
            treeTracks.AppendColumn(trkHeadColumn);
            treeTracks.AppendColumn(trkCylColumn);
            treeTracks.AppendColumn(trkStartColumn);
            treeTracks.AppendColumn(trkEndColumn);
            treeTracks.AppendColumn(trkSectorsColumn);
            treeTracks.AppendColumn(trkBpsColumn);
            treeTracks.AppendColumn(trkFormatColumn);

            treeTracks.Selection.Mode = SelectionMode.Single;
            #endregion Set tracks table

            lstAdditionalInformation = new ListStore(typeof(string));
            CellRendererText addInfoCell = new CellRendererText();
            TreeViewColumn addInfoColumn = new TreeViewColumn("Information", addInfoCell, "text", 0);
            treeAdditionalInformation.Model = lstAdditionalInformation;
            treeAdditionalInformation.AppendColumn(addInfoColumn);
            treeAdditionalInformation.Selection.Mode = SelectionMode.Single;
        }

        public void FillFields()
        {
            if(Metadata == null)
                return;

            txtImage.Text = Metadata.Image.Value;
            txtFormat.Text = Metadata.Image.format;
            if(Metadata.Image.offsetSpecified)
                txtOffset.Text = Metadata.Image.offset.ToString();
            txtSize.Text = Metadata.Size.ToString();
            checksums = Metadata.Checksums;
            if(Metadata.Sequence != null)
            {
                lblMediaTitle.Visible = true;
                txtMediaTitle.Visible = true;
                lblSequence.Visible = true;
                spSequence.Visible = true;
                lblTotalMedia.Visible = true;
                spTotalMedia.Visible = true;
                lblSide.Visible = true;
                spSide.Visible = true;
                lblLayer.Visible = true;
                spLayer.Visible = true;
                chkSequence.Active = true;
                txtMediaTitle.Text = Metadata.Sequence.MediaTitle;
                spSequence.Value = Metadata.Sequence.MediaSequence;
                spTotalMedia.Value = Metadata.Sequence.TotalMedia;
                if(Metadata.Sequence.SideSpecified)
                    spSide.Value = Metadata.Sequence.Side;
                if(Metadata.Sequence.LayerSpecified)
                    spLayer.Value = Metadata.Sequence.Layer;
            }
            if(Metadata.Manufacturer != null)
                txtManufacturer.Text = Metadata.Manufacturer;
            if(Metadata.Model != null)
                txtModel.Text = Metadata.Model;
            if(Metadata.Serial != null)
                txtSerial.Text = Metadata.Serial;
            if(Metadata.Firmware != null)
                txtFirmware.Text = Metadata.Firmware;
            if(Metadata.Interface != null)
                txtInterface.Text = Metadata.Interface;
            spPhysicalBlockSize.Value = Metadata.PhysicalBlockSize;
            spLogicalBlockSize.Value = Metadata.LogicalBlockSize;
            txtBlocks.Text = Metadata.LogicalBlocks.ToString();
            variableBlockSize = Metadata.VariableBlockSize;
            tapeInformation = Metadata.TapeInformation;
            scans = Metadata.Scans;
            if(Metadata.ATA != null && Metadata.ATA.Identify != null)
            {
                chkATA.Active = true;
                treeATA.Visible = true;
                lstAta.AppendValues(Metadata.ATA.Identify.Image, Metadata.ATA.Identify.Size, Metadata.ATA.Identify.Checksums);
            }
            if(Metadata.PCI != null)
            {
                chkPCI.Active = true;
                lblPCIVendor.Visible = true;
                txtPCIVendor.Visible = true;
                lblPCIProduct.Visible = true;
                txtPCIProduct.Visible = true;
                txtPCIVendor.Text = string.Format("0x{0:X4}", Metadata.PCI.VendorID);
                txtPCIProduct.Text = string.Format("0x{0:X4}", Metadata.PCI.DeviceID);
                if(Metadata.PCI.Configuration != null)
                {
                    frmPCIConfiguration.Visible = true;
                    lstPCIConfiguration.AppendValues(Metadata.PCI.Configuration.Image, Metadata.PCI.Configuration.Size, Metadata.PCI.Configuration.Checksums);
                }
                if(Metadata.PCI.ExpansionROM != null)
                {
                    frmOptionROM.Visible = true;
                    lstPCIOptionROM.AppendValues(Metadata.PCI.ExpansionROM.Image.Value, Metadata.PCI.ExpansionROM.Image.offset, Metadata.PCI.ExpansionROM.Size, Metadata.PCI.ExpansionROM);
                }
            }
            if(Metadata.PCMCIA != null)
            {
                chkPCMCIA.Active = true;
                chkCIS.Visible = true;
                lblPCMCIAManufacturer.Visible = true;
                txtPCMCIAManufacturer.Visible = true;
                lblManufacturerCode.Visible = true;
                txtMfgCode.Visible = true;
                lblProductName.Visible = true;
                txtPCMCIAProductName.Visible = true;
                lblCardCode.Visible = true;
                txtCardCode.Visible = true;
                lblCompliance.Visible = true;
                txtCompliance.Visible = true;

                if(Metadata.PCMCIA.CIS != null)
                {
                    treeCIS.Visible = true;
                    lstPCMCIACIS.AppendValues(Metadata.PCMCIA.CIS.Image, Metadata.PCMCIA.CIS.Size, Metadata.PCMCIA.CIS.Checksums);
                }

                if(Metadata.PCMCIA.Compliance != null)
                    txtCompliance.Text = Metadata.PCMCIA.Compliance;
                if(Metadata.PCMCIA.ManufacturerCodeSpecified)
                    txtMfgCode.Text = string.Format("0x{0:X4}", Metadata.PCMCIA.ManufacturerCode);
                if(Metadata.PCMCIA.CardCodeSpecified)
                    txtCardCode.Text = string.Format("0x{0:X4}", Metadata.PCMCIA.CardCode);
                if(Metadata.PCMCIA.Manufacturer != null)
                    txtPCMCIAManufacturer.Text = Metadata.PCMCIA.Manufacturer;
                if(Metadata.PCMCIA.ProductName != null)
                    txtPCMCIAProductName.Text = Metadata.PCMCIA.ProductName;
                if(Metadata.PCMCIA.AdditionalInformation != null)
                {
                    lblAdditionalInformation.Visible = true;
                    treeAdditionalInformation.Visible = true;

                    foreach(string addinfo in Metadata.PCMCIA.AdditionalInformation)
                        lstAdditionalInformation.AppendValues(addinfo);
                }
            }
            if(Metadata.SecureDigital != null && Metadata.SecureDigital.CID != null)
            {
                chkSecureDigital.Active = true;
                chkCSD.Visible = true;
                chkECSD.Visible = true;
                lblCID.Visible = true;
                treeCID.Visible = true;
                lstCID.AppendValues(Metadata.SecureDigital.CID.Image, Metadata.SecureDigital.CID.Size, Metadata.SecureDigital.CID.Checksums);

                if(Metadata.SecureDigital.CSD != null)
                {
                    chkCSD.Active = true;
                    treeCSD.Visible = true;
                    lstCSD.AppendValues(Metadata.SecureDigital.CSD.Image, Metadata.SecureDigital.CSD.Size, Metadata.SecureDigital.CSD.Checksums);
                }

                if(Metadata.SecureDigital.ExtendedCSD != null)
                {
                    chkECSD.Active = true;
                    treeECSD.Visible = true;
                    lstECSD.AppendValues(Metadata.SecureDigital.ExtendedCSD.Image, Metadata.SecureDigital.ExtendedCSD.Size, Metadata.SecureDigital.ExtendedCSD.Checksums);
                }
            }
            if(Metadata.SCSI != null && Metadata.SCSI.Inquiry != null)
            {
                chkSCSI.Active = true;
                frmInquiry.Visible = true;
                lstInquiry.AppendValues(Metadata.SCSI.Inquiry.Image, Metadata.SCSI.Inquiry.Size, Metadata.SCSI.Inquiry.Checksums);

                if(Metadata.SCSI.ModeSense != null)
                {
                    frmModeSense.Visible = true;
                    lstModeSense.AppendValues(Metadata.SCSI.ModeSense.Image, Metadata.SCSI.ModeSense.Size, Metadata.SCSI.ModeSense.Checksums);
                }
                if(Metadata.SCSI.ModeSense10 != null)
                {
                    frmModeSense10.Visible = true;
                    lstModeSense10.AppendValues(Metadata.SCSI.ModeSense10.Image, Metadata.SCSI.ModeSense10.Size, Metadata.SCSI.ModeSense10.Checksums);
                }
                if(Metadata.SCSI.LogSense != null)
                {
                    frmLogSense.Visible = true;
                    lstLogSense.AppendValues(Metadata.SCSI.LogSense.Image, Metadata.SCSI.LogSense.Size, Metadata.SCSI.LogSense.Checksums);
                }
                if(Metadata.SCSI.EVPD != null)
                {
                    frmEVPDs.Visible = true;
                    foreach(EVPDType evpd in Metadata.SCSI.EVPD)
                        lstEVPDs.AppendValues(evpd.page, evpd.Image, evpd.Size, evpd.Checksums);
                }
            }
            if(Metadata.USB != null)
            {
                chkUSB.Active = true;
                lblUSBVendor.Visible = true;
                txtUSBVendor.Visible = true;
                lblUSBProduct.Visible = true;
                txtUSBProduct.Visible = true;
                txtUSBVendor.Text = string.Format("0x{0:X4}", Metadata.USB.VendorID);
                txtUSBProduct.Text = string.Format("0x{0:X4}", Metadata.USB.ProductID);
                if(Metadata.USB.Descriptors != null)
                {
                    frmDescriptors.Visible = true;
                    lstUSBDescriptors.AppendValues(Metadata.USB.Descriptors.Image, Metadata.USB.Descriptors.Size, Metadata.USB.Descriptors.Checksums);
                }
            }
            if(Metadata.MAM != null)
            {
                chkMAM.Active = true;
                treeMAM.Visible = true;
                lstMAM.AppendValues(Metadata.MAM.Image, Metadata.MAM.Size, Metadata.MAM.Checksums);
            }
            if(Metadata.HeadsSpecified)
                spHeads.Value = Metadata.Heads;
            if(Metadata.CylindersSpecified)
                spCylinders.Value = Metadata.Cylinders;
            if(Metadata.SectorsPerTrackSpecified)
                spSectors.Value = Metadata.SectorsPerTrack;
            if(Metadata.Track != null)
            {
                chkTracks.Active = true;
                treeTracks.Visible = true;
                foreach(BlockTrackType track in Metadata.Track)
                    lstTracks.AppendValues(track.Image.Value, track.Image.offset, track.Size, track.Image.format, track.Head, track.Cylinder,
                                           track.StartSector, track.EndSector, track.Sectors, track.BytesPerSector, track.Format, track.Checksums);
            }
            if(Metadata.CopyProtection != null)
                txtCopyProtection.Text = Metadata.CopyProtection;
            if(Metadata.Dimensions != null)
            {
                chkDimensions.Active = true;
                if(Metadata.Dimensions.DiameterSpecified)
                {
                    chkRound.Active = true;
                    lblDiameter.Visible = true;
                    spDiameter.Visible = true;
                    lblDiametersUnits.Visible = true;
                    spDiameter.Value = Metadata.Dimensions.Diameter;
                }
                else
                {
                    lblHeight.Visible = true;
                    spHeight.Visible = true;
                    lblHeightUnits.Visible = true;
                    spHeight.Value = Metadata.Dimensions.Height;
                    lblWidth.Visible = true;
                    spWidth.Visible = true;
                    lblWidthUnits.Visible = true;
                    spWidth.Value = Metadata.Dimensions.Width;
                }
                lblThickness.Visible = true;
                spThickness.Visible = true;
                lblThicknessUnits.Visible = true;
                spThickness.Value = Metadata.Dimensions.Thickness;
            }
            if(Metadata.FileSystemInformation != null)
            {
                foreach(PartitionType partition in Metadata.FileSystemInformation)
                {
                    ListStore lstFilesystems = new ListStore(typeof(string), typeof(string), typeof(FileSystemType));
                    if(partition.FileSystems != null)
                    {
                        foreach(FileSystemType fs in partition.FileSystems)
                            lstFilesystems.AppendValues(fs.Type, fs.VolumeName, fs);
                    }
                    lstPartitions.AppendValues(partition.Sequence, partition.StartSector, partition.EndSector, partition.Type, partition.Name, partition.Description, lstFilesystems);
                }
            }
            if(Metadata.DumpHardwareArray != null)
            {
                chkDumpHardware.Active = true;
                treeDumpHardware.Visible = true;
                btnAddHardware.Visible = true;
                btnRemoveHardware.Visible = true;

                foreach(DumpHardwareType hw in Metadata.DumpHardwareArray)
                {
                    if(hw.Extents != null)
                    {
                        ListStore lstExtents = new ListStore(typeof(int), typeof(int));
                        foreach(ExtentType extent in hw.Extents)
                            lstExtents.AppendValues(extent.Start, extent.End);
                        if(hw.Software != null)
                            lstDumpHw.AppendValues(hw.Manufacturer, hw.Model, hw.Revision, hw.Firmware, hw.Serial, hw.Software.Name, hw.Software.Version, hw.Software.OperatingSystem, lstExtents);
                        else
                            lstDumpHw.AppendValues(hw.Manufacturer, hw.Model, hw.Revision, hw.Firmware, hw.Serial, null, null, null, lstExtents);
                    }
                }
            }
            if(Metadata.DiskType != null)
                txtMediaType.Text = Metadata.DiskType;
            if(Metadata.DiskSubType != null)
                txtMediaSubtype.Text = Metadata.DiskSubType;
        }

        protected void OnChkSequenceToggled(object sender, EventArgs e)
        {
            lblMediaTitle.Visible = chkSequence.Active;
            txtMediaTitle.Visible = chkSequence.Active;
            lblSequence.Visible = chkSequence.Active;
            spSequence.Visible = chkSequence.Active;
            lblTotalMedia.Visible = chkSequence.Active;
            spTotalMedia.Visible = chkSequence.Active;
            lblSide.Visible = chkSequence.Active;
            spSide.Visible = chkSequence.Active;
            lblLayer.Visible = chkSequence.Active;
            spLayer.Visible = chkSequence.Active;
        }

        protected void OnChkDimensionsToggled(object sender, EventArgs e)
        {
            chkRound.Visible = chkDimensions.Active;
            lblThickness.Visible = chkDimensions.Active;
            spThickness.Visible = chkDimensions.Active;
            lblThicknessUnits.Visible = chkDimensions.Active;
            if(chkDimensions.Active)
                OnChkRoundToggled(sender, e);
            else
            {
                lblDiameter.Visible = false;
                spDiameter.Visible = false;
                lblDiametersUnits.Visible = false;
                lblHeight.Visible = false;
                spHeight.Visible = false;
                lblHeightUnits.Visible = false;
                lblWidth.Visible = false;
                spWidth.Visible = false;
                lblWidthUnits.Visible = false;
            }
        }

        protected void OnChkRoundToggled(object sender, EventArgs e)
        {
            lblDiameter.Visible = chkRound.Active;
            spDiameter.Visible = chkRound.Active;
            lblDiametersUnits.Visible = chkRound.Active;
            lblHeight.Visible = !chkRound.Active;
            spHeight.Visible = !chkRound.Active;
            lblHeightUnits.Visible = !chkRound.Active;
            lblWidth.Visible = !chkRound.Active;
            spWidth.Visible = !chkRound.Active;
            lblWidthUnits.Visible = !chkRound.Active;
        }

        protected void OnChkPCMCIAToggled(object sender, EventArgs e)
        {
            chkCIS.Visible = chkPCMCIA.Active;
            treeCIS.Visible = chkPCMCIA.Active;
            lblPCMCIAManufacturer.Visible = chkPCMCIA.Active;
            txtPCMCIAManufacturer.Visible = chkPCMCIA.Active;
            lblManufacturerCode.Visible = chkPCMCIA.Active;
            txtMfgCode.Visible = chkPCMCIA.Active;
            lblProductName.Visible = chkPCMCIA.Active;
            txtPCMCIAProductName.Visible = chkPCMCIA.Active;
            lblCardCode.Visible = chkPCMCIA.Active;
            txtCardCode.Visible = chkPCMCIA.Active;
            lblCompliance.Visible = chkPCMCIA.Active;
            txtCompliance.Visible = chkPCMCIA.Active;
            lblAdditionalInformation.Visible = false;
            treeAdditionalInformation.Visible = false;
        }

        protected void OnBtnCancelPartitionClicked(object sender, EventArgs e)
        {
            btnCancelPartition.Visible = false;
            btnApplyPartition.Visible = false;
            btnRemovePartition.Visible = true;
            btnEditPartition.Visible = true;
            btnAddPartition.Visible = true;
            lblPartitionSequence.Visible = false;
            spPartitionSequence.Visible = false;
            lblPartitionStart.Visible = false;
            txtPartitionStart.Visible = false;
            lblPartitionEnd.Visible = false;
            txtPartitionEnd.Visible = false;
            lblPartitionType.Visible = false;
            txtPartitionType.Visible = false;
            lblPartitionName.Visible = false;
            txtPartitionName.Visible = false;
            lblPartitionDescription.Visible = false;
            txtPartitionDescription.Visible = false;
            frmFilesystems.Visible = false;
        }

        protected void OnBtnRemovePartitionClicked(object sender, EventArgs e)
        {
            if(treePartitions.Selection.GetSelected(out partitionIter))
                lstPartitions.Remove(ref partitionIter);
        }

        protected void OnBtnEditPartitionClicked(object sender, EventArgs e)
        {
            if(!treePartitions.Selection.GetSelected(out partitionIter))
                return;

            spPartitionSequence.Value = (int)lstPartitions.GetValue(partitionIter, 0);
            txtPartitionStart.Text = ((int)lstPartitions.GetValue(partitionIter, 1)).ToString();
            txtPartitionEnd.Text = ((int)lstPartitions.GetValue(partitionIter, 2)).ToString();
            txtPartitionType.Text = (string)lstPartitions.GetValue(partitionIter, 3);
            txtPartitionName.Text = (string)lstPartitions.GetValue(partitionIter, 4);
            txtPartitionDescription.Text = (string)lstPartitions.GetValue(partitionIter, 5);
            treeFilesystems.Model = (ListStore)lstPartitions.GetValue(partitionIter, 6);

            btnCancelPartition.Visible = true;
            btnApplyPartition.Visible = true;
            btnRemovePartition.Visible = false;
            btnEditPartition.Visible = false;
            btnAddPartition.Visible = false;
            lblPartitionSequence.Visible = true;
            spPartitionSequence.Visible = true;
            lblPartitionStart.Visible = true;
            txtPartitionStart.Visible = true;
            lblPartitionEnd.Visible = true;
            txtPartitionEnd.Visible = true;
            lblPartitionType.Visible = true;
            txtPartitionType.Visible = true;
            lblPartitionName.Visible = true;
            txtPartitionName.Visible = true;
            lblPartitionDescription.Visible = true;
            txtPartitionDescription.Visible = true;
            frmFilesystems.Visible = true;


            editingPartition = true;
        }

        protected void OnBtnApplyPartitionClicked(object sender, EventArgs e)
        {
            MessageDialog dlgMsg;
            int temp, temp2;

            if(spPartitionSequence.Value < 1)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Partition sequence must be bigger than 0");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(!int.TryParse(txtPartitionStart.Text, out temp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Partition start must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(!int.TryParse(txtPartitionEnd.Text, out temp2))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Partition end must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(temp2 <= temp)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Partition must end after start, and be bigger than 1 sector");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(editingPartition)
                lstPartitions.Remove(ref partitionIter);

            lstPartitions.AppendValues(spPartitionSequence.ValueAsInt, int.Parse(txtPartitionStart.Text), int.Parse(txtPartitionEnd.Text), txtPartitionType.Text,
                                       txtPartitionName.Text, txtPartitionDescription.Text, (ListStore)treeFilesystems.Model);

            btnCancelPartition.Visible = false;
            btnApplyPartition.Visible = false;
            btnRemovePartition.Visible = true;
            btnEditPartition.Visible = true;
            btnAddPartition.Visible = true;
            lblPartitionSequence.Visible = false;
            spPartitionSequence.Visible = false;
            lblPartitionStart.Visible = false;
            txtPartitionStart.Visible = false;
            lblPartitionEnd.Visible = false;
            txtPartitionEnd.Visible = false;
            lblPartitionType.Visible = false;
            txtPartitionType.Visible = false;
            lblPartitionName.Visible = false;
            txtPartitionName.Visible = false;
            lblPartitionDescription.Visible = false;
            txtPartitionDescription.Visible = false;
            frmFilesystems.Visible = false;
        }

        protected void OnBtnAddPartitionClicked(object sender, EventArgs e)
        {
            spPartitionSequence.Value = 0;
            txtPartitionStart.Text = "";
            txtPartitionEnd.Text = "";
            txtPartitionType.Text = "";
            txtPartitionName.Text = "";
            txtPartitionDescription.Text = "";
            treeFilesystems.Model = new ListStore(typeof(string), typeof(string), typeof(FileSystemType));

            btnCancelPartition.Visible = true;
            btnApplyPartition.Visible = true;
            btnRemovePartition.Visible = false;
            btnEditPartition.Visible = false;
            btnAddPartition.Visible = false;
            lblPartitionSequence.Visible = true;
            spPartitionSequence.Visible = true;
            lblPartitionStart.Visible = true;
            txtPartitionStart.Visible = true;
            lblPartitionEnd.Visible = true;
            txtPartitionEnd.Visible = true;
            lblPartitionType.Visible = true;
            txtPartitionType.Visible = true;
            lblPartitionName.Visible = true;
            txtPartitionName.Visible = true;
            lblPartitionDescription.Visible = true;
            txtPartitionDescription.Visible = true;
            frmFilesystems.Visible = true;

            editingPartition = false;
        }

        protected void OnBtnRemoveFilesystemClicked(object sender, EventArgs e)
        {
            if(treeFilesystems.Selection.GetSelected(out filesystemIter))
                ((ListStore)treeFilesystems.Model).Remove(ref filesystemIter);
        }

        protected void OnBtnEditFilesystemClicked(object sender, EventArgs e)
        {
            if(!treeFilesystems.Selection.GetSelected(out filesystemIter))
                return;

            dlgFilesystem _dlgFilesystem = new dlgFilesystem();
            _dlgFilesystem.Metadata = (FileSystemType)((ListStore)treeFilesystems.Model).GetValue(filesystemIter, 2);
            _dlgFilesystem.FillFields();

            if(_dlgFilesystem.Run() == (int)ResponseType.Ok)
            {
                ((ListStore)treeFilesystems.Model).Remove(ref filesystemIter);
                ((ListStore)treeFilesystems.Model).AppendValues(_dlgFilesystem.Metadata.Type, _dlgFilesystem.Metadata.VolumeName, _dlgFilesystem.Metadata);
            }

            _dlgFilesystem.Destroy();
        }

        protected void OnBtnAddFilesystemClicked(object sender, EventArgs e)
        {
            dlgFilesystem _dlgFilesystem = new dlgFilesystem();

            if(_dlgFilesystem.Run() == (int)ResponseType.Ok)
                ((ListStore)treeFilesystems.Model).AppendValues(_dlgFilesystem.Metadata.Type, _dlgFilesystem.Metadata.VolumeName, _dlgFilesystem.Metadata);

            _dlgFilesystem.Destroy();
        }

        protected void OnChkDumpHardwareToggled(object sender, EventArgs e)
        {
            treeDumpHardware.Visible = chkDumpHardware.Active;
            btnAddHardware.Visible = chkDumpHardware.Active;
            btnRemoveHardware.Visible = chkDumpHardware.Active;

            btnCancelHardware.Visible = false;
            btnEditHardware.Visible = false;
            btnApplyHardware.Visible = false;
            lblHWManufacturer.Visible = false;
            txtHWManufacturer.Visible = false;
            lblHWModel.Visible = false;
            txtHWModel.Visible = false;
            lblHWRevision.Visible = false;
            txtHWRevision.Visible = false;
            lblHWFirmware.Visible = false;
            txtHWFirmware.Visible = false;
            lblHWSerial.Visible = false;
            txtHWSerial.Visible = false;
            frmExtents.Visible = false;
            frmDumpSoftware.Visible = false;
        }

        protected void OnBtnCancelHardwareClicked(object sender, EventArgs e)
        {
            btnAddHardware.Visible = true;
            btnRemoveHardware.Visible = true;
            btnCancelHardware.Visible = false;
            btnEditHardware.Visible = true;
            btnApplyHardware.Visible = false;
            lblHWManufacturer.Visible = false;
            txtHWManufacturer.Visible = false;
            lblHWModel.Visible = false;
            txtHWModel.Visible = false;
            lblHWRevision.Visible = false;
            txtHWRevision.Visible = false;
            lblHWFirmware.Visible = false;
            txtHWFirmware.Visible = false;
            lblHWSerial.Visible = false;
            txtHWSerial.Visible = false;
            frmExtents.Visible = false;
            frmDumpSoftware.Visible = false;
        }

        protected void OnBtnRemoveHardwareClicked(object sender, EventArgs e)
        {
            TreeIter dumphwIter;
            if(treeDumpHardware.Selection.GetSelected(out dumphwIter))
                lstDumpHw.Remove(ref dumphwIter);
        }

        protected void OnBtnEditHardwareClicked(object sender, EventArgs e)
        {
            if(!treeDumpHardware.Selection.GetSelected(out dumpHwIter))
                return;

            txtHWManufacturer.Text = (string)lstDumpHw.GetValue(dumpHwIter, 0);
            txtHWModel.Text = (string)lstDumpHw.GetValue(dumpHwIter, 1);
            txtHWRevision.Text = (string)lstDumpHw.GetValue(dumpHwIter, 2);
            txtHWFirmware.Text = (string)lstDumpHw.GetValue(dumpHwIter, 3);
            txtHWSerial.Text = (string)lstDumpHw.GetValue(dumpHwIter, 4);
            txtDumpName.Text = (string)lstDumpHw.GetValue(dumpHwIter, 5);
            txtDumpVersion.Text = (string)lstDumpHw.GetValue(dumpHwIter, 6);
            txtDumpOS.Text = (string)lstDumpHw.GetValue(dumpHwIter, 7);
            treeExtents.Model = (ListStore)lstDumpHw.GetValue(dumpHwIter, 8);

            btnAddHardware.Visible = false;
            btnRemoveHardware.Visible = false;
            btnCancelHardware.Visible = true;
            btnEditHardware.Visible = false;
            btnApplyHardware.Visible = true;
            lblHWManufacturer.Visible = true;
            txtHWManufacturer.Visible = true;
            lblHWModel.Visible = true;
            txtHWModel.Visible = true;
            lblHWRevision.Visible = true;
            txtHWRevision.Visible = true;
            lblHWFirmware.Visible = true;
            txtHWFirmware.Visible = true;
            lblHWSerial.Visible = true;
            txtHWSerial.Visible = true;
            frmExtents.Visible = true;
            frmDumpSoftware.Visible = true;

            editingDumpHw = true;
        }

        protected void OnBtnApplyHardwareClicked(object sender, EventArgs e)
        {
            if(editingDumpHw)
                lstDumpHw.Remove(ref dumpHwIter);

            lstDumpHw.AppendValues(txtHWManufacturer.Text, txtHWModel.Text, txtHWRevision.Text, txtHWFirmware.Text, txtHWSerial.Text, txtDumpName.Text,
                                   txtDumpVersion.Text, txtDumpOS.Text, (ListStore)treeExtents.Model);

            btnAddHardware.Visible = true;
            btnRemoveHardware.Visible = true;
            btnCancelHardware.Visible = false;
            btnEditHardware.Visible = true;
            btnApplyHardware.Visible = false;
            lblHWManufacturer.Visible = false;
            txtHWManufacturer.Visible = false;
            lblHWModel.Visible = false;
            txtHWModel.Visible = false;
            lblHWRevision.Visible = false;
            txtHWRevision.Visible = false;
            lblHWFirmware.Visible = false;
            txtHWFirmware.Visible = false;
            lblHWSerial.Visible = false;
            txtHWSerial.Visible = false;
            frmExtents.Visible = false;
            frmDumpSoftware.Visible = false;
        }

        protected void OnBtnAddHardwareClicked(object sender, EventArgs e)
        {
            txtHWManufacturer.Text = "";
            txtHWModel.Text = "";
            txtHWRevision.Text = "";
            txtHWFirmware.Text = "";
            txtHWSerial.Text = "";
            txtDumpName.Text = "";
            txtDumpVersion.Text = "";
            txtDumpOS.Text = "";
            treeExtents.Model = new ListStore(typeof(int), typeof(int));

            btnAddHardware.Visible = false;
            btnRemoveHardware.Visible = false;
            btnCancelHardware.Visible = true;
            btnEditHardware.Visible = false;
            btnApplyHardware.Visible = true;
            lblHWManufacturer.Visible = true;
            txtHWManufacturer.Visible = true;
            lblHWModel.Visible = true;
            txtHWModel.Visible = true;
            lblHWRevision.Visible = true;
            txtHWRevision.Visible = true;
            lblHWFirmware.Visible = true;
            txtHWFirmware.Visible = true;
            lblHWSerial.Visible = true;
            txtHWSerial.Visible = true;
            frmExtents.Visible = true;
            frmDumpSoftware.Visible = true;

            editingDumpHw = false;
        }

        protected void OnBtnRemoveExtentClicked(object sender, EventArgs e)
        {
            TreeIter extentIter;
            if(treeExtents.Selection.GetSelected(out extentIter))
                ((ListStore)treeExtents.Model).Remove(ref extentIter);
        }

        protected void OnBtnAddExtentClicked(object sender, EventArgs e)
        {
            ((ListStore)treeExtents.Model).AppendValues(spExtentStart.ValueAsInt, spExtentEnd.ValueAsInt);
        }

        protected void OnButtonOkClicked(object sender, EventArgs e)
        {
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            buttonCancel.Click();
        }

        protected void OnBtnSaveClicked(object sender, EventArgs e)
        {
            MessageDialog dlgMsg;
            long ltmp;

            #region Sanity checks
            if(string.IsNullOrEmpty(txtFormat.Text))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Image format cannot be null");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(chkSequence.Active)
            {
                if(spSequence.Value < 1)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Media sequence must be bigger than 0");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                if(spTotalMedia.Value < 1)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Total medias must be bigger than 0");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                if(spSequence.Value > spTotalMedia.Value)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Media sequence cannot be bigger than total medias");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
            }

            if(string.IsNullOrEmpty(txtBlocks.Text) || !long.TryParse(txtBlocks.Text, out ltmp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Blocks must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(ltmp < 1)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Blocks must be bigger than 0");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(spPhysicalBlockSize.Value < 1)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Physical Block Size must be bigger than 0");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(spLogicalBlockSize.Value < 1)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Logical Block Size must be bigger than 0");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(spPhysicalBlockSize.Value < spLogicalBlockSize.Value)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Physical Block Size must be bigger than Logical Block Size");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(chkDimensions.Active)
            {
                if(chkRound.Active)
                {
                    if(spDiameter.ValueAsInt <= 0)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Diameter must be bigger than 0");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }
                else
                {
                    if(spHeight.ValueAsInt <= 0)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Height must be bigger than 0");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                    if(spWidth.ValueAsInt <= 0)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Width must be bigger than 0");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }
                if(spThickness.ValueAsInt <= 0)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Thickness must be bigger than 0");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
            }

            if(chkPCI.Active)
            {
                if(string.IsNullOrWhiteSpace(txtPCIVendor.Text))
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCI Vendor ID must be set");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                if(string.IsNullOrWhiteSpace(txtPCIProduct.Text))
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCI Product ID must be set");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                try
                {
                    if(Convert.ToInt32(txtPCIVendor.Text, 16) < 0 || Convert.ToInt32(txtPCIVendor.Text, 16) > 0xFFFF)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCI Vendor ID must be between 0x0000 and 0xFFFF");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }
                catch(FormatException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCI Vendor ID must be a number in hexadecimal format");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
                catch(OverflowException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCI Vendor ID must not be negative");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                try
                {
                    if(Convert.ToInt32(txtPCIProduct.Text, 16) < 0 || Convert.ToInt32(txtPCIProduct.Text, 16) > 0xFFFF)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCI Product ID must be between 0x0000 and 0xFFFF");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }
                catch(FormatException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCI Product ID must be a number in hexadecimal format");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
                catch(OverflowException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCI Product ID must not be negative");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
            }

            if(chkPCMCIA.Active)
            {
                if(string.IsNullOrWhiteSpace(txtPCMCIAManufacturer.Text))
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCMCIA Manufacturer Code must be set");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                if(string.IsNullOrWhiteSpace(txtCardCode.Text))
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCMCIA Card Code must be set");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                try
                {
                    if(Convert.ToInt32(txtMfgCode.Text, 16) < 0 || Convert.ToInt32(txtMfgCode.Text, 16) > 0xFFFF)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCMCIA Manufacturer Code must be between 0x0000 and 0xFFFF");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }
                catch(FormatException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCMCIA Manufacturer Code must be a number in hexadecimal format");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
                catch(OverflowException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCMCIA Manufacturer Code must not be negative");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                try
                {
                    if(Convert.ToInt32(txtCardCode.Text, 16) < 0 || Convert.ToInt32(txtCardCode.Text, 16) > 0xFFFF)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCMCIA Card Code must be between 0x0000 and 0xFFFF");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }
                catch(FormatException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCMCIA Card Code must be a number in hexadecimal format");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
                catch(OverflowException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "PCMCIA Card Code must not be negative");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
            }

            if(chkUSB.Active)
            {
                if(string.IsNullOrWhiteSpace(txtUSBVendor.Text))
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "USB Vendor ID must be set");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                if(string.IsNullOrWhiteSpace(txtUSBProduct.Text))
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "USB Product ID must be set");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                try
                {
                    if(Convert.ToInt32(txtUSBVendor.Text, 16) < 0 || Convert.ToInt32(txtUSBVendor.Text, 16) > 0xFFFF)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "USB Vendor ID must be between 0x0000 and 0xFFFF");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }
                catch(FormatException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "USB Vendor ID must be a number in hexadecimal format");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
                catch(OverflowException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "USB Vendor ID must not be negative");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                try
                {
                    if(Convert.ToInt32(txtUSBProduct.Text, 16) < 0 || Convert.ToInt32(txtUSBProduct.Text, 16) > 0xFFFF)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "USB Product ID must be between 0x0000 and 0xFFFF");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }
                catch(FormatException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "USB Product ID must be a number in hexadecimal format");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
                catch(OverflowException)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "USB Product ID must not be negative");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
            }

            if(chkDumpHardware.Active)
            {
                if(lstDumpHw.IterNChildren() < 1)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "If dump hardware is known at least an entry must be created");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
            }
            #endregion Sanity checks

            TreeIter outIter;
            Metadata = new BlockMediaType();

            Metadata.Image = new Schemas.ImageType();
            Metadata.Image.Value = txtImage.Text;
            Metadata.Image.format = txtFormat.Text;
            if(!string.IsNullOrWhiteSpace(txtOffset.Text) && long.TryParse(txtOffset.Text, out ltmp))
            {
                Metadata.Image.offsetSpecified = true;
                Metadata.Image.offset = long.Parse(txtOffset.Text);
            }
            Metadata.Size = long.Parse(txtSize.Text);
            Metadata.Checksums = checksums;

            if(chkSequence.Active)
            {
                Metadata.Sequence = new SequenceType();
                Metadata.Sequence.MediaTitle = txtMediaTitle.Text;
                Metadata.Sequence.MediaSequence = spSequence.ValueAsInt;
                Metadata.Sequence.TotalMedia = spTotalMedia.ValueAsInt;
                if(spSide.ValueAsInt > 0)
                {
                    Metadata.Sequence.SideSpecified = true;
                    Metadata.Sequence.Side = spSide.ValueAsInt;
                }
                if(spLayer.ValueAsInt > 0)
                {
                    Metadata.Sequence.LayerSpecified = true;
                    Metadata.Sequence.Layer = spLayer.ValueAsInt;
                }
            }

            if(!string.IsNullOrWhiteSpace(txtManufacturer.Text))
                Metadata.Manufacturer = txtManufacturer.Text;
            if(!string.IsNullOrWhiteSpace(txtModel.Text))
                Metadata.Model = txtModel.Text;
            if(!string.IsNullOrWhiteSpace(txtSerial.Text))
                Metadata.Serial = txtSerial.Text;
            if(!string.IsNullOrWhiteSpace(txtFirmware.Text))
                Metadata.Firmware = txtFirmware.Text;
            if(!string.IsNullOrWhiteSpace(txtInterface.Text))
                Metadata.Interface = txtInterface.Text;

            Metadata.PhysicalBlockSize = spPhysicalBlockSize.ValueAsInt;
            Metadata.LogicalBlockSize = spLogicalBlockSize.ValueAsInt;
            Metadata.LogicalBlocks = long.Parse(txtBlocks.Text);
            Metadata.VariableBlockSize = variableBlockSize;
            Metadata.TapeInformation = tapeInformation;
            Metadata.Scans = scans;

            if(chkATA.Active && lstAta.GetIterFirst(out outIter))
            {
                Metadata.ATA = new ATAType();
                Metadata.ATA.Identify = new DumpType();
                Metadata.ATA.Identify.Image = (string)lstAta.GetValue(outIter, 0);
                Metadata.ATA.Identify.Size = (int)lstAta.GetValue(outIter, 1);
                Metadata.ATA.Identify.Checksums = (ChecksumType[])lstAta.GetValue(outIter, 2);
            }

            if(chkPCI.Active)
            {
                Metadata.PCI = new PCIType();
                Metadata.PCI.VendorID = Convert.ToUInt16(txtPCIVendor.Text, 16);
                Metadata.PCI.DeviceID = Convert.ToUInt16(txtPCIProduct.Text, 16);

                if(lstPCIConfiguration.GetIterFirst(out outIter))
                {
                    Metadata.PCI.Configuration = new DumpType();
                    Metadata.PCI.Configuration.Image = (string)lstPCIConfiguration.GetValue(outIter, 0);
                    Metadata.PCI.Configuration.Size = (int)lstPCIConfiguration.GetValue(outIter, 1);
                    Metadata.PCI.Configuration.Checksums = (ChecksumType[])lstPCIConfiguration.GetValue(outIter, 2);
                }

                if(lstPCIOptionROM.GetIterFirst(out outIter))
                    Metadata.PCI.ExpansionROM = (LinearMediaType)lstPCIOptionROM.GetValue(outIter, 3);
            }

            if(chkPCMCIA.Active)
            {
                Metadata.PCMCIA = new PCMCIAType();

                if(lstPCMCIACIS.GetIterFirst(out outIter))
                {
                    Metadata.PCMCIA.CIS = new DumpType();
                    Metadata.PCMCIA.CIS.Image = (string)lstPCMCIACIS.GetValue(outIter, 0);
                    Metadata.PCMCIA.CIS.Size = (int)lstPCMCIACIS.GetValue(outIter, 1);
                    Metadata.PCMCIA.CIS.Checksums = (ChecksumType[])lstPCMCIACIS.GetValue(outIter, 2);
                }

                if(!string.IsNullOrWhiteSpace(txtCompliance.Text))
                    Metadata.PCMCIA.Compliance = txtCompliance.Text;
                if(!string.IsNullOrWhiteSpace(txtPCMCIAManufacturer.Text))
                    Metadata.PCMCIA.Manufacturer = txtPCMCIAManufacturer.Text;
                if(!string.IsNullOrWhiteSpace(txtPCMCIAProductName.Text))
                    Metadata.PCMCIA.ProductName = txtPCMCIAProductName.Text;
                if(!string.IsNullOrWhiteSpace(txtMfgCode.Text))
                {
                    Metadata.PCMCIA.ManufacturerCodeSpecified = true;
                    Metadata.PCMCIA.ManufacturerCode = Convert.ToUInt16(txtMfgCode.Text, 16);
                }
                if(!string.IsNullOrWhiteSpace(txtCardCode.Text))
                {
                    Metadata.PCMCIA.CardCodeSpecified = true;
                    Metadata.PCMCIA.CardCode = Convert.ToUInt16(txtCardCode.Text, 16);
                }

                if(lstAdditionalInformation.GetIterFirst(out outIter))
                {
                    List<string> addinfos = new List<string>();
                    do
                        addinfos.Add((string)lstAdditionalInformation.GetValue(outIter, 0));
                    while(lstAdditionalInformation.IterNext(ref outIter));
                    Metadata.PCMCIA.AdditionalInformation = addinfos.ToArray();
                }
            }

            if(chkSecureDigital.Active)
            {
                Metadata.SecureDigital = new SecureDigitalType();

                if(lstCID.GetIterFirst(out outIter))
                {
                    Metadata.SecureDigital.CID = new DumpType();
                    Metadata.SecureDigital.CID.Image = (string)lstCID.GetValue(outIter, 0);
                    Metadata.SecureDigital.CID.Size = (int)lstCID.GetValue(outIter, 1);
                    Metadata.SecureDigital.CID.Checksums = (ChecksumType[])lstCID.GetValue(outIter, 2);
                }
                if(lstCSD.GetIterFirst(out outIter))
                {
                    Metadata.SecureDigital.CSD = new DumpType();
                    Metadata.SecureDigital.CSD.Image = (string)lstCSD.GetValue(outIter, 0);
                    Metadata.SecureDigital.CSD.Size = (int)lstCSD.GetValue(outIter, 1);
                    Metadata.SecureDigital.CSD.Checksums = (ChecksumType[])lstCSD.GetValue(outIter, 2);
                }
                if(lstECSD.GetIterFirst(out outIter))
                {
                    Metadata.SecureDigital.ExtendedCSD = new DumpType();
                    Metadata.SecureDigital.ExtendedCSD.Image = (string)lstECSD.GetValue(outIter, 0);
                    Metadata.SecureDigital.ExtendedCSD.Size = (int)lstECSD.GetValue(outIter, 1);
                    Metadata.SecureDigital.ExtendedCSD.Checksums = (ChecksumType[])lstECSD.GetValue(outIter, 2);
                }
            }

            if(chkSCSI.Active)
            {
                Metadata.SCSI = new SCSIType();

                if(lstInquiry.GetIterFirst(out outIter))
                {
                    Metadata.SCSI.Inquiry = new DumpType();
                    Metadata.SCSI.Inquiry.Image = (string)lstInquiry.GetValue(outIter, 0);
                    Metadata.SCSI.Inquiry.Size = (int)lstInquiry.GetValue(outIter, 1);
                    Metadata.SCSI.Inquiry.Checksums = (ChecksumType[])lstInquiry.GetValue(outIter, 2);
                }
                if(lstModeSense.GetIterFirst(out outIter))
                {
                    Metadata.SCSI.ModeSense = new DumpType();
                    Metadata.SCSI.ModeSense.Image = (string)lstModeSense.GetValue(outIter, 0);
                    Metadata.SCSI.ModeSense.Size = (int)lstModeSense.GetValue(outIter, 1);
                    Metadata.SCSI.ModeSense.Checksums = (ChecksumType[])lstModeSense.GetValue(outIter, 2);
                }
                if(lstModeSense10.GetIterFirst(out outIter))
                {
                    Metadata.SCSI.ModeSense10 = new DumpType();
                    Metadata.SCSI.ModeSense10.Image = (string)lstModeSense10.GetValue(outIter, 0);
                    Metadata.SCSI.ModeSense10.Size = (int)lstModeSense10.GetValue(outIter, 1);
                    Metadata.SCSI.ModeSense10.Checksums = (ChecksumType[])lstModeSense10.GetValue(outIter, 2);
                }
                if(lstLogSense.GetIterFirst(out outIter))
                {
                    Metadata.SCSI.LogSense = new DumpType();
                    Metadata.SCSI.LogSense.Image = (string)lstLogSense.GetValue(outIter, 0);
                    Metadata.SCSI.LogSense.Size = (int)lstLogSense.GetValue(outIter, 1);
                    Metadata.SCSI.LogSense.Checksums = (ChecksumType[])lstLogSense.GetValue(outIter, 2);
                }
                if(lstEVPDs.GetIterFirst(out outIter))
                {
                    List<EVPDType> evpds = new List<EVPDType>();
                    do
                    {
                        EVPDType evpd = new EVPDType();
                        evpd.page = (int)lstEVPDs.GetValue(outIter, 0);
                        evpd.pageSpecified = true;
                        evpd.Image = (string)lstEVPDs.GetValue(outIter, 1);
                        evpd.Size = (long)lstEVPDs.GetValue(outIter, 2);
                        evpd.Checksums = (ChecksumType[])lstEVPDs.GetValue(outIter, 3);
                        evpds.Add(evpd);
                    }
                    while(lstEVPDs.IterNext(ref outIter));
                    Metadata.SCSI.EVPD = evpds.ToArray();
                }
            }

            if(chkUSB.Active)
            {
                Metadata.USB = new USBType();
                Metadata.USB.VendorID = Convert.ToUInt16(txtUSBVendor.Text, 16);
                Metadata.USB.ProductID = Convert.ToUInt16(txtUSBProduct.Text, 16);

                if(lstUSBDescriptors.GetIterFirst(out outIter))
                {
                    Metadata.USB.Descriptors = new DumpType();
                    Metadata.USB.Descriptors.Image = (string)lstUSBDescriptors.GetValue(outIter, 0);
                    Metadata.USB.Descriptors.Size = (int)lstUSBDescriptors.GetValue(outIter, 1);
                    Metadata.USB.Descriptors.Checksums = (ChecksumType[])lstUSBDescriptors.GetValue(outIter, 2);
                }
            }

            if(chkMAM.Active && lstMAM.GetIterFirst(out outIter))
            {
                Metadata.MAM = new DumpType();
                Metadata.MAM.Image = (string)lstMAM.GetValue(outIter, 0);
                Metadata.MAM.Size = (int)lstMAM.GetValue(outIter, 1);
                Metadata.MAM.Checksums = (ChecksumType[])lstMAM.GetValue(outIter, 2);
            }

            if(spHeads.ValueAsInt > 0 && spCylinders.ValueAsInt > 0 && spSectors.ValueAsInt > 0)
            {
                Metadata.HeadsSpecified = true;
                Metadata.CylindersSpecified = true;
                Metadata.SectorsPerTrackSpecified = true;
                Metadata.Heads = spHeads.ValueAsInt;
                Metadata.Cylinders = spCylinders.ValueAsInt;
                Metadata.SectorsPerTrack = spSectors.ValueAsInt;
            }

            if(lstTracks.GetIterFirst(out outIter))
            {
                List<BlockTrackType> tracks = new List<BlockTrackType>();
                do
                {
                    BlockTrackType track = new BlockTrackType();
                    track.Image = new Schemas.ImageType();
                    track.Image.Value = (string)lstTracks.GetValue(outIter, 0);
                    track.Image.offset = (long)lstTracks.GetValue(outIter, 1);
                    if(track.Image.offset > 0)
                        track.Image.offsetSpecified = true;
                    track.Size = (long)lstTracks.GetValue(outIter, 2);
                    track.Image.format = (string)lstTracks.GetValue(outIter, 3);
                    track.Head = (long)lstTracks.GetValue(outIter, 4);
                    track.Cylinder = (long)lstTracks.GetValue(outIter, 5);
                    track.StartSector = (long)lstTracks.GetValue(outIter, 6);
                    track.EndSector = (long)lstTracks.GetValue(outIter, 7);
                    track.Sectors = (long)lstTracks.GetValue(outIter, 8);
                    track.BytesPerSector = (int)lstTracks.GetValue(outIter, 9);
                    track.Format = (string)lstTracks.GetValue(outIter, 10);
                    track.Checksums = (ChecksumType[])lstTracks.GetValue(outIter, 11);
                    tracks.Add(track);
                }
                while(lstTracks.IterNext(ref outIter));
                Metadata.Track = tracks.ToArray();
            }

            if(!string.IsNullOrWhiteSpace(txtCopyProtection.Text))
                Metadata.CopyProtection = txtCopyProtection.Text;

            if(chkDimensions.Active)
            {
                Metadata.Dimensions = new DimensionsType();
                if(chkRound.Active)
                {
                    Metadata.Dimensions.DiameterSpecified = true;
                    Metadata.Dimensions.Diameter = spDiameter.Value;
                }
                else
                {
                    Metadata.Dimensions.HeightSpecified = true;
                    Metadata.Dimensions.WidthSpecified = true;
                    Metadata.Dimensions.Height = spHeight.Value;
                    Metadata.Dimensions.Width = spWidth.Value;
                }
                Metadata.Dimensions.Thickness = spThickness.Value;
            }

            if(lstPartitions.GetIterFirst(out outIter))
            {
                List<PartitionType> partitions = new List<PartitionType>();
                do
                {
                    PartitionType partition = new PartitionType();
                    partition.Sequence = (int)lstPartitions.GetValue(outIter, 0);
                    partition.StartSector = (int)lstPartitions.GetValue(outIter, 1);
                    partition.EndSector = (int)lstPartitions.GetValue(outIter, 2);
                    partition.Type = (string)lstPartitions.GetValue(outIter, 3);
                    partition.Name = (string)lstPartitions.GetValue(outIter, 4);
                    partition.Description = (string)lstPartitions.GetValue(outIter, 5);
                    ListStore lstFilesystems = (ListStore)lstPartitions.GetValue(outIter, 6);
                    TreeIter fsIter;

                    if(lstFilesystems.GetIterFirst(out fsIter))
                    {
                        List<FileSystemType> fss = new List<FileSystemType>();
                        do
                        {
                            FileSystemType fs = (FileSystemType)lstFilesystems.GetValue(fsIter, 2);
                            fss.Add(fs);
                        }
                        while(lstFilesystems.IterNext(ref fsIter));
                        partition.FileSystems = fss.ToArray();
                    }

                    partitions.Add(partition);
                }
                while(lstPartitions.IterNext(ref outIter));
                Metadata.FileSystemInformation = partitions.ToArray();
            }

            if(chkDumpHardware.Active && lstDumpHw.GetIterFirst(out outIter))
            {
                List<DumpHardwareType> dumps = new List<DumpHardwareType>();
                do
                {
                    DumpHardwareType dump = new DumpHardwareType();
                    dump.Software = new SoftwareType();
                    ListStore lstExtents;
                    TreeIter extIter;

                    dump.Manufacturer = (string)lstDumpHw.GetValue(outIter, 0);
                    dump.Model = (string)lstDumpHw.GetValue(outIter, 1);
                    dump.Revision = (string)lstDumpHw.GetValue(outIter, 2);
                    dump.Firmware = (string)lstDumpHw.GetValue(outIter, 3);
                    dump.Serial = (string)lstDumpHw.GetValue(outIter, 4);
                    dump.Software.Name = (string)lstDumpHw.GetValue(outIter, 5);
                    dump.Software.Version = (string)lstDumpHw.GetValue(outIter, 6);
                    dump.Software.OperatingSystem = (string)lstDumpHw.GetValue(outIter, 7);
                    lstExtents = (ListStore)lstDumpHw.GetValue(outIter, 8);

                    if(lstExtents.GetIterFirst(out extIter))
                    {
                        List<ExtentType> extents = new List<ExtentType>();
                        do
                        {
                            ExtentType extent = new ExtentType();
                            extent.Start = (int)lstExtents.GetValue(extIter, 0);
                            extent.End = (int)lstExtents.GetValue(extIter, 1);
                            extents.Add(extent);
                        }
                        while(lstExtents.IterNext(ref extIter));
                        dump.Extents = extents.ToArray();
                    }

                    dumps.Add(dump);
                }
                while(lstDumpHw.IterNext(ref outIter));
                Metadata.DumpHardwareArray = dumps.ToArray();
            }

            if(!string.IsNullOrWhiteSpace(txtMediaType.Text))
                Metadata.DiskType = txtMediaType.Text;
            if(!string.IsNullOrWhiteSpace(txtMediaSubtype.Text))
                Metadata.DiskSubType = txtMediaSubtype.Text;

            buttonOk.Click();
        }
    }
}
