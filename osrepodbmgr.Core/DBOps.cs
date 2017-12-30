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
using System.Data;
using System.IO;

namespace osrepodbmgr.Core
{
    public struct DbEntry
    {
        public long   Id;
        public string Developer;
        public string Product;
        public string Version;
        public string Languages;
        public string Architecture;
        public string Machine;
        public string Format;
        public string Description;
        public bool   Oem;
        public bool   Upgrade;
        public bool   Update;
        public bool   Source;
        public bool   Files;
        public bool   Netinstall;
        public byte[] Xml;
        public byte[] Json;
        public string Mdid;
    }

    public class DbFile
    {
        public ulong     Id             { get; set; }
        public string    Sha256         { get; set; }
        public bool      Crack          { get; set; }
        public bool?     HasVirus       { get; set; }
        public DateTime? ClamTime       { get; set; }
        public DateTime? VirusTotalTime { get; set; }
        public string    Virus          { get; set; }
        public long      Length         { get; set; }
    }

    public struct DbOsFile
    {
        public ulong          Id;
        public string         Path;
        public string         Sha256;
        public long           Length;
        public DateTime       CreationTimeUtc;
        public DateTime       LastAccessTimeUtc;
        public DateTime       LastWriteTimeUtc;
        public FileAttributes Attributes;
        public bool           Crack;
    }

    public struct DbFolder
    {
        public ulong          Id;
        public string         Path;
        public DateTime       CreationTimeUtc;
        public DateTime       LastAccessTimeUtc;
        public DateTime       LastWriteTimeUtc;
        public FileAttributes Attributes;
    }

    public class DbOps
    {
        readonly IDbConnection dbCon;
        readonly DbCore        dbCore;

        public DbOps(IDbConnection connection, DbCore core)
        {
            dbCon  = connection;
            dbCore = core;
        }

        public bool GetAllOSes(out List<DbEntry> entries)
        {
            entries = new List<DbEntry>();

            const string SQL = "SELECT * from oses";

            IDbCommand     dbcmd       = dbCon.CreateCommand();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dbcmd.CommandText          = SQL;
            DataSet dataSet            = new DataSet();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                DbEntry fEntry = new DbEntry
                {
                    Id           = long.Parse(dRow["id"].ToString()),
                    Developer    = dRow["developer"].ToString(),
                    Product      = dRow["product"].ToString(),
                    Version      = dRow["version"].ToString(),
                    Languages    = dRow["languages"].ToString(),
                    Architecture = dRow["architecture"].ToString(),
                    Machine      = dRow["machine"].ToString(),
                    Format       = dRow["format"].ToString(),
                    Description  = dRow["description"].ToString(),
                    Oem          = bool.Parse(dRow["oem"].ToString()),
                    Upgrade      = bool.Parse(dRow["upgrade"].ToString()),
                    Update       = bool.Parse(dRow["update"].ToString()),
                    Source       = bool.Parse(dRow["source"].ToString()),
                    Files        = bool.Parse(dRow["files"].ToString()),
                    Netinstall   = bool.Parse(dRow["netinstall"].ToString()),
                    Mdid         = dRow["mdid"].ToString()
                };

                if(dRow["xml"]  != DBNull.Value) fEntry.Xml  = (byte[])dRow["xml"];
                if(dRow["json"] != DBNull.Value) fEntry.Json = (byte[])dRow["json"];
                entries.Add(fEntry);
            }

            return true;
        }

        IDbCommand GetOsCommand(DbEntry entry)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();

            IDbDataParameter param1  = dbcmd.CreateParameter();
            IDbDataParameter param2  = dbcmd.CreateParameter();
            IDbDataParameter param3  = dbcmd.CreateParameter();
            IDbDataParameter param4  = dbcmd.CreateParameter();
            IDbDataParameter param5  = dbcmd.CreateParameter();
            IDbDataParameter param6  = dbcmd.CreateParameter();
            IDbDataParameter param7  = dbcmd.CreateParameter();
            IDbDataParameter param8  = dbcmd.CreateParameter();
            IDbDataParameter param9  = dbcmd.CreateParameter();
            IDbDataParameter param10 = dbcmd.CreateParameter();
            IDbDataParameter param11 = dbcmd.CreateParameter();
            IDbDataParameter param12 = dbcmd.CreateParameter();
            IDbDataParameter param13 = dbcmd.CreateParameter();
            IDbDataParameter param14 = dbcmd.CreateParameter();
            IDbDataParameter param15 = dbcmd.CreateParameter();
            IDbDataParameter param16 = dbcmd.CreateParameter();
            IDbDataParameter param17 = dbcmd.CreateParameter();

            param1.ParameterName  = "@developer";
            param2.ParameterName  = "@product";
            param3.ParameterName  = "@version";
            param4.ParameterName  = "@languages";
            param5.ParameterName  = "@architecture";
            param6.ParameterName  = "@machine";
            param7.ParameterName  = "@format";
            param8.ParameterName  = "@description";
            param9.ParameterName  = "@oem";
            param10.ParameterName = "@upgrade";
            param11.ParameterName = "@update";
            param12.ParameterName = "@source";
            param13.ParameterName = "@files";
            param14.ParameterName = "@netinstall";
            param15.ParameterName = "@xml";
            param16.ParameterName = "@json";
            param17.ParameterName = "@mdid";

            param1.DbType  = DbType.String;
            param2.DbType  = DbType.String;
            param3.DbType  = DbType.String;
            param4.DbType  = DbType.String;
            param5.DbType  = DbType.String;
            param7.DbType  = DbType.String;
            param8.DbType  = DbType.String;
            param9.DbType  = DbType.Boolean;
            param10.DbType = DbType.Boolean;
            param11.DbType = DbType.Boolean;
            param12.DbType = DbType.Boolean;
            param13.DbType = DbType.Boolean;
            param14.DbType = DbType.Boolean;
            param15.DbType = DbType.Object;
            param16.DbType = DbType.Object;
            param17.DbType = DbType.String;

            param1.Value  = entry.Developer;
            param2.Value  = entry.Product;
            param3.Value  = entry.Version;
            param4.Value  = entry.Languages;
            param5.Value  = entry.Architecture;
            param6.Value  = entry.Machine;
            param7.Value  = entry.Format;
            param8.Value  = entry.Description;
            param9.Value  = entry.Oem;
            param10.Value = entry.Upgrade;
            param11.Value = entry.Update;
            param12.Value = entry.Source;
            param13.Value = entry.Files;
            param14.Value = entry.Netinstall;
            param15.Value = entry.Xml;
            param16.Value = entry.Json;
            param17.Value = entry.Mdid;

            dbcmd.Parameters.Add(param1);
            dbcmd.Parameters.Add(param2);
            dbcmd.Parameters.Add(param3);
            dbcmd.Parameters.Add(param4);
            dbcmd.Parameters.Add(param5);
            dbcmd.Parameters.Add(param6);
            dbcmd.Parameters.Add(param7);
            dbcmd.Parameters.Add(param8);
            dbcmd.Parameters.Add(param9);
            dbcmd.Parameters.Add(param10);
            dbcmd.Parameters.Add(param11);
            dbcmd.Parameters.Add(param12);
            dbcmd.Parameters.Add(param13);
            dbcmd.Parameters.Add(param14);
            dbcmd.Parameters.Add(param15);
            dbcmd.Parameters.Add(param16);
            dbcmd.Parameters.Add(param17);

            return dbcmd;
        }

        public bool AddOs(DbEntry entry, out long id)
        {
            IDbCommand     dbcmd = GetOsCommand(entry);
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction    = trans;

            const string SQL =
                "INSERT INTO oses (developer, product, version, languages, architecture, machine, format, description, oem, upgrade, `update`, source, files, netinstall, xml, json, mdid)" +
                " VALUES (@developer, @product, @version, @languages, @architecture, @machine, @format, @description, @oem, @upgrade, @update, @source, @files, @netinstall, @xml, @json, @mdid)";

            dbcmd.CommandText = SQL;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            id = dbCore.LastInsertRowId;

            return true;
        }

        IDbCommand GetFileCommand(DbFile entry)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();

            IDbDataParameter param1 = dbcmd.CreateParameter();
            IDbDataParameter param2 = dbcmd.CreateParameter();
            IDbDataParameter param3 = dbcmd.CreateParameter();
            IDbDataParameter param4 = dbcmd.CreateParameter();
            IDbDataParameter param5 = dbcmd.CreateParameter();
            IDbDataParameter param6 = dbcmd.CreateParameter();
            IDbDataParameter param7 = dbcmd.CreateParameter();

            param1.ParameterName = "@sha256";
            param2.ParameterName = "@crack";
            param3.ParameterName = "@hasvirus";
            param4.ParameterName = "@clamtime";
            param5.ParameterName = "@vtotaltime";
            param6.ParameterName = "@virus";
            param7.ParameterName = "@length";

            param1.DbType = DbType.String;
            param2.DbType = DbType.Boolean;
            param3.DbType = DbType.String;
            param4.DbType = DbType.String;
            param5.DbType = DbType.String;
            param7.DbType = DbType.UInt64;

            param1.Value = entry.Sha256;
            param2.Value = entry.Crack;
            param3.Value = entry.HasVirus;
            param4.Value = entry.ClamTime?.ToString("yyyy-MM-dd HH:mm");
            param5.Value = entry.VirusTotalTime?.ToString("yyyy-MM-dd HH:mm");
            param6.Value = entry.Virus;
            param7.Value = entry.Length;

            dbcmd.Parameters.Add(param1);
            dbcmd.Parameters.Add(param2);
            dbcmd.Parameters.Add(param3);
            dbcmd.Parameters.Add(param4);
            dbcmd.Parameters.Add(param5);
            dbcmd.Parameters.Add(param6);
            dbcmd.Parameters.Add(param7);

            return dbcmd;
        }

        public bool UpdateFile(DbFile file)
        {
            IDbCommand     dbcmd = GetFileCommand(file);
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction    = trans;

            const string SQL =
                "UPDATE files SET crack = @crack, hasvirus = @hasvirus, clamtime = @clamtime, vtotaltime = @vtotaltime, virus = @virus, length = @length " +
                "WHERE sha256 = @sha256";

            dbcmd.CommandText = SQL;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool AddFile(DbFile file)
        {
            IDbCommand     dbcmd = GetFileCommand(file);
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction    = trans;

            const string SQL =
                "INSERT INTO `files` (`sha256`, `crack`, `hasvirus`, `clamtime`, `vtotaltime`, `virus`, `length`)" +
                " VALUES (@sha256, @crack, @hasvirus, @clamtime, @vtotaltime, @virus, @length)";

            dbcmd.CommandText = SQL;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool ExistsFile(string hash)
        {
            IDbCommand       dbcmd  = dbCon.CreateCommand();
            IDbDataParameter param1 = dbcmd.CreateParameter();

            param1.ParameterName = "@hash";
            param1.DbType        = DbType.String;
            param1.Value         = hash;
            dbcmd.Parameters.Add(param1);
            dbcmd.CommandText          = "SELECT * FROM files WHERE sha256 = @hash";
            DataSet        dataSet     = new DataSet();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows) return true;

            return false;
        }

        public ulong GetFilesCount()
        {
            IDbCommand dbcmd  = dbCon.CreateCommand();
            dbcmd.CommandText = "SELECT COUNT(*) FROM files";
            object count      = dbcmd.ExecuteScalar();
            dbcmd.Dispose();
            try { return Convert.ToUInt64(count); }
            catch { return 0; }
        }

        public DbFile GetFile(string hash)
        {
            string sql = $"SELECT * FROM files WHERE sha256 = '{hash}'";

            IDbCommand     dbcmd       = dbCon.CreateCommand();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dbcmd.CommandText          = sql;
            DataSet dataSet            = new DataSet();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                DbFile fEntry = new DbFile
                {
                    Id     = ulong.Parse(dRow["id"].ToString()),
                    Sha256 = dRow["sha256"].ToString(),
                    Crack  = bool.Parse(dRow["crack"].ToString()),
                    Virus  = dRow["virus"].ToString(),
                    Length = long.Parse(dRow["length"].ToString())
                };

                if(dRow["hasvirus"] == DBNull.Value) fEntry.HasVirus = null;
                else fEntry.HasVirus                                 = bool.Parse(dRow["hasvirus"].ToString());
                if(dRow["clamtime"] == DBNull.Value) fEntry.ClamTime = null;
                else
                    fEntry.ClamTime =
                        DateTime.Parse(dRow["clamtime"].ToString());
                if(dRow["vtotaltime"] == DBNull.Value) fEntry.VirusTotalTime = null;
                else
                    fEntry.VirusTotalTime =
                        DateTime.Parse(dRow["vtotaltime"].ToString());

                return fEntry;
            }

            return null;
        }

        public bool GetFiles(out List<DbFile> entries, ulong start, ulong count)
        {
            entries = new List<DbFile>();

            string sql = $"SELECT * FROM files ORDER BY sha256 LIMIT {start}, {count}";

            IDbCommand     dbcmd       = dbCon.CreateCommand();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dbcmd.CommandText          = sql;
            DataSet dataSet            = new DataSet();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                DbFile fEntry = new DbFile
                {
                    Id     = ulong.Parse(dRow["id"].ToString()),
                    Sha256 = dRow["sha256"].ToString(),
                    Crack  = bool.Parse(dRow["crack"].ToString()),
                    Virus  = dRow["virus"].ToString(),
                    Length = long.Parse(dRow["length"].ToString())
                };

                if(dRow["hasvirus"] == DBNull.Value) fEntry.HasVirus = null;
                else fEntry.HasVirus                                 = bool.Parse(dRow["hasvirus"].ToString());
                if(dRow["clamtime"] == DBNull.Value) fEntry.ClamTime = null;
                else
                    fEntry.ClamTime =
                        DateTime.Parse(dRow["clamtime"].ToString());
                if(dRow["vtotaltime"] == DBNull.Value) fEntry.VirusTotalTime = null;
                else
                    fEntry.VirusTotalTime =
                        DateTime.Parse(dRow["vtotaltime"].ToString());

                entries.Add(fEntry);
            }

            return true;
        }

        public bool GetNotAvFiles(out List<DbFile> entries)
        {
            entries = new List<DbFile>();

            const string SQL = "SELECT * FROM files WHERE hasvirus IS NULL ORDER BY sha256";

            IDbCommand     dbcmd       = dbCon.CreateCommand();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dbcmd.CommandText          = SQL;
            DataSet dataSet            = new DataSet();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                DbFile fEntry = new DbFile
                {
                    Id     = ulong.Parse(dRow["id"].ToString()),
                    Sha256 = dRow["sha256"].ToString(),
                    Crack  = bool.Parse(dRow["crack"].ToString()),
                    Virus  = dRow["virus"].ToString(),
                    Length = long.Parse(dRow["length"].ToString())
                };

                if(dRow["hasvirus"] == DBNull.Value) fEntry.HasVirus = null;
                else fEntry.HasVirus                                 = bool.Parse(dRow["hasvirus"].ToString());
                if(dRow["clamtime"] == DBNull.Value) fEntry.ClamTime = null;
                else
                    fEntry.ClamTime =
                        DateTime.Parse(dRow["clamtime"].ToString());
                if(dRow["vtotaltime"] == DBNull.Value) fEntry.VirusTotalTime = null;
                else
                    fEntry.VirusTotalTime =
                        DateTime.Parse(dRow["vtotaltime"].ToString());

                entries.Add(fEntry);
            }

            return true;
        }

        IDbCommand GetOsFileCommand(DbOsFile person)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();

            IDbDataParameter param1 = dbcmd.CreateParameter();
            IDbDataParameter param2 = dbcmd.CreateParameter();
            IDbDataParameter param3 = dbcmd.CreateParameter();
            IDbDataParameter param4 = dbcmd.CreateParameter();
            IDbDataParameter param5 = dbcmd.CreateParameter();
            IDbDataParameter param6 = dbcmd.CreateParameter();
            IDbDataParameter param7 = dbcmd.CreateParameter();

            param1.ParameterName = "@path";
            param2.ParameterName = "@sha256";
            param3.ParameterName = "@length";
            param4.ParameterName = "@creation";
            param5.ParameterName = "@access";
            param6.ParameterName = "@modification";
            param7.ParameterName = "@attributes";

            param1.DbType = DbType.String;
            param2.DbType = DbType.String;
            param3.DbType = DbType.String;
            param4.DbType = DbType.String;
            param5.DbType = DbType.String;
            param6.DbType = DbType.String;
            param7.DbType = DbType.Int32;

            param1.Value = person.Path;
            param2.Value = person.Sha256;
            param3.Value = person.Length;
            param4.Value = person.CreationTimeUtc.ToString("yyyy-MM-dd HH:mm");
            param5.Value = person.LastAccessTimeUtc.ToString("yyyy-MM-dd HH:mm");
            param6.Value = person.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm");
            param7.Value = (int)person.Attributes;

            dbcmd.Parameters.Add(param1);
            dbcmd.Parameters.Add(param2);
            dbcmd.Parameters.Add(param3);
            dbcmd.Parameters.Add(param4);
            dbcmd.Parameters.Add(param5);
            dbcmd.Parameters.Add(param6);
            dbcmd.Parameters.Add(param7);

            return dbcmd;
        }

        public bool AddFileToOs(DbOsFile file, long os)
        {
            IDbCommand     dbcmd = GetOsFileCommand(file);
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction    = trans;

            string sql =
                $"INSERT INTO `os_{os}` (`path`, `sha256`, `length`, `creation`, `access`, `modification`, `attributes`)" +
                " VALUES (@path, @sha256, @length, @creation, @access, @modification, @attributes)";

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        IDbCommand GetFolderCommand(DbFolder person)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();

            IDbDataParameter param1 = dbcmd.CreateParameter();
            IDbDataParameter param4 = dbcmd.CreateParameter();
            IDbDataParameter param5 = dbcmd.CreateParameter();
            IDbDataParameter param6 = dbcmd.CreateParameter();
            IDbDataParameter param7 = dbcmd.CreateParameter();

            param1.ParameterName = "@path";
            param4.ParameterName = "@creation";
            param5.ParameterName = "@access";
            param6.ParameterName = "@modification";
            param7.ParameterName = "@attributes";

            param1.DbType = DbType.String;
            param4.DbType = DbType.String;
            param5.DbType = DbType.String;
            param6.DbType = DbType.String;
            param7.DbType = DbType.Int32;

            param1.Value = person.Path;
            param4.Value = person.CreationTimeUtc.ToString("yyyy-MM-dd HH:mm");
            param5.Value = person.LastAccessTimeUtc.ToString("yyyy-MM-dd HH:mm");
            param6.Value = person.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm");
            param7.Value = (int)person.Attributes;

            dbcmd.Parameters.Add(param1);
            dbcmd.Parameters.Add(param4);
            dbcmd.Parameters.Add(param5);
            dbcmd.Parameters.Add(param6);
            dbcmd.Parameters.Add(param7);

            return dbcmd;
        }

        public bool AddFolderToOs(DbFolder folder, long os)
        {
            IDbCommand     dbcmd = GetFolderCommand(folder);
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction    = trans;

            string sql = $"INSERT INTO `os_{os}_folders` (`path`, `creation`, `access`, `modification`, `attributes`)" +
                         " VALUES (@path, @creation, @access, @modification, @attributes)";

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool RemoveOs(long id)
        {
            IDbCommand     dbcmd = dbCon.CreateCommand();
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction    = trans;

            string sql = $"DROP TABLE IF EXISTS `os_{id}`;";

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            dbcmd             = dbCon.CreateCommand();
            trans             = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            sql = $"DROP TABLE IF EXISTS `os_{id}_folders`;";

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            dbcmd             = dbCon.CreateCommand();
            trans             = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            sql = $"DROP TABLE IF EXISTS `os_{id}_symlinks`;";

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            dbcmd             = dbCon.CreateCommand();
            trans             = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            sql = $"DELETE FROM oses WHERE id = '{id}';";

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool CreateTableForOs(long id)
        {
            IDbCommand     dbcmd = dbCon.CreateCommand();
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction    = trans;

            string sql =
                string.Format("DROP TABLE IF EXISTS `os_{0}`;\n\n" + "CREATE TABLE IF NOT EXISTS `os_{0}` (\n" + "  `id` INTEGER PRIMARY KEY AUTOINCREMENT,\n" + "  `path` VARCHAR(8192) NOT NULL,\n" + "  `sha256` VARCHAR(64) NOT NULL,\n\n" + "  `length` BIGINT NOT NULL,\n" + "  `creation` DATETIME NULL,\n" + "  `access` DATETIME NULL,\n" + "  `modification` DATETIME NULL,\n" + "  `attributes` INTEGER NULL);\n\n" + "CREATE UNIQUE INDEX `os_{0}_id_UNIQUE` ON `os_{0}` (`id` ASC);\n\n" + "CREATE INDEX `os_{0}_path_idx` ON `os_{0}` (`path` ASC);",
                              id);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            dbcmd             = dbCon.CreateCommand();
            trans             = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            sql =
                string.Format("DROP TABLE IF EXISTS `os_{0}_folders`;\n\n" + "CREATE TABLE IF NOT EXISTS `os_{0}_folders` (\n" + "  `id` INTEGER PRIMARY KEY AUTOINCREMENT,\n" + "  `path` VARCHAR(8192) NOT NULL,\n" + "  `creation` DATETIME NULL,\n" + "  `access` DATETIME NULL,\n" + "  `modification` DATETIME NULL,\n" + "  `attributes` INTEGER NULL);\n\n" + "CREATE UNIQUE INDEX `os_{0}_folders_id_UNIQUE` ON `os_{0}_folders` (`id` ASC);\n\n" + "CREATE INDEX `os_{0}_folders_path_idx` ON `os_{0}_folders` (`path` ASC);",
                              id);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool ExistsFileInOs(string hash, long osId)
        {
            IDbCommand       dbcmd  = dbCon.CreateCommand();
            IDbDataParameter param1 = dbcmd.CreateParameter();

            param1.ParameterName = "@hash";
            param1.DbType        = DbType.String;
            param1.Value         = hash;
            dbcmd.Parameters.Add(param1);
            dbcmd.CommandText          = $"SELECT * FROM `os_{osId}` WHERE sha256 = @hash";
            DataSet        dataSet     = new DataSet();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows) return true;

            return false;
        }

        public bool ExistsOs(string mdid)
        {
            IDbCommand       dbcmd  = dbCon.CreateCommand();
            IDbDataParameter param1 = dbcmd.CreateParameter();

            param1.ParameterName = "@mdid";
            param1.DbType        = DbType.String;
            param1.Value         = mdid;
            dbcmd.Parameters.Add(param1);
            dbcmd.CommandText          = "SELECT * FROM `oses` WHERE mdid = @mdid";
            DataSet        dataSet     = new DataSet();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows) return true;

            return false;
        }

        public bool GetAllFilesInOs(out List<DbOsFile> entries, long id)
        {
            entries = new List<DbOsFile>();

            string sql = $"SELECT * from os_{id}";

            IDbCommand     dbcmd       = dbCon.CreateCommand();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dbcmd.CommandText          = sql;
            DataSet dataSet            = new DataSet();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                DbOsFile fEntry = new DbOsFile
                {
                    Id                = ulong.Parse(dRow["id"].ToString()),
                    Path              = dRow["path"].ToString(),
                    Sha256            = dRow["sha256"].ToString(),
                    Length            = long.Parse(dRow["length"].ToString()),
                    CreationTimeUtc   = DateTime.Parse(dRow["creation"].ToString()),
                    LastAccessTimeUtc = DateTime.Parse(dRow["access"].ToString()),
                    LastWriteTimeUtc  = DateTime.Parse(dRow["modification"].ToString()),
                    Attributes        = (FileAttributes)int.Parse(dRow["attributes"].ToString())
                };

                entries.Add(fEntry);
            }

            return true;
        }

        public bool GetAllFolders(out List<DbFolder> entries, long id)
        {
            entries = new List<DbFolder>();

            string sql = $"SELECT * from os_{id}_folders";

            IDbCommand     dbcmd       = dbCon.CreateCommand();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dbcmd.CommandText          = sql;
            DataSet dataSet            = new DataSet();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                DbFolder fEntry = new DbFolder
                {
                    Id                = ulong.Parse(dRow["id"].ToString()),
                    Path              = dRow["path"].ToString(),
                    CreationTimeUtc   = DateTime.Parse(dRow["creation"].ToString()),
                    LastAccessTimeUtc = DateTime.Parse(dRow["access"].ToString()),
                    LastWriteTimeUtc  = DateTime.Parse(dRow["modification"].ToString()),
                    Attributes        = (FileAttributes)int.Parse(dRow["attributes"].ToString())
                };

                entries.Add(fEntry);
            }

            return true;
        }

        public bool ToggleCrack(string hash, bool crack)
        {
            IDbCommand       dbcmd  = dbCon.CreateCommand();
            IDbTransaction   trans  = dbCon.BeginTransaction();
            IDbDataParameter param1 = dbcmd.CreateParameter();
            IDbDataParameter param2 = dbcmd.CreateParameter();

            param1.ParameterName = "@hash";
            param1.DbType        = DbType.String;
            param1.Value         = hash;
            param2.ParameterName = "@crack";
            param2.DbType        = DbType.Boolean;
            param2.Value         = crack;
            dbcmd.Parameters.Add(param1);
            dbcmd.Parameters.Add(param2);
            dbcmd.CommandText = "UPDATE files SET crack = @crack WHERE sha256 = @hash";
            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool DeleteFile(string hash)
        {
            IDbCommand       dbcmd  = dbCon.CreateCommand();
            IDbDataParameter param1 = dbcmd.CreateParameter();

            param1.ParameterName = "@sha256";
            param1.DbType        = DbType.String;
            param1.Value         = hash;
            dbcmd.Parameters.Add(param1);
            dbcmd.CommandText = "DELETE FROM `files` WHERE sha256 = @sha256";
            dbcmd.ExecuteNonQuery();

            return true;
        }

        public bool HasSymlinks(long osId)
        {
            IDbCommand dbcmd  = dbCon.CreateCommand();
            dbcmd.CommandText =
                $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'os_{osId}_symlinks'";
            object count = dbcmd.ExecuteScalar();
            dbcmd.Dispose();

            return Convert.ToUInt64(count) > 0;
        }

        public bool CreateSymlinkTableForOs(long id)
        {
            IDbCommand     dbcmd = dbCon.CreateCommand();
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction    = trans;

            string sql =
                string.Format("DROP TABLE IF EXISTS `os_{0}_symlinks`;\n\n" + "CREATE TABLE IF NOT EXISTS `os_{0}_symlinks` (\n" + "  `path` VARCHAR(8192) PRIMARY KEY,\n" + "  `target` VARCHAR(8192) NOT NULL);\n\n" + "CREATE UNIQUE INDEX `os_{0}_symlinks_path_UNIQUE` ON `os_{0}_symlinks` (`path` ASC);\n\n" + "CREATE INDEX `os_{0}_symlinks_target_idx` ON `os_{0}_symlinks` (`target` ASC);",
                              id);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool AddSymlinkToOs(string path, string target, long os)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();

            IDbDataParameter param1 = dbcmd.CreateParameter();
            IDbDataParameter param2 = dbcmd.CreateParameter();

            param1.ParameterName = "@path";
            param2.ParameterName = "@target";

            param1.DbType = DbType.String;
            param2.DbType = DbType.String;

            param1.Value = path;
            param2.Value = target;

            dbcmd.Parameters.Add(param1);
            dbcmd.Parameters.Add(param2);

            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction    = trans;

            string sql = $"INSERT INTO `os_{os}_symlinks` (`path`, `target`)" + " VALUES (@path, @target)";

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool GetAllSymlinks(out Dictionary<string, string> entries, long id)
        {
            entries = new Dictionary<string, string>();

            string sql = $"SELECT * from os_{id}_symlinks";

            IDbCommand     dbcmd       = dbCon.CreateCommand();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dbcmd.CommandText          = sql;
            DataSet dataSet            = new DataSet();
            dataAdapter.SelectCommand  = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
                if(!entries.ContainsKey(dRow["path"].ToString()))
                    entries.Add(dRow["path"].ToString(), dRow["target"].ToString());

            return true;
        }
    }
}