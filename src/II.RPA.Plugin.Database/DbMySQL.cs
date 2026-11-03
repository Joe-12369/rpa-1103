using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.WinAutomation.CommonUtility;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Database
{
    public class DbMySQL : Database
    {
        private static ILogger logger = Logger.OfContext();
        public MySqlConnection myConnection;

        MySqlTransaction myTransaction = null;

        public override bool TestConnection(string dbConnectStr)
        {
            bool canConnectioned = false;
            DbServerConnStr conStr = JsonHelper.JsonDesByNewtonJson<DbServerConnStr>(dbConnectStr);
            string dbStr = string.Format("server={0};port={1};database={2};user id={3};password={4};persistsecurityinfo=False;charset={5};SslMode=Preferred;Connection Timeout={6};", conStr.DataSource, conStr.Port, conStr.DbName, conStr.UserName, conStr.Pwd, conStr.Charset, this.ConnectTimeOut);
            myConnection = new MySqlConnection(dbStr);
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
                string dbStr = string.Format("server={0};port={1};database={2};user id={3};password={4};persistsecurityinfo=False;charset={5};SslMode=Preferred;Connection Timeout={6};", conStr.DataSource, conStr.Port, conStr.DbName, conStr.UserName, conStr.Pwd, conStr.Charset, this.ConnectTimeOut);
                myConnection = new MySqlConnection(dbStr);
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
            using (MySqlCommand cmd = new MySqlCommand())
            {
                try
                {
                    if (cmdParams.Length > 0)
                    {
                        MySqlParameter[] paras = new MySqlParameter[cmdParams.Length];
                        int index = 0;
                        List<string> pas = ExtractParam(sqlString);
                        if (pas.Count != cmdParams.Length)
                        {
                            throw new Exception("SQL语句参数错误".I18n("controlType_DbMySQL_D19805D3"));
                        }
                        foreach (var itm in cmdParams)
                        {
                            paras[index] = new MySqlParameter(pas[index], itm);
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
                    MySqlDataReader myReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (cmdParams.Length > 0)
                    {
                        cmd.Parameters.Clear();
                    }
                    return myReader;
                }
                catch (MySqlException e)
                {
                    throw e;
                }
            }
        }

        public override int ExecuteSql(string sqlString, params object[] cmdParams)
        {
            bool hasTran = (this.myTransaction != null);
            using (MySqlCommand cmd = new MySqlCommand())
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
                        MySqlParameter[] paras = new MySqlParameter[cmdParams.Length];
                        int index = 0;
                        List<string> pas = ExtractParam(sqlString);
                        if (pas.Count != cmdParams.Length)
                        {
                            throw new Exception("SQL语句参数错误".I18n("controlType_DbMySQL_D19805D3"));
                        }
                        foreach (var itm in cmdParams)
                        {
                            paras[index] = new MySqlParameter(pas[index], itm);
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
                catch (MySqlException e)
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
            using (MySqlCommand cmd = new MySqlCommand())
            {
                try
                {
                    cmd.CommandTimeout = CommandTimeOut;
                    if (cmdParams.Length > 0)
                    {
                        MySqlParameter[] paras = new MySqlParameter[cmdParams.Length];
                        int index = 0;
                        List<string> pas = ExtractParam(sqlString);
                        if (pas.Count != cmdParams.Length)
                        {
                            throw new Exception("SQL语句参数错误".I18n("controlType_DbMySQL_D19805D3"));
                        }
                        foreach (var itm in cmdParams)
                        {
                            paras[index] = new MySqlParameter(pas[index], itm);
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
                catch (MySqlException e)
                {
                    throw e;
                }
            }
        }
        public override DataSet Query(string sqlString, params object[] cmdParams)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandTimeout = CommandTimeOut;
                MySqlDataAdapter da;
                if (cmdParams.Length > 0)
                {
                    MySqlParameter[] paras = new MySqlParameter[cmdParams.Length];
                    int index = 0;
                    List<string> pas = ExtractParam(sqlString);
                    if (pas.Count != cmdParams.Length)
                    {
                        throw new Exception("SQL语句参数错误".I18n("controlType_DbMySQL_D19805D3"));
                    }
                    foreach (var itm in cmdParams)
                    {
                        paras[index] = new MySqlParameter(pas[index], itm);
                        index++;
                    }
                    PrepareCommand(cmd, myConnection, null, sqlString, paras);
                    da = new MySqlDataAdapter(cmd);
                }
                else
                {
                    PrepareCommand(cmd, myConnection, null, sqlString, null);
                    da = new MySqlDataAdapter(cmd);
                    //da = new MySqlDataAdapter(sqlString, myConnection);
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
                    catch (System.Data.SqlClient.SqlException ex)
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
                inputParas = JsonHelper.JsonDesByNewtonJson<List<object[]>>(paras);
            }
            catch (Exception ex)
            {
                throw new Exception("SQL语句参数错误".I18n("controlType_DbMySQL_D19805D3"));
            }
            bool hasTran = (this.myTransaction != null);
            int count = 0;
            using (MySqlCommand cmd = new MySqlCommand())
            {
                try
                {
                    cmd.CommandTimeout = CommandTimeOut;
                    if (inputParas.Count > 0)
                    {

                        //MySqlTransaction tx = myConnection.BeginTransaction();
                        try
                        {
                            //cmd.Transaction = tx;
                            if (!hasTran)
                            {
                                this.BeginTransaction();
                            }
                            cmd.Transaction = this.myTransaction;
                            for (int i = 0; i < inputParas.Count; i++)
                            {
                                object[] objs = inputParas[i];
                                MySqlParameter[] sqlparas = new MySqlParameter[objs.Length];
                                int index = 0;
                                List<string> pas = ExtractParam(sqlString);
                                if (pas.Count != objs.Length)
                                {
                                    throw new Exception("SQL语句参数错误".I18n("controlType_DbMySQL_D19805D3"));
                                }
                                foreach (var itm in objs)
                                {
                                    sqlparas[index] = new MySqlParameter(pas[index], itm);
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
                            //tx.Commit();
                        }
                        catch (Exception e)
                        {
                            if (!hasTran)
                            {
                                this.RollbackTransaction();
                            }
                            //tx.Rollback();
                            throw e;
                        }
                    }
                    else
                    {
                        cmd.Connection = myConnection;
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
                using (MySqlCommand cmd = new MySqlCommand())
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

                            sb.Append($"'{colum}'");

                            if (j < row.Length - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append("),");
                        if (useCount % 500 == 0 || useCount == totalCount)
                        {
                            logger.LogTrace("即将执行一次");
                            sb.Length--;
                            cmd.CommandText = sb.ToString();
                            insertCount += cmd.ExecuteNonQuery();
                            logger.LogTrace("执行结束，准备下一次执行的数据");
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
                //if (ex is MySqlException)
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
    }
}
