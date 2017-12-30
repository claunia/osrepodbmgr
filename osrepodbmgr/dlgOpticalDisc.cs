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
using ImageType = Schemas.ImageType;

namespace osrepodbmgr
{
    public partial class dlgOpticalDisc : Dialog
    {
        // Non-editable fields
        ChecksumType[] checksums;
        TreeIter       dumpHwIter;
        bool           editingDumpHw;

        bool      editingPartition;
        TreeIter  filesystemIter;
        ListStore lstADIP;
        ListStore lstATIP;
        ListStore lstBCA;
        ListStore lstCDText;
        ListStore lstCMI;
        ListStore lstDCB;
        ListStore lstDDS;
        ListStore lstDI;
        ListStore lstDMI;

        ListStore              lstDumpHw;
        ListStore              lstLastRMD;
        ListStore              lstLayers;
        ListStore              lstLayerTypes;
        ListStore              lstLeadIns;
        ListStore              lstLeadOuts;
        ListStore              lstMasteringSIDs;
        ListStore              lstMediaID;
        ListStore              lstMouldSIDs;
        ListStore              lstMouldTexts;
        ListStore              lstPAC;
        ListStore              lstPFI;
        ListStore              lstPFIR;
        ListStore              lstPMA;
        ListStore              lstPRI;
        ListStore              lstRingCodes;
        ListStore              lstSAI;
        ListStore              lstTOC;
        ListStore              lstToolstamps;
        ListStore              lstTracks;
        ListStore              lstTrackTypes;
        ListStore              lstXboxSS;
        CaseType               mediaCase;
        public OpticalDiscType Metadata;
        TreeIter               partitionIter;
        ScansType              scans;

        TreeIter trackIter;
        XboxType xbox;

        public dlgOpticalDisc()
        {
            Build();

            #region Set partitions table
            CellRendererText partSequenceCell    = new CellRendererText();
            CellRendererText partStartCell       = new CellRendererText();
            CellRendererText partEndCell         = new CellRendererText();
            CellRendererText partTypeCell        = new CellRendererText();
            CellRendererText partNameCell        = new CellRendererText();
            CellRendererText partDescriptionCell = new CellRendererText();

            TreeViewColumn partSequenceColumn    = new TreeViewColumn("Sequence",    partSequenceCell,    "text", 0);
            TreeViewColumn partStartColumn       = new TreeViewColumn("Start",       partStartCell,       "text", 1);
            TreeViewColumn partEndColumn         = new TreeViewColumn("End",         partEndCell,         "text", 2);
            TreeViewColumn partTypeColumn        = new TreeViewColumn("Type",        partTypeCell,        "text", 3);
            TreeViewColumn partNameColumn        = new TreeViewColumn("Name",        partNameCell,        "text", 4);
            TreeViewColumn partDescriptionColumn = new TreeViewColumn("Description", partDescriptionCell, "text", 5);

            treePartitions.AppendColumn(partSequenceColumn);
            treePartitions.AppendColumn(partStartColumn);
            treePartitions.AppendColumn(partEndColumn);
            treePartitions.AppendColumn(partTypeColumn);
            treePartitions.AppendColumn(partNameColumn);
            treePartitions.AppendColumn(partDescriptionColumn);

            treePartitions.Selection.Mode = SelectionMode.Single;
            #endregion Set partitions table

            #region Set filesystems table
            CellRendererText fsTypeCell   = new CellRendererText();
            CellRendererText fsNameCell   = new CellRendererText();
            TreeViewColumn   fsTypeColumn = new TreeViewColumn("Type", fsTypeCell, "text", 0);
            TreeViewColumn   fsNameColumn = new TreeViewColumn("Name", fsNameCell, "text", 1);
            treeFilesystems.AppendColumn(fsTypeColumn);
            treeFilesystems.AppendColumn(fsNameColumn);
            treeFilesystems.Selection.Mode = SelectionMode.Single;
            #endregion Set filesystems table

            #region Set dump hardware table
            lstDumpHw = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string),
                                      typeof(string), typeof(string), typeof(string), typeof(ListStore));

            CellRendererText hwManufacturerCell = new CellRendererText();
            CellRendererText hwModelCell        = new CellRendererText();
            CellRendererText hwRevisionCell     = new CellRendererText();
            CellRendererText hwFirmwareCell     = new CellRendererText();
            CellRendererText hwSerialCell       = new CellRendererText();
            CellRendererText swNameCell         = new CellRendererText();
            CellRendererText swVersionCell      = new CellRendererText();
            CellRendererText swOSCell           = new CellRendererText();

            TreeViewColumn hwManufacturerColumn = new TreeViewColumn("Manufacturer",     hwManufacturerCell, "text", 0);
            TreeViewColumn hwModelColumn        = new TreeViewColumn("Model",            hwModelCell,        "text", 1);
            TreeViewColumn hwRevisionColumn     = new TreeViewColumn("Revision",         hwRevisionCell,     "text", 2);
            TreeViewColumn hwFirmwareColumn     = new TreeViewColumn("Firmware",         hwFirmwareCell,     "text", 3);
            TreeViewColumn hwSerialColumn       = new TreeViewColumn("Serial",           hwSerialCell,       "text", 4);
            TreeViewColumn swNameColumn         = new TreeViewColumn("Software",         swNameCell,         "text", 5);
            TreeViewColumn swVersionColumn      = new TreeViewColumn("Version",          swVersionCell,      "text", 6);
            TreeViewColumn swOSColumn           = new TreeViewColumn("Operating system", swOSCell,           "text", 7);

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

            CellRendererText extentStartCell   = new CellRendererText();
            CellRendererText extentEndCell     = new CellRendererText();
            TreeViewColumn   extentStartColumn = new TreeViewColumn("Start", extentStartCell, "text", 0);
            TreeViewColumn   extentEndColumn   = new TreeViewColumn("End",   extentEndCell,   "text", 1);
            treeExtents.AppendColumn(extentStartColumn);
            treeExtents.AppendColumn(extentEndColumn);
            treeExtents.Selection.Mode = SelectionMode.Single;
            #endregion Set dump hardware table

            CellRendererText fileCell   = new CellRendererText();
            CellRendererText sizeCell   = new CellRendererText();
            TreeViewColumn   fileColumn = new TreeViewColumn("File", fileCell, "text", 0);
            TreeViewColumn   sizeColumn = new TreeViewColumn("Size", sizeCell, "text", 1);

            #region Set TOC table
            lstTOC        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeTOC.Model = lstTOC;
            treeTOC.AppendColumn(fileColumn);
            treeTOC.AppendColumn(sizeColumn);
            #endregion Set TOC table

            #region Set CD-Text table
            lstCDText        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeCDText.Model = lstCDText;
            treeCDText.AppendColumn(fileColumn);
            treeCDText.AppendColumn(sizeColumn);
            #endregion Set CD-Text table

            #region Set ATIP table
            lstATIP        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeATIP.Model = lstATIP;
            treeATIP.AppendColumn(fileColumn);
            treeATIP.AppendColumn(sizeColumn);
            #endregion Set ATIP table

            #region Set PMA table
            lstPMA        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treePMA.Model = lstPMA;
            treePMA.AppendColumn(fileColumn);
            treePMA.AppendColumn(sizeColumn);
            #endregion Set PMA table

            #region Set PFI table
            lstPFI        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treePFI.Model = lstPFI;
            treePFI.AppendColumn(fileColumn);
            treePFI.AppendColumn(sizeColumn);
            #endregion Set PFI table

            #region Set DMI table
            lstDMI        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeDMI.Model = lstDMI;
            treeDMI.AppendColumn(fileColumn);
            treeDMI.AppendColumn(sizeColumn);
            #endregion Set DMI table

            #region Set CMI table
            lstCMI        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeCMI.Model = lstCMI;
            treeCMI.AppendColumn(fileColumn);
            treeCMI.AppendColumn(sizeColumn);
            #endregion Set CMI table

            #region Set BCA table
            lstBCA        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeBCA.Model = lstBCA;
            treeBCA.AppendColumn(fileColumn);
            treeBCA.AppendColumn(sizeColumn);
            #endregion Set BCA table

            #region Set DCB table
            lstDCB        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeDCB.Model = lstDCB;
            treeDCB.AppendColumn(fileColumn);
            treeDCB.AppendColumn(sizeColumn);
            #endregion Set DCB table

            #region Set PRI table
            lstPRI        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treePRI.Model = lstPRI;
            treePRI.AppendColumn(fileColumn);
            treePRI.AppendColumn(sizeColumn);
            #endregion Set PRI table

            #region Set MediaID table
            lstMediaID        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeMediaID.Model = lstMediaID;
            treeMediaID.AppendColumn(fileColumn);
            treeMediaID.AppendColumn(sizeColumn);
            #endregion Set MediaID table

            #region Set PFIR table
            lstPFIR        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treePFIR.Model = lstPFIR;
            treePFIR.AppendColumn(fileColumn);
            treePFIR.AppendColumn(sizeColumn);
            #endregion Set PFIR table

            #region Set LastRMD table
            lstLastRMD        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeLastRMD.Model = lstLastRMD;
            treeLastRMD.AppendColumn(fileColumn);
            treeLastRMD.AppendColumn(sizeColumn);
            #endregion Set LastRMD table

            #region Set ADIP table
            lstADIP        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeADIP.Model = lstADIP;
            treeADIP.AppendColumn(fileColumn);
            treeADIP.AppendColumn(sizeColumn);
            #endregion Set ADIP table

            #region Set DDS table
            lstDDS        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeDDS.Model = lstDDS;
            treeDDS.AppendColumn(fileColumn);
            treeDDS.AppendColumn(sizeColumn);
            #endregion Set DDS table

            #region Set SAI table
            lstSAI        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeSAI.Model = lstSAI;
            treeSAI.AppendColumn(fileColumn);
            treeSAI.AppendColumn(sizeColumn);
            #endregion Set SAI table

            #region Set DI table
            lstDI        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treeDI.Model = lstDI;
            treeDI.AppendColumn(fileColumn);
            treeDI.AppendColumn(sizeColumn);
            #endregion Set DI table

            #region Set PAC table
            lstPAC        = new ListStore(typeof(string), typeof(int), typeof(ChecksumType[]));
            treePAC.Model = lstPAC;
            treePAC.AppendColumn(fileColumn);
            treePAC.AppendColumn(sizeColumn);
            #endregion Set PAC table

            CellRendererText layerCell   = new CellRendererText();
            CellRendererText codeCell    = new CellRendererText();
            TreeViewColumn   layerColumn = new TreeViewColumn("Layer", layerCell, "text", 0);
            TreeViewColumn   codeColumn  = new TreeViewColumn("Code",  codeCell,  "text", 1);

            #region Set ring code table
            lstRingCodes        = new ListStore(typeof(int), typeof(string));
            treeRingCodes.Model = lstRingCodes;
            treeRingCodes.AppendColumn(layerColumn);
            treeRingCodes.AppendColumn(codeColumn);
            #endregion Set ring code table

            #region Set mastering sid table
            lstMasteringSIDs        = new ListStore(typeof(int), typeof(string));
            treeMasteringSIDs.Model = lstMasteringSIDs;
            treeMasteringSIDs.AppendColumn(layerColumn);
            treeMasteringSIDs.AppendColumn(codeColumn);
            #endregion Set mastering sid table

            #region Set toolstamp table
            lstToolstamps        = new ListStore(typeof(int), typeof(string));
            treeToolstamps.Model = lstToolstamps;
            treeToolstamps.AppendColumn(layerColumn);
            treeToolstamps.AppendColumn(codeColumn);
            #endregion Set toolstamp table

            #region Set mould sid table
            lstMouldSIDs        = new ListStore(typeof(int), typeof(string));
            treeMouldSIDs.Model = lstMouldSIDs;
            treeMouldSIDs.AppendColumn(layerColumn);
            treeMouldSIDs.AppendColumn(codeColumn);
            #endregion Set mould sid table

            #region Set mould text table
            lstMouldTexts        = new ListStore(typeof(int), typeof(string));
            treeMouldTexts.Model = lstMouldTexts;
            treeMouldTexts.AppendColumn(layerColumn);
            treeMouldTexts.AppendColumn(codeColumn);
            #endregion Set mould text table

            #region Set layer type combo box
            CellRendererText cmbCell = new CellRendererText();
            cmbLayerType.Clear();
            lstLayerTypes = new ListStore(typeof(string));
            cmbLayerType.PackStart(cmbCell, true);
            cmbLayerType.AddAttribute(cmbCell, "text", 0);
            cmbLayerType.Model = lstLayerTypes;
            foreach(LayersTypeType type in Enum.GetValues(typeof(LayersTypeType)))
                lstLayerTypes.AppendValues(type.ToString());
            #endregion Set layer type combo box

            #region Set layers table
            CellRendererText layerSizeCell   = new CellRendererText();
            TreeViewColumn   layerSizeColumn = new TreeViewColumn("Sectors", layerSizeCell, "text", 1);
            lstLayers                        = new ListStore(typeof(int), typeof(long));
            treeLayers.Model                 = lstLayers;
            treeLayers.AppendColumn(layerColumn);
            treeLayers.AppendColumn(layerSizeColumn);
            #endregion Set layers table

            CellRendererText sessionCell   = new CellRendererText();
            TreeViewColumn   sessionColumn = new TreeViewColumn("Session", sessionCell, "text", 2);

            #region Set Lead-In table
            lstLeadIns       = new ListStore(typeof(string), typeof(int), typeof(int), typeof(ChecksumType[]));
            treeLeadIn.Model = lstLeadIns;
            treeLeadIn.AppendColumn(fileColumn);
            treeLeadIn.AppendColumn(sizeColumn);
            treeLeadIn.AppendColumn(sessionColumn);
            #endregion Set Lead-In table

            #region Set Lead-Out table
            lstLeadOuts       = new ListStore(typeof(string), typeof(int), typeof(int), typeof(ChecksumType[]));
            treeLeadOut.Model = lstLeadOuts;
            treeLeadOut.AppendColumn(fileColumn);
            treeLeadOut.AppendColumn(sizeColumn);
            treeLeadOut.AppendColumn(sessionColumn);
            #endregion Set Lead-Out table

            #region Set tracks table
            lstTracks = new ListStore(typeof(int), typeof(int), typeof(string), typeof(long), typeof(string),
                                      typeof(long), typeof(string), typeof(string), typeof(long), typeof(long),
                                      typeof(string), typeof(int), typeof(string), typeof(ChecksumType[]),
                                      typeof(SubChannelType), typeof(ListStore));
            CellRendererText trackSequenceCell   = new CellRendererText();
            CellRendererText sessionSequenceCell = new CellRendererText();
            CellRendererText trackFileCell       = new CellRendererText();
            CellRendererText trackFileSizeCell   = new CellRendererText();
            CellRendererText trackFileFormatCell = new CellRendererText();
            CellRendererText trackFileOffsetCell = new CellRendererText();
            CellRendererText trackMSFStartCell   = new CellRendererText();
            CellRendererText trackMSFEndCell     = new CellRendererText();
            CellRendererText trackStartCell      = new CellRendererText();
            CellRendererText trackEndCell        = new CellRendererText();
            CellRendererText trackTypeCell       = new CellRendererText();
            CellRendererText trackBpsCell        = new CellRendererText();
            CellRendererText trackAccoustCell    = new CellRendererText();
            TreeViewColumn   trackSequenceColumn =
                new TreeViewColumn("Track", trackSequenceCell, "text", 0);
            TreeViewColumn sessionSequenceColumn =
                new TreeViewColumn("Session", sessionSequenceCell, "text", 1);
            TreeViewColumn trackFileColumn =
                new TreeViewColumn("File", trackFileCell, "text", 2);
            TreeViewColumn trackFileSizeColumn =
                new TreeViewColumn("Size", trackFileSizeCell, "text", 3);
            TreeViewColumn trackFileFormatColumn =
                new TreeViewColumn("Format", trackFileFormatCell, "text", 4);
            TreeViewColumn trackFileOffsetColumn =
                new TreeViewColumn("Offset", trackFileOffsetCell, "text", 5);
            TreeViewColumn trackMSFStartColumn =
                new TreeViewColumn("MSF Start", trackMSFStartCell, "text", 6);
            TreeViewColumn trackMSFEndColumn =
                new TreeViewColumn("MSF End", trackMSFEndCell, "text", 7);
            TreeViewColumn trackStartColumn =
                new TreeViewColumn("LBA Start", trackStartCell, "text", 8);
            TreeViewColumn trackEndColumn =
                new TreeViewColumn("LBA End", trackEndCell, "text", 9);
            TreeViewColumn trackTypeColumn =
                new TreeViewColumn("Type", trackTypeCell, "text", 10);
            TreeViewColumn trackBpsColumn =
                new TreeViewColumn("Bytes per sector", trackBpsCell, "text", 11);
            TreeViewColumn trackAccoustColumn =
                new TreeViewColumn("Accoust ID", trackAccoustCell, "text", 12);
            treeTracks.Model = lstTracks;
            treeTracks.AppendColumn(trackSequenceColumn);
            treeTracks.AppendColumn(sessionSequenceColumn);
            treeTracks.AppendColumn(trackFileColumn);
            treeTracks.AppendColumn(trackFileSizeColumn);
            treeTracks.AppendColumn(trackFileFormatColumn);
            treeTracks.AppendColumn(trackFileOffsetColumn);
            treeTracks.AppendColumn(trackMSFStartColumn);
            treeTracks.AppendColumn(trackMSFEndColumn);
            treeTracks.AppendColumn(trackStartColumn);
            treeTracks.AppendColumn(trackEndColumn);
            treeTracks.AppendColumn(trackTypeColumn);
            treeTracks.AppendColumn(trackBpsColumn);
            treeTracks.AppendColumn(trackAccoustColumn);
            #endregion Set tracks table

            #region Set track type combo box
            cmbTrackType.Clear();
            lstTrackTypes = new ListStore(typeof(string));
            cmbTrackType.PackStart(cmbCell, true);
            cmbTrackType.AddAttribute(cmbCell, "text", 0);
            cmbTrackType.Model = lstTrackTypes;
            foreach(TrackTypeTrackType type in Enum.GetValues(typeof(TrackTypeTrackType)))
                lstTrackTypes.AppendValues(type.ToString());
            #endregion Set track type combo box

            spExtentStart.Adjustment.Upper = double.MaxValue;
            spExtentEnd.Adjustment.Upper   = double.MaxValue;
        }

        public void FillFields()
        {
            if(Metadata == null) return;

            txtImage.Text                                     = Metadata.Image.Value;
            txtFormat.Text                                    = Metadata.Image.format;
            if(Metadata.Image.offsetSpecified) txtOffset.Text = Metadata.Image.offset.ToString();
            txtSize.Text                                      = Metadata.Size.ToString();
            if(Metadata.Sequence != null)
            {
                lblDiscTitle.Visible                                = true;
                lblDiscTitle.Visible                                = true;
                lblSequence.Visible                                 = true;
                spSequence.Visible                                  = true;
                lblTotalMedia.Visible                               = true;
                spTotalMedia.Visible                                = true;
                lblSide.Visible                                     = true;
                spSide.Visible                                      = true;
                lblLayer.Visible                                    = true;
                spLayer1.Visible                                    = true;
                chkSequence.Active                                  = true;
                txtDiscTitle.Text                                   = Metadata.Sequence.MediaTitle;
                spSequence.Value                                    = Metadata.Sequence.MediaSequence;
                spTotalMedia.Value                                  = Metadata.Sequence.TotalMedia;
                if(Metadata.Sequence.SideSpecified) spSide.Value    = Metadata.Sequence.Side;
                if(Metadata.Sequence.LayerSpecified) spLayer1.Value = Metadata.Sequence.Layer;
            }

            if(Metadata.Layers != null)
            {
                chkLayers.Active  = true;
                frmLayers.Visible = true;

                cmbLayerType.Model.GetIterFirst(out TreeIter layerIter);
                do
                {
                    if((string)cmbLayerType.Model.GetValue(layerIter, 0) != Metadata.Layers.type.ToString()) continue;

                    cmbLayerType.SetActiveIter(layerIter);
                    break;
                }
                while(cmbLayerType.Model.IterNext(ref layerIter));

                foreach(SectorsType layer in Metadata.Layers.Sectors) lstLayers.AppendValues(layer.layer, layer.Value);
            }

            checksums = Metadata.Checksums;
            xbox      = Metadata.Xbox;

            if(Metadata.RingCode != null)
                foreach(LayeredTextType ringcode in Metadata.RingCode)
                    lstRingCodes.AppendValues(ringcode.layer, ringcode.Value);

            if(Metadata.MasteringSID != null)
                foreach(LayeredTextType masteringsid in Metadata.MasteringSID)
                    lstMasteringSIDs.AppendValues(masteringsid.layer, masteringsid.Value);

            if(Metadata.Toolstamp != null)
                foreach(LayeredTextType toolstamp in Metadata.Toolstamp)
                    lstToolstamps.AppendValues(toolstamp.layer, toolstamp.Value);

            if(Metadata.MouldSID != null)
                foreach(LayeredTextType mouldsid in Metadata.MouldSID)
                    lstMouldSIDs.AppendValues(mouldsid.layer, mouldsid.Value);

            if(Metadata.MouldText != null)
                foreach(LayeredTextType mouldtext in Metadata.MouldText)
                    lstMouldTexts.AppendValues(mouldtext.layer, mouldtext.Value);

            if(Metadata.DiscType    != null) txtDiscType.Text          = Metadata.DiscType;
            if(Metadata.DiscSubType != null) txtDiscSubType.Text       = Metadata.DiscSubType;
            if(Metadata.OffsetSpecified) txtWriteOffset.Text           = Metadata.Offset.ToString();
            txtMediaTracks.Text                                        = Metadata.Tracks[0].ToString();
            txtMediaSessions.Text                                      = Metadata.Sessions.ToString();
            if(Metadata.CopyProtection != null) txtCopyProtection.Text = Metadata.CopyProtection;

            if(Metadata.Dimensions != null)
            {
                chkDimensions.Active = true;
                if(Metadata.Dimensions.DiameterSpecified)
                {
                    chkRound.Active          = true;
                    lblDiameter.Visible      = true;
                    spDiameter.Visible       = true;
                    lblDiameterUnits.Visible = true;
                    spDiameter.Value         = Metadata.Dimensions.Diameter;
                }
                else
                {
                    lblHeight.Visible      = true;
                    spHeight.Visible       = true;
                    lblHeightUnits.Visible = true;
                    spHeight.Value         = Metadata.Dimensions.Height;
                    lblWidth.Visible       = true;
                    spWidth.Visible        = true;
                    lblWidthUnits.Visible  = true;
                    spWidth.Value          = Metadata.Dimensions.Width;
                }

                lblThickness.Visible      = true;
                spThickness.Visible       = true;
                lblThicknessUnits.Visible = true;
                spThickness.Value         = Metadata.Dimensions.Thickness;
            }

            mediaCase = Metadata.Case;
            scans     = Metadata.Scans;

            if(Metadata.PFI != null)
            {
                frmPFI.Visible = true;
                lstPFI.AppendValues(Metadata.PFI.Image, Metadata.PFI.Size, Metadata.PFI.Checksums);
            }

            if(Metadata.DMI != null)
            {
                frmDMI.Visible = true;
                lstDMI.AppendValues(Metadata.DMI.Image, Metadata.DMI.Size, Metadata.DMI.Checksums);
            }

            if(Metadata.CMI != null)
            {
                frmCMI.Visible = true;
                lstCMI.AppendValues(Metadata.CMI.Image, Metadata.CMI.Size, Metadata.CMI.Checksums);
            }

            if(Metadata.BCA != null)
            {
                frmBCA.Visible = true;
                lstBCA.AppendValues(Metadata.BCA.Image, Metadata.BCA.Size, Metadata.BCA.Checksums);
            }

            if(Metadata.ATIP != null)
            {
                frmATIP.Visible = true;
                lstATIP.AppendValues(Metadata.ATIP.Image, Metadata.ATIP.Size, Metadata.ATIP.Checksums);
            }

            if(Metadata.ADIP != null)
            {
                frmADIP.Visible = true;
                lstADIP.AppendValues(Metadata.ADIP.Image, Metadata.ADIP.Size, Metadata.ADIP.Checksums);
            }

            if(Metadata.PMA != null)
            {
                frmPMA.Visible = true;
                lstPMA.AppendValues(Metadata.PMA.Image, Metadata.PMA.Size, Metadata.PMA.Checksums);
            }

            if(Metadata.DDS != null)
            {
                frmDDS.Visible = true;
                lstDDS.AppendValues(Metadata.DDS.Image, Metadata.DDS.Size, Metadata.DDS.Checksums);
            }

            if(Metadata.SAI != null)
            {
                frmSAI.Visible = true;
                lstSAI.AppendValues(Metadata.SAI.Image, Metadata.SAI.Size, Metadata.SAI.Checksums);
            }

            if(Metadata.LastRMD != null)
            {
                frmLastRMD.Visible = true;
                lstLastRMD.AppendValues(Metadata.LastRMD.Image, Metadata.LastRMD.Size, Metadata.LastRMD.Checksums);
            }

            if(Metadata.PRI != null)
            {
                frmPRI.Visible = true;
                lstPRI.AppendValues(Metadata.PRI.Image, Metadata.PRI.Size, Metadata.PRI.Checksums);
            }

            if(Metadata.MediaID != null)
            {
                frmMediaID.Visible = true;
                lstMediaID.AppendValues(Metadata.MediaID.Image, Metadata.MediaID.Size, Metadata.MediaID.Checksums);
            }

            if(Metadata.PFIR != null)
            {
                frmPFIR.Visible = true;
                lstPFIR.AppendValues(Metadata.PFIR.Image, Metadata.PFIR.Size, Metadata.PFIR.Checksums);
            }

            if(Metadata.DCB != null)
            {
                frmDCB.Visible = true;
                lstDCB.AppendValues(Metadata.DCB.Image, Metadata.DCB.Size, Metadata.DCB.Checksums);
            }

            if(Metadata.DI != null)
            {
                frmDI.Visible = true;
                lstDI.AppendValues(Metadata.DI.Image, Metadata.DI.Size, Metadata.DI.Checksums);
            }

            if(Metadata.PAC != null)
            {
                frmPAC.Visible = true;
                lstPAC.AppendValues(Metadata.PAC.Image, Metadata.PAC.Size, Metadata.PAC.Checksums);
            }

            if(Metadata.TOC != null)
            {
                frmTOC.Visible = true;
                lstTOC.AppendValues(Metadata.TOC.Image, Metadata.TOC.Size, Metadata.TOC.Checksums);
            }

            if(Metadata.LeadInCdText != null)
            {
                frmCDText.Visible = true;
                lstCDText.AppendValues(Metadata.LeadInCdText.Image, Metadata.LeadInCdText.Size,
                                       Metadata.LeadInCdText.Checksums);
            }

            if(Metadata.LeadIn != null)
            {
                frmLeadIns.Visible = true;
                foreach(BorderType leadin in Metadata.LeadIn)
                    lstLeadIns.AppendValues(leadin.Image, leadin.Size, leadin.session, leadin.Checksums);
            }

            if(Metadata.LeadOut != null)
            {
                frmLeadOuts.Visible = true;
                foreach(BorderType leadout in Metadata.LeadOut)
                    lstLeadOuts.AppendValues(leadout.Image, leadout.Size, leadout.session, leadout.Checksums);
            }

            if(Metadata.PS3Encryption != null)
            {
                txtPS3Key.Text    = Metadata.PS3Encryption.Key;
                txtPS3Serial.Text = Metadata.PS3Encryption.Serial;
            }

            foreach(TrackType track in Metadata.Track)
            {
                ListStore lstPartitions = new ListStore(typeof(int), typeof(int), typeof(int), typeof(string),
                                                        typeof(string), typeof(string), typeof(ListStore));

                if(track.FileSystemInformation != null)
                    foreach(PartitionType partition in track.FileSystemInformation)
                    {
                        ListStore lstFilesystems =
                            new ListStore(typeof(string), typeof(string), typeof(FileSystemType));
                        if(partition.FileSystems != null)
                            foreach(FileSystemType fs in partition.FileSystems)
                                lstFilesystems.AppendValues(fs.Type, fs.VolumeName, fs);
                        lstPartitions.AppendValues(partition.Sequence, partition.StartSector, partition.EndSector,
                                                   partition.Type, partition.Name, partition.Description,
                                                   lstFilesystems);
                    }

                lstTracks.AppendValues(track.Sequence.TrackNumber, track.Sequence.Session, track.Image.Value,
                                       track.Size, track.Image.format, track.Image.offset, track.StartMSF, track.EndMSF,
                                       track.StartSector, track.EndSector, track.TrackType1.ToString(),
                                       track.BytesPerSector, track.AccoustID, track.Checksums, track.SubChannel,
                                       lstPartitions);
            }

            if(Metadata.DumpHardwareArray != null)
            {
                chkDumpHardware.Active    = true;
                treeDumpHardware.Visible  = true;
                btnAddHardware.Visible    = true;
                btnRemoveHardware.Visible = true;
                btnEditHardware.Visible   = true;

                foreach(DumpHardwareType hw in Metadata.DumpHardwareArray)
                    if(hw.Extents != null)
                    {
                        ListStore lstExtents = new ListStore(typeof(ulong), typeof(ulong));
                        foreach(ExtentType extent in hw.Extents) lstExtents.AppendValues(extent.Start, extent.End);
                        if(hw.Software != null)
                            lstDumpHw.AppendValues(hw.Manufacturer, hw.Model, hw.Revision, hw.Firmware, hw.Serial,
                                                   hw.Software.Name, hw.Software.Version, hw.Software.OperatingSystem,
                                                   lstExtents);
                        else
                            lstDumpHw.AppendValues(hw.Manufacturer, hw.Model, hw.Revision, hw.Firmware, hw.Serial, null,
                                                   null, null, lstExtents);
                    }
            }
        }

        protected void OnChkSequencedToggled(object sender, EventArgs e)
        {
            lblDiscTitle.Visible  = chkSequence.Active;
            txtDiscTitle.Visible  = chkSequence.Active;
            lblSequence.Visible   = chkSequence.Active;
            spSequence.Visible    = chkSequence.Active;
            lblTotalMedia.Visible = chkSequence.Active;
            spTotalMedia.Visible  = chkSequence.Active;
            lblSide.Visible       = chkSequence.Active;
            spSide.Visible        = chkSequence.Active;
            lblLayer.Visible      = chkSequence.Active;
            spLayer1.Visible      = chkSequence.Active;
        }

        protected void OnChkDimensionsToggled(object sender, EventArgs e)
        {
            chkRound.Visible          = chkDimensions.Active;
            lblThickness.Visible      = chkDimensions.Active;
            spThickness.Visible       = chkDimensions.Active;
            lblThicknessUnits.Visible = chkDimensions.Active;
            if(chkDimensions.Active) OnChkRoundToggled(sender, e);
            else
            {
                lblDiameter.Visible      = false;
                spDiameter.Visible       = false;
                lblDiameterUnits.Visible = false;
                lblHeight.Visible        = false;
                spHeight.Visible         = false;
                lblHeightUnits.Visible   = false;
                lblWidth.Visible         = false;
                spWidth.Visible          = false;
                lblWidthUnits.Visible    = false;
            }
        }

        protected void OnChkRoundToggled(object sender, EventArgs e)
        {
            lblDiameter.Visible      = chkRound.Active;
            spDiameter.Visible       = chkRound.Active;
            lblDiameterUnits.Visible = chkRound.Active;
            lblHeight.Visible        = !chkRound.Active;
            spHeight.Visible         = !chkRound.Active;
            lblHeightUnits.Visible   = !chkRound.Active;
            lblWidth.Visible         = !chkRound.Active;
            spWidth.Visible          = !chkRound.Active;
            lblWidthUnits.Visible    = !chkRound.Active;
        }

        protected void OnChkLayersToggled(object sender, EventArgs e)
        {
            frmLayers.Visible = chkLayers.Active;
        }

        protected void OnBtnAddLayerClicked(object sender, EventArgs e)
        {
            MessageDialog dlgMsg;

            if(string.IsNullOrWhiteSpace(txtLayerSize.Text))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Layer size must not be empty");
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            if(!long.TryParse(txtLayerSize.Text, out long ltmp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Layer size must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            if(ltmp < 0)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Layer size must be a positive");
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            if(ltmp == 0)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Layer size must be bigger than 0");
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            lstLayers.AppendValues(spLayer1.ValueAsInt, long.Parse(txtLayerSize.Text));
        }

        protected void OnBtnRemoveLayerClicked(object sender, EventArgs e)
        {
            if(treeLayers.Selection.GetSelected(out TreeIter outIter)) lstLayers.Remove(ref outIter);
        }

        protected void OnBtnRemovePartitionClicked(object sender, EventArgs e)
        {
            if(treePartitions.Selection.GetSelected(out partitionIter))
                ((ListStore)treePartitions.Model).Remove(ref partitionIter);
        }

        protected void OnBtnEditPartitionClicked(object sender, EventArgs e)
        {
            if(!treePartitions.Selection.GetSelected(out partitionIter)) return;

            spPartitionSequence.Value = (int)((ListStore)treePartitions.Model).GetValue(partitionIter, 0);
            txtPartitionStart.Text    =
                ((int)((ListStore)treePartitions.Model).GetValue(partitionIter, 1)).ToString();
            txtPartitionEnd.Text =
                ((int)((ListStore)treePartitions.Model).GetValue(partitionIter,                                 2)).ToString();
            txtPartitionType.Text        = (string)((ListStore)treePartitions.Model).GetValue(partitionIter,    3);
            txtPartitionName.Text        = (string)((ListStore)treePartitions.Model).GetValue(partitionIter,    4);
            txtPartitionDescription.Text = (string)((ListStore)treePartitions.Model).GetValue(partitionIter,    5);
            treeFilesystems.Model        = (ListStore)((ListStore)treePartitions.Model).GetValue(partitionIter, 6);

            btnCancelPartition.Visible      = true;
            btnApplyPartition.Visible       = true;
            btnRemovePartition.Visible      = false;
            btnEditPartition.Visible        = false;
            btnAddPartition.Visible         = false;
            lblPartitionSequence.Visible    = true;
            spPartitionSequence.Visible     = true;
            lblPartitionStart.Visible       = true;
            txtPartitionStart.Visible       = true;
            lblPartitionEnd.Visible         = true;
            txtPartitionEnd.Visible         = true;
            lblPartitionType.Visible        = true;
            txtPartitionType.Visible        = true;
            lblPartitionName.Visible        = true;
            txtPartitionName.Visible        = true;
            lblPartitionDescription.Visible = true;
            txtPartitionDescription.Visible = true;
            frmFilesystems.Visible          = true;

            editingPartition = true;
        }

        protected void OnBtnApplyPartitionClicked(object sender, EventArgs e)
        {
            MessageDialog dlgMsg;

            if(spPartitionSequence.Value < 1)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Partition sequence must be bigger than 0");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(!int.TryParse(txtPartitionStart.Text, out int temp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Partition start must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(!int.TryParse(txtPartitionEnd.Text, out int temp2))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Partition end must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(temp2 <= temp)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Partition must end after start, and be bigger than 1 sector");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(editingPartition) ((ListStore)treePartitions.Model).Remove(ref partitionIter);

            ((ListStore)treePartitions.Model).AppendValues(spPartitionSequence.ValueAsInt,
                                                           int.Parse(txtPartitionStart.Text),
                                                           int.Parse(txtPartitionEnd.Text), txtPartitionType.Text,
                                                           txtPartitionName.Text, txtPartitionDescription.Text,
                                                           (ListStore)treeFilesystems.Model);

            btnCancelPartition.Visible      = false;
            btnApplyPartition.Visible       = false;
            btnRemovePartition.Visible      = true;
            btnEditPartition.Visible        = true;
            btnAddPartition.Visible         = true;
            lblPartitionSequence.Visible    = false;
            spPartitionSequence.Visible     = false;
            lblPartitionStart.Visible       = false;
            txtPartitionStart.Visible       = false;
            lblPartitionEnd.Visible         = false;
            txtPartitionEnd.Visible         = false;
            lblPartitionType.Visible        = false;
            txtPartitionType.Visible        = false;
            lblPartitionName.Visible        = false;
            txtPartitionName.Visible        = false;
            lblPartitionDescription.Visible = false;
            txtPartitionDescription.Visible = false;
            frmFilesystems.Visible          = false;
        }

        protected void OnBtnAddPartitionClicked(object sender, EventArgs e)
        {
            spPartitionSequence.Value    = 0;
            txtPartitionStart.Text       = "";
            txtPartitionEnd.Text         = "";
            txtPartitionType.Text        = "";
            txtPartitionName.Text        = "";
            txtPartitionDescription.Text = "";
            treeFilesystems.Model        = new ListStore(typeof(string), typeof(string), typeof(FileSystemType));

            btnCancelPartition.Visible      = true;
            btnApplyPartition.Visible       = true;
            btnRemovePartition.Visible      = false;
            btnEditPartition.Visible        = false;
            btnAddPartition.Visible         = false;
            lblPartitionSequence.Visible    = true;
            spPartitionSequence.Visible     = true;
            lblPartitionStart.Visible       = true;
            txtPartitionStart.Visible       = true;
            lblPartitionEnd.Visible         = true;
            txtPartitionEnd.Visible         = true;
            lblPartitionType.Visible        = true;
            txtPartitionType.Visible        = true;
            lblPartitionName.Visible        = true;
            txtPartitionName.Visible        = true;
            lblPartitionDescription.Visible = true;
            txtPartitionDescription.Visible = true;
            frmFilesystems.Visible          = true;

            editingPartition = false;
        }

        protected void OnBtnRemoveFilesystemClicked(object sender, EventArgs e)
        {
            if(treeFilesystems.Selection.GetSelected(out filesystemIter))
                ((ListStore)treeFilesystems.Model).Remove(ref filesystemIter);
        }

        protected void OnBtnEditFilesystemClicked(object sender, EventArgs e)
        {
            if(!treeFilesystems.Selection.GetSelected(out filesystemIter)) return;

            dlgFilesystem _dlgFilesystem = new dlgFilesystem
            {
                Metadata = (FileSystemType)((ListStore)treeFilesystems.Model).GetValue(filesystemIter, 2)
            };
            _dlgFilesystem.FillFields();

            if(_dlgFilesystem.Run() == (int)ResponseType.Ok)
            {
                ((ListStore)treeFilesystems.Model).Remove(ref filesystemIter);
                ((ListStore)treeFilesystems.Model).AppendValues(_dlgFilesystem.Metadata.Type,
                                                                _dlgFilesystem.Metadata.VolumeName,
                                                                _dlgFilesystem.Metadata);
            }

            _dlgFilesystem.Destroy();
        }

        protected void OnBtnAddFilesystemClicked(object sender, EventArgs e)
        {
            dlgFilesystem _dlgFilesystem = new dlgFilesystem();

            if(_dlgFilesystem.Run() == (int)ResponseType.Ok)
                ((ListStore)treeFilesystems.Model).AppendValues(_dlgFilesystem.Metadata.Type,
                                                                _dlgFilesystem.Metadata.VolumeName,
                                                                _dlgFilesystem.Metadata);

            _dlgFilesystem.Destroy();
        }

        protected void OnBtnCancelTrackClicked(object sender, EventArgs e)
        {
            btnEditTrack.Visible       = true;
            btnApplyTrack.Visible      = false;
            btnCancelTrack.Visible     = false;
            frmPartitions.Visible      = false;
            lblTrackStart.Visible      = false;
            txtTrackStart.Visible      = false;
            lblTrackEnd.Visible        = false;
            txtTrackEnd.Visible        = false;
            lblMSFStart.Visible        = false;
            txtMSFStart.Visible        = false;
            lblMSFEnd.Visible          = false;
            txtMSFEnd.Visible          = false;
            lblTrackSequence.Visible   = false;
            txtTrackSequence.Visible   = false;
            lblSessionSequence.Visible = false;
            txtSessionSequence.Visible = false;
            lblTrackType.Visible       = false;
            cmbTrackType.Visible       = false;
            txtBytesPerSector.Visible  = false;
            lblBytesPerSector.Visible  = false;
            lblAcoustID.Visible        = false;
            txtAcoustID.Visible        = false;
        }

        protected void OnBtnApplyTrackClicked(object sender, EventArgs e)
        {
            string         file       = (string)lstTracks.GetValue(trackIter,         2);
            long           filesize   = (long)lstTracks.GetValue(trackIter,           3);
            string         fileformat = (string)lstTracks.GetValue(trackIter,         4);
            long           fileoffset = (long)lstTracks.GetValue(trackIter,           5);
            ChecksumType[] checksums  = (ChecksumType[])lstTracks.GetValue(trackIter, 13);
            SubChannelType subchannel = (SubChannelType)lstTracks.GetValue(trackIter, 14);

            string trackType;
            if(cmbTrackType.GetActiveIter(out TreeIter typeIter))
                trackType  = (string)cmbTrackType.Model.GetValue(typeIter, 0);
            else trackType = TrackTypeTrackType.mode1.ToString();

            lstTracks.Remove(ref trackIter);
            lstTracks.AppendValues(int.Parse(txtTrackSequence.Text), int.Parse(txtSessionSequence.Text), file, filesize,
                                   fileformat, fileoffset, txtMSFStart.Text, txtMSFEnd.Text,
                                   long.Parse(txtTrackStart.Text), long.Parse(txtTrackEnd.Text), trackType,
                                   int.Parse(txtBytesPerSector.Text), txtAcoustID.Text, checksums, subchannel,
                                   (ListStore)treePartitions.Model);

            btnEditTrack.Visible       = true;
            btnApplyTrack.Visible      = false;
            btnCancelTrack.Visible     = false;
            frmPartitions.Visible      = false;
            lblTrackStart.Visible      = false;
            txtTrackStart.Visible      = false;
            lblTrackEnd.Visible        = false;
            txtTrackEnd.Visible        = false;
            lblMSFStart.Visible        = false;
            txtMSFStart.Visible        = false;
            lblMSFEnd.Visible          = false;
            txtMSFEnd.Visible          = false;
            lblTrackSequence.Visible   = false;
            txtTrackSequence.Visible   = false;
            lblSessionSequence.Visible = false;
            txtSessionSequence.Visible = false;
            lblTrackType.Visible       = false;
            cmbTrackType.Visible       = false;
            txtBytesPerSector.Visible  = false;
            lblBytesPerSector.Visible  = false;
            lblAcoustID.Visible        = false;
            txtAcoustID.Visible        = false;
        }

        protected void OnBtnEditTrackClicked(object sender, EventArgs e)
        {
            if(!treeTracks.Selection.GetSelected(out trackIter)) return;

            txtTrackSequence.Text   = ((int)lstTracks.GetValue(trackIter,      0)).ToString();
            txtSessionSequence.Text = ((int)lstTracks.GetValue(trackIter,      1)).ToString();
            txtMSFStart.Text        = (string)lstTracks.GetValue(trackIter,    6);
            txtMSFEnd.Text          = (string)lstTracks.GetValue(trackIter,    7);
            txtTrackStart.Text      = ((long)lstTracks.GetValue(trackIter,     8)).ToString();
            txtTrackEnd.Text        = ((long)lstTracks.GetValue(trackIter,     9)).ToString();
            string tracktype        = (string)lstTracks.GetValue(trackIter,    10);
            txtBytesPerSector.Text  = ((int)lstTracks.GetValue(trackIter,      11)).ToString();
            txtAcoustID.Text        = (string)lstTracks.GetValue(trackIter,    12);
            treePartitions.Model    = (ListStore)lstTracks.GetValue(trackIter, 15);

            cmbTrackType.Model.GetIterFirst(out TreeIter typeIter);
            do
            {
                if((string)cmbTrackType.Model.GetValue(typeIter, 0) != tracktype) continue;

                cmbTrackType.SetActiveIter(typeIter);
                break;
            }
            while(cmbTrackType.Model.IterNext(ref typeIter));

            btnEditTrack.Visible       = false;
            btnApplyTrack.Visible      = true;
            btnCancelTrack.Visible     = true;
            frmPartitions.Visible      = true;
            lblTrackStart.Visible      = true;
            txtTrackStart.Visible      = true;
            lblTrackEnd.Visible        = true;
            txtTrackEnd.Visible        = true;
            lblMSFStart.Visible        = true;
            txtMSFStart.Visible        = true;
            lblMSFEnd.Visible          = true;
            txtMSFEnd.Visible          = true;
            lblTrackSequence.Visible   = true;
            txtTrackSequence.Visible   = true;
            lblSessionSequence.Visible = true;
            txtSessionSequence.Visible = true;
            lblTrackType.Visible       = true;
            cmbTrackType.Visible       = true;
            txtBytesPerSector.Visible  = true;
            lblBytesPerSector.Visible  = true;
            lblAcoustID.Visible        = true;
            txtAcoustID.Visible        = true;
        }

        protected void OnChkKnownDumpHWToggled(object sender, EventArgs e)
        {
            treeDumpHardware.Visible  = chkDumpHardware.Active;
            btnAddHardware.Visible    = chkDumpHardware.Active;
            btnRemoveHardware.Visible = chkDumpHardware.Active;
            btnEditHardware.Visible   = chkDumpHardware.Active;

            btnCancelHardware.Visible = false;
            btnApplyHardware.Visible  = false;
            lblHWManufacturer.Visible = false;
            txtHWManufacturer.Visible = false;
            lblHWModel.Visible        = false;
            txtHWModel.Visible        = false;
            lblHWRevision.Visible     = false;
            txtHWRevision.Visible     = false;
            lblHWFirmware.Visible     = false;
            txtHWFirmware.Visible     = false;
            lblHWSerial.Visible       = false;
            txtHWSerial.Visible       = false;
            frmExtents.Visible        = false;
            frmDumpSoftware.Visible   = false;
        }

        protected void OnBtnCancelDumpHWClicked(object sender, EventArgs e)
        {
            btnAddHardware.Visible    = true;
            btnRemoveHardware.Visible = true;
            btnCancelHardware.Visible = false;
            btnEditHardware.Visible   = true;
            btnApplyHardware.Visible  = false;
            lblHWManufacturer.Visible = false;
            txtHWManufacturer.Visible = false;
            lblHWModel.Visible        = false;
            txtHWModel.Visible        = false;
            lblHWRevision.Visible     = false;
            txtHWRevision.Visible     = false;
            lblHWFirmware.Visible     = false;
            txtHWFirmware.Visible     = false;
            lblHWSerial.Visible       = false;
            txtHWSerial.Visible       = false;
            frmExtents.Visible        = false;
            frmDumpSoftware.Visible   = false;
        }

        protected void OnBtnRemoveDumpHWClicked(object sender, EventArgs e)
        {
            if(treeDumpHardware.Selection.GetSelected(out TreeIter dumphwIter)) lstDumpHw.Remove(ref dumphwIter);
        }

        protected void OnBtnApplyDumpHWClicked(object sender, EventArgs e)
        {
            if(editingDumpHw) lstDumpHw.Remove(ref dumpHwIter);

            lstDumpHw.AppendValues(txtHWManufacturer.Text, txtHWModel.Text, txtHWRevision.Text, txtHWFirmware.Text,
                                   txtHWSerial.Text, txtSoftwareName.Text, txtSoftwareVersion.Text, txtSoftwareOS.Text,
                                   (ListStore)treeExtents.Model);

            btnAddHardware.Visible    = true;
            btnRemoveHardware.Visible = true;
            btnCancelHardware.Visible = false;
            btnEditHardware.Visible   = true;
            btnApplyHardware.Visible  = false;
            lblHWManufacturer.Visible = false;
            txtHWManufacturer.Visible = false;
            lblHWModel.Visible        = false;
            txtHWModel.Visible        = false;
            lblHWRevision.Visible     = false;
            txtHWRevision.Visible     = false;
            lblHWFirmware.Visible     = false;
            txtHWFirmware.Visible     = false;
            lblHWSerial.Visible       = false;
            txtHWSerial.Visible       = false;
            frmExtents.Visible        = false;
            frmDumpSoftware.Visible   = false;
        }

        protected void OnBtnAddDumpHWClicked(object sender, EventArgs e)
        {
            txtHWManufacturer.Text  = "";
            txtHWModel.Text         = "";
            txtHWRevision.Text      = "";
            txtHWFirmware.Text      = "";
            txtHWSerial.Text        = "";
            txtSoftwareName.Text    = "";
            txtSoftwareVersion.Text = "";
            txtSoftwareOS.Text      = "";
            treeExtents.Model       = new ListStore(typeof(int), typeof(int));

            btnAddHardware.Visible    = false;
            btnRemoveHardware.Visible = false;
            btnCancelHardware.Visible = true;
            btnEditHardware.Visible   = false;
            btnApplyHardware.Visible  = true;
            lblHWManufacturer.Visible = true;
            txtHWManufacturer.Visible = true;
            lblHWModel.Visible        = true;
            txtHWModel.Visible        = true;
            lblHWRevision.Visible     = true;
            txtHWRevision.Visible     = true;
            lblHWFirmware.Visible     = true;
            txtHWFirmware.Visible     = true;
            lblHWSerial.Visible       = true;
            txtHWSerial.Visible       = true;
            frmExtents.Visible        = true;
            frmDumpSoftware.Visible   = true;

            editingDumpHw = false;
        }

        protected void OnBtnRemoveExtentClicked(object sender, EventArgs e)
        {
            if(treeExtents.Selection.GetSelected(out TreeIter extentIter))
                ((ListStore)treeExtents.Model).Remove(ref extentIter);
        }

        protected void OnBtnEditDumpHWClicked(object sender, EventArgs e)
        {
            if(!treeDumpHardware.Selection.GetSelected(out dumpHwIter)) return;

            txtHWManufacturer.Text  = (string)lstDumpHw.GetValue(dumpHwIter,    0);
            txtHWModel.Text         = (string)lstDumpHw.GetValue(dumpHwIter,    1);
            txtHWRevision.Text      = (string)lstDumpHw.GetValue(dumpHwIter,    2);
            txtHWFirmware.Text      = (string)lstDumpHw.GetValue(dumpHwIter,    3);
            txtHWSerial.Text        = (string)lstDumpHw.GetValue(dumpHwIter,    4);
            txtSoftwareName.Text    = (string)lstDumpHw.GetValue(dumpHwIter,    5);
            txtSoftwareVersion.Text = (string)lstDumpHw.GetValue(dumpHwIter,    6);
            txtSoftwareOS.Text      = (string)lstDumpHw.GetValue(dumpHwIter,    7);
            treeExtents.Model       = (ListStore)lstDumpHw.GetValue(dumpHwIter, 8);

            btnAddHardware.Visible    = false;
            btnRemoveHardware.Visible = false;
            btnCancelHardware.Visible = true;
            btnEditHardware.Visible   = false;
            btnApplyHardware.Visible  = true;
            lblHWManufacturer.Visible = true;
            txtHWManufacturer.Visible = true;
            lblHWModel.Visible        = true;
            txtHWModel.Visible        = true;
            lblHWRevision.Visible     = true;
            txtHWRevision.Visible     = true;
            lblHWFirmware.Visible     = true;
            txtHWFirmware.Visible     = true;
            lblHWSerial.Visible       = true;
            txtHWSerial.Visible       = true;
            frmExtents.Visible        = true;
            frmDumpSoftware.Visible   = true;

            editingDumpHw = true;
        }

        protected void OnBtnAddExtentClicked(object sender, EventArgs e)
        {
            ((ListStore)treeExtents.Model).AppendValues(spExtentStart.ValueAsInt, spExtentEnd.ValueAsInt);
        }

        protected void OnBtnAddRingCodeClicked(object sender, EventArgs e)
        {
            lstRingCodes.AppendValues(spRingCodeLayer.ValueAsInt, txtRingCode.Text);
        }

        protected void OnBtnRemoveRingCodeClicked(object sender, EventArgs e)
        {
            if(treeRingCodes.Selection.GetSelected(out TreeIter outIter)) lstRingCodes.Remove(ref outIter);
        }

        protected void OnBtnAddMasteringSIDClicked(object sender, EventArgs e)
        {
            lstMasteringSIDs.AppendValues(spMasteringSIDLayer.ValueAsInt, txtMasteringSID.Text);
        }

        protected void OnBtnRemoveMasteringSIDClicked(object sender, EventArgs e)
        {
            if(treeMasteringSIDs.Selection.GetSelected(out TreeIter outIter)) lstMasteringSIDs.Remove(ref outIter);
        }

        protected void OnBtnAddToolstampClicked(object sender, EventArgs e)
        {
            lstToolstamps.AppendValues(spToolstampLayer.ValueAsInt, txtToolstamp.Text);
        }

        protected void OnBtnRemoveToolstampClicked(object sender, EventArgs e)
        {
            if(treeToolstamps.Selection.GetSelected(out TreeIter outIter)) lstToolstamps.Remove(ref outIter);
        }

        protected void OnBtnAddMouldSIDClicked(object sender, EventArgs e)
        {
            lstMouldSIDs.AppendValues(spMouldSIDLayer.ValueAsInt, txtMouldSID.Text);
        }

        protected void OnBtnRemoveMouldSIDClicked(object sender, EventArgs e)
        {
            if(treeMouldSIDs.Selection.GetSelected(out TreeIter outIter)) lstMouldSIDs.Remove(ref outIter);
        }

        protected void OnBtnAddMouldTextClicked(object sender, EventArgs e)
        {
            lstMouldTexts.AppendValues(spMouldTextLayer.ValueAsInt, txtMouldText.Text);
        }

        protected void OnBtnRemoveMouldTextClicked(object sender, EventArgs e)
        {
            if(treeMouldTexts.Selection.GetSelected(out TreeIter outIter)) lstMouldTexts.Remove(ref outIter);
        }

        protected void OnButtonOkClicked(object sender, EventArgs e) { }

        protected void OnBtnSaveClicked(object sender, EventArgs e)
        {
            MessageDialog dlgMsg;

            #region Sanity checks
            if(string.IsNullOrEmpty(txtFormat.Text))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Image format cannot be null");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(chkSequence.Active)
            {
                if(spSequence.Value < 1)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                               "Media sequence must be bigger than 0");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                if(spTotalMedia.Value < 1)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                               "Total medias must be bigger than 0");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                if(spSequence.Value > spTotalMedia.Value)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                               "Media sequence cannot be bigger than total medias");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
            }

            if(!string.IsNullOrEmpty(txtWriteOffset.Text) && !long.TryParse(txtWriteOffset.Text, out long ltmp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Write offset must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(string.IsNullOrEmpty(txtMediaTracks.Text) || !long.TryParse(txtMediaTracks.Text, out ltmp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Tracks must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(ltmp < 1)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Tracks must be bigger than 0");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(string.IsNullOrEmpty(txtMediaSessions.Text) || !long.TryParse(txtMediaSessions.Text, out ltmp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Sessions must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(ltmp < 1)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                           "Sessions must be bigger than 0");
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
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                                   "Diameter must be bigger than 0");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }
                else
                {
                    if(spHeight.ValueAsInt <= 0)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                                   "Height must be bigger than 0");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }

                    if(spWidth.ValueAsInt <= 0)
                    {
                        dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                                   "Width must be bigger than 0");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        return;
                    }
                }

                if(spThickness.ValueAsInt <= 0)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                               "Thickness must be bigger than 0");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
            }

            if(chkDumpHardware.Active)
                if(lstDumpHw.IterNChildren() < 1)
                {
                    dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                               "If dump hardware is known at least an entry must be created");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }
            #endregion Sanity checks

            TreeIter outIter;
            Metadata = new OpticalDiscType();

            Metadata.Image        = new ImageType();
            Metadata.Image.Value  = txtImage.Text;
            Metadata.Image.format = txtFormat.Text;
            if(!string.IsNullOrWhiteSpace(txtOffset.Text) && long.TryParse(txtOffset.Text, out ltmp))
            {
                Metadata.Image.offsetSpecified = true;
                Metadata.Image.offset          = long.Parse(txtOffset.Text);
            }

            Metadata.Size = long.Parse(txtSize.Text);

            if(chkSequence.Active)
            {
                Metadata.Sequence = new SequenceType
                {
                    MediaTitle    = txtDiscTitle.Text,
                    MediaSequence = spSequence.ValueAsInt,
                    TotalMedia    = spTotalMedia.ValueAsInt
                };
                if(spSide.ValueAsInt > 0)
                {
                    Metadata.Sequence.SideSpecified = true;
                    Metadata.Sequence.Side          = spSide.ValueAsInt;
                }

                if(spLayer1.ValueAsInt > 0)
                {
                    Metadata.Sequence.LayerSpecified = true;
                    Metadata.Sequence.Layer          = spLayer1.ValueAsInt;
                }
            }

            if(lstLayers.IterNChildren() > 0)
            {
                Metadata.Layers = new LayersType();
                if(cmbLayerType.GetActiveIter(out outIter))
                {
                    Metadata.Layers.type =
                        (LayersTypeType)Enum.Parse(typeof(LayersTypeType), (string)lstLayerTypes.GetValue(outIter, 0));
                    Metadata.Layers.typeSpecified = true;
                }

                if(lstLayers.GetIterFirst(out outIter))
                {
                    List<SectorsType> sectors = new List<SectorsType>();
                    do
                    {
                        SectorsType sector = new SectorsType
                        {
                            layer = (int)lstLayers.GetValue(outIter,  0),
                            Value = (long)lstLayers.GetValue(outIter, 1)
                        };

                        if(sector.layer > 0 || lstLayers.IterNChildren() > 1) sector.layerSpecified = true;

                        sectors.Add(sector);
                    }
                    while(lstLayers.IterNext(ref outIter));

                    Metadata.Layers.Sectors = sectors.ToArray();
                }
            }

            Metadata.Checksums = checksums;
            Metadata.Xbox      = xbox;

            if(lstRingCodes.GetIterFirst(out outIter))
            {
                List<LayeredTextType> codes = new List<LayeredTextType>();
                do
                {
                    LayeredTextType code = new LayeredTextType
                    {
                        layer = (int)lstRingCodes.GetValue(outIter,    0),
                        Value = (string)lstRingCodes.GetValue(outIter, 1)
                    };

                    if(code.layer > 0 || lstLayers.IterNChildren() > 1) code.layerSpecified = true;

                    codes.Add(code);
                }
                while(lstRingCodes.IterNext(ref outIter));

                Metadata.RingCode = codes.ToArray();
            }

            if(lstMasteringSIDs.GetIterFirst(out outIter))
            {
                List<LayeredTextType> codes = new List<LayeredTextType>();
                do
                {
                    LayeredTextType code = new LayeredTextType
                    {
                        layer = (int)lstMasteringSIDs.GetValue(outIter,    0),
                        Value = (string)lstMasteringSIDs.GetValue(outIter, 1)
                    };

                    if(code.layer > 0 || lstLayers.IterNChildren() > 1) code.layerSpecified = true;

                    codes.Add(code);
                }
                while(lstMasteringSIDs.IterNext(ref outIter));

                Metadata.MasteringSID = codes.ToArray();
            }

            if(lstToolstamps.GetIterFirst(out outIter))
            {
                List<LayeredTextType> codes = new List<LayeredTextType>();
                do
                {
                    LayeredTextType code = new LayeredTextType
                    {
                        layer = (int)lstToolstamps.GetValue(outIter,    0),
                        Value = (string)lstToolstamps.GetValue(outIter, 1)
                    };

                    if(code.layer > 0 || lstLayers.IterNChildren() > 1) code.layerSpecified = true;

                    codes.Add(code);
                }
                while(lstToolstamps.IterNext(ref outIter));

                Metadata.Toolstamp = codes.ToArray();
            }

            if(lstMouldSIDs.GetIterFirst(out outIter))
            {
                List<LayeredTextType> codes = new List<LayeredTextType>();
                do
                {
                    LayeredTextType code = new LayeredTextType
                    {
                        layer = (int)lstMouldSIDs.GetValue(outIter,    0),
                        Value = (string)lstMouldSIDs.GetValue(outIter, 1)
                    };

                    if(code.layer > 0 || lstLayers.IterNChildren() > 1) code.layerSpecified = true;

                    codes.Add(code);
                }
                while(lstMouldSIDs.IterNext(ref outIter));

                Metadata.MouldSID = codes.ToArray();
            }

            if(lstMouldTexts.GetIterFirst(out outIter))
            {
                List<LayeredTextType> codes = new List<LayeredTextType>();
                do
                {
                    LayeredTextType code = new LayeredTextType
                    {
                        layer = (int)lstMouldTexts.GetValue(outIter,    0),
                        Value = (string)lstMouldTexts.GetValue(outIter, 1)
                    };

                    if(code.layer > 0 || lstLayers.IterNChildren() > 1) code.layerSpecified = true;

                    codes.Add(code);
                }
                while(lstMouldTexts.IterNext(ref outIter));

                Metadata.MouldText = codes.ToArray();
            }

            if(!string.IsNullOrWhiteSpace(txtDiscType.Text)) Metadata.DiscType       = txtDiscType.Text;
            if(!string.IsNullOrWhiteSpace(txtDiscSubType.Text)) Metadata.DiscSubType = txtDiscSubType.Text;
            if(!string.IsNullOrWhiteSpace(txtWriteOffset.Text))
            {
                Metadata.Offset          = int.Parse(txtWriteOffset.Text);
                Metadata.OffsetSpecified = true;
            }

            Metadata.Tracks = !string.IsNullOrWhiteSpace(txtMediaTracks.Text)
                                  ? new[] {int.Parse(txtMediaTracks.Text)}
                                  : new[] {1};

            Metadata.Sessions =
                !string.IsNullOrWhiteSpace(txtMediaSessions.Text) ? int.Parse(txtMediaSessions.Text) : 1;

            if(!string.IsNullOrWhiteSpace(txtCopyProtection.Text)) Metadata.CopyProtection = txtCopyProtection.Text;

            if(chkDimensions.Active)
            {
                Metadata.Dimensions = new DimensionsType();
                if(chkRound.Active)
                {
                    Metadata.Dimensions.DiameterSpecified = true;
                    Metadata.Dimensions.Diameter          = spDiameter.Value;
                }
                else
                {
                    Metadata.Dimensions.HeightSpecified = true;
                    Metadata.Dimensions.WidthSpecified  = true;
                    Metadata.Dimensions.Height          = spHeight.Value;
                    Metadata.Dimensions.Width           = spWidth.Value;
                }

                Metadata.Dimensions.Thickness = spThickness.Value;
            }

            Metadata.Case  = mediaCase;
            Metadata.Scans = scans;

            if(lstPFI.GetIterFirst(out outIter))
                Metadata.PFI = new DumpType
                {
                    Image     = (string)lstPFI.GetValue(outIter,         0),
                    Size      = (int)lstPFI.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstPFI.GetValue(outIter, 2)
                };
            if(lstDMI.GetIterFirst(out outIter))
                Metadata.DMI = new DumpType
                {
                    Image     = (string)lstDMI.GetValue(outIter,         0),
                    Size      = (int)lstDMI.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstDMI.GetValue(outIter, 2)
                };
            if(lstCMI.GetIterFirst(out outIter))
                Metadata.CMI = new DumpType
                {
                    Image     = (string)lstCMI.GetValue(outIter,         0),
                    Size      = (int)lstCMI.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstCMI.GetValue(outIter, 2)
                };
            if(lstBCA.GetIterFirst(out outIter))
                Metadata.BCA = new DumpType
                {
                    Image     = (string)lstBCA.GetValue(outIter,         0),
                    Size      = (int)lstBCA.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstBCA.GetValue(outIter, 2)
                };
            if(lstATIP.GetIterFirst(out outIter))
                Metadata.ATIP = new DumpType
                {
                    Image     = (string)lstATIP.GetValue(outIter,         0),
                    Size      = (int)lstATIP.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstATIP.GetValue(outIter, 2)
                };
            if(lstADIP.GetIterFirst(out outIter))
                Metadata.ADIP = new DumpType
                {
                    Image     = (string)lstADIP.GetValue(outIter,         0),
                    Size      = (int)lstADIP.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstADIP.GetValue(outIter, 2)
                };
            if(lstPMA.GetIterFirst(out outIter))
                Metadata.PMA = new DumpType
                {
                    Image     = (string)lstPMA.GetValue(outIter,         0),
                    Size      = (int)lstPMA.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstPMA.GetValue(outIter, 2)
                };
            if(lstDDS.GetIterFirst(out outIter))
                Metadata.DDS = new DumpType
                {
                    Image     = (string)lstDDS.GetValue(outIter,         0),
                    Size      = (int)lstDDS.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstDDS.GetValue(outIter, 2)
                };
            if(lstSAI.GetIterFirst(out outIter))
                Metadata.SAI = new DumpType
                {
                    Image     = (string)lstSAI.GetValue(outIter,         0),
                    Size      = (int)lstSAI.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstSAI.GetValue(outIter, 2)
                };
            if(lstLastRMD.GetIterFirst(out outIter))
                Metadata.LastRMD = new DumpType
                {
                    Image     = (string)lstLastRMD.GetValue(outIter,         0),
                    Size      = (int)lstLastRMD.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstLastRMD.GetValue(outIter, 2)
                };
            if(lstPRI.GetIterFirst(out outIter))
                Metadata.PRI = new DumpType
                {
                    Image     = (string)lstPRI.GetValue(outIter,         0),
                    Size      = (int)lstPRI.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstPRI.GetValue(outIter, 2)
                };
            if(lstMediaID.GetIterFirst(out outIter))
                Metadata.MediaID = new DumpType
                {
                    Image     = (string)lstMediaID.GetValue(outIter,         0),
                    Size      = (int)lstMediaID.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstMediaID.GetValue(outIter, 2)
                };
            if(lstPFIR.GetIterFirst(out outIter))
                Metadata.PFIR = new DumpType
                {
                    Image     = (string)lstPFIR.GetValue(outIter,         0),
                    Size      = (int)lstPFIR.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstPFIR.GetValue(outIter, 2)
                };
            if(lstDCB.GetIterFirst(out outIter))
                Metadata.DCB = new DumpType
                {
                    Image     = (string)lstDCB.GetValue(outIter,         0),
                    Size      = (int)lstDCB.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstDCB.GetValue(outIter, 2)
                };
            if(lstDI.GetIterFirst(out outIter))
                Metadata.DI = new DumpType
                {
                    Image     = (string)lstDI.GetValue(outIter,         0),
                    Size      = (int)lstDI.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstDI.GetValue(outIter, 2)
                };
            if(lstPAC.GetIterFirst(out outIter))
                Metadata.PAC = new DumpType
                {
                    Image     = (string)lstPAC.GetValue(outIter,         0),
                    Size      = (int)lstPAC.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstPAC.GetValue(outIter, 2)
                };
            if(lstTOC.GetIterFirst(out outIter))
                Metadata.TOC = new DumpType
                {
                    Image     = (string)lstTOC.GetValue(outIter,         0),
                    Size      = (int)lstTOC.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstTOC.GetValue(outIter, 2)
                };
            if(lstCDText.GetIterFirst(out outIter))
                Metadata.LeadInCdText = new DumpType
                {
                    Image     = (string)lstCDText.GetValue(outIter,         0),
                    Size      = (int)lstCDText.GetValue(outIter,            1),
                    Checksums = (ChecksumType[])lstCDText.GetValue(outIter, 2)
                };

            if(lstLeadIns.GetIterFirst(out outIter))
            {
                List<BorderType> leadins = new List<BorderType>();
                do
                {
                    BorderType leadin = new BorderType
                    {
                        Image     = (string)lstLeadIns.GetValue(outIter,         0),
                        Size      = (long)lstLeadIns.GetValue(outIter,           1),
                        session   = (int)lstLeadIns.GetValue(outIter,            2),
                        Checksums = (ChecksumType[])lstLeadIns.GetValue(outIter, 3)
                    };

                    if(leadin.session > Metadata.Sessions) Metadata.Sessions                = leadin.session;
                    if(leadin.session > 0 || Metadata.Sessions > 0) leadin.sessionSpecified = true;

                    leadins.Add(leadin);
                }
                while(lstLeadIns.IterNext(ref outIter));

                Metadata.LeadIn = leadins.ToArray();
            }

            if(lstLeadOuts.GetIterFirst(out outIter))
            {
                List<BorderType> leadouts = new List<BorderType>();
                do
                {
                    BorderType leadout = new BorderType
                    {
                        Image     = (string)lstLeadOuts.GetValue(outIter,         0),
                        Size      = (long)lstLeadOuts.GetValue(outIter,           1),
                        session   = (int)lstLeadOuts.GetValue(outIter,            2),
                        Checksums = (ChecksumType[])lstLeadOuts.GetValue(outIter, 3)
                    };

                    if(leadout.session > Metadata.Sessions) Metadata.Sessions                 = leadout.session;
                    if(leadout.session > 0 || Metadata.Sessions > 0) leadout.sessionSpecified = true;

                    leadouts.Add(leadout);
                }
                while(lstLeadOuts.IterNext(ref outIter));

                Metadata.LeadOut = leadouts.ToArray();
            }

            if(!string.IsNullOrWhiteSpace(txtPS3Key.Text) && !string.IsNullOrWhiteSpace(txtPS3Serial.Text))
                Metadata.PS3Encryption = new PS3EncryptionType {Key = txtPS3Key.Text, Serial = txtPS3Serial.Text};

            if(lstTracks.GetIterFirst(out outIter))
            {
                List<TrackType> tracks = new List<TrackType>();

                do
                {
                    TrackType track = new TrackType
                    {
                        Sequence =
                            new TrackSequenceType
                            {
                                TrackNumber = (int)lstTracks.GetValue(outIter, 0),
                                Session     = (int)lstTracks.GetValue(outIter, 1)
                            },
                        Image =
                            new ImageType
                            {
                                Value  = (string)lstTracks.GetValue(outIter, 2),
                                format = (string)lstTracks.GetValue(outIter, 4),
                                offset = (long)lstTracks.GetValue(outIter,   5)
                            },
                        Size        = (long)lstTracks.GetValue(outIter,   3),
                        StartMSF    = (string)lstTracks.GetValue(outIter, 6),
                        EndMSF      = (string)lstTracks.GetValue(outIter, 7),
                        StartSector = (long)lstTracks.GetValue(outIter,   8),
                        EndSector   = (long)lstTracks.GetValue(outIter,   9),
                        TrackType1  =
                            (TrackTypeTrackType)Enum.Parse(typeof(TrackTypeTrackType),
                                                           (string)lstTracks.GetValue(outIter, 10)),
                        BytesPerSector = (int)lstTracks.GetValue(outIter,                      11),
                        AccoustID      = (string)lstTracks.GetValue(outIter,                   12),
                        Checksums      = (ChecksumType[])lstTracks.GetValue(outIter,           13),
                        SubChannel     = (SubChannelType)lstTracks.GetValue(outIter,           14)
                    };
                    ListStore lstPartitions                                = (ListStore)lstTracks.GetValue(outIter, 15);
                    if(track.Image.offset > 0) track.Image.offsetSpecified = true;

                    if(lstPartitions.GetIterFirst(out TreeIter partIter))
                    {
                        List<PartitionType> partitions = new List<PartitionType>();
                        do
                        {
                            PartitionType partition = new PartitionType
                            {
                                Sequence    = (int)lstPartitions.GetValue(partIter,    0),
                                StartSector = (int)lstPartitions.GetValue(partIter,    1),
                                EndSector   = (int)lstPartitions.GetValue(partIter,    2),
                                Type        = (string)lstPartitions.GetValue(partIter, 3),
                                Name        = (string)lstPartitions.GetValue(partIter, 4),
                                Description = (string)lstPartitions.GetValue(partIter, 5)
                            };
                            ListStore lstFilesystems = (ListStore)lstPartitions.GetValue(partIter, 6);

                            if(lstFilesystems.GetIterFirst(out TreeIter fsIter))
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
                        while(lstPartitions.IterNext(ref partIter));

                        track.FileSystemInformation = partitions.ToArray();
                    }

                    tracks.Add(track);
                }
                while(lstTracks.IterNext(ref outIter));

                Metadata.Track = tracks.ToArray();
            }

            if(chkDumpHardware.Active && lstDumpHw.GetIterFirst(out outIter))
            {
                List<DumpHardwareType> dumps = new List<DumpHardwareType>();
                do
                {
                    DumpHardwareType dump = new DumpHardwareType
                    {
                        Manufacturer = (string)lstDumpHw.GetValue(outIter, 0),
                        Model        = (string)lstDumpHw.GetValue(outIter, 1),
                        Revision     = (string)lstDumpHw.GetValue(outIter, 2),
                        Firmware     = (string)lstDumpHw.GetValue(outIter, 3),
                        Serial       = (string)lstDumpHw.GetValue(outIter, 4),
                        Software     = new SoftwareType
                        {
                            Name            = (string)lstDumpHw.GetValue(outIter, 5),
                            Version         = (string)lstDumpHw.GetValue(outIter, 6),
                            OperatingSystem = (string)lstDumpHw.GetValue(outIter, 7)
                        }
                    };

                    ListStore lstExtents = (ListStore)lstDumpHw.GetValue(outIter, 8);

                    if(lstExtents.GetIterFirst(out TreeIter extIter))
                    {
                        List<ExtentType> extents = new List<ExtentType>();
                        do
                        {
                            ExtentType extent = new ExtentType
                            {
                                Start = (ulong)lstExtents.GetValue(extIter, 0),
                                End   = (ulong)lstExtents.GetValue(extIter, 1)
                            };
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

            buttonOk.Click();
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            buttonCancel.Click();
        }

        protected void OnBtnCancelPartitionClicked(object sender, EventArgs e)
        {
            btnCancelPartition.Visible      = false;
            btnApplyPartition.Visible       = false;
            btnRemovePartition.Visible      = true;
            btnEditPartition.Visible        = true;
            btnAddPartition.Visible         = true;
            lblPartitionSequence.Visible    = false;
            spPartitionSequence.Visible     = false;
            lblPartitionStart.Visible       = false;
            txtPartitionStart.Visible       = false;
            lblPartitionEnd.Visible         = false;
            txtPartitionEnd.Visible         = false;
            lblPartitionType.Visible        = false;
            txtPartitionType.Visible        = false;
            lblPartitionName.Visible        = false;
            txtPartitionName.Visible        = false;
            lblPartitionDescription.Visible = false;
            txtPartitionDescription.Visible = false;
            frmFilesystems.Visible          = false;
        }
    }
}