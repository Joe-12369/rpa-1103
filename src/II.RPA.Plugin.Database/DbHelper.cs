using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Database.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace II.RPA.Plugin.Database
{
    public class DbHelper : INeedReset
    {
        private static ILogger logger = Logger.OfContext();

        static Dictionary<string, Database> dbList = new Dictionary<string, Database>();
        public static readonly DbHelper Instance = null;
        static DbHelper()
        {
            Instance = new DbHelper();
            NeedResetObjectManager.AddNeedResetObject(Instance);
        }
        public void Reset(bool isSamePackage)
        {
            foreach (var item in dbList)
            {
                item.Value.CloseConntection();
            }
            dbList.Clear();
        }
        /// <summary>
        /// 创建数据库对象
        /// </summary>
        /// <param name="dbtype"></param>
        /// <param name="connectStr"></param>
        /// <returns></returns>
        public bool TestDbConnection(EmDbType dbtype, string connectStr, int connectTimeOut = 30)
        {
            Database db = null;
            try
            {
                switch (dbtype)
                {
                    case EmDbType.MYSQL:
                        db = new DbMySQL();
                        break;
                    case EmDbType.ORACLA:
                        db = new DbOracle();
                        break;
                    case EmDbType.PGSQL:
                        db = new DbPgSQL();
                        break;
                    case EmDbType.SQLITE:
                        db = new DbSqlite();
                        break;
                    case EmDbType.SQLSERVER:
                        db = new DbSqlServer();
                        break;
                    case EmDbType.ACCESS:
                        db = new DbAccess();
                        break;
                    case EmDbType.DB2:
                        db = new Db2();
                        break;
                    case EmDbType.GAUSS:
                        db = new DbGauss();
                        break;
                    case EmDbType.DM:
                        db = new DbDM();
                        break;
                    default:
                        break;
                }
                db.ConnectTimeOut = connectTimeOut;
                return db.TestConnection(connectStr);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 创建数据库对象
        /// </summary>
        /// <param name="dbtype"></param>
        /// <param name="connectStr"></param>
        /// <returns></returns>
        public string CreateDbConnection(EmDbType dbtype, string connectStr, int connectTimeOut = 30)
        {
            Database db = null;
            try
            {
                switch (dbtype)
                {
                    case EmDbType.MYSQL:
                        db = new DbMySQL();
                        break;
                    case EmDbType.ORACLA:
                        db = new DbOracle();
                        break;
                    case EmDbType.PGSQL:
                        db = new DbPgSQL();
                        break;
                    case EmDbType.SQLITE:
                        db = new DbSqlite();
                        break;
                    case EmDbType.SQLSERVER:
                        db = new DbSqlServer();
                        break;
                    case EmDbType.ACCESS:
                        db = new DbAccess();
                        break;
                    case EmDbType.DB2:
                        db = new Db2();
                        break;
                    case EmDbType.GAUSS:
                        db = new DbGauss();
                        break;
                    case EmDbType.DM:
                        db = new DbDM();
                        break;
                    default:
                        break;
                }
                db.ConnectTimeOut = connectTimeOut;
                string dbID = db.CreateDbConnection(connectStr);
                AddToCache(dbID, db);
                KeyDbConnection keyDbConnection = new KeyDbConnection
                {
                    Key = dbID,
                    DbConnectionModel = new ParamCreateDbConnectionModel
                    {
                        DbType = dbtype,
                        ConnectStr = connectStr,
                    }
                };
                return keyDbConnection.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        /// <param name="connectionID"></param>
        public void CloseConnection(string connectionID)
        {
            try
            {
                if (dbList.ContainsKey(connectionID))
                {
                    Database db = dbList[connectionID];
                    db.CloseConntection();
                    dbList.Remove(connectionID);
                    return;
                }
                KeyDbConnection keyDbConnection = new KeyDbConnection(connectionID);
                if (dbList.ContainsKey(keyDbConnection.Key))
                {
                    Database db = dbList[keyDbConnection.Key];
                    db.CloseConntection();
                    dbList.Remove(keyDbConnection.Key);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 执行SQL语句
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="sql"></param>
        /// <param name="paras"></param>
        /// <returns>受影响的行数</returns>
        public int ExecuteSql(string connectionID, string sql, string paras, int cmdTimeOut)
        {
            if (!ReConnection(connectionID, out Database db))
            {
                throw new Exception("无效的数据库连接".I18n("controlType_DbHelper_F6E793B3"));
            }
            int result = 0;
            try
            {
                JArray ja = (JArray)JsonHelper.JsonDesByNewtonJson(paras);
                object[] objs = new object[0];
                if (ja != null)
                {
                    objs = new object[ja.Count];
                    for (int i = 0; i < ja.Count; i++)
                    {
                        var type = ja[i].Type;
                        switch (type)
                        {
                            case JTokenType.String:
                                objs[i] = ja[i].ToObject<string>();
                                break;
                            case JTokenType.Integer:
                                objs[i] = ja[i].ToObject<int>();
                                break;
                            case JTokenType.Float:
                                //objs[i] = ja[i].ToObject<float>();
                                objs[i] = ((JValue)ja[i]).Value;
                                break;
                            case JTokenType.Boolean:
                                objs[i] = ja[i].ToObject<bool>();
                                break;
                            case JTokenType.Bytes:
                                objs[i] = ja[i].ToObject<Byte>();
                                break;
                            default:
                                throw new Exception("不支持的入参类型".I18n("controlType_DbHelper_10DA6535"));

                        }
                    }
                }
                db.CommandTimeOut = cmdTimeOut;
                if (objs.Length > 0)
                {
                    result = db.ExecuteSql(sql, objs);
                }
                else
                {
                    result = db.ExecuteSql(sql);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int BatchExecuteSql(string connectionID, string sql, string paras, int cmdTimeOut)
        {
            if (!ReConnection(connectionID, out Database db))
            {
                throw new Exception("无效的数据库连接".I18n("controlType_DbHelper_F6E793B3"));
            }
            int result = 0;
            try
            {
                db.CommandTimeOut = cmdTimeOut;
                result = db.BatchExecuteSql(sql, paras);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        public bool BatchInsert(string connectionID, string sql, string paras)
        {
            if (!ReConnection(connectionID, out Database db))
            {
                throw new Exception("无效的数据库连接".I18n("controlType_DbHelper_F6E793B3"));
            }
            bool result = false;
            try
            {
                sql = sql.Replace("\r", "");
                string pattern = @"insert\s+into\s+(?<table>([\w|""]+\.?)+)\s?(?:\((?<column>.*?)\))?(?:\s*)?(?:values\s*\((?<values>.*)\))?";
                RegexOptions options = RegexOptions.Multiline | RegexOptions.IgnoreCase;
                var match = Regex.Match(sql, pattern, options);
                if (!match.Success)
                {
                    throw new DataBaseException(eErrorMessageType.Error_DataBase_BatchInsert_SqlFormat_Error);
                }

                var groups = match.Groups;
                string tbName = groups["table"].Value;
                string colmunsStr = groups["column"].Value;

                var inputParas = (List<object[]>)JsonHelper.JsonDesByNewtonJson<List<object[]>>(paras.Replace("'", "''"));
                if (inputParas.Count == 0)
                {
                    throw new DataBaseException(eErrorMessageType.Error_DataBase_BatchInsert_InvalidData_Error);
                }
                if (!string.IsNullOrEmpty(colmunsStr))
                {
                    colmunsStr = Regex.Replace(colmunsStr, @"\s", "");
                    if (colmunsStr.Split(',').Length != inputParas[0].Length)
                    {
                        throw new DataBaseException(eErrorMessageType.Error_DataBase_BatchInsert_ColumnsNotMatch_Error);
                    }
                }
                logger.LogDebug($"tbName：{tbName}，colmunsStr：{colmunsStr}");
                result = db.BatchInsert(tbName, colmunsStr, inputParas);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        public bool BeginTransaction(string connectionID)
        {
            if (!ReConnection(connectionID, out Database db))
            {
                throw new Exception("无效的数据库连接".I18n("controlType_DbHelper_F6E793B3"));
            }
            bool result = false;
            try
            {
                result = db.BeginTransaction();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        public bool CommitTransaction(string connectionID)
        {
            if (!ReConnection(connectionID, out Database db))
            {
                throw new Exception("无效的数据库连接".I18n("controlType_DbHelper_F6E793B3"));
            }
            bool result = false;
            try
            {
                result = db.CommitTransaction();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        public bool RollbackTransaction(string connectionID)
        {
            if (!ReConnection(connectionID, out Database db))
            {
                throw new Exception("无效的数据库连接".I18n("controlType_DbHelper_F6E793B3"));
            }
            bool result = false;
            try
            {
                result = db.RollbackTransaction();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        public string QuerySingle(string connectionID, string sql, string paras, bool showdic, int cmdTimeOut)
        {
            if (!ReConnection(connectionID, out Database db))
            {
                throw new Exception("无效的数据库连接".I18n("controlType_DbHelper_F6E793B3"));
            }
            try
            {
                JArray ja = (JArray)JsonHelper.JsonDesByNewtonJson(paras);
                object[] objs = new object[0];
                if (ja != null)
                {
                    objs = new object[ja.Count];
                    for (int i = 0; i < ja.Count; i++)
                    {
                        var type = ja[i].Type;
                        switch (type)
                        {
                            case JTokenType.String:
                                objs[i] = ja[i].ToObject<string>();
                                break;
                            case JTokenType.Integer:
                                objs[i] = ja[i].ToObject<int>();
                                break;
                            case JTokenType.Float:
                                //objs[i] = ja[i].ToObject<float>();
                                objs[i] = ((JValue)ja[i]).Value;
                                break;
                            case JTokenType.Boolean:
                                objs[i] = ja[i].ToObject<bool>();
                                break;
                            case JTokenType.Bytes:
                                objs[i] = ja[i].ToObject<Byte>();
                                break;
                            default:
                                throw new Exception("不支持的入参类型".I18n("controlType_DbHelper_10DA6535"));
                        }
                    }
                }
                DataSet ds = null;
                db.CommandTimeOut = cmdTimeOut;
                ds = db.Query(sql, objs);
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        DataRow dr = ds.Tables[0].Rows[0];
                        if (showdic)
                        {
                            JObject j = new JObject();
                            foreach (var col in ds.Tables[0].Columns)
                            {
                                DataColumn dc = col as DataColumn;
                                if (dr[dc.ColumnName] is int)
                                {
                                    j.Add(dc.ColumnName, int.Parse(dr[dc.ColumnName].ToString()));
                                }
                                else if (dr[dc.ColumnName] is double)
                                {
                                    j.Add(dc.ColumnName, double.Parse(dr[dc.ColumnName].ToString()));
                                }
                                else
                                {
                                    j.Add(dc.ColumnName, dr[dc.ColumnName].ToString());
                                }
                            }
                            return j.ToString();
                        }
                        else
                        {
                            object[] rstObjs = new object[ds.Tables[0].Columns.Count];
                            for (int i = 0; i < ds.Tables[0].Columns.Count; i++)
                            {
                                if (dr[i] is int)
                                {
                                    rstObjs[i] = int.Parse(dr[i].ToString());
                                }
                                else if (dr[i] is double)
                                {
                                    rstObjs[i] = double.Parse(dr[i].ToString());
                                }
                                else
                                {
                                    rstObjs[i] = dr[i].ToString();
                                }
                            }
                            return JsonHelper.JsonByNewtonJson(rstObjs);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return string.Empty;
        }
        public string QueryAll(string connectionID, string sql, string paras, bool showdic, int cmdTimeOut)
        {
            if (!ReConnection(connectionID, out Database db))
            {
                throw new Exception("无效的数据库连接".I18n("controlType_DbHelper_F6E793B3"));
            }
            try
            {
                JArray ja = (JArray)JsonHelper.JsonDesByNewtonJson(paras);
                object[] objs = new object[0];
                if (ja != null)
                {
                    objs = new object[ja.Count];
                    for (int i = 0; i < ja.Count; i++)
                    {
                        var type = ja[i].Type;
                        switch (type)
                        {
                            case JTokenType.String:
                                objs[i] = ja[i].ToObject<string>();
                                break;
                            case JTokenType.Integer:
                                objs[i] = ja[i].ToObject<int>();
                                break;
                            case JTokenType.Float:
                                //objs[i] = ja[i].ToObject<float>();
                                objs[i] = ((JValue)ja[i]).Value;
                                break;
                            case JTokenType.Boolean:
                                objs[i] = ja[i].ToObject<bool>();
                                break;
                            case JTokenType.Bytes:
                                objs[i] = ja[i].ToObject<Byte>();
                                break;
                            default:
                                throw new Exception("不支持的入参类型".I18n("controlType_DbHelper_10DA6535"));
                        }
                    }
                }
                DataSet ds = null;
                db.CommandTimeOut = cmdTimeOut;
                ds = db.Query(sql, objs);
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        List<JObject> jList = new List<JObject>();
                        List<Object[]> objList = new List<object[]>();
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            if (showdic)
                            {
                                JObject j = new JObject();
                                foreach (var col in ds.Tables[0].Columns)
                                {
                                    DataColumn dc = col as DataColumn;
                                    if (dr[dc.ColumnName] is int)
                                    {
                                        j.Add(dc.ColumnName, int.Parse(dr[dc.ColumnName].ToString()));
                                    }
                                    else if (dr[dc.ColumnName] is double)
                                    {
                                        j.Add(dc.ColumnName, double.Parse(dr[dc.ColumnName].ToString()));
                                    }
                                    else
                                    {
                                        j.Add(dc.ColumnName, dr[dc.ColumnName].ToString());
                                    }
                                }
                                jList.Add(j);
                            }
                            else
                            {
                                object[] rstObjs = new object[ds.Tables[0].Columns.Count];
                                for (int i = 0; i < ds.Tables[0].Columns.Count; i++)
                                {
                                    if (dr[i] is int)
                                    {
                                        rstObjs[i] = int.Parse(dr[i].ToString());
                                    }
                                    else if (dr[i] is double)
                                    {
                                        rstObjs[i] = double.Parse(dr[i].ToString());
                                    }
                                    else
                                    {
                                        rstObjs[i] = dr[i].ToString();
                                    }
                                }
                                //return JsonHelper.JsonByNewtonJson(rstObjs);
                                objList.Add(rstObjs);
                            }
                        }
                        if (showdic)
                        {
                            return JsonHelper.JsonByNewtonJson(jList);
                        }
                        else
                        {
                            return JsonHelper.JsonByNewtonJson(objList);
                        }
                    }
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void AddToCache(string id, Database db)
        {
            dbList.Add(id, db);
        }

        private bool ReConnection(string connectStr, out Database db)
        {
            db = null;
            if (dbList.ContainsKey(connectStr))
            {
                db = dbList[connectStr];
                return true;
            }
            KeyDbConnection keyDbConnection = new KeyDbConnection(connectStr);
            if (dbList.ContainsKey(keyDbConnection.Key))
            {
                db = dbList[keyDbConnection.Key];
                return true;
            }
            if (keyDbConnection.DbConnectStatus == EmDbConnectStatus.Closed)
            {
                //数据库已关闭连接
                return false;
            }
            if (keyDbConnection.DbConnectionModel != null)
            {
                switch (keyDbConnection.DbConnectionModel.DbType)
                {
                    case EmDbType.MYSQL:
                        db = new DbMySQL();
                        break;
                    case EmDbType.ORACLA:
                        db = new DbOracle();
                        break;
                    case EmDbType.PGSQL:
                        db = new DbPgSQL();
                        break;
                    case EmDbType.SQLITE:
                        db = new DbSqlite();
                        break;
                    case EmDbType.SQLSERVER:
                        db = new DbSqlServer();
                        break;
                    case EmDbType.ACCESS:
                        db = new DbAccess();
                        break;
                    case EmDbType.DB2:
                        db = new Db2();
                        break;
                    default:
                        throw new Exception("未知的数据库类型".I18n("controlType_DbHelper_2D370725"));
                }
                int timeOut = keyDbConnection.DbConnectionModel.RunParam.RemainTime == -1 || keyDbConnection.DbConnectionModel.RunParam.RemainTime == 0 ? 30 :
                              keyDbConnection.DbConnectionModel.RunParam.RemainTime / 1000;
                db.ConnectTimeOut = timeOut;
                db.CreateDbConnection(keyDbConnection.DbConnectionModel.ConnectStr);
                AddToCache(keyDbConnection.Key, db);
                return true;
            }
            else
            {
                return false;
            }
        }


    }
}
