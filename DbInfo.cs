using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace AutoORMCore
{
    public class DbInfo
    {
        /// <summary>
        /// mssql,db2等..
        /// </summary>
        public string DbType { get; set; }
        /// <summary>
        /// 连接字符
        /// </summary>
        public string DbConntion { get; set; }
    }
}
