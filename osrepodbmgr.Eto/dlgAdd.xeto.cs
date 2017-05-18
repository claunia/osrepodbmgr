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
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;
using Newtonsoft.Json;
using osrepodbmgr.Core;
using Schemas;

namespace osrepodbmgr.Eto
{
    public class dlgAdd : Dialog
    {
        Thread thdFindFiles;
        Thread thdHashFiles;
        Thread thdCheckFiles;
        Thread thdAddFiles;
        Thread thdPackFiles;
        Thread thdOpenArchive;
        Thread thdExtractArchive;
        Thread thdRemoveTemp;
        bool stopped;
        ObservableCollection<FileEntry> fileView;
        ObservableCollection<DBEntryForEto> osView;

        class FileEntry
        {
            public string path { get; set; }
            public string hash { get; set; }
            public bool known { get; set; }
        }

        #region XAML UI elements
#pragma warning disable 0649
        TextBox txtDeveloper;
        TextBox txtProduct;
        TextBox txtVersion;
        TextBox txtLanguages;
        TextBox txtArchitecture;
        TextBox txtMachine;
        TextBox txtFormat;
        TextBox txtDescription;
        CheckBox chkOem;
        CheckBox chkUpdate;
        CheckBox chkUpgrade;
        CheckBox chkFiles;
        CheckBox chkSource;
        CheckBox chkNetinstall;
        GridView treeFiles;
        TabPage tabOSes;
        GridView treeOSes;
        Label lblProgress;
        ProgressBar prgProgress;
        Label lblProgress2;
        ProgressBar prgProgress2;
        Button btnRemoveFile;
        Button btnMetadata;
        Button btnStop;
        Button btnFolder;
        Button btnArchive;
        Button btnPack;
        Button btnClose;
        Button btnExit;
#pragma warning restore 0649
        #endregion XAML UI elements

        public delegate void OnAddedOSDelegate(DBEntry os, bool existsInRepo, string pathInRepo);
        public event OnAddedOSDelegate OnAddedOS;

        public dlgAdd()
        {
            XamlReader.Load(this);

            Context.UnarChangeStatus += UnarChangeStatus;
            Context.CheckUnar();

            fileView = new ObservableCollection<FileEntry>();

            treeFiles.DataStore = fileView;

            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<FileEntry, string>(r => r.path) },
                HeaderText = "Path"
            });
            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<FileEntry, string>(r => r.hash) },
                HeaderText = "SHA256"
            });
            treeFiles.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<FileEntry, bool?>(r => r.known) },
                HeaderText = "Known?"
            });

            treeFiles.AllowMultipleSelection = false;

            treeFiles.CellFormatting += (sender, e) =>
            {
                if(((FileEntry)e.Item).known)
                    e.BackgroundColor = Colors.Green;
                else
                    e.BackgroundColor = Colors.Red;
            };

            osView = new ObservableCollection<DBEntryForEto>();

            treeOSes.DataStore = osView;

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
        }

        void UnarChangeStatus()
        {
            Application.Instance.Invoke(delegate
            {
                btnArchive.Enabled = Context.unarUsable;
            });
        }

        protected void OnDeleteEvent(object sender, CancelEventArgs e)
        {
            if(btnStop.Visible)
                btnStop.PerformClick();
            if(btnClose.Enabled)
                btnClose.PerformClick();
        }

        protected void OnBtnFolderClicked(object sender, EventArgs e)
        {
            SelectFolderDialog dlgFolder = new SelectFolderDialog();
            dlgFolder.Title = "Open folder";

            if(dlgFolder.ShowDialog(this) == DialogResult.Ok)
            {
                stopped = false;
                lblProgress.Text = "Finding files";
                lblProgress.Visible = true;
                prgProgress.Visible = true;
                btnExit.Enabled = false;
                btnFolder.Visible = false;
                btnArchive.Visible = false;
                thdFindFiles = new Thread(Workers.FindFiles);
                Context.path = dlgFolder.Directory;
                Workers.Failed += FindFilesFailed;
                Workers.Finished += FindFilesFinished;
                btnStop.Visible = true;
                thdFindFiles.Start();
            }
        }

        void FindFilesFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                if(!stopped)
                    MessageBox.Show(text, MessageBoxType.Error);
                lblProgress.Visible = false;
                prgProgress.Visible = false;
                btnExit.Enabled = true;
                btnFolder.Visible = true;
                btnArchive.Visible = true;
                Workers.Failed -= FindFilesFailed;
                Workers.Finished -= FindFilesFinished;
                thdFindFiles = null;
            });
        }

        void FindFilesFinished()
        {
            Application.Instance.Invoke(delegate
            {
                Workers.Failed -= FindFilesFailed;
                Workers.Finished -= FindFilesFinished;

                lblProgress.Visible = true;
                prgProgress.Visible = true;
                lblProgress2.Visible = true;
                prgProgress2.Visible = true;

                thdFindFiles = null;
                thdHashFiles = new Thread(Workers.HashFiles);
                Workers.Failed += HashFilesFailed;
                Workers.Finished += HashFilesFinished;
                Workers.UpdateProgress += UpdateProgress;
                Workers.UpdateProgress2 += UpdateProgress2;
                thdHashFiles.Start();
            });
        }

        void HashFilesFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                if(!stopped)
                    MessageBox.Show(text, MessageBoxType.Error);
                lblProgress.Visible = false;
                prgProgress.Visible = false;
                lblProgress2.Visible = false;
                prgProgress2.Visible = false;
                Workers.Failed -= HashFilesFailed;
                Workers.Finished -= HashFilesFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;
                btnExit.Enabled = true;
                btnFolder.Visible = true;
                btnArchive.Visible = true;
                thdHashFiles = null;
            });
        }

        void HashFilesFinished()
        {
            Application.Instance.Invoke(delegate
            {
                lblProgress.Visible = false;
                prgProgress.Visible = false;
                lblProgress.Visible = false;
                prgProgress.Visible = false;
                lblProgress2.Visible = false;
                prgProgress2.Visible = false;
                Workers.Failed -= HashFilesFailed;
                Workers.Finished -= HashFilesFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;
                thdHashFiles = null;

                lblProgress.Visible = false;
                prgProgress.Visible = true;

                thdCheckFiles = new Thread(Workers.CheckDbForFiles);
                Workers.Failed += ChkFilesFailed;
                Workers.Finished += ChkFilesFinished;
                Workers.UpdateProgress += UpdateProgress;
                Workers.UpdateProgress2 += UpdateProgress2;
                Workers.AddFileForOS += AddFile;
                Workers.AddOS += AddOS;
                thdCheckFiles.Start();
            });
        }

        void ChkFilesFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                if(!stopped)
                    MessageBox.Show(text, MessageBoxType.Error);
                prgProgress.Visible = false;
                btnStop.Visible = false;
                btnClose.Visible = false;
                btnExit.Enabled = true;
                Workers.Failed -= ChkFilesFailed;
                Workers.Finished -= ChkFilesFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;
                Workers.AddFileForOS -= AddFile;
                Workers.AddOS -= AddOS;
                if(thdCheckFiles != null)
                    thdCheckFiles.Abort();
                thdHashFiles = null;
                if(fileView != null)
                    fileView.Clear();
                if(osView != null)
                {
                    tabOSes.Visible = false;
                    osView.Clear();
                }
            });
        }

        void ChkFilesFinished()
        {
            Application.Instance.Invoke(delegate
            {
                Workers.Failed -= ChkFilesFailed;
                Workers.Finished -= ChkFilesFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;
                Workers.AddFileForOS -= AddFile;
                Workers.AddOS -= AddOS;

                if(thdCheckFiles != null)
                    thdCheckFiles.Abort();

                thdHashFiles = null;
                prgProgress.Visible = false;
                btnStop.Visible = false;
                btnClose.Visible = true;
                btnExit.Enabled = true;
                btnPack.Visible = true;
                btnPack.Enabled = true;
                btnRemoveFile.Visible = true;

                txtFormat.ReadOnly = false;
                txtMachine.ReadOnly = false;
                txtProduct.ReadOnly = false;
                txtVersion.ReadOnly = false;
                txtLanguages.ReadOnly = false;
                txtDeveloper.ReadOnly = false;
                txtDescription.ReadOnly = false;
                txtArchitecture.ReadOnly = false;
                chkOem.Enabled = true;
                chkFiles.Enabled = true;
                chkUpdate.Enabled = true;
                chkUpgrade.Enabled = true;
                chkNetinstall.Enabled = true;
                chkSource.Enabled = true;

                btnMetadata.Visible = true;
                if(Context.metadata != null)
                {
                    if(Context.metadata.Developer != null)
                    {
                        foreach(string developer in Context.metadata.Developer)
                        {
                            if(!string.IsNullOrWhiteSpace(txtDeveloper.Text))
                                txtDeveloper.Text += ",";
                            txtDeveloper.Text += developer;
                        }
                    }

                    if(!string.IsNullOrWhiteSpace(Context.metadata.Name))
                        txtProduct.Text = Context.metadata.Name;
                    if(!string.IsNullOrWhiteSpace(Context.metadata.Version))
                        txtVersion.Text = Context.metadata.Version;

                    if(Context.metadata.Languages != null)
                    {
                        foreach(LanguagesTypeLanguage language in Context.metadata.Languages)
                        {
                            if(!string.IsNullOrWhiteSpace(txtLanguages.Text))
                                txtLanguages.Text += ",";
                            txtLanguages.Text += language;
                        }
                    }

                    if(Context.metadata.Architectures != null)
                    {
                        foreach(ArchitecturesTypeArchitecture architecture in Context.metadata.Architectures)
                        {
                            if(!string.IsNullOrWhiteSpace(txtArchitecture.Text))
                                txtArchitecture.Text += ",";
                            txtArchitecture.Text += architecture;
                        }
                    }

                    if(Context.metadata.Systems != null)
                    {
                        foreach(string machine in Context.metadata.Systems)
                        {
                            if(!string.IsNullOrWhiteSpace(txtMachine.Text))
                                txtMachine.Text += ",";
                            txtMachine.Text += machine;
                        }
                    }

                    btnMetadata.BackgroundColor = Colors.Green;
                }
                else
                    btnMetadata.BackgroundColor = Colors.Red;
            });
        }

        void AddFile(string filename, string hash, bool known)
        {
            Application.Instance.Invoke(delegate
            {
                fileView.Add(new FileEntry { path = filename, hash = hash, known = known });
                btnPack.Enabled |= !known;
            });
        }

        void AddOS(DBEntry os, bool existsInRepo, string pathInRepo)
        {
            Application.Instance.Invoke(delegate
            {
                tabOSes.Visible = true;
                osView.Add(new DBEntryForEto(os));
            });
        }

        protected void OnBtnExitClicked(object sender, EventArgs e)
        {
            if(btnClose.Enabled)
                OnBtnCloseClicked(sender, e);

            Close();
        }

        protected void OnBtnCloseClicked(object sender, EventArgs e)
        {
            btnFolder.Visible = true;
            btnArchive.Visible = true;
            Context.path = "";
            Context.files = null;
            Context.hashes = null;
            btnStop.Visible = false;
            btnPack.Visible = false;
            btnClose.Visible = false;
            btnRemoveFile.Visible = false;
            if(fileView != null)
                fileView.Clear();
            if(osView != null)
            {
                tabOSes.Visible = false;
                osView.Clear();
            }
            txtFormat.ReadOnly = true;
            txtMachine.ReadOnly = true;
            txtProduct.ReadOnly = true;
            txtVersion.ReadOnly = true;
            txtLanguages.ReadOnly = true;
            txtDeveloper.ReadOnly = true;
            txtDescription.ReadOnly = true;
            txtArchitecture.ReadOnly = true;
            chkOem.Enabled = false;
            chkFiles.Enabled = false;
            chkUpdate.Enabled = false;
            chkUpgrade.Enabled = false;
            chkNetinstall.Enabled = false;
            chkSource.Enabled = false;
            txtFormat.Text = "";
            txtMachine.Text = "";
            txtProduct.Text = "";
            txtVersion.Text = "";
            txtLanguages.Text = "";
            txtDeveloper.Text = "";
            txtDescription.Text = "";
            txtArchitecture.Text = "";
            chkOem.Checked = false;
            chkFiles.Checked = false;
            chkUpdate.Checked = false;
            chkUpgrade.Checked = false;
            chkNetinstall.Checked = false;
            chkSource.Checked = false;

            if(Context.tmpFolder != null)
            {
                btnStop.Visible = false;
                prgProgress.Visible = true;
                lblProgress.Visible = true;
                lblProgress.Text = "Removing temporary files";
                prgProgress.Indeterminate = true;
                Workers.Failed += RemoveTempFilesFailed;
                Workers.Finished += RemoveTempFilesFinished;
                thdRemoveTemp = new Thread(Workers.RemoveTempFolder);
                thdRemoveTemp.Start();
            }

            btnMetadata.Visible = false;
            Context.metadata = null;
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

        protected void OnBtnStopClicked(object sender, EventArgs e)
        {
            stopped = true;

            Workers.AddFileForOS -= AddFile;
            Workers.AddOS -= AddOS;
            Workers.Failed -= AddFilesToDbFailed;
            Workers.Failed -= ChkFilesFailed;
            Workers.Failed -= ExtractArchiveFailed;
            Workers.Failed -= FindFilesFailed;
            Workers.Failed -= HashFilesFailed;
            Workers.Failed -= OpenArchiveFailed;
            Workers.Failed -= PackFilesFailed;
            Workers.Failed -= RemoveTempFilesFailed;
            Workers.Finished -= AddFilesToDbFinished;
            Workers.Finished -= ChkFilesFinished;
            Workers.Finished -= ExtractArchiveFinished;
            Workers.Finished -= FindFilesFinished;
            Workers.Finished -= HashFilesFinished;
            Workers.Finished -= OpenArchiveFinished;
            Workers.Finished -= RemoveTempFilesFinished;
            Workers.FinishedWithText -= PackFilesFinished;
            Workers.UpdateProgress -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;

            if(thdFindFiles != null)
            {
                thdFindFiles.Abort();
                thdFindFiles = null;
            }

            if(thdHashFiles != null)
            {
                thdHashFiles.Abort();
                thdHashFiles = null;
            }

            if(thdCheckFiles != null)
            {
                thdCheckFiles.Abort();
                thdCheckFiles = null;
            }

            if(thdAddFiles != null)
            {
                thdAddFiles.Abort();
                thdAddFiles = null;
            }

            if(thdPackFiles != null)
            {
                thdPackFiles.Abort();
                thdPackFiles = null;
            }

            if(thdOpenArchive != null)
            {
                thdOpenArchive.Abort();
                thdOpenArchive = null;
            }

            if(Context.unarProcess != null)
            {
                Context.unarProcess.Kill();
                Context.unarProcess = null;
            }

            if(Context.tmpFolder != null)
            {
                btnStop.Visible = false;
                lblProgress.Text = "Removing temporary files";
                prgProgress.Indeterminate = true;
                Workers.Failed += RemoveTempFilesFailed;
                Workers.Finished += RemoveTempFilesFinished;
                thdRemoveTemp = new Thread(Workers.RemoveTempFolder);
                thdRemoveTemp.Start();
            }
            else
                RestoreUI();
        }

        public void RestoreUI()
        {
            lblProgress.Visible = false;
            prgProgress.Visible = false;
            lblProgress2.Visible = false;
            prgProgress2.Visible = false;
            btnExit.Enabled = true;
            btnFolder.Visible = true;
            btnArchive.Visible = true;
            lblProgress.Visible = false;
            prgProgress.Visible = false;
            btnExit.Enabled = true;
            btnFolder.Visible = true;
            btnArchive.Visible = true;
            Workers.Failed -= FindFilesFailed;
            Workers.Failed -= HashFilesFailed;
            Workers.Failed -= ChkFilesFailed;
            Workers.Failed -= OpenArchiveFailed;
            Workers.Failed -= AddFilesToDbFailed;
            Workers.Failed -= PackFilesFailed;
            Workers.Failed -= ExtractArchiveFailed;
            Workers.Failed -= RemoveTempFilesFailed;
            Workers.Finished -= FindFilesFinished;
            Workers.Finished -= HashFilesFinished;
            Workers.Finished -= ChkFilesFinished;
            Workers.Finished -= OpenArchiveFinished;
            Workers.Finished -= AddFilesToDbFinished;
            Workers.Finished -= ExtractArchiveFinished;
            Workers.Finished -= RemoveTempFilesFinished;
            Workers.FinishedWithText -= PackFilesFinished;
            Workers.UpdateProgress -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            btnStop.Visible = false;
            if(fileView != null)
                fileView.Clear();
            if(osView != null)
            {
                tabOSes.Visible = false;
                osView.Clear();
            }
        }

        public void RemoveTempFilesFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                MessageBox.Show(text, MessageBoxType.Error);
                Workers.Failed -= RemoveTempFilesFailed;
                Workers.Finished -= RemoveTempFilesFinished;
                Context.path = null;
                Context.tmpFolder = null;
                RestoreUI();
            });
        }

        public void RemoveTempFilesFinished()
        {
            Application.Instance.Invoke(delegate
            {
                Workers.Failed -= RemoveTempFilesFailed;
                Workers.Finished -= RemoveTempFilesFinished;
                Context.path = null;
                Context.tmpFolder = null;
                RestoreUI();
            });
        }

        void AddToDatabase()
        {
            btnRemoveFile.Enabled = false;
            btnPack.Enabled = false;
            btnClose.Enabled = false;
            prgProgress.Visible = true;
            txtFormat.ReadOnly = true;
            txtMachine.ReadOnly = true;
            txtProduct.ReadOnly = true;
            txtVersion.ReadOnly = true;
            txtLanguages.ReadOnly = true;
            txtDeveloper.ReadOnly = true;
            txtDescription.ReadOnly = true;
            txtArchitecture.ReadOnly = true;
            chkOem.Enabled = false;
            chkFiles.Enabled = false;
            chkUpdate.Enabled = false;
            chkUpgrade.Enabled = false;
            chkNetinstall.Enabled = false;
            chkSource.Enabled = false;

            Workers.UpdateProgress += UpdateProgress;
            Workers.Finished += AddFilesToDbFinished;
            Workers.Failed += AddFilesToDbFailed;

            Context.dbInfo.architecture = txtArchitecture.Text;
            Context.dbInfo.description = txtDescription.Text;
            Context.dbInfo.developer = txtDeveloper.Text;
            Context.dbInfo.format = txtFormat.Text;
            Context.dbInfo.languages = txtLanguages.Text;
            Context.dbInfo.machine = txtMachine.Text;
            Context.dbInfo.product = txtProduct.Text;
            Context.dbInfo.version = txtVersion.Text;
            Context.dbInfo.files = chkFiles.Checked.Value;
            Context.dbInfo.netinstall = chkNetinstall.Checked.Value;
            Context.dbInfo.oem = chkOem.Checked.Value;
            Context.dbInfo.source = chkSource.Checked.Value;
            Context.dbInfo.update = chkUpdate.Checked.Value;
            Context.dbInfo.upgrade = chkUpgrade.Checked.Value;

            if(Context.metadata != null)
            {
                MemoryStream ms = new MemoryStream();
                XmlSerializer xs = new XmlSerializer(typeof(CICMMetadataType));
                xs.Serialize(ms, Context.metadata);
                Context.dbInfo.xml = ms.ToArray();
                JsonSerializer js = new JsonSerializer();
                ms = new MemoryStream();
                StreamWriter sw = new StreamWriter(ms);
                js.Serialize(sw, Context.metadata, typeof(CICMMetadataType));
                Context.dbInfo.json = ms.ToArray();
            }
            else
            {
                Context.dbInfo.xml = null;
                Context.dbInfo.json = null;
            }

            thdAddFiles = new Thread(Workers.AddFilesToDb);
            thdAddFiles.Start();
        }

        public void AddFilesToDbFinished()
        {
            Application.Instance.Invoke(delegate
            {
                Workers.UpdateProgress -= UpdateProgress;
                Workers.Finished -= AddFilesToDbFinished;
                Workers.Failed -= AddFilesToDbFailed;

                if(thdAddFiles != null)
                    thdAddFiles.Abort();

                long counter = 0;
                fileView.Clear();
                foreach(KeyValuePair<string, DBOSFile> kvp in Context.hashes)
                {
                    UpdateProgress(null, "Updating table", counter, Context.hashes.Count);
                    fileView.Add(new FileEntry { path = kvp.Key, hash = kvp.Value.Sha256, known = true });
                    counter++;
                }

                // TODO: Update OS table

                if(OnAddedOS != null)
                    OnAddedOS(Context.dbInfo, true, System.IO.Path.Combine(osrepodbmgr.Core.Settings.Current.RepositoryPath,
                                                                           Context.dbInfo.mdid[0].ToString(),
                                                                           Context.dbInfo.mdid[1].ToString(),
                                                                           Context.dbInfo.mdid[2].ToString(),
                                                                           Context.dbInfo.mdid[3].ToString(),
                                                                           Context.dbInfo.mdid[4].ToString(),
                                                                           Context.dbInfo.mdid) + ".zip");

                lblProgress.Visible = false;
                prgProgress.Visible = false;
                btnClose.Enabled = true;
            });
        }

        public void AddFilesToDbFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                if(!stopped)
                    MessageBox.Show(text, MessageBoxType.Error);

                Workers.UpdateProgress -= UpdateProgress;
                Workers.Finished -= AddFilesToDbFinished;
                Workers.Failed -= AddFilesToDbFailed;

                if(thdAddFiles != null)
                    thdAddFiles.Abort();

                ChkFilesFinished();
            });
        }

        protected void OnBtnPackClicked(object sender, EventArgs e)
        {
            btnRemoveFile.Enabled = false;
            btnPack.Enabled = false;
            btnClose.Enabled = false;
            prgProgress.Visible = true;
            prgProgress2.Visible = true;
            lblProgress.Visible = true;
            lblProgress2.Visible = true;
            txtFormat.ReadOnly = true;
            txtMachine.ReadOnly = true;
            txtProduct.ReadOnly = true;
            txtVersion.ReadOnly = true;
            txtLanguages.ReadOnly = true;
            txtDeveloper.ReadOnly = true;
            txtDescription.ReadOnly = true;
            txtArchitecture.ReadOnly = true;
            chkOem.Enabled = false;
            chkFiles.Enabled = false;
            chkUpdate.Enabled = false;
            chkUpgrade.Enabled = false;
            chkNetinstall.Enabled = false;
            chkSource.Enabled = false;

            Workers.UpdateProgress += UpdateProgress;
            Workers.UpdateProgress2 += UpdateProgress2;
            Workers.FinishedWithText += PackFilesFinished;
            Workers.Failed += PackFilesFailed;

            Context.dbInfo.architecture = txtArchitecture.Text;
            Context.dbInfo.description = txtDescription.Text;
            Context.dbInfo.developer = txtDeveloper.Text;
            Context.dbInfo.format = txtFormat.Text;
            Context.dbInfo.languages = txtLanguages.Text;
            Context.dbInfo.machine = txtMachine.Text;
            Context.dbInfo.product = txtProduct.Text;
            Context.dbInfo.version = txtVersion.Text;
            Context.dbInfo.files = chkFiles.Checked.Value;
            Context.dbInfo.netinstall = chkNetinstall.Checked.Value;
            Context.dbInfo.oem = chkOem.Checked.Value;
            Context.dbInfo.source = chkSource.Checked.Value;
            Context.dbInfo.update = chkUpdate.Checked.Value;
            Context.dbInfo.upgrade = chkUpgrade.Checked.Value;

            thdPackFiles = new Thread(Workers.CompressFiles);
            thdPackFiles.Start();
        }

        public void PackFilesFinished(string text)
        {
            Application.Instance.Invoke(delegate
            {
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;
                Workers.FinishedWithText -= PackFilesFinished;
                Workers.Failed -= PackFilesFailed;
                prgProgress2.Visible = false;
                lblProgress2.Visible = false;

                if(thdPackFiles != null)
                    thdPackFiles.Abort();

                AddToDatabase();

                MessageBox.Show(text);
            });
        }

        public void PackFilesFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                if(!stopped)
                    MessageBox.Show(text, MessageBoxType.Error);

                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;
                Workers.FinishedWithText -= PackFilesFinished;
                Workers.Failed -= PackFilesFailed;

                if(thdPackFiles != null)
                    thdPackFiles.Abort();

                btnRemoveFile.Enabled = true;
                btnPack.Enabled = true;
                btnClose.Enabled = true;
                prgProgress.Visible = false;
                prgProgress2.Visible = false;
                lblProgress.Visible = false;
                lblProgress2.Visible = false;
                txtFormat.ReadOnly = false;
                txtMachine.ReadOnly = false;
                txtProduct.ReadOnly = false;
                txtVersion.ReadOnly = false;
                txtLanguages.ReadOnly = false;
                txtDeveloper.ReadOnly = false;
                txtDescription.ReadOnly = false;
                txtArchitecture.ReadOnly = false;
                chkOem.Enabled = true;
                chkFiles.Enabled = true;
                chkUpdate.Enabled = true;
                chkUpgrade.Enabled = true;
                chkNetinstall.Enabled = true;
                chkSource.Enabled = true;
            });
        }

        protected void OnBtnArchiveClicked(object sender, EventArgs e)
        {
            if(!Context.unarUsable)
            {
                MessageBox.Show("Cannot open archives without a working unar installation.", MessageBoxType.Error);
                return;
            }

            OpenFileDialog dlgFile = new OpenFileDialog();
            dlgFile.Title = "Open archive";
            dlgFile.MultiSelect = false;

            if(dlgFile.ShowDialog(this) == DialogResult.Ok)
            {
                stopped = false;
                lblProgress.Text = "Opening archive";
                lblProgress.Visible = false;
                prgProgress.Visible = true;
                btnExit.Enabled = false;
                btnFolder.Visible = false;
                btnArchive.Visible = false;
                prgProgress.Indeterminate = true;

                thdOpenArchive = new Thread(Workers.OpenArchive);
                Context.path = dlgFile.FileName;
                Workers.Failed += OpenArchiveFailed;
                Workers.Finished += OpenArchiveFinished;
                btnStop.Visible = true;
                thdOpenArchive.Start();
            }
        }

        public void OpenArchiveFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                if(!stopped)
                    MessageBox.Show(text, MessageBoxType.Error);
                lblProgress.Visible = false;
                prgProgress.Visible = false;
                btnExit.Enabled = true;
                btnFolder.Visible = true;
                btnArchive.Visible = true;
                Workers.Failed -= OpenArchiveFailed;
                Workers.Finished -= OpenArchiveFinished;
                thdOpenArchive = null;
            });
        }

        public void OpenArchiveFinished()
        {
            Application.Instance.Invoke(delegate
            {
                stopped = false;
                lblProgress.Text = "Extracting archive";
                prgProgress.Visible = true;
                prgProgress2.Visible = true;
                btnExit.Enabled = false;
                btnFolder.Visible = false;
                btnArchive.Visible = false;
                Workers.UpdateProgress += UpdateProgress;
                lblProgress.Visible = true;
                lblProgress2.Visible = true;
                Workers.Failed -= OpenArchiveFailed;
                Workers.Finished -= OpenArchiveFinished;
                thdOpenArchive = null;
                Workers.Failed += ExtractArchiveFailed;
                Workers.Finished += ExtractArchiveFinished;
                Workers.UpdateProgress2 += UpdateProgress2;
                thdExtractArchive = new Thread(Workers.ExtractArchive);
                thdExtractArchive.Start();
            });
        }

        public void ExtractArchiveFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                if(!stopped)
                    MessageBox.Show(text, MessageBoxType.Error);
                lblProgress2.Visible = false;
                prgProgress2.Visible = false;
                btnExit.Enabled = true;
                btnFolder.Visible = true;
                btnArchive.Visible = true;
                Workers.Failed -= ExtractArchiveFailed;
                Workers.Finished -= ExtractArchiveFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;
                thdExtractArchive = null;
                if(Context.tmpFolder != null)
                {
                    btnStop.Visible = false;
                    lblProgress.Text = "Removing temporary files";
                    prgProgress.Indeterminate = true;
                    Workers.Failed += RemoveTempFilesFailed;
                    Workers.Finished += RemoveTempFilesFinished;
                    thdRemoveTemp = new Thread(Workers.RemoveTempFolder);
                    thdRemoveTemp.Start();
                }
            });
        }

        public void ExtractArchiveFinished()
        {
            Application.Instance.Invoke(delegate
            {
                stopped = false;
                lblProgress.Text = "Finding files";
                lblProgress.Visible = true;
                lblProgress2.Visible = false;
                prgProgress.Visible = true;
                btnExit.Enabled = false;
                btnFolder.Visible = false;
                btnArchive.Visible = false;
                Workers.Failed -= ExtractArchiveFailed;
                Workers.Finished -= ExtractArchiveFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;
                prgProgress.Indeterminate = true;

                if(thdExtractArchive != null)
                    thdExtractArchive.Abort();
                thdFindFiles = new Thread(Workers.FindFiles);
                Workers.Failed += FindFilesFailed;
                Workers.Finished += FindFilesFinished;
                btnStop.Visible = true;
                thdFindFiles.Start();
            });
        }

        protected void OnBtnMetadataClicked(object sender, EventArgs e)
        {
            dlgMetadata _dlgMetadata = new dlgMetadata();
            _dlgMetadata.Metadata = Context.metadata;
            _dlgMetadata.FillFields();

            _dlgMetadata.ShowModal(this);

            if(_dlgMetadata.Modified)
            {
                Context.metadata = _dlgMetadata.Metadata;

                if(string.IsNullOrWhiteSpace(txtDeveloper.Text))
                {
                    foreach(string developer in Context.metadata.Developer)
                    {
                        if(!string.IsNullOrWhiteSpace(txtDeveloper.Text))
                            txtDeveloper.Text += ",";
                        txtDeveloper.Text += developer;
                    }
                }

                if(string.IsNullOrWhiteSpace(txtProduct.Text))
                {
                    if(!string.IsNullOrWhiteSpace(Context.metadata.Name))
                        txtProduct.Text = Context.metadata.Name;
                }

                if(string.IsNullOrWhiteSpace(txtVersion.Text))
                {
                    if(!string.IsNullOrWhiteSpace(Context.metadata.Version))
                        txtVersion.Text = Context.metadata.Version;
                }

                if(string.IsNullOrWhiteSpace(txtLanguages.Text))
                {
                    if(Context.metadata.Languages != null)
                    {
                        foreach(LanguagesTypeLanguage language in Context.metadata.Languages)
                        {
                            if(!string.IsNullOrWhiteSpace(txtLanguages.Text))
                                txtLanguages.Text += ",";
                            txtLanguages.Text += language;
                        }
                    }
                }

                if(string.IsNullOrWhiteSpace(txtArchitecture.Text))
                {
                    if(Context.metadata.Architectures != null)
                    {
                        foreach(ArchitecturesTypeArchitecture architecture in Context.metadata.Architectures)
                        {
                            if(!string.IsNullOrWhiteSpace(txtArchitecture.Text))
                                txtArchitecture.Text += ",";
                            txtArchitecture.Text += architecture;
                        }
                    }
                }

                if(string.IsNullOrWhiteSpace(txtMachine.Text))
                {
                    if(Context.metadata.Systems != null)
                    {
                        foreach(string machine in Context.metadata.Systems)
                        {
                            if(!string.IsNullOrWhiteSpace(txtMachine.Text))
                                txtMachine.Text += ",";
                            txtMachine.Text += machine;
                        }
                    }
                }

                btnMetadata.BackgroundColor = Colors.Green;
            }
        }

        protected void OnBtnRemoveFileClicked(object sender, EventArgs e)
        {
            if(treeFiles.SelectedItem != null)
            {
                string name = ((FileEntry)treeFiles.SelectedItem).path;
                string filesPath;

                if(!string.IsNullOrEmpty(Context.tmpFolder) && Directory.Exists(Context.tmpFolder))
                    filesPath = Context.tmpFolder;
                else
                    filesPath = Context.path;

                Context.hashes.Remove(name);
                Context.files.Remove(System.IO.Path.Combine(filesPath, name));
                fileView.Remove((FileEntry)treeFiles.SelectedItem);
            }
        }
    }
}