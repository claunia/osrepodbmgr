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
using Schemas;

public partial class MainWindow : Window
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

    public MainWindow() : base(WindowType.Toplevel)
    {
        Build();

        Core.InitDB();

        MainClass.UnarChangeStatus += UnarChangeStatus;
        MainClass.CheckUnar();

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
            btnArchive.Sensitive = MainClass.unarUsable;
        });
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        if(btnStop.Visible)
            OnBtnStopClicked(sender, a);
        if(btnClose.Sensitive)
            OnBtnCloseClicked(sender, a);

        Application.Quit();
        a.RetVal = true;
    }

    protected void OnBtnHelpClicked(object sender, EventArgs e)
    {
        frmHelp _help = new frmHelp();
        _help.Show();
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
            btnSettings.Sensitive = false;
            thdPulseProgress = new Thread(() =>
            {
                Application.Invoke(delegate
                {
                    prgProgress.Pulse();
                });
                Thread.Sleep(10);
            });

            thdFindFiles = new Thread(Core.FindFiles);
            MainClass.path = dlgFolder.Filename;
            Core.Failed += FindFilesFailed;
            Core.Finished += FindFilesFinished;
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
            btnSettings.Sensitive = true;
            Core.Failed -= FindFilesFailed;
            Core.Finished -= FindFilesFinished;
            thdFindFiles = null;
        });
    }

    void FindFilesFinished()
    {
        Application.Invoke(delegate
        {
            if(thdPulseProgress != null)
                thdPulseProgress.Abort();

            Core.Failed -= FindFilesFailed;
            Core.Finished -= FindFilesFinished;

            lblProgress.Visible = true;
            prgProgress.Visible = true;
            lblProgress2.Visible = true;
            prgProgress2.Visible = true;

            thdFindFiles = null;
            thdHashFiles = new Thread(Core.HashFiles);
            Core.Failed += HashFilesFailed;
            Core.Finished += HashFilesFinished;
            Core.UpdateProgress += UpdateProgress;
            Core.UpdateProgress2 += UpdateProgress2;
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
            lblProgress.Visible = false;
            prgProgress.Visible = false;
            lblProgress2.Visible = false;
            prgProgress2.Visible = false;
            Core.Failed -= HashFilesFailed;
            Core.Finished -= HashFilesFinished;
            Core.UpdateProgress -= UpdateProgress;
            Core.UpdateProgress2 -= UpdateProgress2;
            btnExit.Sensitive = true;
            btnFolder.Visible = true;
            btnArchive.Visible = true;
            btnSettings.Sensitive = true;
            thdHashFiles = null;
        });
    }

    void HashFilesFinished()
    {
        Application.Invoke(delegate
        {
            lblProgress.Visible = false;
            prgProgress.Visible = false;
            lblProgress.Visible = false;
            prgProgress.Visible = false;
            lblProgress2.Visible = false;
            prgProgress2.Visible = false;
            Core.Failed -= HashFilesFailed;
            Core.Finished -= HashFilesFinished;
            Core.UpdateProgress -= UpdateProgress;
            Core.UpdateProgress2 -= UpdateProgress2;
            thdHashFiles = null;

            prgProgress.Visible = true;

            thdCheckFiles = new Thread(Core.CheckDbForFiles);
            Core.Failed += ChkFilesFailed;
            Core.Finished += ChkFilesFinished;
            Core.UpdateProgress += UpdateProgress;
            Core.UpdateProgress2 += UpdateProgress2;
            Core.AddFile += AddFile;
            Core.AddOS += AddOS;
            btnAdd.Sensitive = false;
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
            if(thdCheckFiles != null)
                thdCheckFiles.Abort();
            prgProgress.Visible = false;
            btnStop.Visible = false;
            btnClose.Visible = true;
            btnExit.Sensitive = true;
            Core.Failed -= ChkFilesFailed;
            Core.Finished -= ChkFilesFinished;
            Core.UpdateProgress -= UpdateProgress;
            Core.UpdateProgress2 -= UpdateProgress2;
            Core.AddFile -= AddFile;
            Core.AddOS -= AddOS;
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
            if(thdCheckFiles != null)
                thdCheckFiles.Abort();

            Core.Failed -= ChkFilesFailed;
            Core.Finished -= ChkFilesFinished;
            Core.UpdateProgress -= UpdateProgress;
            Core.UpdateProgress2 -= UpdateProgress2;
            Core.AddFile -= AddFile;
            Core.AddOS -= AddOS;

            thdHashFiles = null;
            prgProgress.Visible = false;
            btnStop.Visible = false;
            btnClose.Visible = true;
            btnExit.Sensitive = true;
            btnSettings.Sensitive = true;
            btnAdd.Visible = true;
            btnPack.Visible = true;
            btnPack.Sensitive = true;

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
            if(MainClass.metadata != null)
            {
                foreach(string developer in MainClass.metadata.Developer)
                {
                    if(!string.IsNullOrWhiteSpace(txtDeveloper.Text))
                        txtDeveloper.Text += ",";
                    txtDeveloper.Text += developer;
                }

                if(!string.IsNullOrWhiteSpace(MainClass.metadata.Name))
                    txtProduct.Text = MainClass.metadata.Name;
                if(!string.IsNullOrWhiteSpace(MainClass.metadata.Version))
                    txtVersion.Text = MainClass.metadata.Version;

                foreach(LanguagesTypeLanguage language in MainClass.metadata.Languages)
                {
                    if(!string.IsNullOrWhiteSpace(txtLanguages.Text))
                        txtLanguages.Text += ",";
                    txtLanguages.Text += language;
                }

                foreach(ArchitecturesTypeArchitecture architecture in MainClass.metadata.Architectures)
                {
                    if(!string.IsNullOrWhiteSpace(txtArchitecture.Text))
                        txtArchitecture.Text += ",";
                    txtArchitecture.Text += architecture;
                }

                foreach(string machine in MainClass.metadata.Systems)
                {
                    if(!string.IsNullOrWhiteSpace(txtMachine.Text))
                        txtMachine.Text += ",";
                    txtMachine.Text += machine;
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
            btnAdd.Sensitive |= !known;
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
            OnBtnCloseClicked(sender, e);

        Application.Quit();
    }

    protected void OnBtnCloseClicked(object sender, EventArgs e)
    {
        btnFolder.Visible = true;
        btnArchive.Visible = true;
        MainClass.path = "";
        MainClass.files = null;
        MainClass.hashes = null;
        btnStop.Visible = false;
        btnAdd.Visible = false;
        btnPack.Visible = false;
        btnClose.Visible = false;
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

        if(MainClass.tmpFolder != null)
        {
            btnStop.Visible = false;
            prgProgress.Visible = true;
            prgProgress.Text = "Removing temporary files";
            thdPulseProgress = new Thread(() =>
            {
                Application.Invoke(delegate
                {
                    prgProgress.Pulse();
                });
                Thread.Sleep(10);
            });
            Core.Failed += RemoveTempFilesFailed;
            Core.Finished += RemoveTempFilesFinished;
            thdRemoveTemp = new Thread(Core.RemoveTempFolder);
            thdRemoveTemp.Start();
        }

        btnMetadata.Visible = false;
        MainClass.metadata = null;
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

        if(MainClass.unarProcess != null)
        {
            MainClass.unarProcess.Kill();
            MainClass.unarProcess = null;
        }

        if(MainClass.tmpFolder != null)
        {
            btnStop.Visible = false;
            prgProgress.Text = "Removing temporary files";
            thdPulseProgress = new Thread(() =>
            {
                Application.Invoke(delegate
                {
                    prgProgress.Pulse();
                });
                Thread.Sleep(10);
            });
            Core.Failed += RemoveTempFilesFailed;
            Core.Finished += RemoveTempFilesFinished;
            thdRemoveTemp = new Thread(Core.RemoveTempFolder);
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
        btnSettings.Sensitive = true;
        lblProgress.Visible = false;
        prgProgress.Visible = false;
        btnExit.Sensitive = true;
        btnFolder.Visible = true;
        btnArchive.Visible = true;
        btnSettings.Sensitive = true;
        Core.Failed -= FindFilesFailed;
        Core.Failed -= HashFilesFailed;
        Core.Failed -= ChkFilesFailed;
        Core.Failed -= OpenArchiveFailed;
        Core.Failed -= AddFilesToDbFailed;
        Core.Failed -= PackFilesFailed;
        Core.Failed -= ExtractArchiveFailed;
        Core.Failed -= RemoveTempFilesFailed;
        Core.Finished -= FindFilesFinished;
        Core.Finished -= HashFilesFinished;
        Core.Finished -= ChkFilesFinished;
        Core.Finished -= OpenArchiveFinished;
        Core.Finished -= AddFilesToDbFinished;
        Core.Finished -= ExtractArchiveFinished;
        Core.Finished -= RemoveTempFilesFinished;
        Core.FinishedWithText -= PackFilesFinished;
        Core.UpdateProgress -= UpdateProgress;
        Core.UpdateProgress2 -= UpdateProgress2;
        btnStop.Visible = false;
        if(fileView != null)
            fileView.Clear();
        if(osView != null)
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
            Core.Failed -= RemoveTempFilesFailed;
            Core.Finished -= RemoveTempFilesFinished;
            MainClass.path = null;
            MainClass.tmpFolder = null;
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
            Core.Failed -= RemoveTempFilesFailed;
            Core.Finished -= RemoveTempFilesFinished;
            MainClass.path = null;
            MainClass.tmpFolder = null;
            RestoreUI();
        });
    }

    protected void OnBtnAddClicked(object sender, EventArgs e)
    {
        btnAdd.Sensitive = false;
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

        Core.UpdateProgress += UpdateProgress;
        Core.Finished += AddFilesToDbFinished;
        Core.Failed += AddFilesToDbFailed;

        MainClass.dbInfo.architecture = txtArchitecture.Text;
        MainClass.dbInfo.description = txtDescription.Text;
        MainClass.dbInfo.developer = txtDeveloper.Text;
        MainClass.dbInfo.format = txtFormat.Text;
        MainClass.dbInfo.languages = txtLanguages.Text;
        MainClass.dbInfo.machine = txtMachine.Text;
        MainClass.dbInfo.product = txtProduct.Text;
        MainClass.dbInfo.version = txtVersion.Text;
        MainClass.dbInfo.files = chkFiles.Active;
        MainClass.dbInfo.netinstall = chkNetinstall.Active;
        MainClass.dbInfo.oem = chkOem.Active;
        MainClass.dbInfo.source = chkSource.Active;
        MainClass.dbInfo.update = chkUpdate.Active;
        MainClass.dbInfo.upgrade = chkUpgrade.Active;

        if(MainClass.metadata != null)
        {
            MemoryStream ms = new MemoryStream();
            XmlSerializer xs = new XmlSerializer(typeof(CICMMetadataType));
            xs.Serialize(ms, MainClass.metadata);
            MainClass.dbInfo.xml = ms.ToArray();
            JsonSerializer js = new JsonSerializer();
            ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            js.Serialize(sw, MainClass.metadata, typeof(CICMMetadataType));
            MainClass.dbInfo.json = ms.ToArray();
        }
        else
        {
            MainClass.dbInfo.xml = null;
            MainClass.dbInfo.json = null;
        }

        thdAddFiles = new Thread(Core.AddFilesToDb);
        thdAddFiles.Start();
    }

    public void AddFilesToDbFinished()
    {
        Application.Invoke(delegate
        {
            if(thdAddFiles != null)
                thdAddFiles.Abort();

            Core.UpdateProgress -= UpdateProgress;
            Core.Finished -= AddFilesToDbFinished;
            Core.Failed -= AddFilesToDbFailed;

            long counter = 0;
            fileView.Clear();
            foreach(KeyValuePair<string, DBFile> kvp in MainClass.hashes)
            {
                UpdateProgress(null, "Updating table", counter, MainClass.hashes.Count);
                fileView.AppendValues(kvp.Key, kvp.Value.Path, true, "green", "black");
                counter++;
            }

            // TODO: Update OS table

            prgProgress.Visible = false;
            btnPack.Sensitive = true;
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

            if(thdAddFiles != null)
                thdAddFiles.Abort();

            Core.UpdateProgress -= UpdateProgress;
            Core.Finished -= AddFilesToDbFinished;
            Core.Failed -= AddFilesToDbFailed;

            ChkFilesFinished();
        });
    }

    protected void OnBtnSettingsClicked(object sender, EventArgs e)
    {
        frmSettings _frmSettings = new frmSettings();
        _frmSettings.Show();
    }

    protected void OnBtnPackClicked(object sender, EventArgs e)
    {
        btnAdd.Sensitive = false;
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

        Core.UpdateProgress += UpdateProgress;
        Core.UpdateProgress2 += UpdateProgress2;
        Core.FinishedWithText += PackFilesFinished;
        Core.Failed += PackFilesFailed;

        MainClass.dbInfo.architecture = txtArchitecture.Text;
        MainClass.dbInfo.description = txtDescription.Text;
        MainClass.dbInfo.developer = txtDeveloper.Text;
        MainClass.dbInfo.format = txtFormat.Text;
        MainClass.dbInfo.languages = txtLanguages.Text;
        MainClass.dbInfo.machine = txtMachine.Text;
        MainClass.dbInfo.product = txtProduct.Text;
        MainClass.dbInfo.version = txtVersion.Text;
        MainClass.dbInfo.files = chkFiles.Active;
        MainClass.dbInfo.netinstall = chkNetinstall.Active;
        MainClass.dbInfo.oem = chkOem.Active;
        MainClass.dbInfo.source = chkSource.Active;
        MainClass.dbInfo.update = chkUpdate.Active;
        MainClass.dbInfo.upgrade = chkUpgrade.Active;

        if(!string.IsNullOrEmpty(MainClass.tmpFolder) && MainClass.copyArchive)
        {
            thdPulseProgress = new Thread(() =>
            {
                Application.Invoke(delegate
                {
                    prgProgress.Pulse();
                });
                Thread.Sleep(10);
            });
            Core.UpdateProgress -= UpdateProgress;
            Core.UpdateProgress2 -= UpdateProgress2;
            prgProgress.Text = "Copying archive as is.";
            prgProgress2.Visible = false;
            lblProgress2.Visible = false;
            thdPackFiles = new Thread(Core.CopyArchive);
            thdPackFiles.Start();
        }
        else
        {
            thdPackFiles = new Thread(Core.CompressFiles);
            thdPackFiles.Start();
        }
    }

    public void PackFilesFinished(string text)
    {
        Application.Invoke(delegate
        {
            if(thdPackFiles != null)
                thdPackFiles.Abort();

            Core.UpdateProgress -= UpdateProgress;
            Core.UpdateProgress2 -= UpdateProgress2;
            Core.FinishedWithText -= PackFilesFinished;
            Core.Failed -= PackFilesFailed;

            prgProgress.Visible = false;
            prgProgress2.Visible = false;
            lblProgress.Visible = false;
            lblProgress2.Visible = false;
            btnPack.Sensitive = false;
            btnClose.Sensitive = true;

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

            if(thdPackFiles != null)
                thdPackFiles.Abort();

            Core.UpdateProgress -= UpdateProgress;
            Core.UpdateProgress2 -= UpdateProgress2;
            Core.FinishedWithText -= PackFilesFinished;
            Core.Failed -= PackFilesFailed;

            btnAdd.Sensitive = true;
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
        if(!MainClass.unarUsable)
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
            btnSettings.Sensitive = false;
            thdPulseProgress = new Thread(() =>
            {
                Application.Invoke(delegate
                {
                    prgProgress.Pulse();
                });
                Thread.Sleep(10);
            });

            thdOpenArchive = new Thread(Core.OpenArchive);
            MainClass.path = dlgFolder.Filename;
            Core.Failed += OpenArchiveFailed;
            Core.Finished += OpenArchiveFinished;
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
            btnSettings.Sensitive = true;
            Core.Failed -= OpenArchiveFailed;
            Core.Finished -= OpenArchiveFinished;
            thdOpenArchive = null;
        });
    }

    public void OpenArchiveFinished()
    {
        Application.Invoke(delegate
        {
            stopped = false;
            prgProgress.Text = "Extracting archive";
            lblProgress.Visible = false;
            prgProgress.Visible = true;
            prgProgress2.Visible = true;
            btnExit.Sensitive = false;
            btnFolder.Visible = false;
            btnArchive.Visible = false;
            btnSettings.Sensitive = false;
            thdPulseProgress = new Thread(() =>
            {
                Application.Invoke(delegate
                {
                    prgProgress.Pulse();
                });
                Thread.Sleep(10);
            });
            Core.Failed -= OpenArchiveFailed;
            Core.Finished -= OpenArchiveFinished;
            thdOpenArchive = null;
            Core.Failed += ExtractArchiveFailed;
            Core.Finished += ExtractArchiveFinished;
            Core.UpdateProgress2 += UpdateProgress2;
            thdExtractArchive = new Thread(Core.ExtractArchive);
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
            prgProgress2.Visible = false;
            btnExit.Sensitive = true;
            btnFolder.Visible = true;
            btnArchive.Visible = true;
            btnSettings.Sensitive = true;
            Core.Failed -= ExtractArchiveFailed;
            Core.Finished -= ExtractArchiveFinished;
            Core.UpdateProgress2 -= UpdateProgress2;
            thdExtractArchive = null;
            if(MainClass.tmpFolder != null)
            {
                btnStop.Visible = false;
                prgProgress.Text = "Removing temporary files";
                thdPulseProgress = new Thread(() =>
                {
                    Application.Invoke(delegate
                    {
                        prgProgress.Pulse();
                    });
                    Thread.Sleep(10);
                });
                Core.Failed += RemoveTempFilesFailed;
                Core.Finished += RemoveTempFilesFinished;
                thdRemoveTemp = new Thread(Core.RemoveTempFolder);
                thdRemoveTemp.Start();
            }
        });
    }

    public void ExtractArchiveFinished()
    {
        Application.Invoke(delegate
        {
            if(thdExtractArchive != null)
                thdExtractArchive.Abort();
            stopped = false;
            lblProgress.Text = "Finding files";
            lblProgress.Visible = true;
            prgProgress.Visible = true;
            btnExit.Sensitive = false;
            btnFolder.Visible = false;
            btnArchive.Visible = false;
            btnSettings.Sensitive = false;
            Core.Failed -= ExtractArchiveFailed;
            Core.Finished -= ExtractArchiveFinished;
            Core.UpdateProgress2 -= UpdateProgress2;
            thdPulseProgress = new Thread(() =>
            {
                Application.Invoke(delegate
                {
                    prgProgress.Pulse();
                });
                Thread.Sleep(10);
            });

            thdFindFiles = new Thread(Core.FindFiles);
            Core.Failed += FindFilesFailed;
            Core.Finished += FindFilesFinished;
            btnStop.Visible = true;
            thdPulseProgress.Start();
            thdFindFiles.Start();
        });
    }

    protected void OnBtnMetadataClicked(object sender, EventArgs e)
    {
        dlgMetadata _dlgMetadata = new dlgMetadata();
        _dlgMetadata.Metadata = MainClass.metadata;
        _dlgMetadata.FillFields();

        if(_dlgMetadata.Run() == (int)ResponseType.Accept)
        {
            MainClass.metadata = _dlgMetadata.Metadata;

            if(string.IsNullOrWhiteSpace(txtDeveloper.Text))
            {
                foreach(string developer in MainClass.metadata.Developer)
                {
                    if(!string.IsNullOrWhiteSpace(txtDeveloper.Text))
                        txtDeveloper.Text += ",";
                    txtDeveloper.Text += developer;
                }
            }

            if(string.IsNullOrWhiteSpace(txtProduct.Text))
            {
                if(!string.IsNullOrWhiteSpace(MainClass.metadata.Name))
                    txtProduct.Text = MainClass.metadata.Name;
            }

            if(string.IsNullOrWhiteSpace(txtVersion.Text))
            {
                if(!string.IsNullOrWhiteSpace(MainClass.metadata.Version))
                    txtVersion.Text = MainClass.metadata.Version;
            }

            if(string.IsNullOrWhiteSpace(txtLanguages.Text))
            {
                foreach(LanguagesTypeLanguage language in MainClass.metadata.Languages)
                {
                    if(!string.IsNullOrWhiteSpace(txtLanguages.Text))
                        txtLanguages.Text += ",";
                    txtLanguages.Text += language;
                }
            }

            if(string.IsNullOrWhiteSpace(txtArchitecture.Text))
            {
                foreach(ArchitecturesTypeArchitecture architecture in MainClass.metadata.Architectures)
                {
                    if(!string.IsNullOrWhiteSpace(txtArchitecture.Text))
                        txtArchitecture.Text += ",";
                    txtArchitecture.Text += architecture;
                }
            }

            if(string.IsNullOrWhiteSpace(txtMachine.Text))
            {
                foreach(string machine in MainClass.metadata.Systems)
                {
                    if(!string.IsNullOrWhiteSpace(txtMachine.Text))
                        txtMachine.Text += ",";
                    txtMachine.Text += machine;
                }
            }

            btnMetadata.ModifyBg(StateType.Normal, new Gdk.Color(0, 127, 0));
        }

        _dlgMetadata.Destroy();
    }
}
