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
using System.Data;
using System.Data.SQLite;

namespace osrepodbmgr
{
    public class SQLite : DBCore
    {
        SQLiteConnection dbCon;

        #region implemented abstract members of DBCore

        public override bool OpenDB(string database, string server, string user, string password)
        {
            try
            {
                string dataSrc = string.Format("URI=file:{0}", database);
                dbCon = new SQLiteConnection(dataSrc);
                dbCon.Open();
                string sql;

                sql = "SELECT * FROM osrepodbmgr";

                SQLiteCommand dbcmd = dbCon.CreateCommand();
                dbcmd.CommandText = sql;
                SQLiteDataAdapter dAdapter = new SQLiteDataAdapter();
                dAdapter.SelectCommand = dbcmd;
                DataSet dSet = new DataSet();
                dAdapter.Fill(dSet);
                DataTable dTable = dSet.Tables[0];

                if(dTable.Rows.Count != 1)
                    return false;

                if((long)dTable.Rows[0]["version"] != 1)
                {
                    dbCon = null;
                    return false;
                }

                DBOps = new DBOps(dbCon, this);

                return true;
            }
            catch(SQLiteException ex)
            {
                Console.WriteLine("Error opening DB.");
                Console.WriteLine(ex.Message);
                dbCon = null;
                return false;
            }
        }

        public override void CloseDB()
        {
            if(dbCon != null)
                dbCon.Close();

            DBOps = null;
        }

        public override bool CreateDB(string database, string server, string user, string password)
        {
            try
            {
                string dataSrc = string.Format("URI=file:{0}", database);
                dbCon = new SQLiteConnection(dataSrc);
                dbCon.Open();
                SQLiteCommand dbCmd = dbCon.CreateCommand();
                string sql;

                Console.WriteLine("Creating osrepodbmgr table");

                sql = "CREATE TABLE osrepodbmgr ( version INTEGER, name TEXT )";
                dbCmd.CommandText = sql;
                dbCmd.ExecuteNonQuery();

                sql = "INSERT INTO osrepodbmgr ( version, name ) VALUES ( '1', 'Canary Islands Computer Museum' )";
                dbCmd.CommandText = sql;
                dbCmd.ExecuteNonQuery();

                Console.WriteLine("Creating oses table");
                dbCmd.CommandText = Schema.OSesTableSql;
                dbCmd.ExecuteNonQuery();

                Console.WriteLine("Creating files table");
                dbCmd.CommandText = Schema.FilesTableSql;
                dbCmd.ExecuteNonQuery();

                dbCmd.Dispose();
                dbCon = null;
                return true;
            }
            catch(SQLiteException ex)
            {
                Console.WriteLine("Error opening DB.");
                Console.WriteLine(ex.Message);
                dbCon = null;
                return false;
            }
        }

        public override IDbDataAdapter GetNewDataAdapter()
        {
            return new SQLiteDataAdapter();
        }

        public override long LastInsertRowId
        {
            get { return dbCon.LastInsertRowId; }
        }

        #endregion
    }
}

