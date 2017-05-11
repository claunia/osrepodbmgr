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
using System.Threading;
using Gtk;
using osrepodbmgr.Core;
using System.IO;

namespace osrepodbmgr
{
    public partial class frmMain : Window
    {
        ListStore osView;
        Thread thdPulseProgress;
        Thread thdPopulateOSes;
        Thread thdExtractArchive;
        Thread thdCopyFile;

        public frmMain() :
                base(WindowType.Toplevel)
        {
            Build();

            Workers.InitDB();

            CellRendererText developerCell = new CellRendererText();
            CellRendererText productCell = new CellRendererText();
            CellRendererText versionCell = new CellRendererText();
            CellRendererText languagesCell = new CellRendererText();
            CellRendererText architectureCell = new CellRendererText();
            CellRendererText machineCell = new CellRendererText();
            CellRendererText formatCell = new CellRendererText();
            CellRendererText descriptionCell = new CellRendererText();
            CellRendererToggle oemCell = new CellRendererToggle();
            CellRendererToggle upgradeCell = new CellRendererToggle();
            CellRendererToggle updateCell = new CellRendererToggle();
            CellRendererToggle sourceCell = new CellRendererToggle();
            CellRendererToggle filesCell = new CellRendererToggle();
            CellRendererToggle netinstallCell = new CellRendererToggle();
            CellRendererText pathCell = new CellRendererText();

            TreeViewColumn developerColumn = new TreeViewColumn("Developer", developerCell, "text", 0, "background", 14, "foreground", 15);
            TreeViewColumn productColumn = new TreeViewColumn("Product", productCell, "text", 1, "background", 14, "foreground", 15);
            TreeViewColumn versionColumn = new TreeViewColumn("Version", versionCell, "text", 2, "background", 14, "foreground", 15);
            TreeViewColumn languagesColumn = new TreeViewColumn("Languages", languagesCell, "text", 3, "background", 14, "foreground", 15);
            TreeViewColumn architectureColumn = new TreeViewColumn("Architecture", architectureCell, "text", 4, "background", 14, "foreground", 15);
            TreeViewColumn machineColumn = new TreeViewColumn("Machine", machineCell, "text", 5, "background", 14, "foreground", 15);
            TreeViewColumn formatColumn = new TreeViewColumn("Format", formatCell, "text", 6, "background", 14, "foreground", 15);
            TreeViewColumn descriptionColumn = new TreeViewColumn("Description", descriptionCell, "text", 7, "background", 14, "foreground", 15);
            TreeViewColumn oemColumn = new TreeViewColumn("OEM?", oemCell, "active", 8);
            TreeViewColumn upgradeColumn = new TreeViewColumn("Upgrade?", upgradeCell, "active", 9);
            TreeViewColumn updateColumn = new TreeViewColumn("Update?", updateCell, "active", 10);
            TreeViewColumn sourceColumn = new TreeViewColumn("Source?", sourceCell, "active", 11);
            TreeViewColumn filesColumn = new TreeViewColumn("Files?", filesCell, "active", 12);
            TreeViewColumn netinstallColumn = new TreeViewColumn("NetInstall?", netinstallCell, "active", 13);
            TreeViewColumn pathColumn = new TreeViewColumn("Path in repo", pathCell, "text", 16, "background", 14, "foreground", 15);

            osView = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string),
                                     typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(string), typeof(string),
                                   typeof(long), typeof(string));

            treeOSes.Model = osView;
            treeOSes.AppendColumn(developerColumn);
            treeOSes.AppendColumn(productColumn);
            treeOSes.AppendColumn(versionColumn);
            treeOSes.AppendColumn(languagesColumn);
            treeOSes.AppendColumn(architectureColumn);
            treeOSes.AppendColumn(machineColumn);
            treeOSes.AppendColumn(formatColumn);
            treeOSes.AppendColumn(descriptionColumn);
            treeOSes.AppendColumn(oemColumn);
            treeOSes.AppendColumn(upgradeColumn);
            treeOSes.AppendColumn(updateColumn);
            treeOSes.AppendColumn(sourceColumn);
            treeOSes.AppendColumn(filesColumn);
            treeOSes.AppendColumn(netinstallColumn);
            treeOSes.AppendColumn(pathColumn);

            treeOSes.Selection.Mode = SelectionMode.Single;

            thdPulseProgress = new Thread(() =>
            {
                while(true)
                {
                    Application.Invoke(delegate
                    {
                        prgProgress.Pulse();
                    });
                    Thread.Sleep(66);
                }
            });
            Workers.Failed += LoadOSesFailed;
            Workers.Finished += LoadOSesFinished;
            Workers.UpdateProgress += UpdateProgress;
            Workers.AddOS += AddOS;
            thdPopulateOSes = new Thread(Workers.GetAllOSes);
            thdPopulateOSes.Start();
        }

        void LoadOSesFailed(string text)
        {
            Application.Invoke(delegate
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                                         string.Format("Error {0} when populating OSes file, exiting...", text));
                dlgMsg.Run();
                dlgMsg.Destroy();
                if(thdPulseProgress != null)
                {
                    thdPulseProgress.Abort();
                    thdPulseProgress = null;
                }
                if(thdPopulateOSes != null)
                {
                    thdPopulateOSes.Abort();
                    thdPopulateOSes = null;
                }
                Workers.Failed -= LoadOSesFailed;
                Workers.Finished -= LoadOSesFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Application.Quit();
            });
        }

        void LoadOSesFinished()
        {
            Application.Invoke(delegate
            {
                if(thdPulseProgress != null)
                {
                    thdPulseProgress.Abort();
                    thdPulseProgress = null;
                }
                if(thdPopulateOSes != null)
                {
                    thdPopulateOSes.Abort();
                    thdPopulateOSes = null;
                }
                Workers.Failed -= LoadOSesFailed;
                Workers.Finished -= LoadOSesFinished;
                Workers.UpdateProgress -= UpdateProgress;
                lblProgress.Visible = false;
                prgProgress.Visible = false;
                treeOSes.Sensitive = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnExtract.Visible = true;
                btnSave.Visible = true;
                btnHelp.Visible = true;
                btnSettings.Visible = true;
            });
        }

        public void UpdateProgress(string text, string inner, long current, long maximum)
        {
            Application.Invoke(delegate
            {
                lblProgress.Text = text;
                prgProgress.Text = inner;
                if(maximum > 0)
                    prgProgress.Fraction = current / (double)maximum;
                else
                    prgProgress.Pulse();
            });
        }

        public void UpdateProgress2(string text, string inner, long current, long maximum)
        {
            Application.Invoke(delegate
            {
                lblProgress2.Text = text;
                prgProgress2.Text = inner;
                if(maximum > 0)
                    prgProgress2.Fraction = current / (double)maximum;
                else
                    prgProgress2.Pulse();
            });
        }

        void AddOS(DBEntry os, bool existsInRepo, string pathInRepo)
        {
            Application.Invoke(delegate
            {
                if(thdPulseProgress != null)
                {
                    thdPulseProgress.Abort();
                    thdPulseProgress = null;
                }

                string color = existsInRepo ? "green" : "red";
                osView.AppendValues(os.developer, os.product, os.version, os.languages, os.architecture, os.machine,
                                    os.format, os.description, os.oem, os.upgrade, os.update, os.source,
                                    os.files, os.netinstall, color, "black", pathInRepo, os.id, os.mdid);
            });
        }

        protected void OnBtnAddClicked(object sender, EventArgs e)
        {
            dlgAdd _dlgAdd = new dlgAdd();
            _dlgAdd.OnAddedOS += (os, existsInRepo, pathInRepo) =>
            {
                string color = existsInRepo ? "green" : "red";
                osView.AppendValues(os.developer, os.product, os.version, os.languages, os.architecture, os.machine,
                                    os.format, os.description, os.oem, os.upgrade, os.update, os.source,
                                    os.files, os.netinstall, color, "black", pathInRepo, os.id, os.mdid);
            };
            _dlgAdd.Run();
            _dlgAdd.Destroy();
        }

        protected void OnBtnRemoveClicked(object sender, EventArgs e)
        {
            TreeIter osIter;
            if(treeOSes.Selection.GetSelected(out osIter))
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo,
                             "Are you sure you want to remove the selected OS?");

                if(dlgMsg.Run() == (int)ResponseType.Yes)
                {
                    Workers.RemoveOS((long)osView.GetValue(osIter, 17), (string)osView.GetValue(osIter, 18));
                    osView.Remove(ref osIter);
                }

                dlgMsg.Destroy();
            }
        }

        protected void OnBtnExtractClicked(object sender, EventArgs e)
        {
            TreeIter osIter;
            if(treeOSes.Selection.GetSelected(out osIter))
            {
                Context.path = (string)osView.GetValue(osIter, 16);

                if(!File.Exists(Context.path))
                {
                    MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "File is not in repository.");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                FileChooserDialog dlgFolder = new FileChooserDialog("Extract to...", this, FileChooserAction.SelectFolder,
                                                     "Cancel", ResponseType.Cancel, "Choose", ResponseType.Accept);
                dlgFolder.SelectMultiple = false;

                if(dlgFolder.Run() == (int)ResponseType.Accept)
                {
                    Context.userExtracting = true;
                    Context.tmpFolder = dlgFolder.Filename;
                    Context.copyArchive = true;

                    dlgFolder.Destroy();

                    lblProgress.Visible = true;
                    lblProgress2.Visible = true;
                    prgProgress.Visible = true;
                    prgProgress2.Visible = true;
                    treeOSes.Sensitive = false;
                    btnAdd.Visible = false;
                    btnRemove.Visible = false;
                    btnExtract.Visible = false;
                    btnSave.Visible = false;
                    btnHelp.Visible = false;
                    btnSettings.Visible = false;
                    btnStop.Visible = true;

                    if(thdPulseProgress != null)
                    {
                        thdPulseProgress.Abort();
                        thdPulseProgress = null;
                    }
                    Workers.Failed += ExtractArchiveFailed;
                    Workers.Finished += ExtractArchiveFinished;
                    Workers.UpdateProgress += UpdateProgress;
                    Workers.UpdateProgress2 += UpdateProgress2;
                    thdExtractArchive = new Thread(Workers.ExtractArchive);
                    thdExtractArchive.Start();
                }
                else
                    dlgFolder.Destroy();
            }
        }

        public void ExtractArchiveFailed(string text)
        {
            Application.Invoke(delegate
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
                if(thdPulseProgress != null)
                {
                    thdPulseProgress.Abort();
                    thdPulseProgress = null;
                }
                if(thdExtractArchive != null)
                {
                    thdExtractArchive.Abort();
                    thdExtractArchive = null;
                }

                lblProgress.Visible = false;
                lblProgress2.Visible = false;
                prgProgress.Visible = false;
                prgProgress2.Visible = false;
                treeOSes.Sensitive = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnExtract.Visible = true;
                btnSave.Visible = true;
                btnHelp.Visible = true;
                btnSettings.Visible = true;
                btnStop.Visible = false;

                Workers.Failed -= ExtractArchiveFailed;
                Workers.Finished -= ExtractArchiveFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;

                Context.userExtracting = false;
                Context.tmpFolder = null;
                Context.copyArchive = false;
                Context.path = null;
            });
        }

        public void ExtractArchiveFinished()
        {
            Application.Invoke(delegate
            {
                if(thdPulseProgress != null)
                {
                    thdPulseProgress.Abort();
                    thdPulseProgress = null;
                }
                if(thdExtractArchive != null)
                {
                    thdExtractArchive.Abort();
                    thdExtractArchive = null;
                }

                lblProgress.Visible = false;
                lblProgress2.Visible = false;
                prgProgress.Visible = false;
                prgProgress2.Visible = false;
                treeOSes.Sensitive = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnExtract.Visible = true;
                btnSave.Visible = true;
                btnHelp.Visible = true;
                btnSettings.Visible = true;
                btnStop.Visible = false;

                Workers.Failed -= ExtractArchiveFailed;
                Workers.Finished -= ExtractArchiveFinished;
                Workers.UpdateProgress -= UpdateProgress;
                Workers.UpdateProgress2 -= UpdateProgress2;

                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok,
                                                         string.Format("Correctly extracted to {0}", Context.tmpFolder));
                dlgMsg.Run();
                dlgMsg.Destroy();

                Context.userExtracting = false;
                Context.tmpFolder = null;
                Context.copyArchive = false;
                Context.path = null;
            });
        }

        protected void OnBtnSaveClicked(object sender, EventArgs e)
        {
            TreeIter osIter;
            if(treeOSes.Selection.GetSelected(out osIter))
            {
                Context.path = (string)osView.GetValue(osIter, 16);

                if(!File.Exists(Context.path))
                {
                    MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "File is not in repository.");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

                FileChooserDialog dlgFolder = new FileChooserDialog("Copy to...", this, FileChooserAction.Save,
                                                     "Cancel", ResponseType.Cancel, "Choose", ResponseType.Accept);
                dlgFolder.SelectMultiple = false;

                if(dlgFolder.Run() == (int)ResponseType.Accept)
                {
                    Context.userExtracting = true;
                    Context.tmpFolder = dlgFolder.Filename;
                    Context.copyArchive = true;

                    dlgFolder.Destroy();

                    lblProgress.Visible = true;
                    prgProgress.Visible = true;
                    treeOSes.Sensitive = false;
                    btnAdd.Visible = false;
                    btnRemove.Visible = false;
                    btnExtract.Visible = false;
                    btnSave.Visible = false;
                    btnHelp.Visible = false;
                    btnSettings.Visible = false;
                    btnStop.Visible = true;

                    if(thdPulseProgress != null)
                    {
                        thdPulseProgress.Abort();
                        thdPulseProgress = null;
                    }
                    Workers.Failed += CopyFileFailed;
                    Workers.Finished += CopyFileFinished;
                    Workers.UpdateProgress += UpdateProgress;
                    thdCopyFile = new Thread(Workers.CopyFile);
                    thdCopyFile.Start();
                }
                else
                    dlgFolder.Destroy();
            }
        }

        public void CopyFileFailed(string text)
        {
            Application.Invoke(delegate
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
                if(thdPulseProgress != null)
                {
                    thdPulseProgress.Abort();
                    thdPulseProgress = null;
                }
                if(thdCopyFile != null)
                {
                    thdCopyFile.Abort();
                    thdCopyFile = null;
                }

                lblProgress.Visible = false;
                prgProgress.Visible = false;
                treeOSes.Sensitive = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnExtract.Visible = true;
                btnSave.Visible = true;
                btnHelp.Visible = true;
                btnSettings.Visible = true;
                btnStop.Visible = false;

                Workers.Failed -= CopyFileFailed;
                Workers.Finished -= CopyFileFinished;
                Workers.UpdateProgress -= UpdateProgress;

                Context.userExtracting = false;
                Context.tmpFolder = null;
                Context.copyArchive = false;
                Context.path = null;
            });
        }

        public void CopyFileFinished()
        {
            Application.Invoke(delegate
            {
                if(thdPulseProgress != null)
                {
                    thdPulseProgress.Abort();
                    thdPulseProgress = null;
                }
                if(thdCopyFile != null)
                {
                    thdCopyFile.Abort();
                    thdCopyFile = null;
                }

                lblProgress.Visible = false;
                prgProgress.Visible = false;
                treeOSes.Sensitive = true;
                btnAdd.Visible = true;
                btnRemove.Visible = true;
                btnExtract.Visible = true;
                btnSave.Visible = true;
                btnHelp.Visible = true;
                btnSettings.Visible = true;
                btnStop.Visible = false;

                Workers.Failed -= CopyFileFailed;
                Workers.Finished -= CopyFileFinished;
                Workers.UpdateProgress -= UpdateProgress;

                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok,
                                                         string.Format("Correctly saved as {0}", Context.tmpFolder));
                dlgMsg.Run();
                dlgMsg.Destroy();

                Context.userExtracting = false;
                Context.tmpFolder = null;
                Context.copyArchive = false;
                Context.path = null;
            });
        }

        protected void OnBtnHelpClicked(object sender, EventArgs e)
        {
            dlgHelp _help = new dlgHelp();
            _help.Run();
            _help.Destroy();
        }

        protected void OnBtnSettingsClicked(object sender, EventArgs e)
        {
            dlgSettings _dlgSettings = new dlgSettings();
            _dlgSettings.Run();
            _dlgSettings.Destroy();
        }

        protected void OnBtnQuitClicked(object sender, EventArgs e)
        {
            OnBtnStopClicked(sender, e);
            Application.Quit();
        }

        protected void OnBtnStopClicked(object sender, EventArgs e)
        {
            if(thdPulseProgress != null)
            {
                thdPulseProgress.Abort();
                thdPulseProgress = null;
            }
            if(thdPopulateOSes != null)
            {
                thdPopulateOSes.Abort();
                thdPopulateOSes = null;
            }
            if(thdExtractArchive != null)
            {
                thdPopulateOSes.Abort();
                thdPopulateOSes = null;
            }
            if(thdCopyFile != null)
            {
                thdCopyFile.Abort();
                thdCopyFile = null;
            }
        }

        protected void OnDeleteEvent(object sender, DeleteEventArgs e)
        {
            OnBtnStopClicked(sender, e);
        }
    }
}