using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace AutoORMCore.DbAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class Key : Attribute { }
    /// <summary>
    /// 用户排除expr
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ExcludeDelegate : Attribute { }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ExcludeColumn : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class Table : Attribute 
    {
        public string Name { get; set; }
        public Table() { }
        public Table(string name) { Name = name; }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class Column:Attribute
    {
        /// <summary>
        /// 获取列名
        /// </summary>
        internal string ColName(string fieldName=null)
        {
            if (!string.IsNullOrEmpty(colName)) { return colName; }
            if (func != null) { return func(fieldName); }
            return fieldName;
        }
        string colName = null;
        Func<string, string> func = null;
        public Column(string name)
        {
            colName = name;
        }
        public Column(Func<string, string> f)
        {
            func = f;
        }
    }
}
