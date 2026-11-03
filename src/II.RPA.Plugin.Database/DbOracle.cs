using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.WinAutomation.CommonUtility;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Database
{
    public class DbOracle : Database
    {
        private static ILogger logger = Logger.OfContext();
        OracleConnection myConnection;
        OracleTransaction myTransaction = null;

        public override bool TestConnection(string dbConnectStr)
        {
            bool canConnectioned = false;
            DbServerConnStr conStr = JsonHelper.JsonDesByNewtonJson<DbServerConnStr>(dbConnectStr);
            string dbStr = string.Format(@"Data Source = (DESCRIPTION =(ADDRESS_LIST =
                                                                      (ADDRESS = (PROTOCOL = TCP)(HOST = {0})(PORT = {1}))
                                                                   )
                                                                    (CONNECT_DATA =
                                                                      ({2})
                                                                      (SERVER = DEDICATED)
                                                                    )
                                                                  ); User Id = {3}; Password ={4};", conStr.DataSource, conStr.Port, conStr.ServerType.ToString() + " = " + conStr.Sid, conStr.UserName, conStr.Pwd);
            myConnection = new OracleConnection(dbStr);
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
                string dbStr = string.Format(@"Data Source = (DESCRIPTION =(ADDRESS_LIST =
                                                                      (ADDRESS = (PROTOCOL = TCP)(HOST = {0})(PORT = {1}))
                                                                   )
                                                                    (CONNECT_DATA =
                                                                      ({2})
                                                                      (SERVER = DEDICATED)
                                                                    )
                                                                  ); User Id = {3}; Password ={4};", conStr.DataSource, conStr.Port, conStr.ServerType.ToString() + " = " + conStr.Sid, conStr.UserName, conStr.Pwd);
                myConnection = new OracleConnection(dbStr);
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
            using (OracleCommand cmd = new OracleCommand())
            {
                try
                {
                    if (cmdParams.Length > 0)
                    {
                        OracleParameter[] paras = new OracleParameter[cmdParams.Length];
                        int index = 0;
                        List<string> pas = ExtractParam(sqlString);
                        if (pas.Count != cmdParams.Length)
                        {
                            throw new Exception("SQL语句参数错误".I18n("controlType_DbOracle_D19805D3"));
                        }
                        foreach (var itm in cmdParams)
                        {
                            paras[index] = new OracleParameter(pas[index], itm);
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
                    OracleDataReader myReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (cmdParams.Length > 0)
                    {
                        cmd.Parameters.Clear();
                    }
                    return myReader;
                }
                catch (OracleException e)
                {
                    throw e;
                }
            }
        }

        public override int ExecuteSql(string sqlString, params object[] cmdParams)
        {
            bool hasTran = (this.myTransaction != null);
            using (OracleCommand cmd = new OracleCommand())
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
                        OracleParameter[] paras = new OracleParameter[cmdParams.Length];
                        int index = 0;
                        List<string> pas = ExtractParam(sqlString);
                        if (pas.Count != cmdParams.Length)
                        {
                            throw new Exception("SQL语句参数错误".I18n("controlType_DbOracle_D19805D3"));
                        }
                        foreach (var itm in cmdParams)
                        {
                            paras[index] = new OracleParameter(pas[index], itm);
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
                catch (OracleException e)
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
            using (OracleCommand cmd = new OracleCommand())
            {
                try
                {
                    cmd.CommandTimeout = this.CommandTimeOut;
                    if (cmdParams.Length > 0)
                    {
                        OracleParameter[] paras = new OracleParameter[cmdParams.Length];
                        int index = 0;
                        List<string> pas = ExtractParam(sqlString);
                        if (pas.Count != cmdParams.Length)
                        {
                            throw new Exception("SQL语句参数错误".I18n("controlType_DbOracle_D19805D3"));
                        }
                        foreach (var itm in cmdParams)
                        {
                            paras[index] = new OracleParameter(pas[index], itm);
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
                catch (OracleException e)
                {
                    throw e;
                }
            }
        }
        public override DataSet Query(string sqlString, params object[] cmdParams)
        {
            using (OracleCommand cmd = new OracleCommand())
            {
                cmd.CommandTimeout = this.CommandTimeOut;
                OracleDataAdapter da;
                if (cmdParams.Length > 0)
                {
                    OracleParameter[] sqlparas = new OracleParameter[cmdParams.Length];
                    int index = 0;
                    List<string> pas = ExtractParam(sqlString);
                    if (pas.Count != cmdParams.Length)
                    {
                        throw new Exception("SQL语句参数错误".I18n("controlType_DbOracle_D19805D3"));
                    }
                    foreach (var itm in cmdParams)
                    {
                        sqlparas[index] = new OracleParameter(pas[index], itm);
                        index++;
                    }
                    PrepareCommand(cmd, myConnection, null, sqlString, sqlparas);
                    da = new OracleDataAdapter(cmd);
                }
                else
                {
                    PrepareCommand(cmd, myConnection, null, sqlString, null);
                    da = new OracleDataAdapter(cmd);
                    //da = new OracleDataAdapter(sqlString, myConnection);
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
                    catch (OracleException ex)
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
            catch (Exception)
            {
                throw new Exception("SQL语句参数错误".I18n("controlType_DbOracle_D19805D3"));
            }
            bool hasTran = (this.myTransaction != null);
            int count = 0;
            using (OracleCommand cmd = new OracleCommand())
            {
                try
                {
                    cmd.CommandTimeout = this.CommandTimeOut;
                    if (inputParas.Count > 0)
                    {
                        //OracleTransaction tx = myConnection.BeginTransaction();
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
                                OracleParameter[] sqlparas = new OracleParameter[objs.Length];
                                int index = 0;
                                List<string> pas = ExtractParam(sqlString);
                                if (pas.Count != objs.Length)
                                {
                                    throw new Exception("SQL语句参数错误".I18n("controlType_DbOracle_D19805D3"));
                                }
                                foreach (var itm in objs)
                                {
                                    sqlparas[index] = new OracleParameter(pas[index], itm);
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
                            //tx.Rollback();
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
                List<string> colnumNameList = new List<string>();
                string columnPartStr = string.Empty;//insert into tbname后的（列1，列2.。。）
                if (string.IsNullOrEmpty(colmunNameStr))//默认列名在，在拼接select时使用
                {
                    for (int i = 0; i < paras[0].Length; i++)
                    {

                        colnumNameList.Add($"colnum_{i}");
                    }
                }
                else
                {
                    colnumNameList.AddRange(colmunNameStr.Split(','));
                    columnPartStr = $"({string.Join(",", colnumNameList)})";
                }
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = myConnection;
                    if (!hasTran)
                    {
                        this.BeginTransaction();
                    }
                    cmd.Transaction = this.myTransaction;
                    Type _datatype = typeof(DateTime);
                    object colum;
                    //string insertSql_tmp = $@"INSERT INTO {tableName}{columnPartStr} SELECT rpatemptb.* FROM({"{0}"}) rpatemptb";

                    StringBuilder sb = new StringBuilder();
                    foreach (object[] row in paras)
                    {
                        useCount++;
                        sb.Append("SELECT ");
                        for (int j = 0; j < row.Length; j++)
                        {
                            colum = row[j];
                            sb.Append($"'{colum}' {colnumNameList[j]}");
                            if (j < row.Length - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append(" FROM DUAL ");
                        if (useCount % 500 == 0 || useCount == totalCount)
                        {
                            cmd.CommandText = $@"INSERT INTO {tableName}{columnPartStr} SELECT rpatemptb.* FROM({sb.ToString()}) rpatemptb";
                            insertCount += cmd.ExecuteNonQuery();
                            sb.Clear();

                        }
                        else
                        {
                            sb.Append(" UNION ALL ");
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
    }
}
