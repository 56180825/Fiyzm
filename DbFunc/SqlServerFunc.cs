using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace AutoORMCore.DbFunc
{
    public abstract class SqlServerFunc:IDbFunc
    {
        public virtual string CreateSql(string select, string tbname, string where, string orderby, int size, int index)
        {
            StringBuilder sb = new StringBuilder();
            int i = 1;
            sb.Append("SELECT ");
            if (index == 1) { sb.Append("TOP " + size + " "); }
            sb.Append(select + " FROM ");
            if (index > 1) { sb.Append("(SELECT ROW_NUMBER() OVER(" + (!string.IsNullOrEmpty(orderby) ? "ORDER BY " + orderby : string.Empty) + ") AS ROWID," + select + " FROM "); }
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
        public virtual string ParStr(string name) { return "@" + name; }
        public virtual string GetIdStr { get { return "SELECT @@IDENTITY"; } }
        public virtual string Quote(string name) { return "[" + name + "]"; }
        public abstract IDbConnection CreateConnect();
    }
}
