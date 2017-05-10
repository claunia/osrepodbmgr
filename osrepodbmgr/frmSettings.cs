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
using System.IO;
using System.Threading;
using Gtk;
using osrepodbmgr.Core;

namespace osrepodbmgr
{
    public partial class frmSettings : Window
    {
        string oldUnarPath;

        public frmSettings() :
                base(WindowType.Toplevel)
        {
            Build();
            txtTmp.Text = Core.Settings.Current.TemporaryFolder;
            txtUnar.Text = Core.Settings.Current.UnArchiverPath;
            txtDatabase.Text = Core.Settings.Current.DatabasePath;
            txtRepository.Text = Core.Settings.Current.RepositoryPath;

            if(!string.IsNullOrWhiteSpace(txtUnar.Text))
                CheckUnar();
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            Destroy();
        }

        protected void OnBtnApplyClicked(object sender, EventArgs e)
        {
            // TODO: Check sanity
            Core.Settings.Current.TemporaryFolder = txtTmp.Text;
            Core.Settings.Current.UnArchiverPath = txtUnar.Text;
            Core.Settings.Current.DatabasePath = txtDatabase.Text;
            Core.Settings.Current.RepositoryPath = txtRepository.Text;
            Core.Settings.SaveSettings();
            Core.Core.CloseDB();
            Core.Core.InitDB();
            Context.CheckUnar();
            Destroy();
        }

        protected void OnBtnUnarClicked(object sender, EventArgs e)
        {
            FileChooserDialog dlgFile = new FileChooserDialog("Choose UnArchiver executable", this, FileChooserAction.Open,
                                                     "Cancel", ResponseType.Cancel, "Choose", ResponseType.Accept);
            dlgFile.SelectMultiple = false;
            dlgFile.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            if(dlgFile.Run() == (int)ResponseType.Accept)
            {
                txtUnar.Text = dlgFile.Filename;
                lblUnarVersion.Visible = false;
                CheckUnar();
            }

            dlgFile.Destroy();
        }

        protected void OnBtnTmpClicked(object sender, EventArgs e)
        {
            FileChooserDialog dlgFolder = new FileChooserDialog("Choose temporary folder", this, FileChooserAction.SelectFolder,
                                                     "Cancel", ResponseType.Cancel, "Choose", ResponseType.Accept);
            dlgFolder.SelectMultiple = false;
            dlgFolder.SetCurrentFolder(System.IO.Path.GetTempPath());

            if(dlgFolder.Run() == (int)ResponseType.Accept)
            {
                txtTmp.Text = dlgFolder.Filename;
            }

            dlgFolder.Destroy();
        }

        protected void OnBtnRepositoryClicked(object sender, EventArgs e)
        {
            FileChooserDialog dlgFolder = new FileChooserDialog("Choose repository folder", this, FileChooserAction.SelectFolder,
                                                     "Cancel", ResponseType.Cancel, "Choose", ResponseType.Accept);
            dlgFolder.SelectMultiple = false;
            dlgFolder.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            if(dlgFolder.Run() == (int)ResponseType.Accept)
            {
                txtRepository.Text = dlgFolder.Filename;
            }

            dlgFolder.Destroy();
        }

        protected void OnBtnDatabaseClicked(object sender, EventArgs e)
        {
            FileChooserDialog dlgFile = new FileChooserDialog("Choose database to open/create", this, FileChooserAction.Save,
                                                     "Cancel", ResponseType.Cancel, "Choose", ResponseType.Accept);
            dlgFile.SelectMultiple = false;
            dlgFile.SetCurrentFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            dlgFile.SetFilename("Core.db");

            if(dlgFile.Run() == (int)ResponseType.Accept)
            {
                if(File.Exists(dlgFile.Filename))
                {
                    DBCore _dbCore = new SQLite();
                    bool notDb = false;

                    try
                    {
                        notDb |= !_dbCore.OpenDB(dlgFile.Filename, null, null, null);
                    }
                    catch
                    {
                        notDb = true;
                    }

                    if(notDb)
                    {
                        MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Cannot open specified file as a database, please choose another.");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        dlgFile.Destroy();
                        return;
                    }
                    _dbCore.CloseDB();
                }
                else {
                    DBCore _dbCore = new SQLite();
                    bool notDb = false;

                    try
                    {
                        notDb |= !_dbCore.CreateDB(dlgFile.Filename, null, null, null);
                    }
                    catch
                    {
                        notDb = true;
                    }

                    if(notDb)
                    {
                        MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Cannot create a database in the specified file as a database.");
                        dlgMsg.Run();
                        dlgMsg.Destroy();
                        dlgFile.Destroy();
                        return;
                    }
                    _dbCore.CloseDB();
                }

                txtDatabase.Text = dlgFile.Filename;
            }

            dlgFile.Destroy();
        }

        void CheckUnar()
        {
            Core.Core.FinishedWithText += CheckUnarFinished;
            Core.Core.Failed += CheckUnarFailed;

            oldUnarPath = Core.Settings.Current.UnArchiverPath;
            Core.Settings.Current.UnArchiverPath = txtUnar.Text;
            Thread thdCheckUnar = new Thread(Core.Core.CheckUnar);
            thdCheckUnar.Start();
        }

        void CheckUnarFinished(string text)
        {
            Application.Invoke(delegate
            {
                Core.Core.FinishedWithText -= CheckUnarFinished;
                Core.Core.Failed -= CheckUnarFailed;

                lblUnarVersion.Text = text;
                lblUnarVersion.Visible = true;
                Core.Settings.Current.UnArchiverPath = oldUnarPath;
            });
        }

        void CheckUnarFailed(string text)
        {
            Application.Invoke(delegate
            {
                Core.Core.FinishedWithText -= CheckUnarFinished;
                Core.Core.Failed -= CheckUnarFailed;

                if(string.IsNullOrWhiteSpace(oldUnarPath))
                    txtUnar.Text = "";
                else
                    txtUnar.Text = oldUnarPath;
                Core.Settings.Current.UnArchiverPath = oldUnarPath;
                MessageDialog dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, text);
                dlgMsg.Run();
                dlgMsg.Destroy();
            });
        }
    }
}
