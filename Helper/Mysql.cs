using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using AutoORMCore;
using System.Data.Common;

namespace AutoORMCore.Helper
{
    class Mysql:IHelper
    {
        MySqlConnection con;
        MySqlCommand cmd;
        MySqlTransaction tran;
        public Mysql(string str)
        {
            con = new MySqlConnection(str);
            cmd = new MySqlCommand();
            cmd.Connection = con;
        }
        void SetCmd(string txt, IEnumerable<IDataParameter> ps, bool issp)
        {
            cmd.CommandText = txt;
            cmd.CommandType = issp ? CommandType.StoredProcedure : CommandType.Text;
            cmd.Parameters.Clear();
            if (ps != null) { cmd.Parameters.AddRange(ps.ToArray()); }
        }
        void Open()
        {
            if(con.State==ConnectionState.Closed){con.Open();}
        }
        void Close()
        {
            if(con.State==ConnectionState.Open){con.Close();}
        }
        public int ExecuteQuery(string txt, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetCmd(txt, ps, issp);
                Open();
                return cmd.ExecuteNonQuery();
            }
            finally{ if (tran == null) { Close(); } }
        }
        public object ExectueScalar(string tex, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetCmd(tex, ps, issp);
                Open();
                return cmd.ExecuteScalar();
            }
            finally { if (tran == null) { Close(); } }
        }
        public IDataReader ExectueReader(string tex, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetCmd(tex, ps, issp);
                Open();
                return cmd.ExecuteReader(tran == null ? CommandBehavior.CloseConnection : CommandBehavior.Default);
            }
            catch { if (tran == null) { Close(); } throw; }
        }
        public void BegTran(IsolationLevel level)
        {
            Open();
            tran = level != IsolationLevel.Unspecified ? con.BeginTransaction(level) : con.BeginTransaction();
            cmd.Transaction = tran;
        }
        public void RollBack()
        {
            if (tran != null)
            {
                tran.Rollback();
                tran = null;
            }
            Close();
        }
        public void Commit()
        {
            if (tran != null)
            {
                tran.Commit();
                tran = null;
            }
            Close();
        }
        public void Import(DbDataReader rd, string tableName, Action<object> act)
        {
            throw new Exception("此方法不存在");
        }
        public DataTable GetDataTable(string txt, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetCmd(txt, ps, issp);
                var ad = new DbDataAdapter(cmd);
                var dt = new DataTable();
                Open();
                //ad.FillSchema(dt, SchemaType.Mapped);
                ad.Fill(dt);
                return dt;
            }
            finally { Close(); }
        }
        public string CreateSql(string select, string tbname, string where, string orderby, int size, int index)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT " + select + " FROM " + tbname);
            if (!string.IsNullOrEmpty(where))
            {
                sb.Append(" WHERE " + where);
            }
            if (!string.IsNullOrEmpty(orderby))
            {
                sb.Append(" ORDER BY " + orderby);
            }
            if (index >= 1)
            {
                sb.Append(" LIMIT " +(index==1?0:(index-1) * size) + "," + size); 
            }
            return sb.ToString();
        }
        public IDataParameter Cp(string name, object value)
        {
            //MySqlDbType t=MySqlDbType.VarChar;
            //var tp=value.GetType();
            //if(tp==typeof(int)){t=MySqlDbType.Int32;}
            //else if(tp==typeof(float)){t=MySqlDbType.Float;}
            //else if(tp==typeof(double)){t=MySqlDbType.Double;}
            //var p=new MySqlParameter(name,value);
            //p.Value = value;
            return new MySqlParameter(name, value);
        }
        public string GetIdStr { get { return ";SELECT LAST_INSERT_ID()"; } }
        public string ParStr(string name)
        {
            return "?" + name;
        }
        void IDisposable.Dispose()
        {
            con.Dispose();
            cmd.Dispose();
            //tran.Dispose();
        }
        public string Quote(string name) { return "`" + name + "`"; }
    }
}
