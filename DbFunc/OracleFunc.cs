using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace AutoORMCore.DbFunc
{
    public abstract class OracleFunc:IDbFunc
    {
        public virtual string CreateSql(string select, string tbname, string where, string orderby, int size, int index)
        {
            var sb = new StringBuilder();
            string str =string.Empty;
            if (index > 0)
            {
                if (string.IsNullOrEmpty(orderby))
                {
                    str = "SELECT ROWNUM AS RN," + (select == "*" ? tbname + ".*" : select) + " FROM " + tbname;
                    if (!string.IsNullOrEmpty(where)) { str += " WHERE " + where; }
                    str = "SELECT " + select + " FROM (" + str + ") WHERE RN BETWEEN " + ((index - 1) * size + 1) + " AND " + index * size;
                    return str;
                }
                else
                {
                    sb.Append("SELECT " + select + " FROM(");
                    sb.Append("SELECT ROWNUM AS RN," + (select == "*" ? "T1.*" : select) + " FROM (SELECT "+select+" FROM "+tbname);
                    if (!string.IsNullOrEmpty(where)) { sb.Append(" WHERE " + where); }
                    if (!string.IsNullOrEmpty(orderby)) { sb.Append(" ORDER BY " + orderby); }
                    sb.Append(")T1) WHERE RN BETWEEN "+((index - 1) * size + 1) + " AND " + index * size);
                    return sb.ToString();
                }
            }
            str= "SELECT "+select+" FROM "+tbname;
            if (!string.IsNullOrEmpty(where)) { str += " WHERE " + where; }
            if (!string.IsNullOrEmpty(orderby)) { str += " ORDER BY " + orderby; }
            return str;
        }
        /// <summary>
        /// 获取新增时的ID
        /// </summary>
        public virtual string GetIdStr { get { return string.Empty; } }
        /// <summary>
        /// 参数的符号比如sqlserver:@name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual string ParStr(string name) { return ":" + name; }
        /// <summary>
        /// 用户表名，字段的引号如sqlserver:[name]
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual string Quote(string name) { return "\"" + name + "\""; }
        public abstract IDbConnection CreateConnect();
    }
}
