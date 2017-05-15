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
using System.Collections.ObjectModel;
using System.Threading;
using Eto.Forms;
using Eto.Serialization.Xaml;
using osrepodbmgr.Core;
using Schemas;

namespace osrepodbmgr.Eto
{
    public class dlgMetadata : Dialog
    {
        public CICMMetadataType Metadata;

        ObservableCollection<StringEntry> lstKeywords;
        ObservableCollection<BarcodeEntry> lstBarcodes;
        ObservableCollection<StringEntry> lstCategories;
        ObservableCollection<StringEntry> lstSubcategories;
        ObservableCollection<StringEntry> lstLanguages;
        ObservableCollection<StringEntry> lstSystems;
        ObservableCollection<StringEntry> lstArchitectures;
        ObservableCollection<DiscEntry> lstDiscs;
        ObservableCollection<DiskEntry> lstDisks;

        ObservableCollection<string> lstBarcodeTypes;
        ObservableCollection<string> lstLanguageTypes;
        ObservableCollection<string> lstArchitecturesTypes;
        ObservableCollection<string> lstFilesForMedia;

        Thread thdDisc;
        Thread thdDisk;
        bool stopped;

        #region XAML UI elements
#pragma warning disable 0649
        TabPage tabGeneral;
        TextBox txtDeveloper;
        TextBox txtPublisher;
        TextBox txtAuthor;
        TextBox txtPerformer;
        TextBox txtName;
        TextBox txtVersion;
        StackLayout stkReleaseType;
        CheckBox chkKnownReleaseType;
        EnumDropDown<CICMMetadataTypeReleaseType> cmbReleaseType;
        DateTimePicker cldReleaseDate;
        CheckBox chkReleaseDate;
        TextBox txtPartNumber;
        TextBox txtSerialNumber;
        TabPage tabKeywords;
        TextBox txtNewKeyword;
        GridView treeKeywords;
        TabPage tabBarcodes;
        TextBox txtNewBarcode;
        ComboBox cmbBarcodes;
        GridView treeBarcodes;
        TabPage tabCategories;
        TextBox txtNewCategory;
        GridView treeCategories;
        TextBox txtNewSubcategory;
        GridView treeSubcategories;
        TabPage tabLanguages;
        ComboBox cmbLanguages;
        GridView treeLanguages;
        TabPage tabSystems;
        TextBox txtNewSystem;
        GridView treeSystems;
        TabPage tabArchitectures;
        ComboBox cmbArchitectures;
        GridView treeArchitectures;
        TabPage tabDiscs;
        ComboBox cmbFilesForNewDisc;
        Button btnAddDisc;
        GridView treeDiscs;
        Button btnStopAddDisc;
        Button btnEditDisc;
        Button btnClearDiscs;
        Button btnRemoveDisc;
        Label lblAddDisc1;
        ProgressBar prgAddDisc1;
        Label lblAddDisc2;
        ProgressBar prgAddDisc2;
        TabPage tabDisks;
        ComboBox cmbFilesForNewDisk;
        Button btnAddDisk;
        GridView treeDisks;
        Button btnStopAddDisk;
        Button btnEditDisk;
        Button btnClearDisks;
        Button btnRemoveDisk;
        Label lblAddDisk1;
        ProgressBar prgAddDisk1;
        Label lblAddDisk2;
        ProgressBar prgAddDisk2;
        Button btnCancel;
        Button btnOK;
#pragma warning restore 0649
        #endregion XAML UI elements

        public bool Modified;

        public dlgMetadata()
        {
            XamlReader.Load(this);

            Modified = false;

            cmbReleaseType = new EnumDropDown<CICMMetadataTypeReleaseType>();
            stkReleaseType.Items.Add(new StackLayoutItem { Control = cmbReleaseType, Expand = true });

            lstKeywords = new ObservableCollection<StringEntry>();
            lstBarcodes = new ObservableCollection<BarcodeEntry>();
            lstCategories = new ObservableCollection<StringEntry>();
            lstSubcategories = new ObservableCollection<StringEntry>();
            lstLanguages = new ObservableCollection<StringEntry>();
            lstSystems = new ObservableCollection<StringEntry>();
            lstArchitectures = new ObservableCollection<StringEntry>();
            lstDiscs = new ObservableCollection<DiscEntry>();
            lstDisks = new ObservableCollection<DiskEntry>();

            treeKeywords.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<StringEntry, string>(r => r.str) },
                HeaderText = "Keyword"
            });
            treeBarcodes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<BarcodeEntry, string>(r => r.code) },
                HeaderText = "Barcode"
            });
            treeBarcodes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<BarcodeEntry, BarcodeTypeType>(r => r.type).Convert(v => v.ToString()) },
                HeaderText = "Type"
            });
            treeCategories.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<StringEntry, string>(r => r.str) },
                HeaderText = "Category"
            });
            treeSubcategories.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<StringEntry, string>(r => r.str) },
                HeaderText = "Subcategory"
            });
            treeLanguages.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<StringEntry, string>(r => r.str) },
                HeaderText = "Language"
            });
            treeSystems.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<StringEntry, string>(r => r.str) },
                HeaderText = "System"
            });
            treeArchitectures.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<StringEntry, string>(r => r.str) },
                HeaderText = "Architecture"
            });
            treeDiscs.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DiscEntry, string>(r => r.path) },
                HeaderText = "File"
            });
            treeDisks.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DiskEntry, string>(r => r.path) },
                HeaderText = "File"
            });

            treeKeywords.DataStore = lstKeywords;
            treeBarcodes.DataStore = lstBarcodes;
            treeCategories.DataStore = lstCategories;
            treeSubcategories.DataStore = lstSubcategories;
            treeLanguages.DataStore = lstLanguages;
            treeSystems.DataStore = lstSystems;
            treeArchitectures.DataStore = lstArchitectures;
            treeDiscs.DataStore = lstDiscs;
            treeDisks.DataStore = lstDisks;

            treeKeywords.AllowMultipleSelection = false;
            treeBarcodes.AllowMultipleSelection = false;
            treeCategories.AllowMultipleSelection = false;
            treeSubcategories.AllowMultipleSelection = false;
            treeLanguages.AllowMultipleSelection = false;
            treeSystems.AllowMultipleSelection = false;
            treeArchitectures.AllowMultipleSelection = false;
            treeDiscs.AllowMultipleSelection = false;
            treeDisks.AllowMultipleSelection = false;

            lstBarcodeTypes = new ObservableCollection<string>();
            lstLanguageTypes = new ObservableCollection<string>();
            lstArchitecturesTypes = new ObservableCollection<string>();
            lstFilesForMedia = new ObservableCollection<string>();

            //cmbBarcodes.ItemTextBinding = Binding.Property<StringEntry, string>(r => r.str);
            cmbBarcodes.DataStore = lstBarcodeTypes;
            cmbLanguages.DataStore = lstLanguageTypes;
            cmbArchitectures.DataStore = lstArchitecturesTypes;
            cmbFilesForNewDisc.DataStore = lstFilesForMedia;
            cmbFilesForNewDisk.DataStore = lstFilesForMedia;

            FillBarcodeCombo();
            FillLanguagesCombo();
            FillArchitecturesCombo();
            FillFilesCombos();
        }

        void FillBarcodeCombo()
        {
            lstBarcodeTypes.Clear();
            foreach(BarcodeTypeType type in Enum.GetValues(typeof(BarcodeTypeType)))
                lstBarcodeTypes.Add(type.ToString());
        }

        void FillLanguagesCombo()
        {
            lstLanguageTypes.Clear();
            foreach(LanguagesTypeLanguage type in Enum.GetValues(typeof(LanguagesTypeLanguage)))
                lstLanguageTypes.Add(type.ToString());
        }

        void FillArchitecturesCombo()
        {
            lstArchitecturesTypes.Clear();
            foreach(ArchitecturesTypeArchitecture type in Enum.GetValues(typeof(ArchitecturesTypeArchitecture)))
                lstArchitecturesTypes.Add(type.ToString());
        }

        void FillFilesCombos()
        {
            foreach(KeyValuePair<string, DBFile> files in Context.hashes)
                lstFilesForMedia.Add(files.Key);
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
                chkKnownReleaseType.Checked = true;
                cmbReleaseType.Enabled = true;
                cmbReleaseType.SelectedValue = Metadata.ReleaseType;
            }

            if(Metadata.ReleaseDateSpecified)
            {
                chkReleaseDate.Checked = false;
                cldReleaseDate.Enabled = true;
                cldReleaseDate.Value = Metadata.ReleaseDate;
            }

            if(Metadata.Keywords != null)
            {
                foreach(string keyword in Metadata.Keywords)
                    lstKeywords.Add(new StringEntry { str = keyword });
            }
            if(Metadata.Categories != null)
            {
                foreach(string category in Metadata.Categories)
                    lstCategories.Add(new StringEntry { str = category });
            }
            if(Metadata.Subcategories != null)
            {
                foreach(string subcategory in Metadata.Subcategories)
                    lstSubcategories.Add(new StringEntry { str = subcategory });
            }

            if(Metadata.Languages != null)
            {
                foreach(LanguagesTypeLanguage language in Metadata.Languages)
                {
                    lstLanguages.Add(new StringEntry { str = language.ToString() });
                    lstLanguageTypes.Remove(language.ToString());
                }
            }

            if(Metadata.Systems != null)
            {
                foreach(string system in Metadata.Systems)
                    lstSystems.Add(new StringEntry { str = system });
            }

            if(Metadata.Architectures != null)
            {
                foreach(ArchitecturesTypeArchitecture architecture in Metadata.Architectures)
                {
                    lstArchitectures.Add(new StringEntry { str = architecture.ToString() });
                    lstArchitecturesTypes.Remove(architecture.ToString());
                }
            }

            if(Metadata.OpticalDisc != null)
            {
                foreach(OpticalDiscType disc in Metadata.OpticalDisc)
                {
                    lstDiscs.Add(new DiscEntry { path = disc.Image.Value, disc = disc });
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
                        foreach(Schemas.BorderType border in disc.LeadIn)
                            files.Add(border.Image);
                    }
                    if(disc.LeadInCdText != null)
                        files.Add(disc.LeadInCdText.Image);
                    if(disc.LeadOut != null)
                    {
                        foreach(Schemas.BorderType border in disc.LeadOut)
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
                        if(lstFilesForMedia.Contains(file))
                            lstFilesForMedia.Remove(file);
                    }
                }
            }

            if(Metadata.BlockMedia != null)
            {
                foreach(BlockMediaType disk in Metadata.BlockMedia)
                {
                    lstDisks.Add(new DiskEntry { path = disk.Image.Value, disk = disk });
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
                        if(disk.SecureDigital.ExtendedCSD != null)
                            files.Add(disk.SecureDigital.ExtendedCSD.Image);
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
                        if(lstFilesForMedia.Contains(file))
                            lstFilesForMedia.Remove(file);
                    }
                }
            }
        }

        protected void OnChkKnownReleaseTypeToggled(object sender, EventArgs e)
        {
            cmbReleaseType.Enabled = chkKnownReleaseType.Checked.Value;
        }

        protected void OnChkReleaseDateToggled(object sender, EventArgs e)
        {
            cldReleaseDate.Enabled = !chkReleaseDate.Checked.Value;
        }

        protected void OnBtnAddKeywordClicked(object sender, EventArgs e)
        {
            lstKeywords.Add(new StringEntry { str = txtNewKeyword.Text });
            txtNewKeyword.Text = "";
        }

        protected void OnBtnRemoveKeywordClicked(object sender, EventArgs e)
        {
            if(treeKeywords.SelectedItem != null)
                lstKeywords.Remove((StringEntry)treeKeywords.SelectedItem);
        }

        protected void OnBtnClearKeywordsClicked(object sender, EventArgs e)
        {
            lstKeywords.Clear();
        }

        protected void OnBtnAddBarcodeClicked(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(cmbBarcodes.Text))
            {
                lstBarcodes.Add(new BarcodeEntry { code = txtNewBarcode.Text, type = (BarcodeTypeType)Enum.Parse(typeof(BarcodeTypeType), cmbBarcodes.Text) });
                txtNewBarcode.Text = "";
            }
        }

        protected void OnBtnClearBarcodesClicked(object sender, EventArgs e)
        {
            lstBarcodes.Clear();
        }

        protected void OnBtnRemoveBarcodeClicked(object sender, EventArgs e)
        {
            if(treeBarcodes.SelectedItem != null)
                lstBarcodes.Remove((BarcodeEntry)treeBarcodes.SelectedItem);
        }

        protected void OnBtnAddCategoryClicked(object sender, EventArgs e)
        {
            lstCategories.Add(new StringEntry { str = txtNewCategory.Text });
            txtNewCategory.Text = "";
        }

        protected void OnBtnAddSubcategoryClicked(object sender, EventArgs e)
        {
            lstSubcategories.Add(new StringEntry { str = txtNewSubcategory.Text });
            txtNewSubcategory.Text = "";
        }

        protected void OnBtnRemoveSubcategoryClicked(object sender, EventArgs e)
        {
            if(treeSubcategories.SelectedItem != null)
                lstSubcategories.Remove((StringEntry)treeSubcategories.SelectedItem);
        }

        protected void OnBtnClearSubcategoriesClicked(object sender, EventArgs e)
        {
            lstSubcategories.Clear();
        }

        protected void OnBtnRemoveCategoryClicked(object sender, EventArgs e)
        {
            if(treeCategories.SelectedItem != null)
                lstCategories.Remove((StringEntry)treeCategories.SelectedItem);
        }

        protected void OnBtnClearCategoriesClicked(object sender, EventArgs e)
        {
            lstCategories.Clear();
        }

        protected void OnBtnAddLanguageClicked(object sender, EventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(cmbLanguages.Text))
            {
                lstLanguages.Add(new StringEntry { str = cmbLanguages.Text });
                lstLanguageTypes.Remove(cmbLanguages.Text);
            }
        }

        protected void OnBtnRemoveLanguageClicked(object sender, EventArgs e)
        {
            if(treeLanguages.SelectedItem != null)
            {
                lstLanguageTypes.Add(((StringEntry)treeLanguages.SelectedItem).str);
                lstLanguages.Remove((StringEntry)treeLanguages.SelectedItem);
            }
        }

        protected void OnBtnClearLanguagesClicked(object sender, EventArgs e)
        {
            lstLanguages.Clear();
            FillLanguagesCombo();
        }

        protected void OnBtnAddSystemClicked(object sender, EventArgs e)
        {
            lstSystems.Add(new StringEntry { str = txtNewSystem.Text });
            txtNewSystem.Text = "";
        }

        protected void OnBtnClearSystemsClicked(object sender, EventArgs e)
        {
            lstSystems.Clear();
        }

        protected void OnBtnRemoveSystemClicked(object sender, EventArgs e)
        {
            if(treeSystems.SelectedItem != null)
                lstSystems.Remove((StringEntry)treeSystems.SelectedItem);
        }

        protected void OnBtnAddArchitectureClicked(object sender, EventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(cmbArchitectures.Text))
            {
                lstArchitectures.Add(new StringEntry { str = cmbArchitectures.Text });
                lstArchitecturesTypes.Remove(cmbArchitectures.Text);
            }
        }

        protected void OnBtnClearArchitecturesClicked(object sender, EventArgs e)
        {
            lstArchitectures.Clear();
            FillArchitecturesCombo();
        }

        protected void OnBtnRemoveArchitectureClicked(object sender, EventArgs e)
        {
            if(treeArchitectures.SelectedItem != null)
            {
                lstArchitecturesTypes.Add(((StringEntry)treeArchitectures.SelectedItem).str);
                lstArchitectures.Remove((StringEntry)treeArchitectures.SelectedItem);
            }
        }

        protected void OnBtnAddDiscClicked(object sender, EventArgs e)
        {
            Context.selectedFile = cmbFilesForNewDisc.Text;
            tabGeneral.Visible = false;
            tabKeywords.Visible = false;
            tabBarcodes.Visible = false;
            tabCategories.Visible = false;
            tabLanguages.Visible = false;
            tabSystems.Visible = false;
            tabArchitectures.Visible = false;
            tabDisks.Visible = false;
            prgAddDisc1.Visible = true;
            prgAddDisc2.Visible = true;
            btnCancel.Visible = false;
            btnOK.Visible = false;
            btnEditDisc.Visible = false;
            btnClearDiscs.Visible = false;
            Workers.Failed += OnDiscAddFailed;
            Workers.Finished += OnDiscAddFinished;
            Workers.UpdateProgress += UpdateDiscProgress1;
            Workers.UpdateProgress2 += UpdateDiscProgress2;
            Context.workingDisc = null;
            btnStopAddDisc.Visible = true;
            btnAddDisc.Visible = false;
            btnRemoveDisc.Visible = false;
            thdDisc = new Thread(Workers.AddMedia);
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
            Application.Instance.Invoke(delegate
            {
                if(!string.IsNullOrWhiteSpace(inner))
                    lblAddDisc1.Text = inner;
                else
                    lblAddDisc1.Text = text;
                if(maximum > 0)
                {
                    prgAddDisc1.Indeterminate = false;
                    prgAddDisc1.MinValue = 0;
                    prgAddDisc1.MaxValue = (int)maximum;
                    prgAddDisc1.Value = (int)current;
                }
                else
                    prgAddDisc1.Indeterminate = true;
            });
        }

        public void UpdateDiscProgress2(string text, string inner, long current, long maximum)
        {
            Application.Instance.Invoke(delegate
            {
                if(!string.IsNullOrWhiteSpace(inner))
                    lblAddDisc2.Text = inner;
                else
                    lblAddDisc2.Text = text;
                if(maximum > 0)
                {
                    prgAddDisc2.Indeterminate = false;
                    prgAddDisc2.MinValue = 0;
                    prgAddDisc2.MaxValue = (int)maximum;
                    prgAddDisc2.Value = (int)current;
                }
                else
                    prgAddDisc2.Indeterminate = true;
            });
        }

        void OnDiscAddFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                if(!stopped)
                    MessageBox.Show(text, MessageBoxType.Error);
                Context.selectedFile = "";
                tabGeneral.Visible = true;
                tabKeywords.Visible = true;
                tabBarcodes.Visible = true;
                tabCategories.Visible = true;
                tabLanguages.Visible = true;
                tabSystems.Visible = true;
                tabArchitectures.Visible = true;
                tabDisks.Visible = true;
                prgAddDisc1.Visible = false;
                prgAddDisc2.Visible = false;
                btnCancel.Visible = true;
                btnOK.Visible = true;
                btnEditDisc.Visible = true;
                btnClearDiscs.Visible = true;
                Workers.Failed -= OnDiscAddFailed;
                Workers.Finished -= OnDiscAddFinished;
                Workers.UpdateProgress -= UpdateDiscProgress1;
                Workers.UpdateProgress2 -= UpdateDiscProgress2;
                Context.workingDisc = null;
                btnStopAddDisc.Visible = false;
                btnAddDisc.Visible = true;
                btnRemoveDisc.Visible = true;
                thdDisc = null;
            });
        }

        void OnDiscAddFinished()
        {
            Application.Instance.Invoke(delegate
            {
                if(Context.workingDisc == null)
                    return;

                OpticalDiscType disc = Context.workingDisc;

                lstDiscs.Add(new DiscEntry { path = Context.selectedFile, disc = disc });
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
                    foreach(Schemas.BorderType border in disc.LeadIn)
                        files.Add(border.Image);
                }
                if(disc.LeadInCdText != null)
                    files.Add(disc.LeadInCdText.Image);
                if(disc.LeadOut != null)
                {
                    foreach(Schemas.BorderType border in disc.LeadOut)
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
                    lstFilesForMedia.Remove(file);

                Context.selectedFile = "";
                tabGeneral.Visible = true;
                tabKeywords.Visible = true;
                tabBarcodes.Visible = true;
                tabCategories.Visible = true;
                tabLanguages.Visible = true;
                tabSystems.Visible = true;
                tabArchitectures.Visible = true;
                tabDisks.Visible = true;
                prgAddDisc1.Visible = false;
                prgAddDisc2.Visible = false;
                btnCancel.Visible = true;
                btnOK.Visible = true;
                btnEditDisc.Visible = true;
                btnClearDiscs.Visible = true;
                Workers.Failed -= OnDiscAddFailed;
                Workers.Finished -= OnDiscAddFinished;
                Workers.UpdateProgress -= UpdateDiscProgress1;
                Workers.UpdateProgress2 -= UpdateDiscProgress2;
                Context.workingDisc = null;
                btnStopAddDisc.Visible = false;
                btnAddDisc.Visible = true;
                btnRemoveDisc.Visible = true;
                thdDisc = null;
            });
        }

        protected void OnBtnClearDiscsClicked(object sender, EventArgs e)
        {
            lstDiscs.Clear();
            // TODO: Don't add files that are in disks
            FillFilesCombos();
        }

        protected void OnBtnRemoveDiscClicked(object sender, EventArgs e)
        {
            if(treeDiscs.SelectedItem != null)
            {
                lstFilesForMedia.Add(((DiscEntry)treeDiscs.SelectedItem).path);
                lstDiscs.Remove((DiscEntry)treeDiscs.SelectedItem);
            }
        }

        protected void OnBtnAddDiskClicked(object sender, EventArgs e)
        {
            Context.selectedFile = cmbFilesForNewDisk.Text;
            tabGeneral.Visible = false;
            tabKeywords.Visible = false;
            tabBarcodes.Visible = false;
            tabCategories.Visible = false;
            tabLanguages.Visible = false;
            tabSystems.Visible = false;
            tabArchitectures.Visible = false;
            tabDiscs.Visible = false;
            prgAddDisk1.Visible = true;
            prgAddDisk2.Visible = true;
            btnCancel.Visible = false;
            btnOK.Visible = false;
            btnEditDisk.Visible = false;
            btnClearDisks.Visible = false;
            Workers.Failed += OnDiskAddFailed;
            Workers.Finished += OnDiskAddFinished;
            Workers.UpdateProgress += UpdateDiskProgress1;
            Workers.UpdateProgress2 += UpdateDiskProgress2;
            Context.workingDisk = null;
            btnStopAddDisk.Visible = true;
            btnAddDisk.Visible = false;
            btnRemoveDisk.Visible = false;
            thdDisk = new Thread(Workers.AddMedia);
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
            Application.Instance.Invoke(delegate
            {
                if(!string.IsNullOrWhiteSpace(inner))
                    lblAddDisk1.Text = inner;
                else
                    lblAddDisk1.Text = text;
                if(maximum > 0)
                {
                    prgAddDisk1.Indeterminate = false;
                    prgAddDisk1.MinValue = 0;
                    prgAddDisk1.MaxValue = (int)maximum;
                    prgAddDisk1.Value = (int)current;
                }
                else
                    prgAddDisk1.Indeterminate = true;
            });
        }

        public void UpdateDiskProgress2(string text, string inner, long current, long maximum)
        {
            Application.Instance.Invoke(delegate
            {
                if(!string.IsNullOrWhiteSpace(inner))
                    lblAddDisk2.Text = inner;
                else
                    lblAddDisk2.Text = text;
                if(maximum > 0)
                {
                    prgAddDisk2.Indeterminate = false;
                    prgAddDisk2.MinValue = 0;
                    prgAddDisk2.MaxValue = (int)maximum;
                    prgAddDisk2.Value = (int)current;
                }
                else
                    prgAddDisk2.Indeterminate = true;
            });
        }

        void OnDiskAddFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                if(!stopped)
                    MessageBox.Show(text, MessageBoxType.Error);
                Context.selectedFile = "";
                tabGeneral.Visible = true;
                tabKeywords.Visible = true;
                tabBarcodes.Visible = true;
                tabCategories.Visible = true;
                tabLanguages.Visible = true;
                tabSystems.Visible = true;
                tabArchitectures.Visible = true;
                tabDiscs.Visible = true;
                prgAddDisk1.Visible = false;
                prgAddDisk2.Visible = false;
                btnCancel.Visible = true;
                btnOK.Visible = true;
                btnEditDisk.Visible = true;
                btnClearDisks.Visible = true;
                Workers.Failed -= OnDiskAddFailed;
                Workers.Finished -= OnDiskAddFinished;
                Workers.UpdateProgress -= UpdateDiskProgress1;
                Workers.UpdateProgress2 -= UpdateDiskProgress2;
                Context.workingDisk = null;
                btnStopAddDisk.Visible = false;
                btnAddDisk.Visible = true;
                btnRemoveDisk.Visible = true;
                thdDisk = null;
            });
        }

        void OnDiskAddFinished()
        {
            Application.Instance.Invoke(delegate
            {
                if(Context.workingDisk == null)
                    return;

                BlockMediaType disk = Context.workingDisk;

                lstDisks.Add(new DiskEntry { path = disk.Image.Value, disk = disk });
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
                    if(disk.SecureDigital.ExtendedCSD != null)
                        files.Add(disk.SecureDigital.ExtendedCSD.Image);
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
                    lstFilesForMedia.Remove(file);

                Context.selectedFile = "";
                tabGeneral.Visible = true;
                tabKeywords.Visible = true;
                tabBarcodes.Visible = true;
                tabCategories.Visible = true;
                tabLanguages.Visible = true;
                tabSystems.Visible = true;
                tabArchitectures.Visible = true;
                tabDiscs.Visible = true;
                prgAddDisk1.Visible = false;
                prgAddDisk2.Visible = false;
                btnCancel.Visible = true;
                btnOK.Visible = true;
                btnEditDisk.Visible = true;
                btnClearDisks.Visible = true;
                Workers.Failed -= OnDiskAddFailed;
                Workers.Finished -= OnDiskAddFinished;
                Workers.UpdateProgress -= UpdateDiskProgress1;
                Workers.UpdateProgress2 -= UpdateDiskProgress2;
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
            // TODO: Don't add files that are discs
            FillFilesCombos();
        }

        protected void OnBtnRemoveDiskClicked(object sender, EventArgs e)
        {
            if(treeDisks.SelectedItem != null)
            {
                lstFilesForMedia.Add(((DiskEntry)treeDisks.SelectedItem).path);
                lstDisks.Remove((DiskEntry)treeDisks.SelectedItem);
            }
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            Modified = false;
            Close();
        }

        protected void OnBtnOKClicked(object sender, EventArgs e)
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
            if(!chkReleaseDate.Checked.Value)
            {
                Metadata.ReleaseDate = cldReleaseDate.Value.Value;
                Metadata.ReleaseDateSpecified = true;
            }
            if(chkKnownReleaseType.Checked.Value)
            {
                Metadata.ReleaseType = cmbReleaseType.SelectedValue;
                Metadata.ReleaseTypeSpecified = true;
            }

            foreach(StringEntry entry in lstArchitectures)
                architectures.Add((ArchitecturesTypeArchitecture)Enum.Parse(typeof(ArchitecturesTypeArchitecture), entry.str));

            foreach(BarcodeEntry entry in lstBarcodes)
            {
                BarcodeType barcode = new BarcodeType();
                barcode.type = entry.type;
                barcode.Value = entry.code;
                barcodes.Add(barcode);
            }

            foreach(DiskEntry entry in lstDisks)
                disks.Add(entry.disk);

            foreach(StringEntry entry in lstCategories)
                categories.Add(entry.str);

            foreach(StringEntry entry in lstKeywords)
                keywords.Add(entry.str);

            foreach(StringEntry entry in lstLanguages)
                languages.Add((LanguagesTypeLanguage)Enum.Parse(typeof(LanguagesTypeLanguage), entry.str));

            foreach(DiscEntry entry in lstDiscs)
                discs.Add(entry.disc);

            foreach(StringEntry entry in lstSubcategories)
                subcategories.Add(entry.str);

            foreach(StringEntry entry in lstSystems)
                systems.Add(entry.str);

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

            Modified = true;
            Close();
        }

        protected void OnBtnEditDiscClicked(object sender, EventArgs e)
        {
            if(treeDiscs.SelectedItem == null)
                return;

            dlgOpticalDisc _dlgOpticalDisc = new dlgOpticalDisc();
            _dlgOpticalDisc.Title = string.Format("Editing disc metadata for {0}", ((DiscEntry)treeDiscs.SelectedItem).path);
            _dlgOpticalDisc.Metadata = ((DiscEntry)treeDiscs.SelectedItem).disc;
            _dlgOpticalDisc.FillFields();
            _dlgOpticalDisc.ShowModal(this);

            if(_dlgOpticalDisc.Modified)
            {
                lstDiscs.Remove((DiscEntry)treeDiscs.SelectedItem);
                lstDiscs.Add(new DiscEntry { path = _dlgOpticalDisc.Metadata.Image.Value, disc = _dlgOpticalDisc.Metadata });
            }
        }

        protected void OnBtnEditDiskClicked(object sender, EventArgs e)
        {
            if(treeDisks.SelectedItem == null)
                return;

            dlgBlockMedia _dlgBlockMedia = new dlgBlockMedia();
            _dlgBlockMedia.Title = string.Format("Editing disk metadata for {0}", ((DiskEntry)treeDisks.SelectedItem).path);
            _dlgBlockMedia.Metadata = ((DiskEntry)treeDisks.SelectedItem).disk;
            _dlgBlockMedia.FillFields();
            _dlgBlockMedia.ShowModal(this);

            if(_dlgBlockMedia.Modified)

            {
                lstDisks.Remove((DiskEntry)treeDisks.SelectedItem);
                lstDisks.Add(new DiskEntry { path = _dlgBlockMedia.Metadata.Image.Value, disk = _dlgBlockMedia.Metadata });
            }
        }
    }
}