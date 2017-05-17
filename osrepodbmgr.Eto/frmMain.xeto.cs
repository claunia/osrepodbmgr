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
#pragma warning restore 0649
        #endregion XAML UI elements

        ObservableCollection<DBEntryForEto> lstOSes;

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
                if(thdPopulateOSes != null)
                {
                    thdPopulateOSes.Abort();
                    thdPopulateOSes = null;
                }
                Workers.Failed -= LoadOSesFailed;
                Workers.Finished -= LoadOSesFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.AddOS -= AddOS;
                lblProgress.Visible = false;
                prgProgress.Visible = false;
                treeOSes.Enabled = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnCompress.Visible = Context.usableDotNetZip;
                btnSave.Visible = true;
                btnHelp.Enabled = true;
                btnSettings.Enabled = true;
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

        void AddOS(DBEntry os, bool existsInRepo, string pathInRepo)
        {
            Application.Instance.Invoke(delegate
            {
                lstOSes.Add(new DBEntryForEto(os));
            });
        }

        protected void OnBtnAddClicked(object sender, EventArgs e)
        {
            dlgAdd _dlgAdd = new dlgAdd();
            _dlgAdd.OnAddedOS += (os, existsInRepo, pathInRepo) =>
            {
                Color color = existsInRepo ? Colors.Green : Colors.Red;
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
                if(thdSaveAs != null)
                {
                    thdSaveAs.Abort();
                    thdSaveAs = null;
                }

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

                Context.path = null;
            });
        }

        public void SaveAsFinished()
        {
            Application.Instance.Invoke(delegate
            {
                if(thdSaveAs != null)
                {
                    thdSaveAs.Abort();
                    thdSaveAs = null;
                }

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
                if(thdCompressTo != null)
                {
                    thdCompressTo.Abort();
                    thdCompressTo = null;
                }

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

                Context.path = null;
            });
        }

        public void CompressToFinished()
        {
            Application.Instance.Invoke(delegate
            {
                if(thdCompressTo != null)
                {
                    thdCompressTo.Abort();
                    thdCompressTo = null;
                }

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

                MessageBox.Show(string.Format("Correctly compressed as {0}", Context.path));

                Context.path = null;
            });
        }
    }
}
