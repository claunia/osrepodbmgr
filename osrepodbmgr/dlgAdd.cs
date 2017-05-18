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
using System.Threading;
using System.Xml.Serialization;
using Gtk;
using Newtonsoft.Json;
using osrepodbmgr;
using osrepodbmgr.Core;
using Schemas;

public partial class dlgAdd : Dialog
{
    Thread thdPulseProgress;
    Thread thdFindFiles;
    Thread thdHashFiles;
    Thread thdCheckFiles;
    Thread thdAddFiles;
    Thread thdPackFiles;
    Thread thdOpenArchive;
    Thread thdExtractArchive;
    Thread thdRemoveTemp;
    bool stopped;
    ListStore fileView;
    ListStore osView;

    public delegate void OnAddedOSDelegate(DBEntry os, bool existsInRepo, string pathInRepo);
    public event OnAddedOSDelegate OnAddedOS;

    public dlgAdd()
    {
        Build();

        Context.UnarChangeStatus += UnarChangeStatus;
        Context.CheckUnar();

        CellRendererText filenameCell = new CellRendererText();
        CellRendererText hashCell = new CellRendererText();
        CellRendererToggle dbCell = new CellRendererToggle();

        TreeViewColumn filenameColumn = new TreeViewColumn("Path", filenameCell, "text", 0, "background", 3, "foreground", 4);
        TreeViewColumn hashColumn = new TreeViewColumn("SHA256", hashCell, "text", 1, "background", 3, "foreground", 4);
        TreeViewColumn dbColumn = new TreeViewColumn("Known?", dbCell, "active", 2);

        fileView = new ListStore(typeof(string), typeof(string), typeof(bool), typeof(string), typeof(string));

        treeFiles.Model = fileView;
        treeFiles.AppendColumn(filenameColumn);
        treeFiles.AppendColumn(hashColumn);
        treeFiles.AppendColumn(dbColumn);

        tabTabs.GetNthPage(1).Visible = false;

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
                                 typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(string), typeof(string));

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
    }

    void UnarChangeStatus()
    {
        Application.Invoke(delegate
        {
            btnArchive.Sensitive = Context.unarUsable;
        });
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        if(btnStop.Visible)
            btnStop.Click();
        if(btnClose.Sensitive)
            btnClose.Click();

        Application.Quit();
        a.RetVal = true;
    }

    protected void OnBtnFolderClicked(object sender, EventArgs e)
    {
        FileChooserDialog dlgFolder = new FileChooserDialog("Open folder", this, FileChooserAction.SelectFolder,
                                                             "Cancel", ResponseType.Cancel, "Choose", ResponseType.Accept);
        dlgFolder.SelectMultiple = false;

        if(dlgFolder.Run() == (int)ResponseType.Accept)
        {
            stopped = false;
            lblProgress.Text = "Finding files";
            lblProgress.Visible = true;
            prgProgress.Visible = true;
            btnExit.Sensitive = false;
            btnFolder.Visible = false;
            btnArchive.Visible = false;
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

            thdFindFiles = new Thread(Workers.FindFiles);
            Context.path = dlgFolder.Filename;
            Workers.Failed += FindFilesFailed;
            Workers.Finished += FindFilesFinished;
            btnStop.Visible = true;
            thdPulseProgress.Start();
            thdFindFiles.Start();
        }

        dlgFolder.Destroy();
    }

    void FindFilesFailed(string text)
    {
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();
            lblProgress.Visible = false;
            prgProgress.Visible = false;
            btnExit.Sensitive = true;
            btnFolder.Visible = true;
            btnArchive.Visible = true;
            Workers.Failed -= FindFilesFailed;
            Workers.Finished -= FindFilesFinished;
            thdFindFiles = null;
        });
    }

    void FindFilesFinished()
    {
        Application.Invoke(delegate
        {
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();

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
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();
            lblProgress.Visible = false;
            prgProgress.Visible = false;
            lblProgress2.Visible = false;
            prgProgress2.Visible = false;
            Workers.Failed -= HashFilesFailed;
            Workers.Finished -= HashFilesFinished;
            Workers.UpdateProgress -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            btnExit.Sensitive = true;
            btnFolder.Visible = true;
            btnArchive.Visible = true;
            thdHashFiles = null;
        });
    }

    void HashFilesFinished()
    {
        Application.Invoke(delegate
        {
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();
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
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }
            prgProgress.Visible = false;
            btnStop.Visible = false;
            btnClose.Visible = false;
            btnExit.Sensitive = true;
            Workers.Failed -= ChkFilesFailed;
            Workers.Finished -= ChkFilesFinished;
            Workers.UpdateProgress -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            Workers.AddFileForOS -= AddFile;
            Workers.AddOS -= AddOS;
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();
            if(thdCheckFiles != null)
                thdCheckFiles.Abort();
            thdHashFiles = null;
            if(fileView != null)
                fileView.Clear();
            if(osView != null)
            {
                tabTabs.GetNthPage(1).Visible = false;
                osView.Clear();
            }
        });
    }

    void ChkFilesFinished()
    {
        Application.Invoke(delegate
        {
            Workers.Failed -= ChkFilesFailed;
            Workers.Finished -= ChkFilesFinished;
            Workers.UpdateProgress -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            Workers.AddFileForOS -= AddFile;
            Workers.AddOS -= AddOS;

            if(thdCheckFiles != null)
                thdCheckFiles.Abort();
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();
            thdHashFiles = null;
            prgProgress.Visible = false;
            btnStop.Visible = false;
            btnClose.Visible = true;
            btnExit.Sensitive = true;
            btnPack.Visible = true;
            btnPack.Sensitive = true;
            btnRemoveFile.Visible = true;

            txtFormat.IsEditable = true;
            txtMachine.IsEditable = true;
            txtProduct.IsEditable = true;
            txtVersion.IsEditable = true;
            txtLanguages.IsEditable = true;
            txtDeveloper.IsEditable = true;
            txtDescription.IsEditable = true;
            txtArchitecture.IsEditable = true;
            chkOem.Sensitive = true;
            chkFiles.Sensitive = true;
            chkUpdate.Sensitive = true;
            chkUpgrade.Sensitive = true;
            chkNetinstall.Sensitive = true;
            chkSource.Sensitive = true;

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

                btnMetadata.ModifyBg(StateType.Normal, new Gdk.Color(0, 127, 0));
            }
            else
                btnMetadata.ModifyBg(StateType.Normal, new Gdk.Color(127, 0, 0));
        });
    }

    void AddFile(string filename, string hash, bool known)
    {
        Application.Invoke(delegate
        {
            string color = known ? "green" : "red";
            fileView.AppendValues(filename, hash, known, color, "black");
            btnPack.Sensitive |= !known;
        });
    }

    void AddOS(DBEntry os, bool existsInRepo, string pathInRepo)
    {
        Application.Invoke(delegate
        {
            string color = existsInRepo ? "green" : "red";
            tabTabs.GetNthPage(1).Visible = true;
            osView.AppendValues(os.developer, os.product, os.version, os.languages, os.architecture, os.machine,
                                os.format, os.description, os.oem, os.upgrade, os.update, os.source,
                                os.files, os.netinstall, color, "black", pathInRepo);
        });
    }

    protected void OnBtnExitClicked(object sender, EventArgs e)
    {
        if(btnClose.Sensitive)
            btnClose.Click();

        btnDialog.Click();
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
            tabTabs.GetNthPage(1).Visible = false;
            osView.Clear();
        }
        txtFormat.IsEditable = false;
        txtMachine.IsEditable = false;
        txtProduct.IsEditable = false;
        txtVersion.IsEditable = false;
        txtLanguages.IsEditable = false;
        txtDeveloper.IsEditable = false;
        txtDescription.IsEditable = false;
        txtArchitecture.IsEditable = false;
        chkOem.Sensitive = false;
        chkFiles.Sensitive = false;
        chkUpdate.Sensitive = false;
        chkUpgrade.Sensitive = false;
        chkNetinstall.Sensitive = false;
        chkSource.Sensitive = false;
        txtFormat.Text = "";
        txtMachine.Text = "";
        txtProduct.Text = "";
        txtVersion.Text = "";
        txtLanguages.Text = "";
        txtDeveloper.Text = "";
        txtDescription.Text = "";
        txtArchitecture.Text = "";
        chkOem.Active = false;
        chkFiles.Active = false;
        chkUpdate.Active = false;
        chkUpgrade.Active = false;
        chkNetinstall.Active = false;
        chkSource.Active = false;

        if(Context.tmpFolder != null)
        {
            btnStop.Visible = false;
            prgProgress.Visible = true;
            prgProgress.Text = "Removing temporary files";
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

        if(thdPulseProgress != null)
        {
            thdPulseProgress.Abort();
            thdPulseProgress = null;
        }

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
            prgProgress.Text = "Removing temporary files";
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
        btnExit.Sensitive = true;
        btnFolder.Visible = true;
        btnArchive.Visible = true;
        lblProgress.Visible = false;
        prgProgress.Visible = false;
        btnExit.Sensitive = true;
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
        if(osView != null && tabTabs != null && tabTabs.GetNthPage(1) != null)
        {
            tabTabs.GetNthPage(1).Visible = false;
            osView.Clear();
        }
    }

    public void RemoveTempFilesFailed(string text)
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
            Workers.Failed -= RemoveTempFilesFailed;
            Workers.Finished -= RemoveTempFilesFinished;
            Context.path = null;
            Context.tmpFolder = null;
            RestoreUI();
        });
    }

    public void RemoveTempFilesFinished()
    {
        Application.Invoke(delegate
        {
            if(thdPulseProgress != null)
            {
                thdPulseProgress.Abort();
                thdPulseProgress = null;
            }
            Workers.Failed -= RemoveTempFilesFailed;
            Workers.Finished -= RemoveTempFilesFinished;
            Context.path = null;
            Context.tmpFolder = null;
            RestoreUI();
        });
    }

    void AddToDatabase()
    {
        btnRemoveFile.Sensitive = false;
        btnPack.Sensitive = false;
        btnClose.Sensitive = false;
        prgProgress.Visible = true;
        txtFormat.IsEditable = false;
        txtMachine.IsEditable = false;
        txtProduct.IsEditable = false;
        txtVersion.IsEditable = false;
        txtLanguages.IsEditable = false;
        txtDeveloper.IsEditable = false;
        txtDescription.IsEditable = false;
        txtArchitecture.IsEditable = false;
        chkOem.Sensitive = false;
        chkFiles.Sensitive = false;
        chkUpdate.Sensitive = false;
        chkUpgrade.Sensitive = false;
        chkNetinstall.Sensitive = false;
        chkSource.Sensitive = false;

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
        Context.dbInfo.files = chkFiles.Active;
        Context.dbInfo.netinstall = chkNetinstall.Active;
        Context.dbInfo.oem = chkOem.Active;
        Context.dbInfo.source = chkSource.Active;
        Context.dbInfo.update = chkUpdate.Active;
        Context.dbInfo.upgrade = chkUpgrade.Active;

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
        Application.Invoke(delegate
        {
            Workers.UpdateProgress -= UpdateProgress;
            Workers.Finished -= AddFilesToDbFinished;
            Workers.Failed -= AddFilesToDbFailed;

            if(thdAddFiles != null)
                thdAddFiles.Abort();
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();

            long counter = 0;
            fileView.Clear();
            foreach(KeyValuePair<string, DBOSFile> kvp in Context.hashes)
            {
                UpdateProgress(null, "Updating table", counter, Context.hashes.Count);
                fileView.AppendValues(kvp.Key, kvp.Value.Sha256, true, "green", "black");
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

            prgProgress.Visible = false;
            btnClose.Sensitive = true;
        });
    }

    public void AddFilesToDbFailed(string text)
    {
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            Workers.UpdateProgress -= UpdateProgress;
            Workers.Finished -= AddFilesToDbFinished;
            Workers.Failed -= AddFilesToDbFailed;

            if(thdAddFiles != null)
                thdAddFiles.Abort();
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();

            ChkFilesFinished();
        });
    }

    protected void OnBtnPackClicked(object sender, EventArgs e)
    {
        btnRemoveFile.Sensitive = false;
        btnPack.Sensitive = false;
        btnClose.Sensitive = false;
        prgProgress.Visible = true;
        prgProgress2.Visible = true;
        lblProgress.Visible = true;
        lblProgress2.Visible = true;
        txtFormat.IsEditable = false;
        txtMachine.IsEditable = false;
        txtProduct.IsEditable = false;
        txtVersion.IsEditable = false;
        txtLanguages.IsEditable = false;
        txtDeveloper.IsEditable = false;
        txtDescription.IsEditable = false;
        txtArchitecture.IsEditable = false;
        chkOem.Sensitive = false;
        chkFiles.Sensitive = false;
        chkUpdate.Sensitive = false;
        chkUpgrade.Sensitive = false;
        chkNetinstall.Sensitive = false;
        chkSource.Sensitive = false;

        Workers.UpdateProgress += UpdateProgress;
        Workers.UpdateProgress2 += UpdateProgress2;
        Workers.FinishedWithText += PackFilesFinished;
        Workers.Failed += PackFilesFailed;

        Context.dbInfo = new DBEntry();
        Context.dbInfo.architecture = txtArchitecture.Text;
        Context.dbInfo.description = txtDescription.Text;
        Context.dbInfo.developer = txtDeveloper.Text;
        Context.dbInfo.format = txtFormat.Text;
        Context.dbInfo.languages = txtLanguages.Text;
        Context.dbInfo.machine = txtMachine.Text;
        Context.dbInfo.product = txtProduct.Text;
        Context.dbInfo.version = txtVersion.Text;
        Context.dbInfo.files = chkFiles.Active;
        Context.dbInfo.netinstall = chkNetinstall.Active;
        Context.dbInfo.oem = chkOem.Active;
        Context.dbInfo.source = chkSource.Active;
        Context.dbInfo.update = chkUpdate.Active;
        Context.dbInfo.upgrade = chkUpgrade.Active;

        thdPackFiles = new Thread(Workers.CompressFiles);
        thdPackFiles.Start();
    }

    public void PackFilesFinished(string text)
    {
        Application.Invoke(delegate
        {
            Workers.UpdateProgress -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            Workers.FinishedWithText -= PackFilesFinished;
            Workers.Failed -= PackFilesFailed;
            prgProgress2.Visible = false;
            lblProgress2.Visible = false;

            if(thdPackFiles != null)
                thdPackFiles.Abort();
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();

            AddToDatabase();

            MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, "Correctly packed to " + text);
            dlgMsg.Run();
            dlgMsg.Destroy();
        });
    }

    public void PackFilesFailed(string text)
    {
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            Workers.UpdateProgress -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            Workers.FinishedWithText -= PackFilesFinished;
            Workers.Failed -= PackFilesFailed;

            if(thdPackFiles != null)
                thdPackFiles.Abort();
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();

            btnRemoveFile.Sensitive = true;
            btnPack.Sensitive = true;
            btnClose.Sensitive = true;
            prgProgress.Visible = false;
            prgProgress2.Visible = false;
            lblProgress.Visible = false;
            lblProgress2.Visible = false;
            txtFormat.IsEditable = true;
            txtMachine.IsEditable = true;
            txtProduct.IsEditable = true;
            txtVersion.IsEditable = true;
            txtLanguages.IsEditable = true;
            txtDeveloper.IsEditable = true;
            txtDescription.IsEditable = true;
            txtArchitecture.IsEditable = true;
            chkOem.Sensitive = true;
            chkFiles.Sensitive = true;
            chkUpdate.Sensitive = true;
            chkUpgrade.Sensitive = true;
            chkNetinstall.Sensitive = true;
            chkSource.Sensitive = true;
        });
    }

    protected void OnBtnArchiveClicked(object sender, EventArgs e)
    {
        if(!Context.unarUsable)
        {
            MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Cannot open archives without a working unar installation.");
            dlgMsg.Run();
            dlgMsg.Destroy();
            return;
        }

        FileChooserDialog dlgFolder = new FileChooserDialog("Open archive", this, FileChooserAction.Open,
                                                     "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        dlgFolder.SelectMultiple = false;

        if(dlgFolder.Run() == (int)ResponseType.Accept)
        {
            stopped = false;
            prgProgress.Text = "Opening archive";
            lblProgress.Visible = false;
            prgProgress.Visible = true;
            btnExit.Sensitive = false;
            btnFolder.Visible = false;
            btnArchive.Visible = false;
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

            thdOpenArchive = new Thread(Workers.OpenArchive);
            Context.path = dlgFolder.Filename;
            Workers.Failed += OpenArchiveFailed;
            Workers.Finished += OpenArchiveFinished;
            btnStop.Visible = true;
            thdPulseProgress.Start();
            thdOpenArchive.Start();
        }

        dlgFolder.Destroy();
    }

    public void OpenArchiveFailed(string text)
    {
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();
            lblProgress.Visible = false;
            prgProgress.Visible = false;
            btnExit.Sensitive = true;
            btnFolder.Visible = true;
            btnArchive.Visible = true;
            Workers.Failed -= OpenArchiveFailed;
            Workers.Finished -= OpenArchiveFinished;
            thdOpenArchive = null;
        });
    }

    public void OpenArchiveFinished()
    {
        Application.Invoke(delegate
        {
            stopped = false;
            prgProgress.Text = "Extracting archive";
            prgProgress.Visible = true;
            prgProgress2.Visible = true;
            btnExit.Sensitive = false;
            btnFolder.Visible = false;
            btnArchive.Visible = false;
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();
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
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();
            lblProgress.Visible = false;
            lblProgress2.Visible = false;
            prgProgress2.Visible = false;
            btnExit.Sensitive = true;
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
                prgProgress.Text = "Removing temporary files";
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
                Workers.Failed += RemoveTempFilesFailed;
                Workers.Finished += RemoveTempFilesFinished;
                thdRemoveTemp = new Thread(Workers.RemoveTempFolder);
                thdRemoveTemp.Start();
            }
        });
    }

    public void ExtractArchiveFinished()
    {
        Application.Invoke(delegate
        {
            stopped = false;
            lblProgress.Text = "Finding files";
            lblProgress.Visible = true;
            lblProgress2.Visible = false;
            prgProgress.Visible = true;
            btnExit.Sensitive = false;
            btnFolder.Visible = false;
            btnArchive.Visible = false;
            Workers.Failed -= ExtractArchiveFailed;
            Workers.Finished -= ExtractArchiveFinished;
            Workers.UpdateProgress -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            if(thdExtractArchive != null)
                thdExtractArchive.Abort();
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();
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

            thdFindFiles = new Thread(Workers.FindFiles);
            Workers.Failed += FindFilesFailed;
            Workers.Finished += FindFilesFinished;
            btnStop.Visible = true;
            thdPulseProgress.Start();
            thdFindFiles.Start();
        });
    }

    protected void OnBtnMetadataClicked(object sender, EventArgs e)
    {
        dlgMetadata _dlgMetadata = new dlgMetadata();
        _dlgMetadata.Metadata = Context.metadata;
        _dlgMetadata.FillFields();

        if(_dlgMetadata.Run() == (int)ResponseType.Ok)
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

            btnMetadata.ModifyBg(StateType.Normal, new Gdk.Color(0, 127, 0));
        }

        _dlgMetadata.Destroy();
    }

    protected void OnBtnRemoveFileClicked(object sender, EventArgs e)
    {
        TreeIter fileIter;
        if(treeFiles.Selection.GetSelected(out fileIter))
        {
            string name = (string)fileView.GetValue(fileIter, 0);
            string filesPath;

            if(!string.IsNullOrEmpty(Context.tmpFolder) && Directory.Exists(Context.tmpFolder))
                filesPath = Context.tmpFolder;
            else
                filesPath = Context.path;

            Context.hashes.Remove(name);
            Context.files.Remove(System.IO.Path.Combine(filesPath, name));
            fileView.Remove(ref fileIter);
        }
    }
}
