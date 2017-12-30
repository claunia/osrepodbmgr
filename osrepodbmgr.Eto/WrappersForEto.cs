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

using osrepodbmgr.Core;
using Schemas;

namespace osrepodbmgr.Eto
{
    class DBEntryForEto
    {
        DbEntry _item;

        public DBEntryForEto(DbEntry item)
        {
            _item = item;
        }

        public long id
        {
            get { return _item.Id; }
            set { }
        }
        public string developer
        {
            get { return _item.Developer; }
            set { }
        }
        public string product
        {
            get { return _item.Product; }
            set { }
        }
        public string version
        {
            get { return _item.Version; }
            set { }
        }
        public string languages
        {
            get { return _item.Languages; }
            set { }
        }
        public string architecture
        {
            get { return _item.Architecture; }
            set { }
        }
        public string machine
        {
            get { return _item.Machine; }
            set { }
        }
        public string format
        {
            get { return _item.Format; }
            set { }
        }
        public string description
        {
            get { return _item.Description; }
            set { }
        }
        public bool oem
        {
            get { return _item.Oem; }
            set { }
        }
        public bool upgrade
        {
            get { return _item.Upgrade; }
            set { }
        }
        public bool update
        {
            get { return _item.Update; }
            set { }
        }
        public bool source
        {
            get { return _item.Source; }
            set { }
        }
        public bool files
        {
            get { return _item.Files; }
            set { }
        }
        public bool netinstall
        {
            get { return _item.Netinstall; }
            set { }
        }
        public byte[] xml
        {
            get { return _item.Xml; }
            set { }
        }
        public byte[] json
        {
            get { return _item.Json; }
            set { }
        }
        public string mdid
        {
            get { return _item.Mdid; }
            set { }
        }

        public DbEntry original
        {
            get { return _item; }
            set { }
        }
    }

    class StringEntry
    {
        public string str { get; set; }
    }

    class BarcodeEntry
    {
        public string          code { get; set; }
        public BarcodeTypeType type { get; set; }
    }

    class DiscEntry
    {
        public string          path { get; set; }
        public OpticalDiscType disc { get; set; }
    }

    class DiskEntry
    {
        public string         path { get; set; }
        public BlockMediaType disk { get; set; }
    }
}