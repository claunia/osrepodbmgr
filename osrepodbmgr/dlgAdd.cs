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
using Gdk;
using Gtk;
using Newtonsoft.Json;
using osrepodbmgr;
using osrepodbmgr.Core;
using Schemas;

public partial class dlgAdd : Dialog
{
    public delegate void OnAddedOSDelegate(DbEntry os);

    ListStore fileView;
    int       knownFiles;
    ListStore osView;
    bool      stopped;
    Thread    thdAddFiles;
    Thread    thdCheckFiles;
    Thread    thdExtractArchive;
    Thread    thdFindFiles;
    Thread    thdHashFiles;
    Thread    thdOpenArchive;
    Thread    thdPackFiles;
    Thread    thdPulseProgress;
    Thread    thdRemoveTemp;

    public dlgAdd()
    {
        Build();

        Context.UnarChangeStatus += UnarChangeStatus;
        Context.CheckUnar();

        CellRendererText   filenameCell = new CellRendererText();
        CellRendererToggle crackCell    = new CellRendererToggle();
        CellRendererText   hashCell     = new CellRendererText();
        CellRendererToggle dbCell       = new CellRendererToggle();

        TreeViewColumn filenameColumn =
            new TreeViewColumn("Path", filenameCell, "text", 0, "background", 3,
                               "foreground", 4);
        TreeViewColumn crackColumn = new TreeViewColumn("Crack?", crackCell, "active", 5);
        TreeViewColumn hashColumn  =
            new TreeViewColumn("SHA256",                       hashCell, "text",   1, "background", 3, "foreground", 4);
        TreeViewColumn dbColumn = new TreeViewColumn("Known?", dbCell,   "active", 2);

        fileView = new ListStore(typeof(string), typeof(string), typeof(bool), typeof(string), typeof(string),
                                 typeof(bool));

        treeFiles.Model = fileView;
        treeFiles.AppendColumn(filenameColumn);
        treeFiles.AppendColumn(crackColumn);
        treeFiles.AppendColumn(hashColumn);
        treeFiles.AppendColumn(dbColumn);

        tabTabs.GetNthPage(1).Visible = false;

        CellRendererText   developerCell    = new CellRendererText();
        CellRendererText   productCell      = new CellRendererText();
        CellRendererText   versionCell      = new CellRendererText();
        CellRendererText   languagesCell    = new CellRendererText();
        CellRendererText   architectureCell = new CellRendererText();
        CellRendererText   machineCell      = new CellRendererText();
        CellRendererText   formatCell       = new CellRendererText();
        CellRendererText   descriptionCell  = new CellRendererText();
        CellRendererToggle oemCell          = new CellRendererToggle();
        CellRendererToggle upgradeCell      = new CellRendererToggle();
        CellRendererToggle updateCell       = new CellRendererToggle();
        CellRendererToggle sourceCell       = new CellRendererToggle();
        CellRendererToggle filesCell        = new CellRendererToggle();
        CellRendererToggle netinstallCell   = new CellRendererToggle();

        TreeViewColumn developerColumn    = new TreeViewColumn("Developer",    developerCell,    "text",   0);
        TreeViewColumn productColumn      = new TreeViewColumn("Product",      productCell,      "text",   1);
        TreeViewColumn versionColumn      = new TreeViewColumn("Version",      versionCell,      "text",   2);
        TreeViewColumn languagesColumn    = new TreeViewColumn("Languages",    languagesCell,    "text",   3);
        TreeViewColumn architectureColumn = new TreeViewColumn("Architecture", architectureCell, "text",   4);
        TreeViewColumn machineColumn      = new TreeViewColumn("Machine",      machineCell,      "text",   5);
        TreeViewColumn formatColumn       = new TreeViewColumn("Format",       formatCell,       "text",   6);
        TreeViewColumn descriptionColumn  = new TreeViewColumn("Description",  descriptionCell,  "text",   7);
        TreeViewColumn oemColumn          = new TreeViewColumn("OEM?",         oemCell,          "active", 8);
        TreeViewColumn upgradeColumn      = new TreeViewColumn("Upgrade?",     upgradeCell,      "active", 9);
        TreeViewColumn updateColumn       = new TreeViewColumn("Update?",      updateCell,       "active", 10);
        TreeViewColumn sourceColumn       = new TreeViewColumn("Source?",      sourceCell,       "active", 11);
        TreeViewColumn filesColumn        = new TreeViewColumn("Files?",       filesCell,        "active", 12);
        TreeViewColumn netinstallColumn   = new TreeViewColumn("NetInstall?",  netinstallCell,   "active", 13);

        osView = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string),
                               typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(bool),
                               typeof(bool), typeof(bool), typeof(bool));

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

        treeFiles.Selection.Changed += treeFilesSelectionChanged;
    }

    public event OnAddedOSDelegate OnAddedOS;

    void UnarChangeStatus()
    {
        Application.Invoke(delegate { btnArchive.Sensitive = Context.UnarUsable; });
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        if(btnStop.Visible) btnStop.Click();
        if(btnClose.Sensitive) btnClose.Click();

        Application.Quit();
        a.RetVal = true;
    }

    protected void OnBtnFolderClicked(object sender, EventArgs e)
    {
        FileChooserDialog dlgFolder =
            new FileChooserDialog("Open folder", this, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel,
                                  "Choose", ResponseType.Accept) {SelectMultiple = false};

        if(dlgFolder.Run() == (int)ResponseType.Accept)
        {
            knownFiles          = 0;
            stopped             = false;
            lblProgress.Text    = "Finding files";
            lblProgress.Visible = true;
            prgProgress.Visible = true;
            btnExit.Sensitive   = false;
            btnFolder.Visible   = false;
            btnArchive.Visible  = false;
            thdPulseProgress    = new Thread(() =>
            {
                while(true)
                {
                    Application.Invoke(delegate { prgProgress.Pulse(); });
                    Thread.Sleep(66);
                }
            });

            thdFindFiles     =  new Thread(Workers.FindFiles);
            Context.Path     =  dlgFolder.Filename;
            Workers.Failed   += FindFilesFailed;
            Workers.Finished += FindFilesFinished;
            btnStop.Visible  =  true;
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
                MessageDialog dlgMsg =
                    new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            thdPulseProgress?.Abort();
            lblProgress.Visible =  false;
            prgProgress.Visible =  false;
            btnExit.Sensitive   =  true;
            btnFolder.Visible   =  true;
            btnArchive.Visible  =  true;
            btnStop.Visible     =  false;
            Workers.Failed      -= FindFilesFailed;
            Workers.Finished    -= FindFilesFinished;
            thdFindFiles        =  null;
        });
    }

    void FindFilesFinished()
    {
        Application.Invoke(delegate
        {
            thdPulseProgress?.Abort();

            Workers.Failed   -= FindFilesFailed;
            Workers.Finished -= FindFilesFinished;

            lblProgress.Visible  = true;
            prgProgress.Visible  = true;
            lblProgress2.Visible = true;
            prgProgress2.Visible = true;

            thdFindFiles            =  null;
            thdHashFiles            =  new Thread(Workers.HashFiles);
            Workers.Failed          += HashFilesFailed;
            Workers.Finished        += HashFilesFinished;
            Workers.UpdateProgress  += UpdateProgress;
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
                MessageDialog dlgMsg =
                    new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            thdPulseProgress?.Abort();
            lblProgress.Visible     =  false;
            prgProgress.Visible     =  false;
            lblProgress2.Visible    =  false;
            prgProgress2.Visible    =  false;
            btnStop.Visible         =  false;
            Workers.Failed          -= HashFilesFailed;
            Workers.Finished        -= HashFilesFinished;
            Workers.UpdateProgress  -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            btnExit.Sensitive       =  true;
            btnFolder.Visible       =  true;
            btnArchive.Visible      =  true;
            thdHashFiles            =  null;
        });
    }

    void HashFilesFinished()
    {
        Application.Invoke(delegate
        {
            thdPulseProgress?.Abort();
            lblProgress.Visible     =  false;
            prgProgress.Visible     =  false;
            lblProgress.Visible     =  false;
            prgProgress.Visible     =  false;
            lblProgress2.Visible    =  false;
            prgProgress2.Visible    =  false;
            Workers.Failed          -= HashFilesFailed;
            Workers.Finished        -= HashFilesFinished;
            Workers.UpdateProgress  -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            thdHashFiles            =  null;

            prgProgress.Visible = true;

            thdCheckFiles           =  new Thread(Workers.CheckDbForFiles);
            Workers.Failed          += ChkFilesFailed;
            Workers.Finished        += ChkFilesFinished;
            Workers.UpdateProgress  += UpdateProgress;
            Workers.UpdateProgress2 += UpdateProgress2;
            Workers.AddFileForOS    += AddFile;
            Workers.AddOS           += AddOS;
            thdCheckFiles.Start();
        });
    }

    void ChkFilesFailed(string text)
    {
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg =
                    new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            prgProgress.Visible     =  false;
            btnStop.Visible         =  false;
            btnClose.Visible        =  false;
            btnExit.Sensitive       =  true;
            Workers.Failed          -= ChkFilesFailed;
            Workers.Finished        -= ChkFilesFinished;
            Workers.UpdateProgress  -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            Workers.AddFileForOS    -= AddFile;
            Workers.AddOS           -= AddOS;
            thdPulseProgress?.Abort();
            thdCheckFiles?.Abort();
            thdHashFiles = null;
            fileView?.Clear();
            if(osView == null) return;

            tabTabs.GetNthPage(1).Visible = false;
            osView.Clear();
        });
    }

    void ChkFilesFinished()
    {
        Application.Invoke(delegate
        {
            Workers.Failed          -= ChkFilesFailed;
            Workers.Finished        -= ChkFilesFinished;
            Workers.UpdateProgress  -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            Workers.AddFileForOS    -= AddFile;
            Workers.AddOS           -= AddOS;

            thdCheckFiles?.Abort();
            thdPulseProgress?.Abort();
            thdHashFiles             = null;
            prgProgress.Visible      = false;
            btnStop.Visible          = false;
            btnClose.Visible         = true;
            btnExit.Sensitive        = true;
            btnPack.Visible          = true;
            btnPack.Sensitive        = true;
            btnRemoveFile.Visible    = true;
            btnToggleCrack.Visible   = true;
            btnRemoveFile.Sensitive  = true;
            btnToggleCrack.Sensitive = true;

            txtFormat.IsEditable       = true;
            txtMachine.IsEditable      = true;
            txtProduct.IsEditable      = true;
            txtVersion.IsEditable      = true;
            txtLanguages.IsEditable    = true;
            txtDeveloper.IsEditable    = true;
            txtDescription.IsEditable  = true;
            txtArchitecture.IsEditable = true;
            chkOem.Sensitive           = true;
            chkFiles.Sensitive         = true;
            chkUpdate.Sensitive        = true;
            chkUpgrade.Sensitive       = true;
            chkNetinstall.Sensitive    = true;
            chkSource.Sensitive        = true;

            btnMetadata.Visible = true;
            if(Context.Metadata != null)
            {
                if(Context.Metadata.Developer != null)
                    foreach(string developer in Context.Metadata.Developer)
                    {
                        if(!string.IsNullOrWhiteSpace(txtDeveloper.Text)) txtDeveloper.Text += ",";
                        txtDeveloper.Text                                                   += developer;
                    }

                if(!string.IsNullOrWhiteSpace(Context.Metadata.Name)) txtProduct.Text    = Context.Metadata.Name;
                if(!string.IsNullOrWhiteSpace(Context.Metadata.Version)) txtVersion.Text = Context.Metadata.Version;

                if(Context.Metadata.Languages != null)
                    foreach(LanguagesTypeLanguage language in Context.Metadata.Languages)
                    {
                        if(!string.IsNullOrWhiteSpace(txtLanguages.Text)) txtLanguages.Text += ",";
                        txtLanguages.Text                                                   += language;
                    }

                if(Context.Metadata.Architectures != null)
                    foreach(ArchitecturesTypeArchitecture architecture in Context.Metadata.Architectures)
                    {
                        if(!string.IsNullOrWhiteSpace(txtArchitecture.Text)) txtArchitecture.Text += ",";
                        txtArchitecture.Text                                                      += architecture;
                    }

                if(Context.Metadata.Systems != null)
                    foreach(string machine in Context.Metadata.Systems)
                    {
                        if(!string.IsNullOrWhiteSpace(txtMachine.Text)) txtMachine.Text += ",";
                        txtMachine.Text                                                 += machine;
                    }

                btnMetadata.ModifyBg(StateType.Normal, new Color(0, 127, 0));
            }
            else btnMetadata.ModifyBg(StateType.Normal, new Color(127, 0, 0));

            lblStatus.Visible = true;
            lblStatus.Text    = $"{fileView.IterNChildren()} files ({knownFiles} already known)";
        });
    }

    void AddFile(string filename, string hash, bool known, bool isCrack)
    {
        Application.Invoke(delegate
        {
            string color = known ? "green" : "red";
            fileView.AppendValues(filename, hash, known, color, "black", isCrack);
            btnPack.Sensitive |= !known;
            if(known) knownFiles++;
        });
    }

    void AddOS(DbEntry os)
    {
        Application.Invoke(delegate
        {
            tabTabs.GetNthPage(1).Visible = true;
            osView.AppendValues(os.Developer, os.Product, os.Version, os.Languages, os.Architecture, os.Machine,
                                os.Format, os.Description, os.Oem, os.Upgrade, os.Update, os.Source, os.Files,
                                os.Netinstall);
        });
    }

    protected void OnBtnExitClicked(object sender, EventArgs e)
    {
        if(btnClose.Sensitive) btnClose.Click();

        btnDialog.Click();
    }

    protected void OnBtnCloseClicked(object sender, EventArgs e)
    {
        btnFolder.Visible      = true;
        btnArchive.Visible     = true;
        Context.Path           = "";
        Context.Files          = null;
        Context.Hashes         = null;
        btnStop.Visible        = false;
        btnPack.Visible        = false;
        btnClose.Visible       = false;
        btnRemoveFile.Visible  = false;
        btnToggleCrack.Visible = false;
        fileView?.Clear();
        if(osView != null)
        {
            tabTabs.GetNthPage(1).Visible = false;
            osView.Clear();
        }

        txtFormat.IsEditable       = false;
        txtMachine.IsEditable      = false;
        txtProduct.IsEditable      = false;
        txtVersion.IsEditable      = false;
        txtLanguages.IsEditable    = false;
        txtDeveloper.IsEditable    = false;
        txtDescription.IsEditable  = false;
        txtArchitecture.IsEditable = false;
        chkOem.Sensitive           = false;
        chkFiles.Sensitive         = false;
        chkUpdate.Sensitive        = false;
        chkUpgrade.Sensitive       = false;
        chkNetinstall.Sensitive    = false;
        chkSource.Sensitive        = false;
        txtFormat.Text             = "";
        txtMachine.Text            = "";
        txtProduct.Text            = "";
        txtVersion.Text            = "";
        txtLanguages.Text          = "";
        txtDeveloper.Text          = "";
        txtDescription.Text        = "";
        txtArchitecture.Text       = "";
        chkOem.Active              = false;
        chkFiles.Active            = false;
        chkUpdate.Active           = false;
        chkUpgrade.Active          = false;
        chkNetinstall.Active       = false;
        chkSource.Active           = false;

        if(Context.TmpFolder != null)
        {
            btnStop.Visible     = false;
            prgProgress.Visible = true;
            prgProgress.Text    = "Removing temporary files";
            thdPulseProgress    = new Thread(() =>
            {
                while(true)
                {
                    Application.Invoke(delegate { prgProgress.Pulse(); });
                    Thread.Sleep(66);
                }
            });
            Workers.Failed   += RemoveTempFilesFailed;
            Workers.Finished += RemoveTempFilesFinished;
            thdRemoveTemp    =  new Thread(Workers.RemoveTempFolder);
            thdRemoveTemp.Start();
        }

        btnMetadata.Visible = false;
        Context.Metadata    = null;
        lblStatus.Visible   = false;
    }

    void UpdateProgress(string text, string inner, long current, long maximum)
    {
        Application.Invoke(delegate
        {
            lblProgress.Text                     = text;
            prgProgress.Text                     = inner;
            if(maximum > 0) prgProgress.Fraction = current / (double)maximum;
            else prgProgress.Pulse();
        });
    }

    void UpdateProgress2(string text, string inner, long current, long maximum)
    {
        Application.Invoke(delegate
        {
            lblProgress2.Text                     = text;
            prgProgress2.Text                     = inner;
            if(maximum > 0) prgProgress2.Fraction = current / (double)maximum;
            else prgProgress2.Pulse();
        });
    }

    protected void OnBtnStopClicked(object sender, EventArgs e)
    {
        stopped = true;

        Workers.AddFileForOS     -= AddFile;
        Workers.AddOS            -= AddOS;
        Workers.Failed           -= AddFilesToDbFailed;
        Workers.Failed           -= ChkFilesFailed;
        Workers.Failed           -= ExtractArchiveFailed;
        Workers.Failed           -= FindFilesFailed;
        Workers.Failed           -= HashFilesFailed;
        Workers.Failed           -= OpenArchiveFailed;
        Workers.Failed           -= PackFilesFailed;
        Workers.Failed           -= RemoveTempFilesFailed;
        Workers.Finished         -= AddFilesToDbFinished;
        Workers.Finished         -= ChkFilesFinished;
        Workers.Finished         -= ExtractArchiveFinished;
        Workers.Finished         -= FindFilesFinished;
        Workers.Finished         -= HashFilesFinished;
        Workers.Finished         -= OpenArchiveFinished;
        Workers.Finished         -= RemoveTempFilesFinished;
        Workers.FinishedWithText -= PackFilesFinished;
        Workers.UpdateProgress   -= UpdateProgress;
        Workers.UpdateProgress2  -= UpdateProgress2;

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

        if(Context.UnarProcess != null)
        {
            Context.UnarProcess.Kill();
            Context.UnarProcess = null;
        }

        if(Context.TmpFolder != null)
        {
            btnStop.Visible  = false;
            prgProgress.Text = "Removing temporary files";
            thdPulseProgress = new Thread(() =>
            {
                while(true)
                {
                    Application.Invoke(delegate { prgProgress.Pulse(); });
                    Thread.Sleep(66);
                }
            });
            Workers.Failed   += RemoveTempFilesFailed;
            Workers.Finished += RemoveTempFilesFinished;
            thdRemoveTemp    =  new Thread(Workers.RemoveTempFolder);
            thdRemoveTemp.Start();
        }
        else RestoreUi();
    }

    void RestoreUi()
    {
        lblProgress.Visible      =  false;
        prgProgress.Visible      =  false;
        lblProgress2.Visible     =  false;
        prgProgress2.Visible     =  false;
        btnExit.Sensitive        =  true;
        btnFolder.Visible        =  true;
        btnArchive.Visible       =  true;
        lblProgress.Visible      =  false;
        prgProgress.Visible      =  false;
        btnExit.Sensitive        =  true;
        btnFolder.Visible        =  true;
        btnArchive.Visible       =  true;
        Workers.Failed           -= FindFilesFailed;
        Workers.Failed           -= HashFilesFailed;
        Workers.Failed           -= ChkFilesFailed;
        Workers.Failed           -= OpenArchiveFailed;
        Workers.Failed           -= AddFilesToDbFailed;
        Workers.Failed           -= PackFilesFailed;
        Workers.Failed           -= ExtractArchiveFailed;
        Workers.Failed           -= RemoveTempFilesFailed;
        Workers.Finished         -= FindFilesFinished;
        Workers.Finished         -= HashFilesFinished;
        Workers.Finished         -= ChkFilesFinished;
        Workers.Finished         -= OpenArchiveFinished;
        Workers.Finished         -= AddFilesToDbFinished;
        Workers.Finished         -= ExtractArchiveFinished;
        Workers.Finished         -= RemoveTempFilesFinished;
        Workers.FinishedWithText -= PackFilesFinished;
        Workers.UpdateProgress   -= UpdateProgress;
        Workers.UpdateProgress2  -= UpdateProgress2;
        btnStop.Visible          =  false;
        fileView?.Clear();
        if(osView == null || tabTabs?.GetNthPage(1) == null) return;

        tabTabs.GetNthPage(1).Visible = false;
        osView.Clear();
    }

    void RemoveTempFilesFailed(string text)
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

            Workers.Failed    -= RemoveTempFilesFailed;
            Workers.Finished  -= RemoveTempFilesFinished;
            Context.Path      =  null;
            Context.TmpFolder =  null;
            RestoreUi();
        });
    }

    void RemoveTempFilesFinished()
    {
        Application.Invoke(delegate
        {
            if(thdPulseProgress != null)
            {
                thdPulseProgress.Abort();
                thdPulseProgress = null;
            }

            Workers.Failed    -= RemoveTempFilesFailed;
            Workers.Finished  -= RemoveTempFilesFinished;
            Context.Path      =  null;
            Context.TmpFolder =  null;
            RestoreUi();
        });
    }

    void AddToDatabase()
    {
        btnRemoveFile.Sensitive    = false;
        btnToggleCrack.Sensitive   = false;
        btnPack.Sensitive          = false;
        btnClose.Sensitive         = false;
        prgProgress.Visible        = true;
        txtFormat.IsEditable       = false;
        txtMachine.IsEditable      = false;
        txtProduct.IsEditable      = false;
        txtVersion.IsEditable      = false;
        txtLanguages.IsEditable    = false;
        txtDeveloper.IsEditable    = false;
        txtDescription.IsEditable  = false;
        txtArchitecture.IsEditable = false;
        chkOem.Sensitive           = false;
        chkFiles.Sensitive         = false;
        chkUpdate.Sensitive        = false;
        chkUpgrade.Sensitive       = false;
        chkNetinstall.Sensitive    = false;
        chkSource.Sensitive        = false;

        Workers.UpdateProgress += UpdateProgress;
        Workers.Finished       += AddFilesToDbFinished;
        Workers.Failed         += AddFilesToDbFailed;

        Context.DbInfo.Architecture = txtArchitecture.Text;
        Context.DbInfo.Description  = txtDescription.Text;
        Context.DbInfo.Developer    = txtDeveloper.Text;
        Context.DbInfo.Format       = txtFormat.Text;
        Context.DbInfo.Languages    = txtLanguages.Text;
        Context.DbInfo.Machine      = txtMachine.Text;
        Context.DbInfo.Product      = txtProduct.Text;
        Context.DbInfo.Version      = txtVersion.Text;
        Context.DbInfo.Files        = chkFiles.Active;
        Context.DbInfo.Netinstall   = chkNetinstall.Active;
        Context.DbInfo.Oem          = chkOem.Active;
        Context.DbInfo.Source       = chkSource.Active;
        Context.DbInfo.Update       = chkUpdate.Active;
        Context.DbInfo.Upgrade      = chkUpgrade.Active;

        if(Context.Metadata != null)
        {
            MemoryStream  ms = new MemoryStream();
            XmlSerializer xs = new XmlSerializer(typeof(CICMMetadataType));
            xs.Serialize(ms, Context.Metadata);
            Context.DbInfo.Xml = ms.ToArray();
            JsonSerializer js  = new JsonSerializer();
            ms                 = new MemoryStream();
            StreamWriter sw    = new StreamWriter(ms);
            js.Serialize(sw, Context.Metadata, typeof(CICMMetadataType));
            Context.DbInfo.Json = ms.ToArray();
        }
        else
        {
            Context.DbInfo.Xml  = null;
            Context.DbInfo.Json = null;
        }

        thdAddFiles = new Thread(Workers.AddFilesToDb);
        thdAddFiles.Start();
    }

    void AddFilesToDbFinished()
    {
        Application.Invoke(delegate
        {
            Workers.UpdateProgress -= UpdateProgress;
            Workers.Finished       -= AddFilesToDbFinished;
            Workers.Failed         -= AddFilesToDbFailed;

            thdAddFiles?.Abort();
            thdPulseProgress?.Abort();

            long counter = 0;
            fileView.Clear();
            foreach(KeyValuePair<string, DbOsFile> kvp in Context.Hashes)
            {
                UpdateProgress(null, "Updating table", counter, Context.Hashes.Count);
                fileView.AppendValues(kvp.Key, kvp.Value.Sha256, true, "green", "black");
                counter++;
            }

            // TODO: Update OS table

            OnAddedOS?.Invoke(Context.DbInfo);

            prgProgress.Visible = false;
            btnClose.Sensitive  = true;
        });
    }

    void AddFilesToDbFailed(string text)
    {
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg =
                    new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            Workers.UpdateProgress -= UpdateProgress;
            Workers.Finished       -= AddFilesToDbFinished;
            Workers.Failed         -= AddFilesToDbFailed;

            thdAddFiles?.Abort();
            thdPulseProgress?.Abort();

            ChkFilesFinished();
        });
    }

    protected void OnBtnPackClicked(object sender, EventArgs e)
    {
        btnRemoveFile.Sensitive    = false;
        btnToggleCrack.Sensitive   = false;
        btnPack.Sensitive          = false;
        btnClose.Sensitive         = false;
        prgProgress.Visible        = true;
        prgProgress2.Visible       = true;
        lblProgress.Visible        = true;
        lblProgress2.Visible       = true;
        txtFormat.IsEditable       = false;
        txtMachine.IsEditable      = false;
        txtProduct.IsEditable      = false;
        txtVersion.IsEditable      = false;
        txtLanguages.IsEditable    = false;
        txtDeveloper.IsEditable    = false;
        txtDescription.IsEditable  = false;
        txtArchitecture.IsEditable = false;
        chkOem.Sensitive           = false;
        chkFiles.Sensitive         = false;
        chkUpdate.Sensitive        = false;
        chkUpgrade.Sensitive       = false;
        chkNetinstall.Sensitive    = false;
        chkSource.Sensitive        = false;

        Workers.UpdateProgress   += UpdateProgress;
        Workers.UpdateProgress2  += UpdateProgress2;
        Workers.FinishedWithText += PackFilesFinished;
        Workers.Failed           += PackFilesFailed;

        Context.DbInfo = new DbEntry
        {
            Architecture = txtArchitecture.Text,
            Description  = txtDescription.Text,
            Developer    = txtDeveloper.Text,
            Format       = txtFormat.Text,
            Languages    = txtLanguages.Text,
            Machine      = txtMachine.Text,
            Product      = txtProduct.Text,
            Version      = txtVersion.Text,
            Files        = chkFiles.Active,
            Netinstall   = chkNetinstall.Active,
            Oem          = chkOem.Active,
            Source       = chkSource.Active,
            Update       = chkUpdate.Active,
            Upgrade      = chkUpgrade.Active
        };

        thdPackFiles = new Thread(Workers.CompressFiles);
        thdPackFiles.Start();
    }

    void PackFilesFinished(string text)
    {
        Application.Invoke(delegate
        {
            Workers.UpdateProgress   -= UpdateProgress;
            Workers.UpdateProgress2  -= UpdateProgress2;
            Workers.FinishedWithText -= PackFilesFinished;
            Workers.Failed           -= PackFilesFailed;
            prgProgress2.Visible     =  false;
            lblProgress2.Visible     =  false;

            thdPackFiles?.Abort();
            thdPulseProgress?.Abort();

            AddToDatabase();

            MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok,
                                                     "Correctly packed to " + text);
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
                MessageDialog dlgMsg =
                    new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            Workers.UpdateProgress   -= UpdateProgress;
            Workers.UpdateProgress2  -= UpdateProgress2;
            Workers.FinishedWithText -= PackFilesFinished;
            Workers.Failed           -= PackFilesFailed;

            thdPackFiles?.Abort();
            thdPulseProgress?.Abort();

            btnRemoveFile.Sensitive    = true;
            btnToggleCrack.Sensitive   = true;
            btnPack.Sensitive          = true;
            btnClose.Sensitive         = true;
            prgProgress.Visible        = false;
            prgProgress2.Visible       = false;
            lblProgress.Visible        = false;
            lblProgress2.Visible       = false;
            txtFormat.IsEditable       = true;
            txtMachine.IsEditable      = true;
            txtProduct.IsEditable      = true;
            txtVersion.IsEditable      = true;
            txtLanguages.IsEditable    = true;
            txtDeveloper.IsEditable    = true;
            txtDescription.IsEditable  = true;
            txtArchitecture.IsEditable = true;
            chkOem.Sensitive           = true;
            chkFiles.Sensitive         = true;
            chkUpdate.Sensitive        = true;
            chkUpgrade.Sensitive       = true;
            chkNetinstall.Sensitive    = true;
            chkSource.Sensitive        = true;
        });
    }

    protected void OnBtnArchiveClicked(object sender, EventArgs e)
    {
        if(!Context.UnarUsable)
        {
            MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                                     "Cannot open archives without a working unar installation.");
            dlgMsg.Run();
            dlgMsg.Destroy();
            return;
        }

        FileChooserDialog dlgFolder =
            new FileChooserDialog("Open archive", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open",
                                  ResponseType.Accept) {SelectMultiple = false};

        if(dlgFolder.Run() == (int)ResponseType.Accept)
        {
            knownFiles          = 0;
            stopped             = false;
            prgProgress.Text    = "Opening archive";
            lblProgress.Visible = false;
            prgProgress.Visible = true;
            btnExit.Sensitive   = false;
            btnFolder.Visible   = false;
            btnArchive.Visible  = false;
            thdPulseProgress    = new Thread(() =>
            {
                while(true)
                {
                    Application.Invoke(delegate { prgProgress.Pulse(); });
                    Thread.Sleep(66);
                }
            });

            thdOpenArchive   =  new Thread(Workers.OpenArchive);
            Context.Path     =  dlgFolder.Filename;
            Workers.Failed   += OpenArchiveFailed;
            Workers.Finished += OpenArchiveFinished;
            btnStop.Visible  =  true;
            thdPulseProgress.Start();
            thdOpenArchive.Start();
        }

        dlgFolder.Destroy();
    }

    void OpenArchiveFailed(string text)
    {
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg =
                    new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            thdPulseProgress?.Abort();
            lblProgress.Visible =  false;
            prgProgress.Visible =  false;
            btnExit.Sensitive   =  true;
            btnFolder.Visible   =  true;
            btnArchive.Visible  =  true;
            btnStop.Visible     =  false;
            Workers.Failed      -= OpenArchiveFailed;
            Workers.Finished    -= OpenArchiveFinished;
            thdOpenArchive      =  null;
        });
    }

    void OpenArchiveFinished()
    {
        Application.Invoke(delegate
        {
            stopped              = false;
            prgProgress.Text     = "Extracting archive";
            prgProgress.Visible  = true;
            prgProgress2.Visible = true;
            btnExit.Sensitive    = false;
            btnFolder.Visible    = false;
            btnArchive.Visible   = false;
            thdPulseProgress?.Abort();
            Workers.UpdateProgress  += UpdateProgress;
            lblProgress.Visible     =  true;
            lblProgress2.Visible    =  true;
            Workers.Failed          -= OpenArchiveFailed;
            Workers.Finished        -= OpenArchiveFinished;
            thdOpenArchive          =  null;
            Workers.Failed          += ExtractArchiveFailed;
            Workers.Finished        += ExtractArchiveFinished;
            Workers.UpdateProgress2 += UpdateProgress2;
            thdExtractArchive       =  new Thread(Workers.ExtractArchive);
            thdExtractArchive.Start();
        });
    }

    void ExtractArchiveFailed(string text)
    {
        Application.Invoke(delegate
        {
            if(!stopped)
            {
                MessageDialog dlgMsg =
                    new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            }

            thdPulseProgress?.Abort();
            lblProgress.Visible     =  false;
            lblProgress2.Visible    =  false;
            prgProgress2.Visible    =  false;
            btnExit.Sensitive       =  true;
            btnFolder.Visible       =  true;
            btnArchive.Visible      =  true;
            Workers.Failed          -= ExtractArchiveFailed;
            Workers.Finished        -= ExtractArchiveFinished;
            Workers.UpdateProgress  -= UpdateProgress;
            Workers.UpdateProgress2 -= UpdateProgress2;
            thdExtractArchive       =  null;
            if(Context.TmpFolder == null) return;

            btnStop.Visible  = false;
            prgProgress.Text = "Removing temporary files";
            thdPulseProgress = new Thread(() =>
            {
                while(true)
                {
                    Application.Invoke(delegate { prgProgress.Pulse(); });
                    Thread.Sleep(66);
                }
            });
            Workers.Failed   += RemoveTempFilesFailed;
            Workers.Finished += RemoveTempFilesFinished;
            thdRemoveTemp    =  new Thread(Workers.RemoveTempFolder);
            thdRemoveTemp.Start();
        });
    }

    void ExtractArchiveFinished()
    {
        Application.Invoke(delegate
        {
            stopped                                         =  false;
            lblProgress.Text                                =  "Finding files";
            lblProgress.Visible                             =  true;
            lblProgress2.Visible                            =  false;
            prgProgress.Visible                             =  true;
            btnExit.Sensitive                               =  false;
            btnFolder.Visible                               =  false;
            btnArchive.Visible                              =  false;
            Workers.Failed                                  -= ExtractArchiveFailed;
            Workers.Finished                                -= ExtractArchiveFinished;
            Workers.UpdateProgress                          -= UpdateProgress;
            Workers.UpdateProgress2                         -= UpdateProgress2;
            if(thdExtractArchive != null) thdExtractArchive =  null;
            thdPulseProgress?.Abort();
            thdPulseProgress = new Thread(() =>
            {
                while(true)
                {
                    Application.Invoke(delegate { prgProgress.Pulse(); });
                    Thread.Sleep(66);
                }
            });

            thdFindFiles     =  new Thread(Workers.FindFiles);
            Workers.Failed   += FindFilesFailed;
            Workers.Finished += FindFilesFinished;
            btnStop.Visible  =  true;
            thdPulseProgress.Start();
            thdFindFiles.Start();
        });
    }

    protected void OnBtnMetadataClicked(object sender, EventArgs e)
    {
        dlgMetadata _dlgMetadata = new dlgMetadata {Metadata = Context.Metadata};
        _dlgMetadata.FillFields();

        if(_dlgMetadata.Run() == (int)ResponseType.Ok)
        {
            Context.Metadata = _dlgMetadata.Metadata;

            if(string.IsNullOrWhiteSpace(txtDeveloper.Text))
                if(Context.Metadata.Developer != null)
                    foreach(string developer in Context.Metadata.Developer)
                    {
                        if(!string.IsNullOrWhiteSpace(txtDeveloper.Text)) txtDeveloper.Text += ",";
                        txtDeveloper.Text                                                   += developer;
                    }

            if(string.IsNullOrWhiteSpace(txtProduct.Text))
                if(!string.IsNullOrWhiteSpace(Context.Metadata.Name))
                    txtProduct.Text = Context.Metadata.Name;

            if(string.IsNullOrWhiteSpace(txtVersion.Text))
                if(!string.IsNullOrWhiteSpace(Context.Metadata.Version))
                    txtVersion.Text = Context.Metadata.Version;

            if(string.IsNullOrWhiteSpace(txtLanguages.Text))
                if(Context.Metadata.Languages != null)
                    foreach(LanguagesTypeLanguage language in Context.Metadata.Languages)
                    {
                        if(!string.IsNullOrWhiteSpace(txtLanguages.Text)) txtLanguages.Text += ",";
                        txtLanguages.Text                                                   += language;
                    }

            if(string.IsNullOrWhiteSpace(txtArchitecture.Text))
                if(Context.Metadata.Architectures != null)
                    foreach(ArchitecturesTypeArchitecture architecture in Context.Metadata.Architectures)
                    {
                        if(!string.IsNullOrWhiteSpace(txtArchitecture.Text)) txtArchitecture.Text += ",";
                        txtArchitecture.Text                                                      += architecture;
                    }

            if(string.IsNullOrWhiteSpace(txtMachine.Text))
                if(Context.Metadata.Systems != null)
                    foreach(string machine in Context.Metadata.Systems)
                    {
                        if(!string.IsNullOrWhiteSpace(txtMachine.Text)) txtMachine.Text += ",";
                        txtMachine.Text                                                 += machine;
                    }

            btnMetadata.ModifyBg(StateType.Normal, new Color(0, 127, 0));
        }

        _dlgMetadata.Destroy();
    }

    protected void OnBtnRemoveFileClicked(object sender, EventArgs e)
    {
        if(!treeFiles.Selection.GetSelected(out TreeIter fileIter)) return;

        string name = (string)fileView.GetValue(fileIter, 0);
        string filesPath;

        if(!string.IsNullOrEmpty(Context.TmpFolder) && Directory.Exists(Context.TmpFolder))
            filesPath  = Context.TmpFolder;
        else filesPath = Context.Path;

        Context.Hashes.Remove(name);
        Context.Files.Remove(System.IO.Path.Combine(filesPath, name));
        fileView.Remove(ref fileIter);
    }

    protected void OnBtnToggleCrackClicked(object sender, EventArgs e)
    {
        if(!treeFiles.Selection.GetSelected(out TreeIter fileIter)) return;

        string name  = (string)fileView.GetValue(fileIter, 0);
        bool   known = (bool)fileView.GetValue(fileIter,   2);
        string color = (string)fileView.GetValue(fileIter, 3);

        if(!Context.Hashes.TryGetValue(name, out DbOsFile osfile)) return;

        osfile.Crack = !osfile.Crack;
        Context.Hashes.Remove(name);
        Context.Hashes.Add(name, osfile);
        fileView.Remove(ref fileIter);
        fileView.AppendValues(name, osfile.Sha256, known, color, "black", osfile.Crack);
    }

    void treeFilesSelectionChanged(object sender, EventArgs e)
    {
        if(!treeFiles.Selection.GetSelected(out TreeIter fileIter)) return;

        if((bool)fileView.GetValue(fileIter, 5)) btnToggleCrack.Label = "Mark as not crack";
        else btnToggleCrack.Label                                     = "Mark as crack";
    }
}