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
using System.Threading;
using Gtk;
using osrepodbmgr.Core;
using Schemas;

namespace osrepodbmgr
{
    public partial class dlgMetadata : Dialog
    {
        public CICMMetadataType Metadata;

        ListStore lstKeywords;
        ListStore lstBarcodes;
        ListStore lstCategories;
        ListStore lstSubcategories;
        ListStore lstLanguages;
        ListStore lstSystems;
        ListStore lstArchitectures;
        ListStore lstDiscs;
        ListStore lstDisks;

        ListStore lstReleaseTypes;
        ListStore lstBarcodeTypes;
        ListStore lstLanguageTypes;
        ListStore lstArchitecturesTypes;
        ListStore lstFilesForDisc;
        ListStore lstFilesForDisk;

        Thread thdDisc;
        Thread thdDisk;
        bool stopped;

        // TODO: Add the options to edit these fields
        MagazineType[] magazines;
        BookType[] books;
        RequiredOperatingSystemType[] requiredOses;
        UserManualType[] usermanuals;
        AdvertisementType[] adverts;
        LinearMediaType[] linearmedias;
        PCIType[] pcis;
        AudioMediaType[] audiomedias;

        public dlgMetadata()
        {
            Build();

            lstKeywords = new ListStore(typeof(string));
            lstBarcodes = new ListStore(typeof(string), typeof(string));
            lstCategories = new ListStore(typeof(string));
            lstSubcategories = new ListStore(typeof(string));
            lstLanguages = new ListStore(typeof(string));
            lstSystems = new ListStore(typeof(string));
            lstArchitectures = new ListStore(typeof(string));
            lstDiscs = new ListStore(typeof(string), typeof(OpticalDiscType));
            lstDisks = new ListStore(typeof(string), typeof(BlockMediaType));

            CellRendererText keywordsCell = new CellRendererText();
            CellRendererText barcodesCell = new CellRendererText();
            CellRendererText barcodesTypeCell = new CellRendererText();
            CellRendererText categoriesCell = new CellRendererText();
            CellRendererText subcategoriesCell = new CellRendererText();
            CellRendererText languagesCell = new CellRendererText();
            CellRendererText systemsCell = new CellRendererText();
            CellRendererText architecturesCell = new CellRendererText();
            CellRendererText discsCell = new CellRendererText();
            CellRendererText disksCell = new CellRendererText();

            TreeViewColumn keywordsColumn = new TreeViewColumn("Keyword", keywordsCell, "text", 0);
            TreeViewColumn barcodesColumn = new TreeViewColumn("Barcode", barcodesCell, "text", 0);
            TreeViewColumn barcodesTypeColumn = new TreeViewColumn("Type", barcodesTypeCell, "text", 1);
            TreeViewColumn categoriesColumn = new TreeViewColumn("Category", categoriesCell, "text", 0);
            TreeViewColumn subcategoriesColumn = new TreeViewColumn("Subcategory", subcategoriesCell, "text", 0);
            TreeViewColumn languagesColumn = new TreeViewColumn("Language", languagesCell, "text", 0);
            TreeViewColumn systemsColumn = new TreeViewColumn("System", systemsCell, "text", 0);
            TreeViewColumn architecturesColumn = new TreeViewColumn("Architecture", architecturesCell, "text", 0);
            TreeViewColumn discsColumn = new TreeViewColumn("File", discsCell, "text", 0);
            TreeViewColumn disksColumn = new TreeViewColumn("File", disksCell, "text", 0);

            treeKeywords.Model = lstKeywords;
            treeBarcodes.Model = lstBarcodes;
            treeCategories.Model = lstCategories;
            treeSubcategories.Model = lstSubcategories;
            treeLanguages.Model = lstLanguages;
            treeSystems.Model = lstSystems;
            treeArchitectures.Model = lstArchitectures;
            treeDiscs.Model = lstDiscs;
            treeDisks.Model = lstDisks;

            treeKeywords.AppendColumn(keywordsColumn);
            treeBarcodes.AppendColumn(barcodesColumn);
            treeBarcodes.AppendColumn(barcodesTypeColumn);
            treeCategories.AppendColumn(categoriesColumn);
            treeSubcategories.AppendColumn(subcategoriesColumn);
            treeLanguages.AppendColumn(languagesColumn);
            treeSystems.AppendColumn(systemsColumn);
            treeArchitectures.AppendColumn(architecturesColumn);
            treeDiscs.AppendColumn(discsColumn);
            treeDisks.AppendColumn(disksColumn);

            treeKeywords.Selection.Mode = SelectionMode.Single;
            treeBarcodes.Selection.Mode = SelectionMode.Single;
            treeCategories.Selection.Mode = SelectionMode.Single;
            treeSubcategories.Selection.Mode = SelectionMode.Single;
            treeLanguages.Selection.Mode = SelectionMode.Single;
            treeSystems.Selection.Mode = SelectionMode.Single;
            treeArchitectures.Selection.Mode = SelectionMode.Single;
            treeDiscs.Selection.Mode = SelectionMode.Single;
            treeDisks.Selection.Mode = SelectionMode.Single;

            CellRendererText textCell = new CellRendererText();

            cmbReleaseType.Clear();
            lstReleaseTypes = new ListStore(typeof(string));
            cmbReleaseType.PackStart(textCell, true);
            cmbReleaseType.AddAttribute(textCell, "text", 0);
            cmbReleaseType.Model = lstReleaseTypes;

            cmbBarcodes.Clear();
            lstBarcodeTypes = new ListStore(typeof(string));
            cmbBarcodes.PackStart(textCell, true);
            cmbBarcodes.AddAttribute(textCell, "text", 0);
            cmbBarcodes.Model = lstBarcodeTypes;

            cmbLanguages.Clear();
            lstLanguageTypes = new ListStore(typeof(string));
            cmbLanguages.PackStart(textCell, true);
            cmbLanguages.AddAttribute(textCell, "text", 0);
            cmbLanguages.Model = lstLanguageTypes;

            cmbArchitectures.Clear();
            lstArchitecturesTypes = new ListStore(typeof(string));
            cmbArchitectures.PackStart(textCell, true);
            cmbArchitectures.AddAttribute(textCell, "text", 0);
            cmbArchitectures.Model = lstArchitecturesTypes;

            cmbFilesForNewDisc.Clear();
            lstFilesForDisc = new ListStore(typeof(string));
            cmbFilesForNewDisc.PackStart(textCell, true);
            cmbFilesForNewDisc.AddAttribute(textCell, "text", 0);
            cmbFilesForNewDisc.Model = lstFilesForDisc;

            cmbFilesForNewDisk.Clear();
            lstFilesForDisk = new ListStore(typeof(string));
            cmbFilesForNewDisk.PackStart(textCell, true);
            cmbFilesForNewDisk.AddAttribute(textCell, "text", 0);
            cmbFilesForNewDisk.Model = lstFilesForDisk;

            lstKeywords.SetSortColumnId(0, SortType.Ascending);
            lstBarcodes.SetSortColumnId(0, SortType.Ascending);
            lstCategories.SetSortColumnId(0, SortType.Ascending);
            lstSubcategories.SetSortColumnId(0, SortType.Ascending);
            lstLanguages.SetSortColumnId(0, SortType.Ascending);
            lstSystems.SetSortColumnId(0, SortType.Ascending);
            lstArchitectures.SetSortColumnId(0, SortType.Ascending);
            lstDiscs.SetSortColumnId(0, SortType.Ascending);
            lstDisks.SetSortColumnId(0, SortType.Ascending);
            lstReleaseTypes.SetSortColumnId(0, SortType.Ascending);
            lstBarcodeTypes.SetSortColumnId(0, SortType.Ascending);
            lstLanguageTypes.SetSortColumnId(0, SortType.Ascending);
            lstArchitecturesTypes.SetSortColumnId(0, SortType.Ascending);
            lstFilesForDisc.SetSortColumnId(0, SortType.Ascending);
            lstFilesForDisk.SetSortColumnId(0, SortType.Ascending);

            FillReleaseTypeCombo();
            FillBarcodeCombo();
            FillLanguagesCombo();
            FillArchitecturesCombo();
            FillFilesCombos();
        }

        void FillReleaseTypeCombo()
        {
            lstReleaseTypes.Clear();
            foreach(CICMMetadataTypeReleaseType type in Enum.GetValues(typeof(CICMMetadataTypeReleaseType)))
                lstReleaseTypes.AppendValues(type.ToString());
        }

        void FillBarcodeCombo()
        {
            lstBarcodeTypes.Clear();
            foreach(BarcodeTypeType type in Enum.GetValues(typeof(BarcodeTypeType)))
                lstBarcodeTypes.AppendValues(type.ToString());
        }

        void FillLanguagesCombo()
        {
            lstLanguageTypes.Clear();
            foreach(LanguagesTypeLanguage type in Enum.GetValues(typeof(LanguagesTypeLanguage)))
                lstLanguageTypes.AppendValues(type.ToString());
        }

        void FillArchitecturesCombo()
        {
            lstArchitecturesTypes.Clear();
            foreach(ArchitecturesTypeArchitecture type in Enum.GetValues(typeof(ArchitecturesTypeArchitecture)))
                lstArchitecturesTypes.AppendValues(type.ToString());
        }

        void FillFilesCombos()
        {
            foreach(KeyValuePair<string, DBOSFile> files in Context.hashes)
            {
                lstFilesForDisc.AppendValues(files.Key);
                lstFilesForDisk.AppendValues(files.Key);
            }
        }

        void FillDiscCombos()
        {
            // TODO: Check that files are not already added as disks
            lstFilesForDisc.Clear();
            foreach(KeyValuePair<string, DBOSFile> files in Context.hashes)
                lstFilesForDisc.AppendValues(files.Key);
        }

        void FillDiskCombos()
        {
            // TODO: Check that files are not already added as discs
            lstFilesForDisk.Clear();
            foreach(KeyValuePair<string, DBOSFile> files in Context.hashes)
                lstFilesForDisk.AppendValues(files.Key);
        }

        public void FillFields()
        {
            if(Metadata == null)
                return;

            if(Metadata.Developer != null)
            {
                foreach(string developer in Metadata.Developer)
                {
                    if(!string.IsNullOrWhiteSpace(txtDeveloper.Text))
                        txtDeveloper.Text += ",";
                    txtDeveloper.Text += developer;
                }
            }

            if(Metadata.Publisher != null)
            {
                foreach(string publisher in Metadata.Publisher)
                {
                    if(!string.IsNullOrWhiteSpace(txtPublisher.Text))
                        txtPublisher.Text += ",";
                    txtPublisher.Text += publisher;
                }
            }

            if(Metadata.Author != null)
            {
                foreach(string author in Metadata.Author)
                {
                    if(!string.IsNullOrWhiteSpace(txtPublisher.Text))
                        txtPublisher.Text += ",";
                    txtPublisher.Text += author;
                }
            }

            if(Metadata.Performer != null)
            {
                foreach(string performer in Metadata.Performer)
                {
                    if(!string.IsNullOrWhiteSpace(txtPublisher.Text))
                        txtPublisher.Text += ",";
                    txtPublisher.Text += performer;
                }
            }

            txtName.Text = Metadata.Name;
            txtVersion.Text = Metadata.Version;
            txtPartNumber.Text = Metadata.PartNumber;
            txtSerialNumber.Text = Metadata.SerialNumber;

            if(Metadata.ReleaseTypeSpecified)
            {
                chkBoxUnknownReleaseType.Active = false;
                cmbReleaseType.Sensitive = true;
                TreeIter iter;
                cmbReleaseType.Model.GetIterFirst(out iter);
                do
                {
                    if((string)cmbReleaseType.Model.GetValue(iter, 0) == Metadata.ReleaseType.ToString())
                    {
                        cmbReleaseType.SetActiveIter(iter);
                        break;
                    }
                }
                while(cmbReleaseType.Model.IterNext(ref iter));
            }

            if(Metadata.ReleaseDateSpecified)
            {
                chkReleaseDate.Active = false;
                cldReleaseDate.Sensitive = true;
                cldReleaseDate.Date = Metadata.ReleaseDate;
            }

            if(Metadata.Keywords != null)
            {
                foreach(string keyword in Metadata.Keywords)
                    lstKeywords.AppendValues(keyword);
            }
            if(Metadata.Categories != null)
            {
                foreach(string category in Metadata.Categories)
                    lstCategories.AppendValues(category);
            }
            if(Metadata.Subcategories != null)
            {
                foreach(string subcategory in Metadata.Subcategories)
                    lstSubcategories.AppendValues(subcategory);
            }

            if(Metadata.Languages != null)
            {
                foreach(LanguagesTypeLanguage language in Metadata.Languages)
                {
                    lstLanguages.AppendValues(language.ToString());
                    TreeIter iter;
                    cmbLanguages.Model.GetIterFirst(out iter);
                    do
                    {
                        if((string)cmbLanguages.Model.GetValue(iter, 0) == language.ToString())
                        {
                            lstLanguageTypes.Remove(ref iter);
                            break;
                        }
                    }
                    while(cmbLanguages.Model.IterNext(ref iter));
                }
            }

            if(Metadata.Systems != null)
            {
                foreach(string system in Metadata.Systems)
                    lstSystems.AppendValues(system);
            }

            if(Metadata.Architectures != null)
            {
                foreach(ArchitecturesTypeArchitecture architecture in Metadata.Architectures)
                {
                    lstArchitectures.AppendValues(architecture.ToString());
                    TreeIter iter;
                    cmbArchitectures.Model.GetIterFirst(out iter);
                    do
                    {
                        if((string)cmbArchitectures.Model.GetValue(iter, 0) == architecture.ToString())
                        {
                            lstArchitecturesTypes.Remove(ref iter);
                            break;
                        }
                    }
                    while(cmbArchitectures.Model.IterNext(ref iter));
                }
            }

            if(Metadata.OpticalDisc != null)
            {
                foreach(OpticalDiscType disc in Metadata.OpticalDisc)
                {
                    lstDiscs.AppendValues(disc.Image.Value, disc);
                    List<string> files = new List<string>();
                    files.Add(disc.Image.Value);
                    if(disc.ADIP != null)
                        files.Add(disc.ADIP.Image);
                    if(disc.ATIP != null)
                        files.Add(disc.ATIP.Image);
                    if(disc.BCA != null)
                        files.Add(disc.BCA.Image);
                    if(disc.CMI != null)
                        files.Add(disc.CMI.Image);
                    if(disc.DCB != null)
                        files.Add(disc.DCB.Image);
                    if(disc.DDS != null)
                        files.Add(disc.DDS.Image);
                    if(disc.DMI != null)
                        files.Add(disc.DMI.Image);
                    if(disc.LastRMD != null)
                        files.Add(disc.LastRMD.Image);
                    if(disc.LeadIn != null)
                    {
                        foreach(BorderType border in disc.LeadIn)
                            files.Add(border.Image);
                    }
                    if(disc.LeadInCdText != null)
                        files.Add(disc.LeadInCdText.Image);
                    if(disc.LeadOut != null)
                    {
                        foreach(BorderType border in disc.LeadOut)
                            files.Add(border.Image);
                    }
                    if(disc.MediaID != null)
                        files.Add(disc.MediaID.Image);
                    if(disc.PAC != null)
                        files.Add(disc.PAC.Image);
                    if(disc.PFI != null)
                        files.Add(disc.PFI.Image);
                    if(disc.PFIR != null)
                        files.Add(disc.PFIR.Image);
                    if(disc.PMA != null)
                        files.Add(disc.PMA.Image);
                    if(disc.PRI != null)
                        files.Add(disc.PRI.Image);
                    if(disc.SAI != null)
                        files.Add(disc.SAI.Image);
                    if(disc.TOC != null)
                        files.Add(disc.TOC.Image);
                    if(disc.Track != null)
                    {
                        foreach(TrackType track in disc.Track)
                            files.Add(track.Image.Value);
                    }

                    foreach(string file in files)
                    {
                        TreeIter iter;
                        cmbFilesForNewDisc.Model.GetIterFirst(out iter);
                        do
                        {
                            if((string)cmbFilesForNewDisc.Model.GetValue(iter, 0) == file)
                            {
                                cmbFilesForNewDisc.SetActiveIter(iter);
                                cmbFilesForNewDisc.RemoveText(cmbFilesForNewDisc.Active);
                                break;
                            }
                        }
                        while(cmbFilesForNewDisc.Model.IterNext(ref iter));

                        cmbFilesForNewDisk.Model.GetIterFirst(out iter);
                        do
                        {
                            if((string)cmbFilesForNewDisk.Model.GetValue(iter, 0) == file)
                            {
                                cmbFilesForNewDisk.SetActiveIter(iter);
                                cmbFilesForNewDisk.RemoveText(cmbFilesForNewDisk.Active);
                                break;
                            }
                        }
                        while(cmbFilesForNewDisk.Model.IterNext(ref iter));
                    }
                }
            }

            if(Metadata.BlockMedia != null)
            {
                foreach(BlockMediaType disk in Metadata.BlockMedia)
                {
                    lstDisks.AppendValues(disk.Image.Value, disk);
                    List<string> files = new List<string>();
                    files.Add(disk.Image.Value);
                    if(disk.ATA != null && disk.ATA.Identify != null)
                        files.Add(disk.ATA.Identify.Image);
                    if(disk.MAM != null)
                        files.Add(disk.MAM.Image);
                    if(disk.PCI != null && disk.PCI.ExpansionROM != null)
                        files.Add(disk.PCI.ExpansionROM.Image.Value);
                    if(disk.PCMCIA != null && disk.PCMCIA.CIS != null)
                        files.Add(disk.PCMCIA.CIS.Image);
                    if(disk.SCSI != null)
                    {
                        if(disk.SCSI.Inquiry != null)
                            files.Add(disk.SCSI.Inquiry.Image);
                        if(disk.SCSI.LogSense != null)
                            files.Add(disk.SCSI.LogSense.Image);
                        if(disk.SCSI.ModeSense != null)
                            files.Add(disk.SCSI.ModeSense.Image);
                        if(disk.SCSI.ModeSense10 != null)
                            files.Add(disk.SCSI.ModeSense10.Image);
                        if(disk.SCSI.EVPD != null)
                        {
                            foreach(EVPDType evpd in disk.SCSI.EVPD)
                                files.Add(evpd.Image);
                        }
                    }
                    if(disk.SecureDigital != null)
                    {
                        if(disk.SecureDigital.CID != null)
                            files.Add(disk.SecureDigital.CID.Image);
                        if(disk.SecureDigital.CSD != null)
                            files.Add(disk.SecureDigital.CSD.Image);
                        if(disk.MultiMediaCard.ExtendedCSD != null)
                            files.Add(disk.MultiMediaCard.ExtendedCSD.Image);
                    }
                    if(disk.TapeInformation != null)
                    {
                        foreach(TapePartitionType tapePart in disk.TapeInformation)
                            files.Add(tapePart.Image.Value);
                    }
                    if(disk.Track != null)
                    {
                        foreach(BlockTrackType track in disk.Track)
                            files.Add(track.Image.Value);
                    }
                    if(disk.USB != null && disk.USB.Descriptors != null)
                        files.Add(disk.USB.Descriptors.Image);

                    foreach(string file in files)
                    {
                        TreeIter iter;
                        cmbFilesForNewDisc.Model.GetIterFirst(out iter);
                        do
                        {
                            if((string)cmbFilesForNewDisc.Model.GetValue(iter, 0) == file)
                            {
                                cmbFilesForNewDisc.SetActiveIter(iter);
                                cmbFilesForNewDisc.RemoveText(cmbFilesForNewDisc.Active);
                                break;
                            }
                        }
                        while(cmbFilesForNewDisc.Model.IterNext(ref iter));

                        cmbFilesForNewDisk.Model.GetIterFirst(out iter);
                        do
                        {
                            if((string)cmbFilesForNewDisk.Model.GetValue(iter, 0) == file)
                            {
                                cmbFilesForNewDisk.SetActiveIter(iter);
                                cmbFilesForNewDisk.RemoveText(cmbFilesForNewDisk.Active);
                                break;
                            }
                        }
                        while(cmbFilesForNewDisk.Model.IterNext(ref iter));
                    }
                }
            }

            magazines = Metadata.Magazine;
            books = Metadata.Book;
            requiredOses = Metadata.RequiredOperatingSystems;
            usermanuals = Metadata.UserManual;
            adverts = Metadata.Advertisement;
            linearmedias = Metadata.LinearMedia;
            pcis = Metadata.PCICard;
            audiomedias = Metadata.AudioMedia;
        }

        protected void OnChkBoxUnknownReleaseTypeToggled(object sender, EventArgs e)
        {
            cmbReleaseType.Sensitive = !chkBoxUnknownReleaseType.Active;
        }

        protected void OnChkReleaseDateToggled(object sender, EventArgs e)
        {
            cldReleaseDate.Sensitive = !chkReleaseDate.Active;
        }

        protected void OnTxtAddKeywordClicked(object sender, EventArgs e)
        {
            lstKeywords.AppendValues(txtNewKeyword.Text);
            txtNewKeyword.Text = "";
        }

        protected void OnBtnRemoveKeywordClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(treeKeywords.Selection.GetSelected(out selectedIter))
                lstKeywords.Remove(ref selectedIter);
        }

        protected void OnBtnClearKeywordsClicked(object sender, EventArgs e)
        {
            lstKeywords.Clear();
        }

        protected void OnBtnAddBarcodeClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(cmbBarcodes.GetActiveIter(out selectedIter))
            {
                lstBarcodes.AppendValues(txtNewBarcode.Text, (string)cmbBarcodes.Model.GetValue(selectedIter, 0));
                txtNewBarcode.Text = "";
            }
        }

        protected void OnBtnClearBarcodesClicked(object sender, EventArgs e)
        {
            lstBarcodes.Clear();
        }

        protected void OnBtnRemoveBarcodeClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(treeBarcodes.Selection.GetSelected(out selectedIter))
                lstBarcodes.Remove(ref selectedIter);
        }

        protected void OnBtnAddCategoryClicked(object sender, EventArgs e)
        {
            lstCategories.AppendValues(txtNewCategory.Text);
            txtNewCategory.Text = "";
        }

        protected void OnBtnAddSubcategoryClicked(object sender, EventArgs e)
        {
            lstSubcategories.AppendValues(txtNewSubcategory.Text);
            txtNewSubcategory.Text = "";
        }

        protected void OnBtnRemoveSubcategoryClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(treeSubcategories.Selection.GetSelected(out selectedIter))
                lstSubcategories.Remove(ref selectedIter);
        }

        protected void OnBtnClearSubcategoriesClicked(object sender, EventArgs e)
        {
            lstSubcategories.Clear();
        }

        protected void OnBtnRemoveCategoryClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(treeCategories.Selection.GetSelected(out selectedIter))
                lstCategories.Remove(ref selectedIter);
        }

        protected void OnBtnClearCategoriesClicked(object sender, EventArgs e)
        {
            lstCategories.Clear();
        }

        protected void OnBtnAddLanguagesClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(cmbLanguages.GetActiveIter(out selectedIter))
            {
                lstLanguages.AppendValues((string)cmbLanguages.Model.GetValue(selectedIter, 0));
                cmbLanguages.SetActiveIter(selectedIter);
                lstLanguageTypes.Remove(ref selectedIter);
            }
        }

        protected void OnBtnRemoveLanguageClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(treeLanguages.Selection.GetSelected(out selectedIter))
            {
                lstLanguageTypes.AppendValues((string)lstLanguages.GetValue(selectedIter, 0));
                lstLanguages.Remove(ref selectedIter);
            }
        }

        protected void OnBtnClearLanguagesClicked(object sender, EventArgs e)
        {
            lstLanguages.Clear();
            FillLanguagesCombo();
        }

        protected void OnBtnAddSystemClicked(object sender, EventArgs e)
        {
            lstSystems.AppendValues(txtNewSystem.Text);
            txtNewSystem.Text = "";
        }

        protected void OnBtnClearSystemsClicked(object sender, EventArgs e)
        {
            lstSystems.Clear();
        }

        protected void OnBtnRemoveSystemsClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(treeSystems.Selection.GetSelected(out selectedIter))
                lstSystems.Remove(ref selectedIter);
        }

        protected void OnBtnAddArchitectureClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(cmbArchitectures.GetActiveIter(out selectedIter))
            {
                lstArchitectures.AppendValues((string)cmbArchitectures.Model.GetValue(selectedIter, 0));
                cmbArchitectures.SetActiveIter(selectedIter);
                lstArchitecturesTypes.Remove(ref selectedIter);
            }
        }

        protected void OnBtnClearArchitecturesClicked(object sender, EventArgs e)
        {
            lstArchitectures.Clear();
            FillArchitecturesCombo();
        }

        protected void OnBtnRemoveArchitectureClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(treeArchitectures.Selection.GetSelected(out selectedIter))
            {
                lstArchitecturesTypes.AppendValues((string)lstArchitectures.GetValue(selectedIter, 0));
                lstArchitectures.Remove(ref selectedIter);
            }
        }

        protected void OnBtnAddDiscClicked(object sender, EventArgs e)
        {
            Context.selectedFile = cmbFilesForNewDisc.ActiveText;
            notebook3.GetNthPage(0).Visible = false;
            notebook3.GetNthPage(1).Visible = false;
            notebook3.GetNthPage(2).Visible = false;
            notebook3.GetNthPage(3).Visible = false;
            notebook3.GetNthPage(4).Visible = false;
            notebook3.GetNthPage(5).Visible = false;
            notebook3.GetNthPage(6).Visible = false;
            notebook3.GetNthPage(8).Visible = false;
            prgAddDisc1.Visible = true;
            prgAddDisc2.Visible = true;
            buttonCancel.Visible = false;
            buttonOk.Visible = false;
            btnEditDisc.Visible = false;
            btnClearDiscs.Visible = false;
            Core.Workers.Failed += OnDiscAddFailed;
            Core.Workers.Finished += OnDiscAddFinished;
            Core.Workers.UpdateProgress += UpdateDiscProgress1;
            Core.Workers.UpdateProgress2 += UpdateDiscProgress2;
            Context.workingDisc = null;
            btnStopAddDisc.Visible = true;
            btnAddDisc.Visible = false;
            btnRemoveDiscs.Visible = false;
            thdDisc = new Thread(Core.Workers.AddMedia);
            thdDisc.Start();
        }

        protected void OnBtnStopAddDiscClicked(object sender, EventArgs e)
        {
            if(thdDisc != null)
                thdDisc.Abort();
            stopped = true;
            OnDiscAddFailed(null);
        }

        public void UpdateDiscProgress1(string text, string inner, long current, long maximum)
        {
            Application.Invoke(delegate
            {
                prgAddDisc1.Text = text + inner;
                if(maximum > 0)
                    prgAddDisc1.Fraction = current / (double)maximum;
                else
                    prgAddDisc1.Pulse();
            });
        }

        public void UpdateDiscProgress2(string text, string inner, long current, long maximum)
        {
            Application.Invoke(delegate
            {
                prgAddDisc2.Text = text + inner;
                if(maximum > 0)
                    prgAddDisc2.Fraction = current / (double)maximum;
                else
                    prgAddDisc2.Pulse();
            });
        }

        void OnDiscAddFailed(string text)
        {
            Application.Invoke(delegate
            {
                if(!stopped)
                {
                    MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                }
                Context.selectedFile = "";
                notebook3.GetNthPage(0).Visible = true;
                notebook3.GetNthPage(1).Visible = true;
                notebook3.GetNthPage(2).Visible = true;
                notebook3.GetNthPage(3).Visible = true;
                notebook3.GetNthPage(4).Visible = true;
                notebook3.GetNthPage(5).Visible = true;
                notebook3.GetNthPage(6).Visible = true;
                notebook3.GetNthPage(8).Visible = true;
                prgAddDisc1.Visible = false;
                prgAddDisc2.Visible = false;
                buttonCancel.Visible = true;
                buttonOk.Visible = true;
                btnEditDisc.Visible = true;
                btnClearDiscs.Visible = true;
                Core.Workers.Failed -= OnDiscAddFailed;
                Core.Workers.Finished -= OnDiscAddFinished;
                Core.Workers.UpdateProgress -= UpdateDiscProgress1;
                Core.Workers.UpdateProgress2 -= UpdateDiscProgress2;
                Context.workingDisc = null;
                btnStopAddDisc.Visible = false;
                btnAddDisc.Visible = true;
                btnRemoveDiscs.Visible = true;
                thdDisc = null;
            });
        }

        void OnDiscAddFinished()
        {
            Application.Invoke(delegate
            {
                if(Context.workingDisc == null)
                    return;

                OpticalDiscType disc = Context.workingDisc;

                lstDiscs.AppendValues(Context.selectedFile, disc);
                List<string> files = new List<string>();
                files.Add(disc.Image.Value);
                if(disc.ADIP != null)
                    files.Add(disc.ADIP.Image);
                if(disc.ATIP != null)
                    files.Add(disc.ATIP.Image);
                if(disc.BCA != null)
                    files.Add(disc.BCA.Image);
                if(disc.CMI != null)
                    files.Add(disc.CMI.Image);
                if(disc.DCB != null)
                    files.Add(disc.DCB.Image);
                if(disc.DDS != null)
                    files.Add(disc.DDS.Image);
                if(disc.DMI != null)
                    files.Add(disc.DMI.Image);
                if(disc.LastRMD != null)
                    files.Add(disc.LastRMD.Image);
                if(disc.LeadIn != null)
                {
                    foreach(BorderType border in disc.LeadIn)
                        files.Add(border.Image);
                }
                if(disc.LeadInCdText != null)
                    files.Add(disc.LeadInCdText.Image);
                if(disc.LeadOut != null)
                {
                    foreach(BorderType border in disc.LeadOut)
                        files.Add(border.Image);
                }
                if(disc.MediaID != null)
                    files.Add(disc.MediaID.Image);
                if(disc.PAC != null)
                    files.Add(disc.PAC.Image);
                if(disc.PFI != null)
                    files.Add(disc.PFI.Image);
                if(disc.PFIR != null)
                    files.Add(disc.PFIR.Image);
                if(disc.PMA != null)
                    files.Add(disc.PMA.Image);
                if(disc.PRI != null)
                    files.Add(disc.PRI.Image);
                if(disc.SAI != null)
                    files.Add(disc.SAI.Image);
                if(disc.TOC != null)
                    files.Add(disc.TOC.Image);
                if(disc.Track != null)
                {
                    foreach(TrackType track in disc.Track)
                        files.Add(track.Image.Value);
                }

                foreach(string file in files)
                {
                    TreeIter iter;
                    cmbFilesForNewDisc.Model.GetIterFirst(out iter);
                    do
                    {
                        if((string)cmbFilesForNewDisc.Model.GetValue(iter, 0) == file)
                        {
                            cmbFilesForNewDisc.SetActiveIter(iter);
                            cmbFilesForNewDisc.RemoveText(cmbFilesForNewDisc.Active);
                            break;
                        }
                    }
                    while(cmbFilesForNewDisc.Model.IterNext(ref iter));

                    cmbFilesForNewDisk.Model.GetIterFirst(out iter);
                    do
                    {
                        if((string)cmbFilesForNewDisk.Model.GetValue(iter, 0) == file)
                        {
                            cmbFilesForNewDisk.SetActiveIter(iter);
                            cmbFilesForNewDisk.RemoveText(cmbFilesForNewDisk.Active);
                            break;
                        }
                    }
                    while(cmbFilesForNewDisk.Model.IterNext(ref iter));
                }

                Context.selectedFile = "";
                notebook3.GetNthPage(0).Visible = true;
                notebook3.GetNthPage(1).Visible = true;
                notebook3.GetNthPage(2).Visible = true;
                notebook3.GetNthPage(3).Visible = true;
                notebook3.GetNthPage(4).Visible = true;
                notebook3.GetNthPage(5).Visible = true;
                notebook3.GetNthPage(6).Visible = true;
                notebook3.GetNthPage(8).Visible = true;
                prgAddDisc1.Visible = false;
                prgAddDisc2.Visible = false;
                buttonCancel.Visible = true;
                buttonOk.Visible = true;
                btnEditDisc.Visible = true;
                btnClearDiscs.Visible = true;
                Core.Workers.Failed -= OnDiscAddFailed;
                Core.Workers.Finished -= OnDiscAddFinished;
                Core.Workers.UpdateProgress -= UpdateDiscProgress1;
                Core.Workers.UpdateProgress2 -= UpdateDiscProgress2;
                Context.workingDisc = null;
                btnStopAddDisc.Visible = false;
                btnAddDisc.Visible = true;
                btnRemoveDiscs.Visible = true;
                thdDisc = null;
            });
        }

        protected void OnBtnClearDiscsClicked(object sender, EventArgs e)
        {
            lstDiscs.Clear();
            FillDiscCombos();
        }

        protected void OnBtnRemoveDiscsClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(treeDiscs.Selection.GetSelected(out selectedIter))
            {
                cmbFilesForNewDisc.AppendText((string)lstDiscs.GetValue(selectedIter, 0));
                cmbFilesForNewDisk.AppendText((string)lstDiscs.GetValue(selectedIter, 0));
                lstDiscs.Remove(ref selectedIter);
            }
        }

        protected void OnBtnAddDiskClicked(object sender, EventArgs e)
        {
            Context.selectedFile = cmbFilesForNewDisk.ActiveText;
            notebook3.GetNthPage(0).Visible = false;
            notebook3.GetNthPage(1).Visible = false;
            notebook3.GetNthPage(2).Visible = false;
            notebook3.GetNthPage(3).Visible = false;
            notebook3.GetNthPage(4).Visible = false;
            notebook3.GetNthPage(5).Visible = false;
            notebook3.GetNthPage(6).Visible = false;
            notebook3.GetNthPage(7).Visible = false;
            prgAddDisk1.Visible = true;
            prgAddDisk2.Visible = true;
            buttonCancel.Visible = false;
            buttonOk.Visible = false;
            btnEditDisk.Visible = false;
            btnClearDisks.Visible = false;
            Core.Workers.Failed += OnDiskAddFailed;
            Core.Workers.Finished += OnDiskAddFinished;
            Core.Workers.UpdateProgress += UpdateDiskProgress1;
            Core.Workers.UpdateProgress2 += UpdateDiskProgress2;
            Context.workingDisk = null;
            btnStopAddDisk.Visible = true;
            btnAddDisk.Visible = false;
            btnRemoveDisk.Visible = false;
            thdDisk = new Thread(Core.Workers.AddMedia);
            thdDisk.Start();
        }

        protected void OnBtnStopAddDiskClicked(object sender, EventArgs e)
        {
            if(thdDisk != null)
                thdDisk.Abort();
            stopped = true;
            OnDiskAddFailed(null);
        }

        public void UpdateDiskProgress1(string text, string inner, long current, long maximum)
        {
            Application.Invoke(delegate
            {
                prgAddDisk1.Text = text + inner;
                if(maximum > 0)
                    prgAddDisk1.Fraction = current / (double)maximum;
                else
                    prgAddDisk1.Pulse();
            });
        }

        public void UpdateDiskProgress2(string text, string inner, long current, long maximum)
        {
            Application.Invoke(delegate
            {
                prgAddDisk2.Text = text + inner;
                if(maximum > 0)
                    prgAddDisk2.Fraction = current / (double)maximum;
                else
                    prgAddDisk2.Pulse();
            });
        }

        void OnDiskAddFailed(string text)
        {
            Application.Invoke(delegate
            {
                if(!stopped)
                {
                    MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                }
                Context.selectedFile = "";
                notebook3.GetNthPage(0).Visible = true;
                notebook3.GetNthPage(1).Visible = true;
                notebook3.GetNthPage(2).Visible = true;
                notebook3.GetNthPage(3).Visible = true;
                notebook3.GetNthPage(4).Visible = true;
                notebook3.GetNthPage(5).Visible = true;
                notebook3.GetNthPage(6).Visible = true;
                notebook3.GetNthPage(7).Visible = true;
                prgAddDisk1.Visible = false;
                prgAddDisk2.Visible = false;
                buttonCancel.Visible = true;
                buttonOk.Visible = true;
                btnEditDisk.Visible = true;
                btnClearDisks.Visible = true;
                Core.Workers.Failed -= OnDiskAddFailed;
                Core.Workers.Finished -= OnDiskAddFinished;
                Core.Workers.UpdateProgress -= UpdateDiskProgress1;
                Core.Workers.UpdateProgress2 -= UpdateDiskProgress2;
                Context.workingDisk = null;
                btnStopAddDisk.Visible = false;
                btnAddDisk.Visible = true;
                btnRemoveDisk.Visible = true;
                thdDisk = null;
            });
        }

        void OnDiskAddFinished()
        {
            Application.Invoke(delegate
            {
                if(Context.workingDisk == null)
                    return;

                BlockMediaType disk = Context.workingDisk;

                lstDisks.AppendValues(disk.Image.Value, disk);
                List<string> files = new List<string>();
                files.Add(disk.Image.Value);
                if(disk.ATA != null && disk.ATA.Identify != null)
                    files.Add(disk.ATA.Identify.Image);
                if(disk.MAM != null)
                    files.Add(disk.MAM.Image);
                if(disk.PCI != null && disk.PCI.ExpansionROM != null)
                    files.Add(disk.PCI.ExpansionROM.Image.Value);
                if(disk.PCMCIA != null && disk.PCMCIA.CIS != null)
                    files.Add(disk.PCMCIA.CIS.Image);
                if(disk.SCSI != null)
                {
                    if(disk.SCSI.Inquiry != null)
                        files.Add(disk.SCSI.Inquiry.Image);
                    if(disk.SCSI.LogSense != null)
                        files.Add(disk.SCSI.LogSense.Image);
                    if(disk.SCSI.ModeSense != null)
                        files.Add(disk.SCSI.ModeSense.Image);
                    if(disk.SCSI.ModeSense10 != null)
                        files.Add(disk.SCSI.ModeSense10.Image);
                    if(disk.SCSI.EVPD != null)
                    {
                        foreach(EVPDType evpd in disk.SCSI.EVPD)
                            files.Add(evpd.Image);
                    }
                }
                if(disk.SecureDigital != null)
                {
                    if(disk.SecureDigital.CID != null)
                        files.Add(disk.SecureDigital.CID.Image);
                    if(disk.SecureDigital.CSD != null)
                        files.Add(disk.SecureDigital.CSD.Image);
                    if(disk.MultiMediaCard.ExtendedCSD != null)
                        files.Add(disk.MultiMediaCard.ExtendedCSD.Image);
                }
                if(disk.TapeInformation != null)
                {
                    foreach(TapePartitionType tapePart in disk.TapeInformation)
                        files.Add(tapePart.Image.Value);
                }
                if(disk.Track != null)
                {
                    foreach(BlockTrackType track in disk.Track)
                        files.Add(track.Image.Value);
                }
                if(disk.USB != null && disk.USB.Descriptors != null)
                    files.Add(disk.USB.Descriptors.Image);

                foreach(string file in files)
                {
                    TreeIter iter;
                    cmbFilesForNewDisc.Model.GetIterFirst(out iter);
                    do
                    {
                        if((string)cmbFilesForNewDisc.Model.GetValue(iter, 0) == file)
                        {
                            cmbFilesForNewDisc.SetActiveIter(iter);
                            cmbFilesForNewDisc.RemoveText(cmbFilesForNewDisc.Active);
                            break;
                        }
                    }
                    while(cmbFilesForNewDisc.Model.IterNext(ref iter));

                    cmbFilesForNewDisk.Model.GetIterFirst(out iter);
                    do
                    {
                        if((string)cmbFilesForNewDisk.Model.GetValue(iter, 0) == file)
                        {
                            cmbFilesForNewDisk.SetActiveIter(iter);
                            cmbFilesForNewDisk.RemoveText(cmbFilesForNewDisk.Active);
                            break;
                        }
                    }
                    while(cmbFilesForNewDisk.Model.IterNext(ref iter));
                }

                Context.selectedFile = "";
                notebook3.GetNthPage(0).Visible = true;
                notebook3.GetNthPage(1).Visible = true;
                notebook3.GetNthPage(2).Visible = true;
                notebook3.GetNthPage(3).Visible = true;
                notebook3.GetNthPage(4).Visible = true;
                notebook3.GetNthPage(5).Visible = true;
                notebook3.GetNthPage(6).Visible = true;
                notebook3.GetNthPage(7).Visible = true;
                prgAddDisk1.Visible = false;
                prgAddDisk2.Visible = false;
                buttonCancel.Visible = true;
                buttonOk.Visible = true;
                btnEditDisk.Visible = true;
                btnClearDisks.Visible = true;
                Core.Workers.Failed -= OnDiskAddFailed;
                Core.Workers.Finished -= OnDiskAddFinished;
                Core.Workers.UpdateProgress -= UpdateDiskProgress1;
                Core.Workers.UpdateProgress2 -= UpdateDiskProgress2;
                Context.workingDisk = null;
                btnStopAddDisk.Visible = false;
                btnAddDisk.Visible = true;
                btnRemoveDisk.Visible = true;
                thdDisk = null;
            });
        }

        protected void OnBtnClearDisksClicked(object sender, EventArgs e)
        {
            lstDisks.Clear();
            FillDiskCombos();
        }

        protected void OnBtnRemoveDiskClicked(object sender, EventArgs e)
        {
            TreeIter selectedIter;
            if(treeDisks.Selection.GetSelected(out selectedIter))
            {
                cmbFilesForNewDisk.AppendText((string)lstDisks.GetValue(selectedIter, 0));
                cmbFilesForNewDisc.AppendText((string)lstDisks.GetValue(selectedIter, 0));
                lstDisks.Remove(ref selectedIter);
            }
        }

        protected void OnButtonOkClicked(object sender, EventArgs e)
        {
            Metadata = new CICMMetadataType();
            List<ArchitecturesTypeArchitecture> architectures = new List<ArchitecturesTypeArchitecture>();
            List<BarcodeType> barcodes = new List<BarcodeType>();
            List<BlockMediaType> disks = new List<BlockMediaType>();
            List<string> categories = new List<string>();
            List<string> keywords = new List<string>();
            List<LanguagesTypeLanguage> languages = new List<LanguagesTypeLanguage>();
            List<OpticalDiscType> discs = new List<OpticalDiscType>();
            List<string> subcategories = new List<string>();
            List<string> systems = new List<string>();

            if(!string.IsNullOrEmpty(txtAuthor.Text))
                Metadata.Author = txtAuthor.Text.Split(',');
            if(!string.IsNullOrEmpty(txtDeveloper.Text))
                Metadata.Developer = txtDeveloper.Text.Split(',');
            if(!string.IsNullOrEmpty(txtName.Text))
                Metadata.Name = txtName.Text;
            if(!string.IsNullOrEmpty(txtPartNumber.Text))
                Metadata.PartNumber = txtPartNumber.Text;
            if(!string.IsNullOrEmpty(txtPerformer.Text))
                Metadata.Performer = txtPerformer.Text.Split(',');
            if(!string.IsNullOrEmpty(txtPublisher.Text))
                Metadata.Publisher = txtPublisher.Text.Split(',');
            if(!string.IsNullOrEmpty(txtSerialNumber.Text))
                Metadata.SerialNumber = txtSerialNumber.Text;
            if(!string.IsNullOrEmpty(txtVersion.Text))
                Metadata.Version = txtVersion.Text;
            if(!chkReleaseDate.Active)
            {
                Metadata.ReleaseDate = cldReleaseDate.Date;
                Metadata.ReleaseDateSpecified = true;
            }
            if(!chkBoxUnknownReleaseType.Active)
            {
                Metadata.ReleaseType = (CICMMetadataTypeReleaseType)Enum.Parse(typeof(CICMMetadataTypeReleaseType), cmbReleaseType.ActiveText);
                Metadata.ReleaseTypeSpecified = true;
            }

            TreeIter iter;

            if(lstArchitectures.GetIterFirst(out iter))
            {
                do
                {
                    architectures.Add((ArchitecturesTypeArchitecture)Enum.Parse(typeof(ArchitecturesTypeArchitecture), (string)lstArchitectures.GetValue(iter, 0)));
                }
                while(lstArchitectures.IterNext(ref iter));
            }

            if(lstBarcodes.GetIterFirst(out iter))
            {
                do
                {
                    BarcodeType barcode = new BarcodeType();
                    barcode.type = (BarcodeTypeType)Enum.Parse(typeof(BarcodeTypeType), (string)lstBarcodes.GetValue(iter, 1));
                    barcode.Value = (string)lstBarcodes.GetValue(iter, 0);
                    barcodes.Add(barcode);
                }
                while(lstBarcodes.IterNext(ref iter));
            }

            if(lstDisks.GetIterFirst(out iter))
            {
                do
                {
                    disks.Add((BlockMediaType)lstDisks.GetValue(iter, 1));
                }
                while(lstDisks.IterNext(ref iter));
            }

            if(lstCategories.GetIterFirst(out iter))
            {
                do
                {
                    categories.Add((string)lstCategories.GetValue(iter, 0));
                }
                while(lstCategories.IterNext(ref iter));
            }

            if(lstKeywords.GetIterFirst(out iter))
            {
                do
                {
                    keywords.Add((string)lstKeywords.GetValue(iter, 0));
                }
                while(lstKeywords.IterNext(ref iter));
            }

            if(lstLanguages.GetIterFirst(out iter))
            {
                do
                {
                    languages.Add((LanguagesTypeLanguage)Enum.Parse(typeof(LanguagesTypeLanguage), (string)lstLanguages.GetValue(iter, 0)));
                }
                while(lstLanguages.IterNext(ref iter));
            }

            if(lstDiscs.GetIterFirst(out iter))
            {
                do
                {
                    discs.Add((OpticalDiscType)lstDiscs.GetValue(iter, 1));
                }
                while(lstDiscs.IterNext(ref iter));
            }

            if(lstSubcategories.GetIterFirst(out iter))
            {
                do
                {
                    subcategories.Add((string)lstSubcategories.GetValue(iter, 0));
                }
                while(lstSubcategories.IterNext(ref iter));
            }

            if(lstSystems.GetIterFirst(out iter))
            {
                do
                {
                    systems.Add((string)lstSystems.GetValue(iter, 0));
                }
                while(lstSystems.IterNext(ref iter));
            }

            if(architectures.Count > 0)
                Metadata.Architectures = architectures.ToArray();
            if(barcodes.Count > 0)
                Metadata.Barcodes = barcodes.ToArray();
            if(disks.Count > 0)
                Metadata.BlockMedia = disks.ToArray();
            if(categories.Count > 0)
                Metadata.Categories = categories.ToArray();
            if(keywords.Count > 0)
                Metadata.Keywords = keywords.ToArray();
            if(languages.Count > 0)
                Metadata.Languages = languages.ToArray();
            if(discs.Count > 0)
                Metadata.OpticalDisc = discs.ToArray();
            if(subcategories.Count > 0)
                Metadata.Subcategories = subcategories.ToArray();
            if(systems.Count > 0)
                Metadata.Systems = systems.ToArray();

            Metadata.Magazine = magazines;
            Metadata.Book = books;
            Metadata.RequiredOperatingSystems = requiredOses;
            Metadata.UserManual = usermanuals;
            Metadata.Advertisement = adverts;
            Metadata.LinearMedia = linearmedias;
            Metadata.PCICard = pcis;
            Metadata.AudioMedia = audiomedias;
        }

        protected void OnBtnEditDiscClicked(object sender, EventArgs e)
        {
            TreeIter discIter;
            if(!treeDiscs.Selection.GetSelected(out discIter))
                return;

            dlgOpticalDisc _dlgOpticalDisc = new dlgOpticalDisc();
            _dlgOpticalDisc.Title = string.Format("Editing disc metadata for {0}", (string)lstDiscs.GetValue(discIter, 0));
            _dlgOpticalDisc.Metadata = (OpticalDiscType)lstDiscs.GetValue(discIter, 1);
            _dlgOpticalDisc.FillFields();

            if(_dlgOpticalDisc.Run() == (int)ResponseType.Ok)
            {
                lstDiscs.Remove(ref discIter);
                lstDiscs.AppendValues(_dlgOpticalDisc.Metadata.Image.Value, _dlgOpticalDisc.Metadata);
            }

            _dlgOpticalDisc.Destroy();
        }

        protected void OnBtnEditDiskClicked(object sender, EventArgs e)
        {
            TreeIter diskIter;
            if(!treeDisks.Selection.GetSelected(out diskIter))
                return;

            dlgBlockMedia _dlgBlockMedia = new dlgBlockMedia();
            _dlgBlockMedia.Title = string.Format("Editing disk metadata for {0}", (string)lstDisks.GetValue(diskIter, 0));
            _dlgBlockMedia.Metadata = (BlockMediaType)lstDisks.GetValue(diskIter, 1);
            _dlgBlockMedia.FillFields();

            if(_dlgBlockMedia.Run() == (int)ResponseType.Ok)
            {
                lstDisks.Remove(ref diskIter);
                lstDisks.AppendValues(_dlgBlockMedia.Metadata.Image.Value, _dlgBlockMedia.Metadata);
            }

            _dlgBlockMedia.Destroy();
        }
    }
}
