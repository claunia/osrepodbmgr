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
