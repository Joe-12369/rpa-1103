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
    public abstract class Database
    {
        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="dbConnectStr"></param>
        /// <returns></returns>
        public abstract bool TestConnection(string dbConnectStr);
        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <param name="dbConnectStr"></param>
        /// <returns></returns>
        public abstract string CreateDbConnection(string dbConnectStr);
        /// <summary>
        /// 执行sql语句，返回受影响行数
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public abstract int ExecuteSql(string sqlString, params object[] cmdParams);
        /// <summary>
        /// 批量执行SQL ,执行事务
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public abstract int BatchExecuteSql(string sqlString, string paras);
        /// <summary>
        /// 执行一条计算查询语句，返回查询结果
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="cmdParams"></param>
        /// <returns></returns>
        public abstract object GetSingle(string sqlString, params object[] cmdParams);
        /// <summary>
        /// 执行查询语句，返回SqlDataReader
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        public abstract DbDataReader ExecuteReader(string sqlString, params object[] cmdParams);
        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        public abstract DataSet Query(string sqlString, params object[] cmdParams);
        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        public abstract bool CloseConntection();
        /// <summary>
        /// 批量插入
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="colmunNameStr">列名字符串，逗号分隔</param>
        /// <param name="paras">插入的数据</param>
        /// <returns></returns>
        public abstract bool BatchInsert(string tableName, string colmunNameStr, List<object[]> paras);
        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        public abstract bool BeginTransaction();
        /// <summary>
        /// 提交事务
        /// </summary>
        /// <returns></returns>
        public abstract bool CommitTransaction();
        /// <summary>
        /// 回滚事务
        /// </summary>
        /// <returns></returns>
        public abstract bool RollbackTransaction();

        public int ConnectTimeOut { get; set; }

        public int CommandTimeOut { get; set; }

        protected List<string> ExtractParam(string sql)
        {
            List<string> list = new List<string>();
            System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(sql, @"[@:]\w+");
            foreach (var item in mc)
            {
                list.Add(item.ToString().TrimStart('@', ':'));
            }
            return list;
        }

        protected void PrepareCommand(DbCommand cmd, DbConnection conn, DbTransaction trans, string cmdText, DbParameter[] cmdParms)
        {
            trans = trans ?? cmd.Transaction;
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = CommandType.Text;
            if (cmdParms != null)
            {
                foreach (var parameter in cmdParms)
                {
                    if ((parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Input) &&
                        (parameter.Value == null))
                    {
                        parameter.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(parameter);
                }
            }
        }
    }

    public enum EmDbType
    {
        MYSQL,
        SQLSERVER,
        ORACLA,
        PGSQL,
        SQLITE,
        ACCESS,
        DB2,
        GAUSS,
        DM
    }
    public enum EmDbConnectStatus
    {
        Open,
        Closed
    }
}
