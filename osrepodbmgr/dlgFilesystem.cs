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
using Gtk;
using Schemas;

namespace osrepodbmgr
{
    public partial class dlgFilesystem : Dialog
    {
        public FileSystemType Metadata;

        public dlgFilesystem()
        {
            Build();
        }

        public void FillFields()
        {
            if(Metadata == null)
                return;

            if(Metadata.Type != null)
                txtType.Text = Metadata.Type;
            if(Metadata.CreationDateSpecified)
            {
                chkCreationDate.Active = true;
                cldCreationDate.Sensitive = true;
                cldCreationDate.Date = Metadata.CreationDate;
            }
            if(Metadata.ModificationDateSpecified)
            {
                chkModificationDate.Active = true;
                cldModificationDate.Sensitive = true;
                cldModificationDate.Date = Metadata.ModificationDate;
            }
            if(Metadata.BackupDateSpecified)
            {
                chkBackupDate.Active = true;
                cldBackupDate.Sensitive = true;
                cldBackupDate.Date = Metadata.BackupDate;
            }
            spClusterSize.Value = Metadata.ClusterSize;
            txtClusters.Text = Metadata.Clusters.ToString();
            if(Metadata.FilesSpecified)
                txtFiles.Text = Metadata.Files.ToString();
            chkBootable.Active = Metadata.Bootable;
            if(Metadata.VolumeSerial != null)
                txtSerial.Text = Metadata.VolumeSerial;
            if(Metadata.VolumeName != null)
                txtLabel.Text = Metadata.VolumeName;
            if(Metadata.FreeClustersSpecified)
                txtFreeClusters.Text = Metadata.FreeClusters.ToString();
            chkDirty.Active = Metadata.Dirty;
            if(Metadata.ExpirationDateSpecified)
            {
                chkExpirationDate.Active = true;
                cldExpirationDate.Sensitive = true;
                cldExpirationDate.Date = Metadata.ExpirationDate;
            }
            if(Metadata.EffectiveDateSpecified)
            {
                chkEffectiveDate.Active = true;
                cldEffectiveDate.Sensitive = true;
                cldEffectiveDate.Date = Metadata.EffectiveDate;
            }
            if(Metadata.SystemIdentifier != null)
                txtSysId.Text = Metadata.SystemIdentifier;
            if(Metadata.VolumeSetIdentifier != null)
                txtVolId.Text = Metadata.VolumeSetIdentifier;
            if(Metadata.PublisherIdentifier != null)
                txtPubId.Text = Metadata.PublisherIdentifier;
            if(Metadata.DataPreparerIdentifier != null)
                txtDataId.Text = Metadata.DataPreparerIdentifier;
            if(Metadata.ApplicationIdentifier != null)
                txtAppId.Text = Metadata.ApplicationIdentifier;
        }

        protected void OnChkCreationDateToggled(object sender, EventArgs e)
        {
            cldCreationDate.Sensitive = chkCreationDate.Active;
        }

        protected void OnChkModificationDateToggled(object sender, EventArgs e)
        {
            cldModificationDate.Sensitive = chkModificationDate.Active;
        }

        protected void OnChkEffectiveDateToggled(object sender, EventArgs e)
        {
            cldEffectiveDate.Sensitive = chkEffectiveDate.Active;
        }

        protected void OnChkExpirationDateToggled(object sender, EventArgs e)
        {
            cldExpirationDate.Sensitive = chkExpirationDate.Active;
        }

        protected void OnChkBackupDateToggled(object sender, EventArgs e)
        {
            cldBackupDate.Sensitive = chkBackupDate.Active;
        }

        protected void OnButtonOkClicked(object sender, EventArgs e)
        {
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            buttonCancel.Click();
        }

        protected void OnBtnSaveClicked(object sender, EventArgs e)
        {
            MessageDialog dlgMsg;
            long temp;

            if(string.IsNullOrWhiteSpace(txtType.Text))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Filesystem type cannot be empty");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(spClusterSize.ValueAsInt < 1)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Clusters must be bigger than 0 bytes");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(!long.TryParse(txtClusters.Text, out temp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Clusters must be a number");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(temp < 1)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Filesystem must have more than 0 clusters");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(!string.IsNullOrWhiteSpace(txtFiles.Text) && !long.TryParse(txtFiles.Text, out temp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Files must be a number, or empty for unknown");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(!string.IsNullOrWhiteSpace(txtFiles.Text) && temp < 0)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Files must be positive");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(!string.IsNullOrWhiteSpace(txtFreeClusters.Text) && !long.TryParse(txtFreeClusters.Text, out temp))
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Free clusters must be a number or empty for unknown");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            if(!string.IsNullOrWhiteSpace(txtFreeClusters.Text) && temp < 0)
            {
                dlgMsg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Free clusters must be positive");
                dlgMsg.Run();
                dlgMsg.Destroy();
                return;
            }

            Metadata = new FileSystemType();
            Metadata.Type = txtType.Text;
            if(chkCreationDate.Active)
            {
                Metadata.CreationDateSpecified = true;
                Metadata.CreationDate = cldCreationDate.Date;
            }
            if(chkModificationDate.Active)
            {
                Metadata.ModificationDateSpecified = true;
                Metadata.ModificationDate = cldModificationDate.Date;
            }
            if(chkBackupDate.Active)
            {
                Metadata.BackupDateSpecified = true;
                Metadata.BackupDate = cldBackupDate.Date;
            }
            Metadata.ClusterSize = (int)spClusterSize.Value;
            Metadata.Clusters = long.Parse(txtClusters.Text);
            if(!string.IsNullOrWhiteSpace(txtFiles.Text))
            {
                Metadata.FilesSpecified = true;
                Metadata.Files = long.Parse(txtFiles.Text);
            }
            Metadata.Bootable = chkBootable.Active;
            if(!string.IsNullOrWhiteSpace(txtSerial.Text))
                Metadata.VolumeSerial = txtSerial.Text;
            if(!string.IsNullOrWhiteSpace(txtLabel.Text))
                Metadata.VolumeName = txtLabel.Text;
            if(!string.IsNullOrWhiteSpace(txtFreeClusters.Text))
            {
                Metadata.FreeClustersSpecified = true;
                Metadata.FreeClusters = long.Parse(txtFreeClusters.Text);
            }
            Metadata.Dirty = chkDirty.Active;
            if(chkExpirationDate.Active)
            {
                Metadata.ExpirationDateSpecified = true;
                Metadata.ExpirationDate = cldExpirationDate.Date;
            }
            if(chkEffectiveDate.Active)
            {
                Metadata.EffectiveDateSpecified = true;
                Metadata.EffectiveDate = cldEffectiveDate.Date;
            }
            if(!string.IsNullOrWhiteSpace(txtSysId.Text))
                Metadata.SystemIdentifier = txtSysId.Text;
            if(!string.IsNullOrWhiteSpace(txtVolId.Text))
                Metadata.VolumeSetIdentifier = txtVolId.Text;
            if(!string.IsNullOrWhiteSpace(txtPubId.Text))
                Metadata.PublisherIdentifier = txtPubId.Text;
            if(!string.IsNullOrWhiteSpace(txtDataId.Text))
                Metadata.DataPreparerIdentifier = txtDataId.Text;
            if(!string.IsNullOrWhiteSpace(txtAppId.Text))
                Metadata.ApplicationIdentifier = txtAppId.Text;

            buttonOk.Click();
        }
    }
}
