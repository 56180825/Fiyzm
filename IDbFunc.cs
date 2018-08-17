using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace AutoORMCore
{
    public interface IDbFunc
    {
        string CreateSql(string select, string tbname, string where, string orderby, int size, int index);
        /// <summary>
        /// 获取新增时的ID
        /// </summary>
        string GetIdStr { get; }
        /// <summary>
        /// 参数的符号比如sqlserver:@name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string ParStr(string name);
        /// <summary>
        /// 用户表名，字段的引号如sqlserver:[name]
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string Quote(string name);
        IDbConnection CreateConnect();
    }
}
