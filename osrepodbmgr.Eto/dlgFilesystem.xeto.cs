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
using Eto.Forms;
using Eto.Serialization.Xaml;
using Schemas;

namespace osrepodbmgr.Eto
{
    public class dlgFilesystem : Dialog
    {
        public FileSystemType Metadata;
        public bool           Modified;

        public dlgFilesystem()
        {
            XamlReader.Load(this);
            Modified = false;
        }

        public void FillFields()
        {
            if(Metadata == null) return;

            if(Metadata.Type != null) txtType.Text = Metadata.Type;
            if(Metadata.CreationDateSpecified)
            {
                chkCreationDate.Checked = true;
                cldCreationDate.Enabled = true;
                cldCreationDate.Value   = Metadata.CreationDate;
            }

            if(Metadata.ModificationDateSpecified)
            {
                chkModificationDate.Checked = true;
                cldModificationDate.Enabled = true;
                cldModificationDate.Value   = Metadata.ModificationDate;
            }

            if(Metadata.BackupDateSpecified)
            {
                chkBackupDate.Checked = true;
                cldBackupDate.Enabled = true;
                cldBackupDate.Value   = Metadata.BackupDate;
            }

            spClusterSize.Value                                     = Metadata.ClusterSize;
            txtClusters.Text                                        = Metadata.Clusters.ToString();
            if(Metadata.FilesSpecified) txtFiles.Text               = Metadata.Files.ToString();
            chkBootable.Checked                                     = Metadata.Bootable;
            if(Metadata.VolumeSerial != null) txtSerial.Text        = Metadata.VolumeSerial;
            if(Metadata.VolumeName   != null) txtLabel.Text         = Metadata.VolumeName;
            if(Metadata.FreeClustersSpecified) txtFreeClusters.Text = Metadata.FreeClusters.ToString();
            chkDirty.Checked                                        = Metadata.Dirty;
            if(Metadata.ExpirationDateSpecified)
            {
                chkExpirationDate.Checked = true;
                cldExpirationDate.Enabled = true;
                cldExpirationDate.Value   = Metadata.ExpirationDate;
            }

            if(Metadata.EffectiveDateSpecified)
            {
                chkEffectiveDate.Checked = true;
                cldEffectiveDate.Enabled = true;
                cldEffectiveDate.Value   = Metadata.EffectiveDate;
            }

            if(Metadata.SystemIdentifier       != null) txtSysId.Text  = Metadata.SystemIdentifier;
            if(Metadata.VolumeSetIdentifier    != null) txtVolId.Text  = Metadata.VolumeSetIdentifier;
            if(Metadata.PublisherIdentifier    != null) txtPubId.Text  = Metadata.PublisherIdentifier;
            if(Metadata.DataPreparerIdentifier != null) txtDataId.Text = Metadata.DataPreparerIdentifier;
            if(Metadata.ApplicationIdentifier  != null) txtAppId.Text  = Metadata.ApplicationIdentifier;
        }

        protected void OnChkCreationDateToggled(object sender, EventArgs e)
        {
            cldCreationDate.Enabled = chkCreationDate.Checked.Value;
        }

        protected void OnChkModificationDateToggled(object sender, EventArgs e)
        {
            cldModificationDate.Enabled = chkModificationDate.Checked.Value;
        }

        protected void OnChkEffectiveDateToggled(object sender, EventArgs e)
        {
            cldEffectiveDate.Enabled = chkEffectiveDate.Checked.Value;
        }

        protected void OnChkExpirationDateToggled(object sender, EventArgs e)
        {
            cldExpirationDate.Enabled = chkExpirationDate.Checked.Value;
        }

        protected void OnChkBackupDateToggled(object sender, EventArgs e)
        {
            cldBackupDate.Enabled = chkBackupDate.Checked.Value;
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            Modified = false;
            Close();
        }

        protected void OnBtnSaveClicked(object sender, EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(txtType.Text))
                MessageBox.Show("Filesystem type cannot be empty", MessageBoxType.Error);

            if(spClusterSize.Value < 1) MessageBox.Show("Clusters must be bigger than 0 bytes", MessageBoxType.Error);

            if(!long.TryParse(txtClusters.Text, out long temp))
                MessageBox.Show("Clusters must be a number", MessageBoxType.Error);

            if(temp < 1) MessageBox.Show("Filesystem must have more than 0 clusters", MessageBoxType.Error);

            if(!string.IsNullOrWhiteSpace(txtFiles.Text) && !long.TryParse(txtFiles.Text, out temp))
                MessageBox.Show("Files must be a number, or empty for unknown", MessageBoxType.Error);

            if(!string.IsNullOrWhiteSpace(txtFiles.Text) && temp < 0)
                MessageBox.Show("Files must be positive", MessageBoxType.Error);

            if(!string.IsNullOrWhiteSpace(txtFreeClusters.Text) && !long.TryParse(txtFreeClusters.Text, out temp))
                MessageBox.Show("Free clusters must be a number or empty for unknown", MessageBoxType.Error);

            if(!string.IsNullOrWhiteSpace(txtFreeClusters.Text) && temp < 0)
                MessageBox.Show("Free clusters must be positive", MessageBoxType.Error);

            Metadata = new FileSystemType {Type = txtType.Text};
            if(chkCreationDate.Checked.Value)
            {
                Metadata.CreationDateSpecified = true;
                Metadata.CreationDate          = cldCreationDate.Value.Value;
            }

            if(chkModificationDate.Checked.Value)
            {
                Metadata.ModificationDateSpecified = true;
                Metadata.ModificationDate          = cldModificationDate.Value.Value;
            }

            if(chkBackupDate.Checked.Value)
            {
                Metadata.BackupDateSpecified = true;
                Metadata.BackupDate          = cldBackupDate.Value.Value;
            }

            Metadata.ClusterSize = (int)spClusterSize.Value;
            Metadata.Clusters    = long.Parse(txtClusters.Text);
            if(!string.IsNullOrWhiteSpace(txtFiles.Text))
            {
                Metadata.FilesSpecified = true;
                Metadata.Files          = long.Parse(txtFiles.Text);
            }

            Metadata.Bootable                                                    = chkBootable.Checked.Value;
            if(!string.IsNullOrWhiteSpace(txtSerial.Text)) Metadata.VolumeSerial = txtSerial.Text;
            if(!string.IsNullOrWhiteSpace(txtLabel.Text)) Metadata.VolumeName    = txtLabel.Text;
            if(!string.IsNullOrWhiteSpace(txtFreeClusters.Text))
            {
                Metadata.FreeClustersSpecified = true;
                Metadata.FreeClusters          = long.Parse(txtFreeClusters.Text);
            }

            Metadata.Dirty = chkDirty.Checked.Value;
            if(chkExpirationDate.Checked.Value)
            {
                Metadata.ExpirationDateSpecified = true;
                Metadata.ExpirationDate          = cldExpirationDate.Value.Value;
            }

            if(chkEffectiveDate.Checked.Value)
            {
                Metadata.EffectiveDateSpecified = true;
                Metadata.EffectiveDate          = cldEffectiveDate.Value.Value;
            }

            if(!string.IsNullOrWhiteSpace(txtSysId.Text)) Metadata.SystemIdentifier        = txtSysId.Text;
            if(!string.IsNullOrWhiteSpace(txtVolId.Text)) Metadata.VolumeSetIdentifier     = txtVolId.Text;
            if(!string.IsNullOrWhiteSpace(txtPubId.Text)) Metadata.PublisherIdentifier     = txtPubId.Text;
            if(!string.IsNullOrWhiteSpace(txtDataId.Text)) Metadata.DataPreparerIdentifier = txtDataId.Text;
            if(!string.IsNullOrWhiteSpace(txtAppId.Text)) Metadata.ApplicationIdentifier   = txtAppId.Text;

            Modified = true;
            Close();
        }

        #region XAML UI elements
        #pragma warning disable 0649
        TextBox        txtType;
        TextBox        txtFiles;
        CheckBox       chkBootable;
        CheckBox       chkDirty;
        NumericUpDown  spClusterSize;
        TextBox        txtClusters;
        TextBox        txtFreeClusters;
        CheckBox       chkCreationDate;
        DateTimePicker cldCreationDate;
        CheckBox       chkModificationDate;
        DateTimePicker cldModificationDate;
        CheckBox       chkEffectiveDate;
        DateTimePicker cldEffectiveDate;
        CheckBox       chkExpirationDate;
        DateTimePicker cldExpirationDate;
        CheckBox       chkBackupDate;
        DateTimePicker cldBackupDate;
        TextBox        txtLabel;
        TextBox        txtSerial;
        TextBox        txtSysId;
        TextBox        txtVolId;
        TextBox        txtPubId;
        TextBox        txtDataId;
        TextBox        txtAppId;
        #pragma warning restore 0649
        #endregion XAML UI elements
    }
}