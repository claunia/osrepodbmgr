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
        DBEntry _item;
        public DBEntryForEto(DBEntry item)
        {
            _item = item;
        }

        public long id { get { return _item.id; } set { } }
        public string developer { get { return _item.developer; } set { } }
        public string product { get { return _item.product; } set { } }
        public string version { get { return _item.version; } set { } }
        public string languages { get { return _item.languages; } set { } }
        public string architecture { get { return _item.architecture; } set { } }
        public string machine { get { return _item.machine; } set { } }
        public string format { get { return _item.format; } set { } }
        public string description { get { return _item.description; } set { } }
        public bool oem { get { return _item.oem; } set { } }
        public bool upgrade { get { return _item.upgrade; } set { } }
        public bool update { get { return _item.update; } set { } }
        public bool source { get { return _item.source; } set { } }
        public bool files { get { return _item.files; } set { } }
        public bool netinstall { get { return _item.netinstall; } set { } }
        public byte[] xml { get { return _item.xml; } set { } }
        public byte[] json { get { return _item.json; } set { } }
        public string mdid { get { return _item.mdid; } set { } }

        public DBEntry original { get { return _item; } set { } }
    }

    class StringEntry
    {
        public string str { get; set; }
    }

    class BarcodeEntry
    {
        public string code { get; set; }
        public BarcodeTypeType type { get; set; }
    }

    class DiscEntry
    {
        public string path { get; set; }
        public OpticalDiscType disc { get; set; }
    }

    class DiskEntry
    {
        public string path { get; set; }
        public BlockMediaType disk { get; set; }
    }
}
