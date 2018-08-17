using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Data.Common;
using AutoORMCore;

namespace AutoORMCore.Helper
{
    /// <summary>
    /// 不支持mars的mssql
    /// </summary>
    class Mssql:IHelper
    {
        SqlCommand cmd;
        SqlConnection con;
        SqlTransaction tran;
        public Mssql(string constr)
        {
            con = new SqlConnection(constr);//test
            cmd = new SqlCommand();
            cmd.Connection = con;
        }
        void SetCmd(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
            cmd.CommandText = str;
            cmd.CommandType = issp ? CommandType.StoredProcedure : CommandType.Text;
            cmd.Parameters.Clear();
            foreach (var n in ps) { cmd.Parameters.Add(n); }
        }
        void CloseCon(){if (con.State == ConnectionState.Open) { con.Close(); }}
        void OpenCon() { if (con.State == ConnectionState.Closed) { con.Open(); } }
        int IHelper.ExecuteQuery(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetCmd(str, ps, issp);
                OpenCon();
                return cmd.ExecuteNonQuery();
            }
            finally{if(tran==null){CloseCon();}}
        }
        object IHelper.ExectueScalar(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetCmd(str, ps, issp);
                OpenCon();
                return cmd.ExecuteScalar();
            }
            finally{if(tran==null){CloseCon();}}
        }
        DataTable IHelper.GetDataTable(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                var adapter = new DbDataAdapter(str, con);
                var dt = new DataTable();
                //adapter.FillSchema(dt, SchemaType.Mapped);
                adapter.Fill(dt);
                return dt;
            }
            finally { CloseCon(); }
        }
        IDataReader IHelper.ExectueReader(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetCmd(str, ps, issp);
                OpenCon();
                return cmd.ExecuteReader(tran == null ? CommandBehavior.CloseConnection : CommandBehavior.Default);
            }
            catch { if (tran == null) { CloseCon(); } throw; }
        }
        void IHelper.BegTran(IsolationLevel level)
        {
            OpenCon();
            tran = level == IsolationLevel.Unspecified ? con.BeginTransaction(level) : con.BeginTransaction();
            cmd.Transaction = tran;
        }
        void IHelper.Commit() { if (tran != null) { tran.Commit(); tran = null; } CloseCon(); }
        void IHelper.RollBack() { if (tran != null) { tran.Rollback();tran = null; } CloseCon(); }
        void IHelper.Import(DbDataReader rd, string tableName,Action<Object> act)
        {
            try
            {
                SqlBulkCopy bc = tran != null ? new SqlBulkCopy(con, SqlBulkCopyOptions.UseInternalTransaction, tran) : new SqlBulkCopy(con);
                OpenCon();
                bc.DestinationTableName = tableName;
                if (act == null)
                {
                    for (var i=0;i<rd.FieldCount;i++)
                    {
                        bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(rd.GetName(i), rd.GetName(i)));
                    }
                }
                else
                {
                    act(bc);
                }
                bc.WriteToServer(rd);
            }
            finally { if (tran == null) { CloseCon(); } }
        }
        string IHelper.CreateSql(string select, string tbname, string where, string orderby, int size, int index)
        {
            StringBuilder sb = new StringBuilder();
            int i = 1;
            sb.Append("SELECT ");
            if (index == 1) { sb.Append("TOP " + size + " "); }
            sb.Append(select+" FROM ");
            if (index > 1){sb.Append("(SELECT ROW_NUMBER() OVER(" + (!string.IsNullOrEmpty(orderby)?"ORDER BY "+orderby:string.Empty) + ") AS ROWID," + select + " FROM ");}
            sb.Append(tbname);
            sb.Append(!string.IsNullOrEmpty(where) ? " WHERE " + where : string.Empty);
            if (index > 1)
            {
                sb.Append(") AS T" + i.ToString("000") + " WHERE ROWID BETWEEN " + ((index - 1) * size + 1) + " AND " + size * index);
            }
            else
            {
                sb.Append(!string.IsNullOrEmpty(orderby) ? " ORDER BY " + orderby : string.Empty);
            }
            return sb.ToString();
        }
        IDataParameter IHelper.Cp(string name, object value) { return new SqlParameter(name, value); }
        string IHelper.ParStr(string name) { return "@"+name;}
        string IHelper.GetIdStr { get { return "SELECT @@IDENTITY"; } }
        void IDisposable.Dispose()
        {
            CloseCon(); con.Dispose(); cmd.Dispose();
        }
        public string Quote(string name) { return "[" + name + "]"; }
    }
}
