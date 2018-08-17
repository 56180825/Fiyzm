using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;

namespace AutoORMCore.Helper
{
    public class CommHelper:IHelper
    {
        IDbConnection con;
        IDbCommand cmd;
        IDbTransaction tran;
        IDbFunc func;
        public CommHelper(IDbFunc cn)
        {
            func = cn;
            con = func.CreateConnect();
            cmd = con.CreateCommand();
        }
        void SetPar(string txt, IEnumerable<IDataParameter> ps, bool issp)
        {
            cmd.CommandText = txt;
            cmd.CommandType = issp ? CommandType.StoredProcedure : CommandType.Text;
            cmd.Parameters.Clear();
            if (ps == null) { return; }
            foreach (var n in ps)
            {
                cmd.Parameters.Add(n);
            }
        }
        void Open() { if (con.State == ConnectionState.Closed) { con.Open(); } }
        void Close() { if (con.State == ConnectionState.Open) { con.Close(); } }
        public int ExecuteQuery(string txt, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetPar(txt, ps, issp);
                Open();
                return cmd.ExecuteNonQuery();
            }
            finally { if (tran == null) { Close(); } }
        }
        public object ExectueScalar(string tex, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetPar(tex, ps, issp);
                Open();
                return cmd.ExecuteScalar();
            }
            finally { if (tran == null) { Close(); } }
        }
        public IDataReader ExectueReader(string tex, IEnumerable<IDataParameter> ps, bool issp)
        {
            try
            {
                SetPar(tex, ps, issp);
                Open();
                return cmd.ExecuteReader(tran==null?CommandBehavior.CloseConnection:CommandBehavior.Default);
            }
            catch { if (tran == null) { Close(); } throw; }
        }
        public void BegTran(IsolationLevel level) {
            Open();
            tran = level==IsolationLevel.Unspecified ? con.BeginTransaction(level) : con.BeginTransaction();
            cmd.Transaction = tran;
        }
        public void RollBack() { if (tran != null) { tran.Rollback(); tran = null; } Close(); }
        public void Commit() { if (tran != null) { tran.Commit(); tran = null; } Close(); }
        public void Import(DbDataReader dt, string tableName, Action<object> act)
        {
            throw new Exception("方式不支持");
        }
        public DataTable GetDataTable(string txt, IEnumerable<IDataParameter> ps, bool issp)
        {
            throw new Exception("方式不支持");
        }
        public string CreateSql(string select, string tbname, string where, string orderby, int size, int index)
        {
            return func.CreateSql(select,tbname,where,orderby,size,index);
        }
        public IDataParameter Cp(string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            return p;
        }
        public string GetIdStr { get{return func.GetIdStr;}}
        public string ParStr(string name){return func.ParStr(name);}
        public string Quote(string name){return func.Quote(name);}
        public void Dispose()
        {
            Close(); con.Dispose(); cmd.Dispose();
        }
    }
}
