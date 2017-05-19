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
using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;
using osrepodbmgr.Core;

namespace osrepodbmgr.Eto
{
    public class frmMain : Form
    {
        Thread thdPopulateOSes;
        Thread thdCompressTo;
        Thread thdSaveAs;
        Thread thdPopulateFiles;
        bool populatingFiles;
        Thread thdScanFile;
        DBFile outIter;
        bool scanningFiles;
        Thread thdCleanFiles;
        int infectedFiles;

        #region XAML UI elements
#pragma warning disable 0649
        GridView treeOSes;
        Label lblProgress;
        ProgressBar prgProgress;
        Label lblProgress2;
        ProgressBar prgProgress2;
        Button btnAdd;
        Button btnRemove;
        Button btnCompress;
        Button btnSave;
        Button btnStop;
        ButtonMenuItem btnSettings;
        ButtonMenuItem btnHelp;
        ButtonMenuItem mnuCompress;
        GridView treeFiles;
        Label lblProgressFiles1;
        ProgressBar prgProgressFiles1;
        Label lblProgressFiles2;
        ProgressBar prgProgressFiles2;
        Button btnStopFiles;
        Button btnToggleCrack;
        Button btnScanWithClamd;
        Button btnCheckInVirusTotal;
        Button btnPopulateFiles;
        TabPage tabOSes;
        Button btnScanAllPending;
        Button btnCleanFiles;
        ButtonMenuItem btnQuit;
        ButtonMenuItem mnuFile;
        Label lblOSStatus;
        Label lblFileStatus;
#pragma warning restore 0649
        #endregion XAML UI elements

        ObservableCollection<DBEntryForEto> lstOSes;
        ObservableCollection<DBFile> lstFiles;

        public frmMain()
        {
            XamlReader.Load(this);

            Workers.InitDB();

            lstOSes = new ObservableCollection<DBEntryForEto>();

            treeOSes.DataStore = lstOSes;
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBEntryForEto, string>(r => r.developer) },
                HeaderText = "Developer"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBEntryForEto, string>(r => r.product) },
                HeaderText = "Product"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBEntryForEto, string>(r => r.version) },
                HeaderText = "Version"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBEntryForEto, string>(r => r.languages) },
                HeaderText = "Languages"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBEntryForEto, string>(r => r.architecture) },
                HeaderText = "Architecture"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBEntryForEto, string>(r => r.machine) },
                HeaderText = "Machine"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBEntryForEto, string>(r => r.format) },
                HeaderText = "Format"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBEntryForEto, string>(r => r.description) },
                HeaderText = "Description"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<DBEntryForEto, bool?>(r => r.oem) },
                HeaderText = "OEM?"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<DBEntryForEto, bool?>(r => r.upgrade) },
                HeaderText = "Upgrade?"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<DBEntryForEto, bool?>(r => r.update) },
                HeaderText = "Update?"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<DBEntryForEto, bool?>(r => r.source) },
                HeaderText = "Source?"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<DBEntryForEto, bool?>(r => r.files) },
                HeaderText = "Files?"
            });
            treeOSes.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<DBEntryForEto, bool?>(r => r.netinstall) },
                HeaderText = "NetInstall?"
            });

            treeOSes.AllowMultipleSelection = false;

            lstFiles = new ObservableCollection<DBFile>();

            treeFiles.DataStore = lstFiles;
            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBFile, string>(r => r.Sha256) },
                HeaderText = "SHA256"
            });
            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBFile, long>(r => r.Length).Convert(s => s.ToString()) },
                HeaderText = "Length"
            });
            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<DBFile, bool?>(r => r.Crack) },
                HeaderText = "Crack?"
            });
            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<DBFile, bool?>(r => r.HasVirus) },
                HeaderText = "Has virus?"
            });
            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBFile, DateTime?>(r => r.ClamTime).Convert(s => s == null ? "Never" : s.Value.ToString()) },
                HeaderText = "Last scanned with clamd"
            });
            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBFile, DateTime?>(r => r.VirusTotalTime).Convert(s => s == null ? "Never" : s.Value.ToString()) },
                HeaderText = "Last checked on VirusTotal"
            });
            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<DBFile, string>(r => r.Virus).Convert(s => s ?? "None") },
                HeaderText = "Virus"
            });

            treeFiles.AllowMultipleSelection = false;
            treeFiles.CellFormatting += (sender, e) =>
            {
                if(((DBFile)e.Item).HasVirus.HasValue)
                {
                    e.BackgroundColor = ((DBFile)e.Item).HasVirus.Value ? Colors.Red : Colors.Green;
                }
                else
                    e.BackgroundColor = Colors.Yellow;

                e.ForegroundColor = Colors.Black;
            };

            prgProgress.Indeterminate = true;

            if(!Context.usableDotNetZip)
            {
                btnCompress.Visible = false;
                mnuCompress.Enabled = false;
            }

            Workers.Failed += LoadOSesFailed;
            Workers.Finished += LoadOSesFinished;
            Workers.UpdateProgress += UpdateProgress;
            Workers.AddOS += AddOS;
            thdPopulateOSes = new Thread(Workers.GetAllOSes);
            thdPopulateOSes.Start();
        }

        void LoadOSesFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                MessageBox.Show(string.Format("Error {0} when populating OSes file, exiting...", text), MessageBoxButtons.OK, MessageBoxType.Error, MessageBoxDefaultButton.OK);
                if(thdPopulateOSes != null)
                {
                    thdPopulateOSes.Abort();
                    thdPopulateOSes = null;
                }
                Workers.Failed -= LoadOSesFailed;
                Workers.Finished -= LoadOSesFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Application.Instance.Quit();
            });
        }

        void LoadOSesFinished()
        {
            Application.Instance.Invoke(delegate
            {
                Workers.Failed -= LoadOSesFailed;
                Workers.Finished -= LoadOSesFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.AddOS -= AddOS;
                if(thdPopulateOSes != null)
                {
                    thdPopulateOSes.Abort();
                    thdPopulateOSes = null;
                }
                lblProgress.Visible = false;
                prgProgress.Visible = false;
                treeOSes.Enabled = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnCompress.Visible = Context.usableDotNetZip;
                btnSave.Visible = true;
                btnHelp.Enabled = true;
                btnSettings.Enabled = true;
                lblOSStatus.Visible = true;
                lblOSStatus.Text = string.Format("{0} operating systems", lstOSes.Count);
            });
        }

        public void UpdateProgress(string text, string inner, long current, long maximum)
        {
            Application.Instance.Invoke(delegate
            {
                if(!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(inner))
                    lblProgress.Text = string.Format("{0}: {1}", text, inner);
                else if(!string.IsNullOrWhiteSpace(inner))
                    lblProgress.Text = inner;
                else
                    lblProgress.Text = text;
                if(maximum > 0)
                {
                    prgProgress.Indeterminate = false;
                    prgProgress.MinValue = 0;
                    prgProgress.MaxValue = (int)maximum;
                    prgProgress.Value = (int)current;
                }
                else
                    prgProgress.Indeterminate = true;
            });
        }

        public void UpdateProgress2(string text, string inner, long current, long maximum)
        {
            Application.Instance.Invoke(delegate
            {
                if(!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(inner))
                    lblProgress2.Text = string.Format("{0}: {1}", text, inner);
                else if(!string.IsNullOrWhiteSpace(inner))
                    lblProgress2.Text = inner;
                else
                    lblProgress2.Text = text;
                if(maximum > 0)
                {
                    prgProgress2.Indeterminate = false;
                    prgProgress2.MinValue = 0;
                    prgProgress2.MaxValue = (int)maximum;
                    prgProgress2.Value = (int)current;
                }
                else
                    prgProgress2.Indeterminate = true;
            });
        }

        void AddOS(DBEntry os)
        {
            Application.Instance.Invoke(delegate
            {
                lstOSes.Add(new DBEntryForEto(os));
            });
        }

        protected void OnBtnAddClicked(object sender, EventArgs e)
        {
            dlgAdd _dlgAdd = new dlgAdd();
            _dlgAdd.OnAddedOS += (os) =>
            {
                lstOSes.Add(new DBEntryForEto(os));
            };
            _dlgAdd.ShowModal(this);
        }

        protected void OnBtnRemoveClicked(object sender, EventArgs e)
        {
            if(treeOSes.SelectedItem != null)
            {
                if(MessageBox.Show("Are you sure you want to remove the selected OS?", MessageBoxButtons.YesNo, MessageBoxType.Question,
                                   MessageBoxDefaultButton.No) == DialogResult.Yes)
                {
                    Workers.RemoveOS(((DBEntryForEto)treeOSes.SelectedItem).id, ((DBEntryForEto)treeOSes.SelectedItem).mdid);
                    lstOSes.Remove((DBEntryForEto)treeOSes.SelectedItem);
                }
            }
        }

        protected void OnBtnSaveClicked(object sender, EventArgs e)
        {
            if(treeOSes.SelectedItem != null)
            {
                SelectFolderDialog dlgFolder = new SelectFolderDialog();
                dlgFolder.Title = "Save to...";
                if(dlgFolder.ShowDialog(this) == DialogResult.Ok)
                {
                    Context.dbInfo.id = ((DBEntryForEto)treeOSes.SelectedItem).id;
                    Context.path = dlgFolder.Directory;

                    lblProgress.Visible = true;
                    prgProgress.Visible = true;
                    lblProgress2.Visible = true;
                    prgProgress2.Visible = true;
                    treeOSes.Enabled = false;
                    btnAdd.Visible = false;
                    btnRemove.Visible = false;
                    btnCompress.Visible = false;
                    btnSave.Visible = false;
                    btnHelp.Enabled = true;
                    btnSettings.Enabled = false;
                    btnStop.Visible = true;

                    Workers.Failed += SaveAsFailed;
                    Workers.Finished += SaveAsFinished;
                    Workers.UpdateProgress += UpdateProgress;
                    Workers.UpdateProgress2 += UpdateProgress2;
                    thdSaveAs = new Thread(Workers.SaveAs);
                    thdSaveAs.Start();
                }
            }
        }

        public void SaveAsFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                MessageBox.Show(text, MessageBoxButtons.OK, MessageBoxType.Error);

                lblProgress.Visible = false;
                prgProgress.Visible = false;
                lblProgress2.Visible = false;
                prgProgress2.Visible = false;
                treeOSes.Enabled = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnCompress.Visible = Context.usableDotNetZip;
                btnSave.Visible = true;
                btnHelp.Enabled = true;
                btnSettings.Enabled = true;
                btnStop.Visible = false;

                Workers.Failed -= SaveAsFailed;
                Workers.Finished -= SaveAsFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;

                if(thdSaveAs != null)
                {
                    thdSaveAs.Abort();
                    thdSaveAs = null;
                }

                Context.path = null;
            });
        }

        public void SaveAsFinished()
        {
            Application.Instance.Invoke(delegate
            {
                lblProgress.Visible = false;
                prgProgress.Visible = false;
                lblProgress2.Visible = false;
                prgProgress2.Visible = false;
                treeOSes.Enabled = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnCompress.Visible = Context.usableDotNetZip;
                btnSave.Visible = true;
                btnHelp.Enabled = true;
                btnSettings.Enabled = true;
                btnStop.Visible = false;

                Workers.Failed -= SaveAsFailed;
                Workers.Finished -= SaveAsFinished;
                Workers.UpdateProgress -= UpdateProgress;

                if(thdSaveAs != null)
                {
                    thdSaveAs.Abort();
                    thdSaveAs = null;
                }

                MessageBox.Show(string.Format("Correctly saved to {0}", Context.path));

                Context.path = null;
            });
        }

        protected void OnBtnHelpClicked(object sender, EventArgs e)
        {
            dlgHelp _dlgHelp = new dlgHelp();
            _dlgHelp.ShowModal();
        }

        protected void OnBtnSettingsClicked(object sender, EventArgs e)
        {
            dlgSettings _dlgSettings = new dlgSettings();
            _dlgSettings.ShowModal();
        }

        protected void OnBtnQuitClicked(object sender, EventArgs e)
        {
            OnBtnStopClicked(sender, e);
            Application.Instance.Quit();
        }

        protected void OnBtnStopClicked(object sender, EventArgs e)
        {
            Workers.AddOS -= AddOS;
            Workers.Failed -= CompressToFailed;
            Workers.Failed -= LoadOSesFailed;
            Workers.Failed -= SaveAsFailed;
            Workers.Finished -= CompressToFinished;
            Workers.Finished -= LoadOSesFinished;
            Workers.Finished -= SaveAsFinished;
            Workers.UpdateProgress -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;

            if(thdPopulateOSes != null)
            {
                thdPopulateOSes.Abort();
                thdPopulateOSes = null;
            }
            if(thdCompressTo != null)
            {
                thdPopulateOSes.Abort();
                thdPopulateOSes = null;
            }
            if(thdSaveAs != null)
            {
                thdSaveAs.Abort();
                thdSaveAs = null;
            }
        }

        protected void OnDeleteEvent(object sender, EventArgs e)
        {
            OnBtnStopClicked(sender, e);
        }

        protected void OnBtnCompressClicked(object sender, EventArgs e)
        {
            if(treeOSes.SelectedItem != null)
            {
                SaveFileDialog dlgFile = new SaveFileDialog();
                dlgFile.Title = "Compress to...";

                if(dlgFile.ShowDialog(this) == DialogResult.Ok)
                {
                    Context.dbInfo.id = ((DBEntryForEto)treeOSes.SelectedItem).id;
                    Context.path = dlgFile.FileName;

                    lblProgress.Visible = true;
                    prgProgress.Visible = true;
                    lblProgress2.Visible = true;
                    prgProgress2.Visible = true;
                    treeOSes.Enabled = false;
                    btnAdd.Visible = false;
                    btnRemove.Visible = false;
                    btnCompress.Visible = false;
                    btnSave.Visible = false;
                    //btnHelp.Visible = false;
                    btnSettings.Enabled = false;
                    btnStop.Visible = true;

                    Workers.Failed += CompressToFailed;
                    Workers.Finished += CompressToFinished;
                    Workers.UpdateProgress += UpdateProgress;
                    Workers.UpdateProgress2 += UpdateProgress2;
                    thdCompressTo = new Thread(Workers.CompressTo);
                    thdCompressTo.Start();
                }
            }
        }

        public void CompressToFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                MessageBox.Show(text, MessageBoxButtons.OK, MessageBoxType.Error);
                lblProgress.Visible = false;
                lblProgress2.Visible = false;
                prgProgress.Visible = false;
                prgProgress2.Visible = false;
                treeOSes.Enabled = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnCompress.Visible = Context.usableDotNetZip;
                btnSave.Visible = true;
                btnHelp.Enabled = true;
                btnSettings.Enabled = true;
                btnStop.Visible = false;

                Workers.Failed -= CompressToFailed;
                Workers.Finished -= CompressToFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;

                if(thdCompressTo != null)
                {
                    thdCompressTo.Abort();
                    thdCompressTo = null;
                }

                Context.path = null;
            });
        }

        public void CompressToFinished()
        {
            Application.Instance.Invoke(delegate
            {
                lblProgress.Visible = false;
                lblProgress2.Visible = false;
                prgProgress.Visible = false;
                prgProgress2.Visible = false;
                treeOSes.Enabled = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnCompress.Visible = Context.usableDotNetZip;
                btnSave.Visible = true;
                btnHelp.Enabled = true;
                btnSettings.Enabled = true;
                btnStop.Visible = false;

                Workers.Failed -= CompressToFailed;
                Workers.Finished -= CompressToFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;

                if(thdCompressTo != null)
                {
                    thdCompressTo.Abort();
                    thdCompressTo = null;
                }

                MessageBox.Show(string.Format("Correctly compressed as {0}", Context.path));

                Context.path = null;
            });
        }

        protected void OnBtnStopFilesClicked(object sender, EventArgs e)
        {
            if(populatingFiles)
            {
                Workers.Failed -= LoadFilesFailed;
                Workers.Finished -= LoadFilesFinished;
                Workers.UpdateProgress -= UpdateFileProgress2;
                Workers.AddFile -= AddFile;
                Workers.AddFiles -= AddFiles;

                if(thdPopulateFiles != null)
                {
                    thdPopulateFiles.Abort();
                    thdPopulateFiles = null;
                }

                lstFiles.Clear();
                btnStopFiles.Visible = false;
                btnPopulateFiles.Visible = true;
            }

            if(scanningFiles)
            {
                if(thdScanFile != null)
                {
                    thdScanFile.Abort();
                    thdScanFile = null;
                }
            }
            AllClamdFinished();
        }

        protected void OnBtnToggleCrackClicked(object sender, EventArgs e)
        {
            if(treeFiles.SelectedItem != null)
            {
                DBFile file = (DBFile)treeFiles.SelectedItem;
                bool crack = !file.Crack;

                Workers.ToggleCrack(file.Sha256, crack);

                lstFiles.Remove(file);
                file.Crack = crack;
                lstFiles.Add(file);
            }
        }

        protected void OnBtnScanWithClamdClicked(object sender, EventArgs e)
        {
            if(treeFiles.SelectedItem != null)
            {
                DBFile file = Workers.GetDBFile(((DBFile)treeFiles.SelectedItem).Sha256);
                outIter = (osrepodbmgr.Core.DBFile)treeFiles.SelectedItem;

                if(file == null)
                {
                    MessageBox.Show("Cannot get file from database", MessageBoxType.Error);
                    return;
                }

                treeFiles.Enabled = false;
                btnToggleCrack.Enabled = false;
                btnScanWithClamd.Enabled = false;
                btnCheckInVirusTotal.Enabled = false;
                prgProgressFiles1.Visible = true;
                lblProgressFiles1.Visible = true;
                Workers.Failed += ClamdFailed;
                Workers.ScanFinished += ClamdFinished;
                Workers.UpdateProgress += UpdateVirusProgress;

                lblProgressFiles1.Text = "Scanning file with clamd.";
                prgProgressFiles1.Indeterminate = true;

                thdScanFile = new Thread(() => Workers.ClamScanFileFromRepo(file));
                thdScanFile.Start();
            }
        }

        void ClamdFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                treeFiles.Enabled = true;
                btnToggleCrack.Enabled = true;
                btnScanWithClamd.Enabled = true;
                btnCheckInVirusTotal.Enabled = true;
                prgProgressFiles1.Visible = false;
                lblProgressFiles1.Visible = false;
                Workers.Failed -= ClamdFailed;
                Workers.ScanFinished -= ClamdFinished;
                Workers.UpdateProgress -= UpdateVirusProgress;
                lblProgressFiles1.Text = "";
                if(thdScanFile != null)
                {
                    thdScanFile.Abort();
                    thdScanFile = null;
                }
            });
        }

        void ClamdFinished(DBFile file)
        {
            Application.Instance.Invoke(delegate
            {
                treeFiles.Enabled = true;
                btnToggleCrack.Enabled = true;
                btnScanWithClamd.Enabled = true;
                btnCheckInVirusTotal.Enabled = true;
                Workers.Failed -= ClamdFailed;
                Workers.ScanFinished -= ClamdFinished;
                Workers.UpdateProgress -= UpdateVirusProgress;
                lblProgressFiles1.Text = "";
                prgProgressFiles1.Visible = false;
                lblProgressFiles1.Visible = false;
                if(thdScanFile != null)
                    thdScanFile = null;

                if((!outIter.HasVirus.HasValue || (outIter.HasVirus.HasValue && !outIter.HasVirus.Value)) && file.HasVirus.HasValue && file.HasVirus.Value)
                    infectedFiles++;

                lstFiles.Remove(outIter);
                AddFile(file);

                lblFileStatus.Text = string.Format("{0} files ({1} infected)", lstFiles.Count, infectedFiles);
            });
        }

        protected void OnBtnCheckInVirusTotalClicked(object sender, EventArgs e)
        {
            if(treeFiles.SelectedItem != null)
            {
                DBFile file = Workers.GetDBFile(((DBFile)treeFiles.SelectedItem).Sha256);
                outIter = (osrepodbmgr.Core.DBFile)treeFiles.SelectedItem;

                if(file == null)
                {
                    MessageBox.Show("Cannot get file from database", MessageBoxType.Error);
                    return;
                }

                treeFiles.Enabled = false;
                btnToggleCrack.Enabled = false;
                btnScanWithClamd.Enabled = false;
                btnCheckInVirusTotal.Enabled = false;
                prgProgressFiles1.Visible = true;
                lblProgressFiles1.Visible = true;
                Workers.Failed += VirusTotalFailed;
                Workers.ScanFinished += VirusTotalFinished;
                Workers.UpdateProgress += UpdateVirusProgress;

                lblProgressFiles1.Text = "Scanning file with VirusTotal.";
                prgProgressFiles1.Indeterminate = true;

                thdScanFile = new Thread(() => Workers.VirusTotalFileFromRepo(file));
                thdScanFile.Start();
            }
        }

        void VirusTotalFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                treeFiles.Enabled = true;
                btnToggleCrack.Enabled = true;
                btnScanWithClamd.Enabled = true;
                btnCheckInVirusTotal.Enabled = true;
                prgProgressFiles1.Visible = false;
                Workers.Failed -= VirusTotalFailed;
                Workers.ScanFinished -= VirusTotalFinished;
                Workers.UpdateProgress -= UpdateVirusProgress;
                lblProgressFiles1.Text = "";
                if(thdScanFile != null)
                    thdScanFile = null;
                MessageBox.Show(text, MessageBoxType.Error);
            });
        }

        void VirusTotalFinished(DBFile file)
        {
            Application.Instance.Invoke(delegate
            {
                treeFiles.Enabled = true;
                btnToggleCrack.Enabled = true;
                btnScanWithClamd.Enabled = true;
                btnCheckInVirusTotal.Enabled = true;
                Workers.Failed -= VirusTotalFailed;
                Workers.ScanFinished -= VirusTotalFinished;
                Workers.UpdateProgress -= UpdateVirusProgress;
                lblProgressFiles1.Text = "";
                prgProgressFiles1.Visible = false;
                if(thdScanFile != null)
                    thdScanFile = null;

                if((!outIter.HasVirus.HasValue || (outIter.HasVirus.HasValue && !outIter.HasVirus.Value)) && file.HasVirus.HasValue && file.HasVirus.Value)
                    infectedFiles++;

                lstFiles.Remove(outIter);
                AddFile(file);

                lblFileStatus.Text = string.Format("{0} files ({1} infected)", lstFiles.Count, infectedFiles);
            });
        }

        public void UpdateVirusProgress(string text, string inner, long current, long maximum)
        {
            Application.Instance.Invoke(delegate
            {
                lblProgressFiles1.Text = text;
            });
        }

        protected void OnBtnPopulateFilesClicked(object sender, EventArgs e)
        {
            lstFiles.Clear();
            tabOSes.Enabled = false;
            btnStopFiles.Visible = true;
            btnPopulateFiles.Visible = false;

            lblProgressFiles1.Text = "Loading files from database";
            lblProgressFiles1.Visible = true;
            lblProgressFiles2.Visible = true;
            prgProgressFiles1.Visible = true;
            prgProgressFiles2.Visible = true;
            prgProgressFiles1.Indeterminate = true;
            Workers.Failed += LoadFilesFailed;
            Workers.Finished += LoadFilesFinished;
            Workers.UpdateProgress += UpdateFileProgress2;
            Workers.AddFile += AddFile;
            Workers.AddFiles += AddFiles;
            populatingFiles = true;
            infectedFiles = 0;
            thdPopulateFiles = new Thread(Workers.GetFilesFromDb);
            thdPopulateFiles.Start();
        }

        public void UpdateFileProgress(string text, string inner, long current, long maximum)
        {
            Application.Instance.Invoke(delegate
            {
                if(!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(inner))
                    lblProgressFiles1.Text = string.Format("{0}: {1}", text, inner);
                else if(!string.IsNullOrWhiteSpace(inner))
                    lblProgressFiles1.Text = inner;
                else
                    lblProgressFiles1.Text = text;
                if(maximum > 0)
                {
                    prgProgressFiles1.Indeterminate = false;
                    prgProgressFiles1.MinValue = 0;
                    prgProgressFiles1.MaxValue = (int)maximum;
                    prgProgressFiles1.Value = (int)current;
                }
                else
                    prgProgressFiles1.Indeterminate = true;
            });
        }

        public void UpdateFileProgress2(string text, string inner, long current, long maximum)
        {
            Application.Instance.Invoke(delegate
            {
                if(!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(inner))
                    lblProgressFiles2.Text = string.Format("{0}: {1}", text, inner);
                else if(!string.IsNullOrWhiteSpace(inner))
                    lblProgressFiles2.Text = inner;
                else
                    lblProgressFiles2.Text = text;
                if(maximum > 0)
                {
                    prgProgressFiles2.Indeterminate = false;
                    prgProgressFiles2.MinValue = 0;
                    prgProgressFiles2.MaxValue = (int)maximum;
                    prgProgressFiles2.Value = (int)current;
                }
                else
                    prgProgressFiles2.Indeterminate = true;
            });
        }

        void AddFile(Core.DBFile file)
        {
            Application.Instance.Invoke(delegate
            {
                if(file.HasVirus.HasValue && file.HasVirus.Value)
                    infectedFiles++;

                lstFiles.Add(file);
            });
        }

        void AddFiles(List<DBFile> files)
        {
            Application.Instance.Invoke(delegate
            {
                List<DBFile> foo = new List<DBFile>();
                foo.AddRange(lstFiles);
                foo.AddRange(files);
                lstFiles = new ObservableCollection<DBFile>(foo);

                foreach(DBFile file in files)
                    if(file.HasVirus.HasValue && file.HasVirus.Value)
                        infectedFiles++;
            });
        }


        void LoadFilesFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                MessageBox.Show(string.Format("Error {0} when populating files, exiting...", text), MessageBoxType.Error);
                Workers.Failed -= LoadFilesFailed;
                Workers.Finished -= LoadFilesFinished;
                Workers.UpdateProgress -= UpdateFileProgress2;
                if(thdPopulateFiles != null)
                {
                    thdPopulateFiles.Abort();
                    thdPopulateFiles = null;
                }
                tabOSes.Enabled = true;
                lstFiles.Clear();
                btnStopFiles.Visible = false;
                btnPopulateFiles.Visible = true;
                populatingFiles = false;
            });
        }

        void LoadFilesFinished()
        {
            Application.Instance.Invoke(delegate
            {
                Workers.Failed -= LoadFilesFailed;
                Workers.Finished -= LoadFilesFinished;
                Workers.UpdateProgress -= UpdateFileProgress2;
                if(thdPopulateFiles != null)
                {
                    thdPopulateFiles.Abort();
                    thdPopulateFiles = null;
                }
                treeFiles.DataStore = lstFiles;
                lblProgressFiles1.Visible = false;
                lblProgressFiles2.Visible = false;
                prgProgressFiles1.Visible = false;
                prgProgressFiles2.Visible = false;
                btnToggleCrack.Visible = true;
                btnScanWithClamd.Visible = true;
                btnCheckInVirusTotal.Visible = true;
                btnStopFiles.Visible = false;
                btnPopulateFiles.Visible = false;
                populatingFiles = false;
                treeFiles.Enabled = true;
                tabOSes.Enabled = true;
                btnScanAllPending.Visible = true;
                btnCleanFiles.Visible = true;
                lblFileStatus.Visible = true;
                lblFileStatus.Text = string.Format("{0} files ({1} infected)", lstFiles.Count, infectedFiles);
            });
        }

        void treeFilesSelectionChanged(object sender, EventArgs e)
        {
            if(treeFiles.SelectedItem != null)
            {
                if(((DBFile)treeFiles.SelectedItem).Crack)
                    btnToggleCrack.Text = "Mark as not crack";
                else
                    btnToggleCrack.Text = "Mark as crack";
            }
        }


        protected void OnBtnScanAllPendingClicked(object sender, EventArgs e)
        {
            treeFiles.Enabled = false;
            btnToggleCrack.Enabled = false;
            btnScanWithClamd.Enabled = false;
            btnCheckInVirusTotal.Enabled = false;
            lblProgressFiles1.Visible = true;
            lblProgressFiles2.Visible = true;
            prgProgressFiles1.Visible = true;
            prgProgressFiles2.Visible = true;
            btnScanAllPending.Enabled = false;
            Workers.Finished += AllClamdFinished;
            Workers.UpdateProgress += UpdateVirusProgress2;
            Workers.UpdateProgress2 += UpdateFileProgress;
            btnStopFiles.Visible = true;
            scanningFiles = true;

            lblProgressFiles2.Text = "Scanning file with clamd.";
            prgProgressFiles2.Indeterminate = true;

            thdScanFile = new Thread(Workers.ClamScanAllFiles);
            thdScanFile.Start();
        }

        void AllClamdFinished()
        {
            Application.Instance.Invoke(delegate
            {
                treeFiles.Enabled = true;
                btnToggleCrack.Enabled = true;
                btnScanWithClamd.Enabled = true;
                btnCheckInVirusTotal.Enabled = true;
                btnScanAllPending.Enabled = true;
                Workers.Finished -= AllClamdFinished;
                Workers.UpdateProgress -= UpdateVirusProgress2;
                Workers.UpdateProgress2 -= UpdateFileProgress;
                lblProgressFiles1.Text = "";
                prgProgressFiles1.Visible = false;
                lblProgressFiles2.Text = "";
                prgProgressFiles2.Visible = false;
                btnStopFiles.Visible = false;
                scanningFiles = false;
                if(thdScanFile != null)
                    thdScanFile = null;

                OnBtnPopulateFilesClicked(null, new EventArgs());
            });
        }

        public void UpdateVirusProgress2(string text, string inner, long current, long maximum)
        {
            Application.Instance.Invoke(delegate
            {
                lblProgressFiles2.Text = text;
            });
        }

        protected void OnBtnCleanFilesClicked(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("This option will search the database for any known file that doesn't\n" +
                                                     "belong to any OS and remove it from the database.\n\n" +
                                                     "It will then search the repository for any file not on the database and remove it.\n\n" +
                                                     "THIS OPERATION MAY VERY LONG, CANNOT BE CANCELED AND REMOVES DATA ON DISK.\n\n" +
                                                     "Are you sure to continue?", MessageBoxButtons.YesNo, MessageBoxType.Question); 
            if(result == DialogResult.Yes)
            {
                btnCleanFiles.Visible = false;
                btnToggleCrack.Visible = false;
                btnScanWithClamd.Visible = false;
                btnScanAllPending.Visible = false;
                btnCheckInVirusTotal.Visible = false;
                tabOSes.Enabled = false;
                treeFiles.Enabled = false;
                lblProgressFiles1.Visible = true;
                lblProgressFiles2.Visible = true;
                Workers.Finished += CleanFilesFinished;
                Workers.UpdateProgress += UpdateFileProgress;
                Workers.UpdateProgress2 += UpdateFileProgress2;
                lblProgressFiles1.Text = "";
                prgProgressFiles1.Visible = true;
                lblProgressFiles2.Text = "";
                prgProgressFiles2.Visible = true;
                btnStopFiles.Visible = false;
                prgProgress2.Indeterminate = true;
                btnSettings.Enabled = false;
                btnHelp.Enabled = false;
                mnuCompress.Enabled = false;
                btnQuit.Enabled = false;
                mnuFile.Enabled = false;
                thdCleanFiles = new Thread(Workers.CleanFiles);
                thdCleanFiles.Start();
            }
        }

        void CleanFilesFinished()
        {
            Application.Instance.Invoke(delegate
            {
                btnCleanFiles.Visible = true;
                btnToggleCrack.Visible = true;
                btnScanWithClamd.Visible = true;
                btnScanAllPending.Visible = true;
                btnCheckInVirusTotal.Visible = true;
                tabOSes.Enabled = true;
                treeFiles.Enabled = true;
                Workers.Finished -= CleanFilesFinished;
                Workers.UpdateProgress -= UpdateFileProgress;
                Workers.UpdateProgress2 -= UpdateFileProgress2;
                lblProgressFiles1.Text = "";
                prgProgressFiles1.Visible = false;
                lblProgressFiles2.Text = "";
                prgProgressFiles2.Visible = false;
                btnSettings.Enabled = true;
                btnHelp.Enabled = true;
                mnuCompress.Enabled = true;
                btnQuit.Enabled = true;
                mnuFile.Enabled = true;
                if(thdCleanFiles != null)
                    thdCleanFiles = null;

                OnBtnPopulateFilesClicked(null, new EventArgs());
            });
        }
    }
}
