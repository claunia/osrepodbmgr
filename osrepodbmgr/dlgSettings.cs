﻿//
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
using System.IO;
using System.Threading;
using Gtk;
using osrepodbmgr.Core;

namespace osrepodbmgr
{
    public partial class dlgSettings : Dialog
    {
        string oldUnarPath;

        public dlgSettings()
        {
            Build();
            txtTmp.Text        = Core.Settings.Current.TemporaryFolder;
            txtUnar.Text       = Core.Settings.Current.UnArchiverPath;
            txtDatabase.Text   = Core.Settings.Current.DatabasePath;
            txtRepository.Text = Core.Settings.Current.RepositoryPath;

            if(!string.IsNullOrWhiteSpace(txtUnar.Text)) CheckUnar();

            CellRendererText textCell = new CellRendererText();
            cmbCompAlg.Clear();
            ListStore lstCompAlgs = new ListStore(typeof(string));
            cmbCompAlg.PackStart(textCell, true);
            cmbCompAlg.AddAttribute(textCell, "text", 0);
            cmbCompAlg.Model = lstCompAlgs;

            lstCompAlgs.Clear();
            foreach(AlgoEnum type in Enum.GetValues(typeof(AlgoEnum))) lstCompAlgs.AppendValues(type.ToString());

            cmbCompAlg.Active = 0;
            cmbCompAlg.Model.GetIterFirst(out TreeIter iter);
            do
            {
                if((string)cmbCompAlg.Model.GetValue(iter, 0) !=
                   Core.Settings.Current.CompressionAlgorithm.ToString()) continue;

                cmbCompAlg.SetActiveIter(iter);
                break;
            }
            while(cmbCompAlg.Model.IterNext(ref iter));

            spClamdPort.Value   = 3310;
            chkAntivirus.Active = Core.Settings.Current.UseAntivirus;
            frmClamd.Visible    = chkAntivirus.Active;
            if(Core.Settings.Current.UseAntivirus && Core.Settings.Current.UseClamd)
            {
                chkClamd.Active           = Core.Settings.Current.UseClamd;
                txtClamdHost.Text         = Core.Settings.Current.ClamdHost;
                spClamdPort.Value         = Core.Settings.Current.ClamdPort;
                chkClamdIsLocal.Active    = Core.Settings.Current.ClamdIsLocal;
                chkClamd.Sensitive        = true;
                chkClamdIsLocal.Sensitive = true;
                txtClamdHost.Sensitive    = true;
                spClamdPort.Sensitive     = true;
                btnClamdTest.Sensitive    = true;
            }

            if(!Core.Settings.Current.UseAntivirus || !Core.Settings.Current.UseVirusTotal) return;

            chkVirusTotal.Active    = true;
            chkVirusTotal.Sensitive = true;
            txtVirusTotal.Sensitive = true;
            txtVirusTotal.Text      = Core.Settings.Current.VirusTotalKey;
            btnVirusTotal.Sensitive = true;
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            btnDialog.Click();
        }

        protected void OnBtnApplyClicked(object sender, EventArgs e)
        {
            if(chkAntivirus.Active && chkClamd.Active)
                if(string.IsNullOrEmpty(txtClamdHost.Text))
                {
                    MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                                             "clamd host cannot be empty");
                    dlgMsg.Run();
                    dlgMsg.Destroy();
                    return;
                }

            // TODO: Check sanity
            Core.Settings.Current.TemporaryFolder      = txtTmp.Text;
            Core.Settings.Current.UnArchiverPath       = txtUnar.Text;
            Core.Settings.Current.DatabasePath         = txtDatabase.Text;
            Core.Settings.Current.RepositoryPath       = txtRepository.Text;
            Core.Settings.Current.UseAntivirus         = chkAntivirus.Active;
            Core.Settings.Current.UseClamd             = chkClamd.Active;
            Core.Settings.Current.ClamdHost            = txtClamdHost.Text;
            Core.Settings.Current.ClamdPort            = (ushort)spClamdPort.Value;
            Core.Settings.Current.ClamdIsLocal         = chkClamdIsLocal.Active;
            Core.Settings.Current.CompressionAlgorithm = (AlgoEnum)Enum.Parse(typeof(AlgoEnum), cmbCompAlg.ActiveText);
            if(!chkClamd.Active || !chkAntivirus.Active)
            {
                Core.Settings.Current.UseClamd     = false;
                Core.Settings.Current.ClamdHost    = null;
                Core.Settings.Current.ClamdPort    = 3310;
                Core.Settings.Current.ClamdIsLocal = false;
            }

            if(chkVirusTotal.Active && chkAntivirus.Active)
            {
                Core.Settings.Current.UseVirusTotal = true;
                Core.Settings.Current.VirusTotalKey = txtVirusTotal.Text;
                Workers.InitVirusTotal(Core.Settings.Current.VirusTotalKey);
            }
            else
            {
                Core.Settings.Current.UseVirusTotal = false;
                Core.Settings.Current.VirusTotalKey = null;
            }

            Core.Settings.SaveSettings();
            Workers.CloseDB();
            Workers.InitDB();
            Context.ClamdVersion = null;
            Workers.InitClamd();
            Context.CheckUnar();
            btnDialog.Click();
        }

        protected void OnBtnUnarClicked(object sender, EventArgs e)
        {
            FileChooserDialog dlgFile =
                new FileChooserDialog("Choose UnArchiver executable", this, FileChooserAction.Open, "Cancel",
                                      ResponseType.Cancel, "Choose", ResponseType.Accept) {SelectMultiple = false};
            dlgFile.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            if(dlgFile.Run() == (int)ResponseType.Accept)
            {
                txtUnar.Text           = dlgFile.Filename;
                lblUnarVersion.Visible = false;
                CheckUnar();
            }

            dlgFile.Destroy();
        }

        protected void OnBtnTmpClicked(object sender, EventArgs e)
        {
            FileChooserDialog dlgFolder =
                new FileChooserDialog("Choose temporary folder", this, FileChooserAction.SelectFolder, "Cancel",
                                      ResponseType.Cancel, "Choose", ResponseType.Accept) {SelectMultiple = false};
            dlgFolder.SetCurrentFolder(System.IO.Path.GetTempPath());

            if(dlgFolder.Run() == (int)ResponseType.Accept) txtTmp.Text = dlgFolder.Filename;

            dlgFolder.Destroy();
        }

        protected void OnBtnRepositoryClicked(object sender, EventArgs e)
        {
            FileChooserDialog dlgFolder =
                new FileChooserDialog("Choose repository folder", this, FileChooserAction.SelectFolder, "Cancel",
                                      ResponseType.Cancel, "Choose", ResponseType.Accept) {SelectMultiple = false};
            dlgFolder.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            if(dlgFolder.Run() == (int)ResponseType.Accept) txtRepository.Text = dlgFolder.Filename;

            dlgFolder.Destroy();
        }

        protected void OnBtnDatabaseClicked(object sender, EventArgs e)
        {
            FileChooserDialog dlgFile =
                new FileChooserDialog("Choose database to open/create", this, FileChooserAction.Save, "Cancel",
                                      ResponseType.Cancel, "Choose", ResponseType.Accept) {SelectMultiple = false};
            dlgFile.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            dlgFile.SetFilename("Core.db");

            if(dlgFile.Run() == (int)ResponseType.Accept)
            {
                if(File.Exists(dlgFile.Filename))
                {
                    DbCore dbCore = new SQLite();
                    bool   notDb  = false;

                    try { notDb   |= !dbCore.OpenDb(dlgFile.Filename, null, null, null); }
                    catch { notDb =  true; }

                    if(notDb)
                    {
                        MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error,
                                                                 ButtonsType.Ok,
                                                                 "Cannot open specified file as a database, please choose another.");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        dlgFile.Destroy();
                        return;
                    }

                    dbCore.CloseDb();
                }
                else
                {
                    DbCore dbCore = new SQLite();
                    bool   notDb  = false;

                    try { notDb   |= !dbCore.CreateDb(dlgFile.Filename, null, null, null); }
                    catch { notDb =  true; }

                    if(notDb)
                    {
                        MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error,
                                                                 ButtonsType.Ok,
                                                                 "Cannot create a database in the specified file as a database.");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        dlgFile.Destroy();
                        return;
                    }

                    dbCore.CloseDb();
                }

                txtDatabase.Text = dlgFile.Filename;
            }

            dlgFile.Destroy();
        }

        void CheckUnar()
        {
            Workers.FinishedWithText += CheckUnarFinished;
            Workers.Failed           += CheckUnarFailed;

            oldUnarPath                          = Core.Settings.Current.UnArchiverPath;
            Core.Settings.Current.UnArchiverPath = txtUnar.Text;
            Thread thdCheckUnar                  = new Thread(Workers.CheckUnar);
            thdCheckUnar.Start();
        }

        void CheckUnarFinished(string text)
        {
            Application.Invoke(delegate
            {
                Workers.FinishedWithText -= CheckUnarFinished;
                Workers.Failed           -= CheckUnarFailed;

                lblUnarVersion.Text                  = text;
                lblUnarVersion.Visible               = true;
                Core.Settings.Current.UnArchiverPath = oldUnarPath;
            });
        }

        void CheckUnarFailed(string text)
        {
            Application.Invoke(delegate
            {
                Workers.FinishedWithText -= CheckUnarFinished;
                Workers.Failed           -= CheckUnarFailed;

                txtUnar.Text                         = string.IsNullOrWhiteSpace(oldUnarPath) ? "" : oldUnarPath;
                Core.Settings.Current.UnArchiverPath = oldUnarPath;
                MessageDialog dlgMsg                 =
                    new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            });
        }

        protected void OnChkAntivirusToggled(object sender, EventArgs e)
        {
            frmClamd.Visible      = chkAntivirus.Active;
            frmVirusTotal.Visible = chkAntivirus.Active;
        }

        protected void OnChkClamdToggled(object sender, EventArgs e)
        {
            txtClamdHost.Sensitive    = chkClamd.Active;
            spClamdPort.Sensitive     = chkClamd.Active;
            btnClamdTest.Sensitive    = chkClamd.Active;
            lblClamdVersion.Visible   = false;
            chkClamdIsLocal.Sensitive = chkClamd.Active;
        }

        protected void OnBtnClamdTestClicked(object sender, EventArgs e)
        {
            lblClamdVersion.Visible = false;

            if(string.IsNullOrEmpty(txtClamdHost.Text))
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                                         "clamd host cannot be empty");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            string oldVersion    = Context.ClamdVersion;
            Context.ClamdVersion = null;

            string oldHost                  = Core.Settings.Current.ClamdHost;
            ushort oldPort                  = Core.Settings.Current.ClamdPort;
            Core.Settings.Current.ClamdHost = txtClamdHost.Text;
            Core.Settings.Current.ClamdPort = (ushort)spClamdPort.Value;

            Workers.TestClamd();

            Core.Settings.Current.ClamdHost = oldHost;
            Core.Settings.Current.ClamdPort = oldPort;

            if(string.IsNullOrEmpty(Context.ClamdVersion))
            {
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                                         "Cannot connect to clamd");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            lblClamdVersion.Text    = Context.ClamdVersion;
            Context.ClamdVersion    = oldVersion;
            lblClamdVersion.Visible = true;
        }

        protected void OnChkVirusTotalToggled(object sender, EventArgs e)
        {
            txtVirusTotal.Sensitive = chkVirusTotal.Active;
            btnVirusTotal.Sensitive = chkVirusTotal.Active;
            lblVirusTotal.Visible   = false;
        }

        protected void OnBtnVirusTotalClicked(object sender, EventArgs e)
        {
            Workers.Failed += VirusTotalTestFailed;
            if(!Workers.TestVirusTotal(txtVirusTotal.Text)) return;

            lblVirusTotal.Visible = true;
            lblVirusTotal.Text    = "Working!";
        }

        void VirusTotalTestFailed(string text)
        {
            MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
            dlgMsg.Run();
            dlgMsg.Destroy();
        }
    }
}