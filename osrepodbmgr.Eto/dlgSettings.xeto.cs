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
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using osrepodbmgr.Core;
using System.IO;
using System.Threading;

namespace osrepodbmgr.Eto
{
    public class dlgSettings : Dialog
    {
        #region XAML UI elements
#pragma warning disable 0649
        TextBox txtTmp;
        TextBox txtUnar;
        TextBox txtDatabase;
        TextBox txtRepository;
        Label lblUnarVersion;
        EnumDropDown<AlgoEnum> cmbCompAlg;
        StackLayout StackLayoutForAlgoEnum;
#pragma warning restore 0649
        #endregion XAML UI elements

        string oldUnarPath;

        public dlgSettings()
        {
            XamlReader.Load(this);
            txtTmp.Text = Settings.Current.TemporaryFolder;
            txtUnar.Text = Settings.Current.UnArchiverPath;
            txtDatabase.Text = Settings.Current.DatabasePath;
            txtRepository.Text = Settings.Current.RepositoryPath;

            if(!string.IsNullOrWhiteSpace(txtUnar.Text))
                CheckUnar();

            cmbCompAlg = new EnumDropDown<AlgoEnum>();
            StackLayoutForAlgoEnum.Items.Add(new StackLayoutItem(cmbCompAlg, HorizontalAlignment.Stretch, true));
            cmbCompAlg.SelectedValue = Settings.Current.CompressionAlgorithm;
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            Close();
        }

        protected void OnBtnApplyClicked(object sender, EventArgs e)
        {
            // TODO: Check sanity
            Settings.Current.TemporaryFolder = txtTmp.Text;
            Settings.Current.UnArchiverPath = txtUnar.Text;
            Settings.Current.DatabasePath = txtDatabase.Text;
            Settings.Current.RepositoryPath = txtRepository.Text;
            Settings.Current.CompressionAlgorithm = cmbCompAlg.SelectedValue;
            Settings.SaveSettings();
            Workers.CloseDB();
            Workers.InitDB();
            Context.CheckUnar();
            Close();
        }

        protected void OnBtnUnarClicked(object sender, EventArgs e)
        {
            OpenFileDialog dlgFile = new OpenFileDialog();
            dlgFile.Title = "Choose UnArchiver executable";
            dlgFile.MultiSelect = false;
            dlgFile.Directory = new Uri(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            if(dlgFile.ShowDialog(this) == DialogResult.Ok)
            {
                txtUnar.Text = dlgFile.FileName;
                lblUnarVersion.Visible = false;
                CheckUnar();
            }
        }

        protected void OnBtnTmpClicked(object sender, EventArgs e)
        {
            SelectFolderDialog dlgFolder = new SelectFolderDialog();
            dlgFolder.Title = "Choose temporary folder";
            dlgFolder.Directory = System.IO.Path.GetTempPath();

            if(dlgFolder.ShowDialog(this) == DialogResult.Ok)
                txtTmp.Text = dlgFolder.Directory;
        }

        protected void OnBtnRepositoryClicked(object sender, EventArgs e)
        {
            SelectFolderDialog dlgFolder = new SelectFolderDialog();
            dlgFolder.Title = "Choose repository folder";
            dlgFolder.Directory = System.IO.Path.GetTempPath();

            if(dlgFolder.ShowDialog(this) == DialogResult.Ok)
                txtRepository.Text = dlgFolder.Directory;
        }

        protected void OnBtnDatabaseClicked(object sender, EventArgs e)
        {
            SaveFileDialog dlgFile = new SaveFileDialog();
            dlgFile.Title = "Choose database to open/create";
            dlgFile.Directory = new Uri(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            dlgFile.CheckFileExists = false;
            dlgFile.FileName = "osrepodbmgr.db";

            if(dlgFile.ShowDialog(this) == DialogResult.Ok)
            {
                if(File.Exists(dlgFile.FileName))
                {
                    DBCore _dbCore = new SQLite();
                    bool notDb = false;

                    try
                    {
                        notDb |= !_dbCore.OpenDB(dlgFile.FileName, null, null, null);
                    }
                    catch
                    {
                        notDb = true;
                    }

                    if(notDb)
                    {
                        MessageBox.Show("Cannot open specified file as a database, please choose another.", MessageBoxType.Error);
                        return;
                    }
                    _dbCore.CloseDB();
                }
                else
                {
                    DBCore _dbCore = new SQLite();
                    bool notDb = false;

                    try
                    {
                        notDb |= !_dbCore.CreateDB(dlgFile.FileName, null, null, null);
                    }
                    catch
                    {
                        notDb = true;
                    }

                    if(notDb)
                    {
                        MessageBox.Show("Cannot create a database in the specified file as a database.", MessageBoxType.Error);
                        return;
                    }
                    _dbCore.CloseDB();
                }

                txtDatabase.Text = dlgFile.FileName;
            }
        }

        void CheckUnar()
        {
            Workers.FinishedWithText += CheckUnarFinished;
            Workers.Failed += CheckUnarFailed;

            oldUnarPath = Settings.Current.UnArchiverPath;
            Settings.Current.UnArchiverPath = txtUnar.Text;
            Thread thdCheckUnar = new Thread(Workers.CheckUnar);
            thdCheckUnar.Start();
        }

        void CheckUnarFinished(string text)
        {
            Application.Instance.Invoke(delegate
            {
                Workers.FinishedWithText -= CheckUnarFinished;
                Workers.Failed -= CheckUnarFailed;

                lblUnarVersion.Text = text;
                lblUnarVersion.Visible = true;
                Settings.Current.UnArchiverPath = oldUnarPath;
            });
        }

        void CheckUnarFailed(string text)
        {
            Application.Instance.Invoke(delegate
            {
                Workers.FinishedWithText -= CheckUnarFinished;
                Workers.Failed -= CheckUnarFailed;

                if(string.IsNullOrWhiteSpace(oldUnarPath))
                    txtUnar.Text = "";
                else
                    txtUnar.Text = oldUnarPath;
                Settings.Current.UnArchiverPath = oldUnarPath;
                MessageBox.Show(text, MessageBoxType.Error);
            });
        }
    }
}
