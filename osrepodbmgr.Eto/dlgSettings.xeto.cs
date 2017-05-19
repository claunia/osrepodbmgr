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
using Eto.Forms;
using Eto.Serialization.Xaml;
using osrepodbmgr.Core;

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
        GroupBox frmClamd;
        CheckBox chkAntivirus;
        CheckBox chkClamd;
        TextBox txtClamdHost;
        NumericUpDown spClamdPort;
        Button btnClamdTest;
        Label lblClamdVersion;
        CheckBox chkClamdIsLocal;
        GroupBox frmVirusTotal;
        CheckBox chkVirusTotal;
        TextBox txtVirusTotal;
        Button btnVirusTotal;
        Label lblVirusTotal;
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

            spClamdPort.Value = 3310;
            chkAntivirus.Checked = Core.Settings.Current.UseAntivirus;
            frmClamd.Visible = chkAntivirus.Checked.Value;
            if(Core.Settings.Current.UseAntivirus && Core.Settings.Current.UseClamd)
            {
                chkClamd.Checked = Core.Settings.Current.UseClamd;
                txtClamdHost.Text = Core.Settings.Current.ClamdHost;
                spClamdPort.Value = Core.Settings.Current.ClamdPort;
                chkClamdIsLocal.Checked = Core.Settings.Current.ClamdIsLocal;
            }
            if(Core.Settings.Current.UseAntivirus && Core.Settings.Current.UseVirusTotal)
            {
                chkVirusTotal.Checked = true;
                chkVirusTotal.Enabled = true;
                txtVirusTotal.Enabled = true;
                txtVirusTotal.Text = Core.Settings.Current.VirusTotalKey;
                btnVirusTotal.Enabled = true;
            }
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
            if(!chkClamd.Checked.Value || !chkAntivirus.Checked.Value)
            {
                Core.Settings.Current.UseClamd = false;
                Core.Settings.Current.ClamdHost = null;
                Core.Settings.Current.ClamdPort = 3310;
                Core.Settings.Current.ClamdIsLocal = false;
            }
            if(chkVirusTotal.Checked.Value && chkAntivirus.Checked.Value)
            {
                Core.Settings.Current.UseVirusTotal = true;
                Core.Settings.Current.VirusTotalKey = txtVirusTotal.Text;
                Core.Workers.InitVirusTotal(Core.Settings.Current.VirusTotalKey);
            }
            else
            {
                Core.Settings.Current.UseVirusTotal = false;
                Core.Settings.Current.VirusTotalKey = null;
            }
            Settings.SaveSettings();
            Workers.CloseDB();
            Workers.InitDB();
            Context.clamdVersion = null;
            Core.Workers.InitClamd();
            Context.CheckUnar();
            Close();
        }

        protected void OnBtnUnarClicked(object sender, EventArgs e)
        {
            OpenFileDialog dlgFile = new OpenFileDialog();
            dlgFile.Title = "Choose UnArchiver executable";
            dlgFile.MultiSelect = false;
            if(!string.IsNullOrWhiteSpace(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)))
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
            dlgFolder.Directory = Path.GetTempPath();

            if(dlgFolder.ShowDialog(this) == DialogResult.Ok)
                txtTmp.Text = dlgFolder.Directory;
        }

        protected void OnBtnRepositoryClicked(object sender, EventArgs e)
        {
            SelectFolderDialog dlgFolder = new SelectFolderDialog();
            dlgFolder.Title = "Choose repository folder";
            dlgFolder.Directory = Path.GetTempPath();

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

        protected void OnChkAntivirusToggled(object sender, EventArgs e)
        {
            frmClamd.Visible = chkAntivirus.Checked.Value;
            frmVirusTotal.Visible = chkAntivirus.Checked.Value;
        }

        protected void OnChkClamdToggled(object sender, EventArgs e)
        {
            txtClamdHost.Enabled = chkClamd.Checked.Value;
            spClamdPort.Enabled = chkClamd.Checked.Value;
            btnClamdTest.Enabled = chkClamd.Checked.Value;
            lblClamdVersion.Visible = false;
            chkClamdIsLocal.Enabled = chkClamd.Checked.Value;
        }

        protected void OnBtnClamdTestClicked(object sender, EventArgs e)
        {
            lblClamdVersion.Visible = false;

            if(string.IsNullOrEmpty(txtClamdHost.Text))
            {
                MessageBox.Show("clamd host cannot be empty", MessageBoxType.Error);
                return;
            }

            string oldVersion = Context.clamdVersion;
            Context.clamdVersion = null;

            string oldHost = Core.Settings.Current.ClamdHost;
            ushort oldPort = Core.Settings.Current.ClamdPort;
            Core.Settings.Current.ClamdHost = txtClamdHost.Text;
            Core.Settings.Current.ClamdPort = (ushort)spClamdPort.Value;

            Workers.TestClamd();

            Core.Settings.Current.ClamdHost = oldHost;
            Core.Settings.Current.ClamdPort = oldPort;

            if(string.IsNullOrEmpty(Context.clamdVersion))
            {
                MessageBox.Show("Cannot connect to clamd", MessageBoxType.Error);
                return;
            }

            lblClamdVersion.Text = Context.clamdVersion;
            Context.clamdVersion = oldVersion;
            lblClamdVersion.Visible = true;
        }

        protected void OnChkVirusTotalToggled(object sender, EventArgs e)
        {
            txtVirusTotal.Enabled = chkVirusTotal.Checked.Value;
            btnVirusTotal.Enabled = chkVirusTotal.Checked.Value;
            lblVirusTotal.Visible = false;
        }

        protected void OnBtnVirusTotalClicked(object sender, EventArgs e)
        {
            Workers.Failed += VirusTotalTestFailed;
            if(Workers.TestVirusTotal(txtVirusTotal.Text))
            {
                lblVirusTotal.Visible = true;
                lblVirusTotal.Text = "Working!";
            }
        }

        void VirusTotalTestFailed(string text)
        {
            MessageBox.Show(text, MessageBoxType.Error);
        }
    }
}
