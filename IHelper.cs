using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;

namespace AutoORMCore
{
    internal interface IHelper:IDisposable
    {
        int ExecuteQuery(string txt, IEnumerable<IDataParameter> ps, bool issp);
        object ExectueScalar(string tex, IEnumerable<IDataParameter> ps, bool issp);
        IDataReader ExectueReader(string tex, IEnumerable<IDataParameter> ps, bool issp);
        void BegTran(IsolationLevel level= IsolationLevel.Unspecified);
        void RollBack();
        void Commit();
        void Import(DbDataReader dt, string tableName,Action<object> act);
        DataTable GetDataTable(string txt, IEnumerable<IDataParameter> ps, bool issp);
        string CreateSql(string select, string tbname, string where, string orderby, int size, int index);
        IDataParameter Cp(string name, object value);
        string GetIdStr { get; }
        string ParStr(string name);
        string Quote(string name);
    }
}
