using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace AutoORMCore.DbFunc
{
    public abstract class PostgresFunc : IDbFunc
    {
        public virtual string CreateSql(string select, string tbname, string where, string orderby, int size, int index)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT " + select + " FROM " + tbname);
            if (!string.IsNullOrEmpty(where)) { sb.Append(" WHERE " + where); }
            if (!string.IsNullOrEmpty(orderby)) { sb.Append(" ORDER BY " + orderby); }
            if (index >= 1) { sb.Append(" LIMIT " + size + " OFFSET " + (index - 1) * size); }
            return sb.ToString();
        }
        public virtual string GetIdStr { get { return ";SELECT LASTVAL();"; } }
        public virtual string ParStr(string name) { return ":" + name; }
        public virtual string Quote(string name) { return "\"" + name + "\""; }
        public abstract IDbConnection CreateConnect();
    }
}
