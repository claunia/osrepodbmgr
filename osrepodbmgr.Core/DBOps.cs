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
    public struct DBEntry
    {
        public long id;
        public string developer;
        public string product;
        public string version;
        public string languages;
        public string architecture;
        public string machine;
        public string format;
        public string description;
        public bool oem;
        public bool upgrade;
        public bool update;
        public bool source;
        public bool files;
        public bool netinstall;
        public byte[] xml;
        public byte[] json;
        public string mdid;
    }

    public struct DBFile
    {
        public ulong Id;
        public string Path;
        public string Sha256;
        public long Length;
        public DateTime CreationTimeUtc;
        public DateTime LastAccessTimeUtc;
        public DateTime LastWriteTimeUtc;
        public FileAttributes Attributes;
    }

    public struct DBFolder
    {
        public ulong Id;
        public string Path;
        public DateTime CreationTimeUtc;
        public DateTime LastAccessTimeUtc;
        public DateTime LastWriteTimeUtc;
        public FileAttributes Attributes;
    }

    public class DBOps
    {
        readonly IDbConnection dbCon;
        readonly DBCore dbCore;

        public DBOps(IDbConnection connection, DBCore core)
        {
            dbCon = connection;
            dbCore = core;
        }

        public bool GetAllOSes(out List<DBEntry> entries)
        {
            entries = new List<DBEntry>();

            const string sql = "SELECT * from oses";

            IDbCommand dbcmd = dbCon.CreateCommand();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dbcmd.CommandText = sql;
            DataSet dataSet = new DataSet();
            dataAdapter.SelectCommand = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                DBEntry fEntry = new DBEntry();
                fEntry.id = long.Parse(dRow["id"].ToString());
                fEntry.developer = dRow["developer"].ToString();
                fEntry.product = dRow["product"].ToString();
                fEntry.version = dRow["version"].ToString();
                fEntry.languages = dRow["languages"].ToString();
                fEntry.architecture = dRow["architecture"].ToString();
                fEntry.machine = dRow["machine"].ToString();
                fEntry.format = dRow["format"].ToString();
                fEntry.description = dRow["description"].ToString();
                fEntry.oem = bool.Parse(dRow["oem"].ToString());
                fEntry.upgrade = bool.Parse(dRow["upgrade"].ToString());
                fEntry.update = bool.Parse(dRow["update"].ToString());
                fEntry.source = bool.Parse(dRow["source"].ToString());
                fEntry.files = bool.Parse(dRow["files"].ToString());
                fEntry.netinstall = bool.Parse(dRow["netinstall"].ToString());
                fEntry.mdid = dRow["mdid"].ToString();

                if(dRow["xml"] != DBNull.Value)
                    fEntry.xml = (byte[])dRow["xml"];
                if(dRow["json"] != DBNull.Value)
                    fEntry.json = (byte[])dRow["json"];
                entries.Add(fEntry);
            }

            return true;
        }

        IDbCommand GetOSCommand(DBEntry entry)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();

            IDbDataParameter param1 = dbcmd.CreateParameter();
            IDbDataParameter param2 = dbcmd.CreateParameter();
            IDbDataParameter param3 = dbcmd.CreateParameter();
            IDbDataParameter param4 = dbcmd.CreateParameter();
            IDbDataParameter param5 = dbcmd.CreateParameter();
            IDbDataParameter param6 = dbcmd.CreateParameter();
            IDbDataParameter param7 = dbcmd.CreateParameter();
            IDbDataParameter param8 = dbcmd.CreateParameter();
            IDbDataParameter param9 = dbcmd.CreateParameter();
            IDbDataParameter param10 = dbcmd.CreateParameter();
            IDbDataParameter param11 = dbcmd.CreateParameter();
            IDbDataParameter param12 = dbcmd.CreateParameter();
            IDbDataParameter param13 = dbcmd.CreateParameter();
            IDbDataParameter param14 = dbcmd.CreateParameter();
            IDbDataParameter param15 = dbcmd.CreateParameter();
            IDbDataParameter param16 = dbcmd.CreateParameter();
            IDbDataParameter param17 = dbcmd.CreateParameter();

            param1.ParameterName = "@developer";
            param2.ParameterName = "@product";
            param3.ParameterName = "@version";
            param4.ParameterName = "@languages";
            param5.ParameterName = "@architecture";
            param6.ParameterName = "@machine";
            param7.ParameterName = "@format";
            param8.ParameterName = "@description";
            param9.ParameterName = "@oem";
            param10.ParameterName = "@upgrade";
            param11.ParameterName = "@update";
            param12.ParameterName = "@source";
            param13.ParameterName = "@files";
            param14.ParameterName = "@netinstall";
            param15.ParameterName = "@xml";
            param16.ParameterName = "@json";
            param17.ParameterName = "@mdid";

            param1.DbType = DbType.String;
            param2.DbType = DbType.String;
            param3.DbType = DbType.String;
            param4.DbType = DbType.String;
            param5.DbType = DbType.String;
            param7.DbType = DbType.String;
            param8.DbType = DbType.String;
            param9.DbType = DbType.Boolean;
            param10.DbType = DbType.Boolean;
            param11.DbType = DbType.Boolean;
            param12.DbType = DbType.Boolean;
            param13.DbType = DbType.Boolean;
            param14.DbType = DbType.Boolean;
            param15.DbType = DbType.Object;
            param16.DbType = DbType.Object;
            param17.DbType = DbType.String;

            param1.Value = entry.developer;
            param2.Value = entry.product;
            param3.Value = entry.version;
            param4.Value = entry.languages;
            param5.Value = entry.architecture;
            param6.Value = entry.machine;
            param7.Value = entry.format;
            param8.Value = entry.description;
            param9.Value = entry.oem;
            param10.Value = entry.upgrade;
            param11.Value = entry.update;
            param12.Value = entry.source;
            param13.Value = entry.files;
            param14.Value = entry.netinstall;
            param15.Value = entry.xml;
            param16.Value = entry.json;
            param17.Value = entry.mdid;

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

        public bool AddOS(DBEntry entry, out long id)
        {
            IDbCommand dbcmd = GetOSCommand(entry);
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            const string sql = "INSERT INTO oses (developer, product, version, languages, architecture, machine, format, description, oem, upgrade, `update`, source, files, netinstall, xml, json, mdid)" +
                " VALUES (@developer, @product, @version, @languages, @architecture, @machine, @format, @description, @oem, @upgrade, @update, @source, @files, @netinstall, @xml, @json, @mdid)";

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            id = dbCore.LastInsertRowId;

            return true;
        }

        public bool AddFile(string hash)
        {
            //Console.WriteLine("Adding {0}", hash);
            IDbCommand dbcmd = dbCon.CreateCommand();

            IDbDataParameter param1 = dbcmd.CreateParameter();

            param1.ParameterName = "@hash";

            param1.DbType = DbType.String;

            param1.Value = hash;

            dbcmd.Parameters.Add(param1);
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            const string sql = "INSERT INTO files (sha256) VALUES (@hash)";

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool ExistsFile(string hash)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();
            IDbDataParameter param1 = dbcmd.CreateParameter();

            param1.ParameterName = "@hash";
            param1.DbType = DbType.String;
            param1.Value = hash;
            dbcmd.Parameters.Add(param1);
            dbcmd.CommandText = "SELECT * FROM files WHERE sha256 = @hash";
            DataSet dataSet = new DataSet();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dataAdapter.SelectCommand = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                return true;
            }

            return false;
        }

        IDbCommand GetFileCommand(DBFile person)
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

        public bool AddFileToOS(DBFile file, long os)
        {
            IDbCommand dbcmd = GetFileCommand(file);
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            string sql = string.Format("INSERT INTO `os_{0}` (`path`, `sha256`, `length`, `creation`, `access`, `modification`, `attributes`)" +
                                       " VALUES (@path, @sha256, @length, @creation, @access, @modification, @attributes)", os);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        IDbCommand GetFolderCommand(DBFolder person)
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

        public bool AddFolderToOS(DBFolder folder, long os)
        {
            IDbCommand dbcmd = GetFolderCommand(folder);
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            string sql = string.Format("INSERT INTO `os_{0}_folders` (`path`, `creation`, `access`, `modification`, `attributes`)" +
                                       " VALUES (@path, @creation, @access, @modification, @attributes)", os);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool RemoveOS(long id)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            string sql = string.Format("DROP TABLE IF EXISTS `os_{0}`;", id);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            dbcmd = dbCon.CreateCommand();
            trans = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            sql = string.Format("DROP TABLE IF EXISTS `os_{0}_folders`;", id);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            dbcmd = dbCon.CreateCommand();
            trans = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            sql = string.Format("DELETE FROM oses WHERE id = '{0}';", id);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool CreateTableForOS(long id)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();
            IDbTransaction trans = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            string sql = string.Format("DROP TABLE IF EXISTS `os_{0}`;\n\n" +
                                         "CREATE TABLE IF NOT EXISTS `os_{0}` (\n" +
                                         "  `id` INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                                         "  `path` VARCHAR(8192) NOT NULL,\n" +
                                         "  `sha256` VARCHAR(64) NOT NULL,\n\n" +
                                         "  `length` BIGINT NOT NULL,\n" +
                                         "  `creation` DATETIME NULL,\n" +
                                         "  `access` DATETIME NULL,\n" +
                                         "  `modification` DATETIME NULL,\n" +
                                         "  `attributes` INTEGER NULL);\n\n" +
                                         "CREATE UNIQUE INDEX `os_{0}_id_UNIQUE` ON `os_{0}` (`id` ASC);\n\n" +
                                         "CREATE INDEX `os_{0}_path_idx` ON `os_{0}` (`path` ASC);", id);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            dbcmd = dbCon.CreateCommand();
            trans = dbCon.BeginTransaction();
            dbcmd.Transaction = trans;

            sql = string.Format("DROP TABLE IF EXISTS `os_{0}_folders`;\n\n" +
                                 "CREATE TABLE IF NOT EXISTS `os_{0}_folders` (\n" +
                                 "  `id` INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                                 "  `path` VARCHAR(8192) NOT NULL,\n" +
                                 "  `creation` DATETIME NULL,\n" +
                                 "  `access` DATETIME NULL,\n" +
                                 "  `modification` DATETIME NULL,\n" +
                                 "  `attributes` INTEGER NULL);\n\n" +
                                 "CREATE UNIQUE INDEX `os_{0}_folders_id_UNIQUE` ON `os_{0}_folders` (`id` ASC);\n\n" +
                                 "CREATE INDEX `os_{0}_folders_path_idx` ON `os_{0}_folders` (`path` ASC);", id);

            dbcmd.CommandText = sql;

            dbcmd.ExecuteNonQuery();
            trans.Commit();
            dbcmd.Dispose();

            return true;
        }

        public bool ExistsFileInOS(string hash, long osId)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();
            IDbDataParameter param1 = dbcmd.CreateParameter();

            param1.ParameterName = "@hash";
            param1.DbType = DbType.String;
            param1.Value = hash;
            dbcmd.Parameters.Add(param1);
            dbcmd.CommandText = string.Format("SELECT * FROM `os_{0}` WHERE sha256 = @hash", osId);
            DataSet dataSet = new DataSet();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dataAdapter.SelectCommand = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                return true;
            }

            return false;
        }

        public bool ExistsOS(string mdid)
        {
            IDbCommand dbcmd = dbCon.CreateCommand();
            IDbDataParameter param1 = dbcmd.CreateParameter();

            param1.ParameterName = "@mdid";
            param1.DbType = DbType.String;
            param1.Value = mdid;
            dbcmd.Parameters.Add(param1);
            dbcmd.CommandText = "SELECT * FROM `oses` WHERE mdid = @mdid";
            DataSet dataSet = new DataSet();
            IDbDataAdapter dataAdapter = dbCore.GetNewDataAdapter();
            dataAdapter.SelectCommand = dbcmd;
            dataAdapter.Fill(dataSet);
            DataTable dataTable = dataSet.Tables[0];

            foreach(DataRow dRow in dataTable.Rows)
            {
                return true;
            }

            return false;
        }
    }
}

