using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Logging.Formatting;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Database
{
    public class Db2 : Database
    {
        private ILogger logger = Logger.OfContext();
        OleDbConnection myConnection;

        OleDbTransaction myTransaction = null;
        public override bool TestConnection(string dbConnectStr)
        {
            bool canConnectioned = false;
            DbServerConnStr conStr = JsonHelper.JsonDesByNewtonJson<DbServerConnStr>(dbConnectStr);
            string location = conStr.DataSource;
            if (location.IndexOf(":") < 0)
            {
                location = $"{location}:{conStr.Port}";
            }
            string dbStr = string.Format("Provider = IBMDADB2.IBMDBCL1; Data Source = {0};Persist Security Info = True; User ID = {1}; pwd = {2}; Location = {3};Connection Timeout={4};", conStr.DbName, conStr.UserName, conStr.Pwd, location, ConnectTimeOut);
            myConnection = new OleDbConnection(dbStr);
            try
            {
                myConnection.Open();
                canConnectioned = true;
            }
            catch (Exception)
            {
                canConnectioned = false;
                throw;
            }
            finally
            {
                myConnection.Close();
            }
            return canConnectioned;
        }

        public override bool CloseConntection()
        {
            try
            {
                if (myConnection != null)
                {
                    if (myConnection.State == ConnectionState.Open)
                    {
                        myConnection.Close();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return false;
        }

        public override string CreateDbConnection(string dbConnectStr)
        {
            string uuid = string.Empty;
            try
            {
                DbServerConnStr conStr = JsonHelper.JsonDesByNewtonJson<DbServerConnStr>(dbConnectStr);
                string location = conStr.DataSource;
                if (location.IndexOf(":") < 0)
                {
                    location = $"{location}:{conStr.Port}";
                }
                string dbStr = string.Format("Provider = IBMDADB2.IBMDBCL1; Data Source = {0};Persist Security Info = True; User ID = {1}; pwd = {2}; Location = {3};Connection Timeout={4};", conStr.DbName, conStr.UserName, conStr.Pwd, location, ConnectTimeOut);
                myConnection = new OleDbConnection(dbStr);
                if (myConnection.State != ConnectionState.Open)
                {
                    myConnection.Open();
                }
                uuid = Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return uuid;
        }

        public override DbDataReader ExecuteReader(string sqlString, params object[] cmdParams)
        {
            using (OleDbCommand cmd = new OleDbCommand())
            {
                try
                {
                    if (cmdParams.Length > 0)
                    {
                        OleDbParameter[] paras = new OleDbParameter[cmdParams.Length];
                        int index = 0;
                        List<string> pas = ExtractParam(ref sqlString);
                        if (pas.Count != cmdParams.Length)
                        {
                            throw new Exception("SQL语句参数错误".I18n("controlType_Db2_D19805D3"));
                        }
                        foreach (var itm in cmdParams)
                        {
                            paras[index] = new OleDbParameter(pas[index], itm);
                            index++;
                        }
                        PrepareCommand(cmd, myConnection, null, sqlString, paras);
                    }
                    else
                    {
                        cmd.CommandText = sqlString;
                    }
                    if (myConnection.State != ConnectionState.Open)
                    {
                        myConnection.Open();
                    }
                    OleDbDataReader myReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (cmdParams.Length > 0)
                    {
                        cmd.Parameters.Clear();
                    }
                    return myReader;
                }
                catch (OleDbException e)
                {
                    throw e;
                }
            }
        }

        public override int ExecuteSql(string sqlString, params object[] cmdParams)
        {
            bool hasTran = (this.myTransaction != null);
            using (OleDbCommand cmd = new OleDbCommand())
            {
                try
                {
                    cmd.CommandTimeout = this.CommandTimeOut;
                    if (hasTran)
                    {
                        cmd.Transaction = this.myTransaction;
                    }
                    if (cmdParams.Length > 0)
                    {
                        OleDbParameter[] paras = new OleDbParameter[cmdParams.Length];
                        int index = 0;
                        List<string> pas = ExtractParam(ref sqlString);
                        if (pas.Count != cmdParams.Length)
                        {
                            throw new Exception("SQL语句参数错误".I18n("controlType_Db2_D19805D3"));
                        }
                        foreach (var itm in cmdParams)
                        {
                            paras[index] = new OleDbParameter(pas[index], itm);
                            index++;
                        }
                        PrepareCommand(cmd, myConnection, null, sqlString, paras);
                    }
                    else
                    {
                        cmd.Connection = myConnection;
                        cmd.CommandText = sqlString;
                    }
                    if (myConnection.State != ConnectionState.Open)
                    {
                        myConnection.Open();
                    }
                    int rows = cmd.ExecuteNonQuery();
                    if (cmdParams.Length > 0)
                    {
                        cmd.Parameters.Clear();
                    }
                    return rows;
                }
                catch (OleDbException e)
                {
                    if (hasTran)
                    {
                        this.RollbackTransaction();
                    }
                    throw e;
                }
            }
        }
        public override object GetSingle(string sqlString, params object[] cmdParams)
        {
            using (OleDbCommand cmd = new OleDbCommand())
            {
                try
                {
                    cmd.CommandTimeout = this.CommandTimeOut;
                    if (cmdParams.Length > 0)
                    {
                        OleDbParameter[] paras = new OleDbParameter[cmdParams.Length];
                        int index = 0;
                        List<string> pas = ExtractParam(ref sqlString);
                        if (pas.Count != cmdParams.Length)
                        {
                            throw new Exception("SQL语句参数错误".I18n("controlType_Db2_D19805D3"));
                        }
                        foreach (var itm in cmdParams)
                        {
                            paras[index] = new OleDbParameter(pas[index], itm);
                            index++;
                        }
                        PrepareCommand(cmd, myConnection, null, sqlString, paras);
                    }
                    else
                    {
                        cmd.Connection = myConnection;
                        cmd.CommandText = sqlString;
                    }
                    if (myConnection.State != ConnectionState.Open)
                    {
                        myConnection.Open();
                    }
                    object obj = cmd.ExecuteScalar();
                    if (cmdParams.Length > 0)
                    {
                        cmd.Parameters.Clear();
                    }
                    if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
                    {
                        return null;
                    }
                    else
                    {
                        return obj;
                    }
                }
                catch (OleDbException e)
                {
                    throw e;
                }
            }
        }
        public override DataSet Query(string sqlString, params object[] cmdParams)
        {
            using (OleDbCommand cmd = new OleDbCommand())
            {
                cmd.CommandTimeout = this.CommandTimeOut;
                OleDbDataAdapter da;
                if (cmdParams.Length > 0)
                {
                    OleDbParameter[] paras = new OleDbParameter[cmdParams.Length];
                    int index = 0;
                    List<string> pas = ExtractParam(ref sqlString);
                    if (pas.Count != cmdParams.Length)
                    {
                        throw new Exception("SQL语句参数错误".I18n("controlType_Db2_D19805D3"));
                    }
                    foreach (var itm in cmdParams)
                    {
                        paras[index] = new OleDbParameter(pas[index], itm);
                        index++;
                    }
                    PrepareCommand(cmd, myConnection, null, sqlString, paras);
                    da = new OleDbDataAdapter(cmd);
                }
                else
                {
                    PrepareCommand(cmd, myConnection, null, sqlString, null);
                    da = new OleDbDataAdapter(cmd);
                    //da = new OleDbDataAdapter(sqlString, myConnection);
                }
                using (da)
                {
                    DataSet ds = new DataSet();
                    try
                    {
                        if (myConnection.State != ConnectionState.Open)
                        {
                            myConnection.Open();
                        }
                        da.Fill(ds, "ds");
                        cmd.Parameters.Clear();
                    }
                    catch (OleDbException ex)
                    {
                        throw ex;
                    }
                    return ds;
                }
            }
        }

        

        public override int BatchExecuteSql(string sqlString, string paras)
        {
            List<object[]> inputParas = new List<object[]>();
            try
            {
                inputParas = (List<object[]>)JsonHelper.JsonDesByNewtonJson<List<object[]>>(paras);
            }
            catch (Exception ex)
            {
                throw new Exception("SQL语句参数错误".I18n("controlType_Db2_D19805D3"));
            }
            bool hasTran = (this.myTransaction != null);
            int count = 0;
            using (OleDbCommand cmd = new OleDbCommand())
            {
                try
                {
                    cmd.CommandTimeout = this.CommandTimeOut;
                    if (inputParas.Count > 0)
                    {
                        try
                        {
                            if (!hasTran)
                            {
                                this.BeginTransaction();
                            }
                            cmd.Transaction = this.myTransaction;
                            for (int i = 0; i < inputParas.Count; i++)
                            {
                                object[] objs = inputParas[i];
                                OleDbParameter[] sqlparas = new OleDbParameter[objs.Length];
                                int index = 0;
                                List<string> pas = ExtractParam(ref sqlString);
                                if (pas.Count != objs.Length)
                                {
                                    throw new Exception("SQL语句参数错误".I18n("controlType_Db2_D19805D3"));
                                }
                                foreach (var itm in objs)
                                {
                                    sqlparas[index] = new OleDbParameter(pas[index], itm);
                                    index++;
                                }
                                PrepareCommand(cmd, myConnection, null, sqlString, sqlparas);
                                if (myConnection.State != ConnectionState.Open)
                                {
                                    myConnection.Open();
                                }
                                count += cmd.ExecuteNonQuery();
                                cmd.Parameters.Clear();
                            }
                            if (!hasTran)
                            {
                                this.CommitTransaction();
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!hasTran)
                            {
                                this.RollbackTransaction();
                            }
                            throw ex;
                        }
                    }
                    else
                    {
                        cmd.CommandText = sqlString;
                        count = cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return count;
            }
        }

        public override bool BatchInsert(string tableName, string colmunNameStr, List<object[]> paras)
        {
            bool hasTran = (this.myTransaction != null);
            try
            {
                int insertCount = 0;
                int totalCount = paras.Count;
                int useCount = 0;
                string columnPartStr = string.Empty;//insert into tbname后的（列1，列2.。。）
                if (!string.IsNullOrEmpty(colmunNameStr))//默认列名在，在拼接select时使用
                {
                    columnPartStr = $"({colmunNameStr})";
                }
                using (OleDbCommand cmd = new OleDbCommand())
                {
                    cmd.Connection = myConnection;
                    if (!hasTran)
                    {
                        this.BeginTransaction();
                    }
                    cmd.Transaction = this.myTransaction;
                    Type _datatype = typeof(DateTime);
                    object colum;
                    StringBuilder sb = new StringBuilder();
                    string insertSql = $"insert into {tableName}{columnPartStr} values ";
                    sb.Append(insertSql);
                    foreach (object[] row in paras)
                    {
                        useCount++;
                        sb.Append("(");
                        for (int j = 0; j < row.Length; j++)
                        {
                            colum = row[j];
                            if (colum is DateTime)
                            {
                                sb.AppendFormat($"'{((DateTime)colum).ToString("yyyy/MM/dd HH:mm:ss")}'");
                            }
                            else
                            {
                                sb.Append($"'{colum}'");
                            }
                            if (j < row.Length - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append("),");
                        if (useCount % 500 == 0 || useCount == totalCount)
                        {
                            sb.Length--;
                            cmd.CommandText = sb.ToString();
                            insertCount += cmd.ExecuteNonQuery();
                            sb.Clear();
                            sb.Append(insertSql);
                        }
                    }
                }
                if (!hasTran && insertCount == totalCount)
                {
                    this.CommitTransaction();
                }
                else if (insertCount != totalCount)
                {
                    this.RollbackTransaction();
                    throw new DataBaseException(eErrorMessageType.Error_DataBase_BatchInsert_NotAllSuccessError);
                }
                return true;
            }
            catch (RPAException ex)
            {
                if (!hasTran)
                {
                    this.RollbackTransaction();
                }
                throw ex;
            }
            catch (Exception ex)
            {
                if (!hasTran)
                {
                    this.RollbackTransaction();
                }
                //if (ex is DbException)
                //{
                //    var dbEx = new DataBaseException(eErrorMessageType.Error_DataBase_BatchInsert_Error);
                //    dbEx.CustomMessage = $"{dbEx.ErrorCodeSuffixMessage}{ex.Message}";
                //    throw dbEx;
                //}
                throw ex;
            }
        }

        public override bool BeginTransaction()
        {
            try
            {
                if (this.myTransaction != null)
                {
                    throw new DataBaseException(eErrorMessageType.Error_DataBase_Transaction_ExistsError);
                }
                if (myConnection.State != ConnectionState.Open)
                {
                    myConnection.Open();
                }
                this.myTransaction = myConnection.BeginTransaction();
                return true;
            }
            catch (RPAException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new DataBaseException(eErrorMessageType.Error_DataBase_BeginTransaction_Error, ex);
            }
        }

        public override bool CommitTransaction()
        {
            try
            {
                if (this.myTransaction == null)
                {
                    throw new DataBaseException(eErrorMessageType.Error_DataBase_Transaction_NotExistsError);
                }
                this.myTransaction.Commit();
                this.myTransaction.Dispose();
                this.myTransaction = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new DataBaseException(eErrorMessageType.Error_DataBase_BeginTransaction_Error, ex);
            }
        }

        public override bool RollbackTransaction()
        {
            try
            {
                if (this.myTransaction == null)
                {
                    return true;
                }
                this.myTransaction?.Rollback();
                this.myTransaction?.Dispose();
                this.myTransaction = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new DataBaseException(eErrorMessageType.Error_DataBase_BeginTransaction_Error, ex);
            }
        }

        private List<string> ExtractParam(ref string sql)
        {
            var result = base.ExtractParam(sql);
            sql = System.Text.RegularExpressions.Regex.Replace(sql, @"@\w+", "?");
            return result;
        }
    }
}
