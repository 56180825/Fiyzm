using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace AutoORMCore
{
    interface IDbPhysiceOper<T>:IDbOper<T> where T:new()
    {
        string PageWhere { get; }
        string OrderBy { get; }
        int PageIndex { get; }
        string SelectStr { get; }
        List<IDataParameter> Pars { get; }
        //List<T> ToList(IDataReader rd);
        //string ToJson(IDataReader rd);
        string GetSql();
        void Clear();
    }
}
