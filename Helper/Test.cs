using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Diagnostics;
#if DEBUG
namespace AutoORMCore.Helper
{
    class Test : IHelper
    {
        public Test(string constr)
        {
            Debug.WriteLine(constr);
        }
        void SetCmd(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
            Debug.WriteLine($"SQL:{str},Issp:{issp}");
            foreach (var n in ps) { Debug.WriteLine($"{n.ParameterName}:{n.Value}"); }
        }
        void CloseCon() {}
        void OpenCon() {}
        int IHelper.ExecuteQuery(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
            SetCmd(str, ps, issp);
            return 0;
        }
        object IHelper.ExectueScalar(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
                SetCmd(str, ps, issp);
                return null;
        }
        DataTable IHelper.GetDataTable(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
            Debug.WriteLine($"SQL:{str},Issp:{issp}");
            foreach (var n in ps) { Debug.WriteLine($"{n.ParameterName}:{n.Value}"); }
            return null;
        }
        IDataReader IHelper.ExectueReader(string str, IEnumerable<IDataParameter> ps, bool issp)
        {
            SetCmd(str, ps, issp);
            return null;
        }
        void IHelper.BegTran(IsolationLevel level) {}
        void IHelper.Commit() {  }
        void IHelper.RollBack() { }
        void IHelper.Import(DbDataReader rd, string tableName, Action<Object> act)
        {
            return;
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
            var sql=sb.ToString();
           // Debug.WriteLine(sql);
            return sql;
        }
        IDataParameter IHelper.Cp(string name, object value) { return new SqlParameter(name, value); }
        string IHelper.ParStr(string name) { return "@" + name; }
        string IHelper.GetIdStr { get { return "SELECT @@IDENTITY"; } }
        void IDisposable.Dispose()
        {
        }
        public string Quote(string name) { return "[" + name + "]"; }
    }
}
#endif
