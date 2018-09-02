using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoORMCore.DbAttributes;
using System.Reflection;

namespace AutoORMCore
{
    public class Field
    {
        public string Name { get; internal set; }
        /// <summary>
        /// 是否是主键
        /// </summary>
        public bool IsKey { get; internal set; }
        /// <summary>
        /// 是否排除自动产生方法
        /// </summary>
        public bool IsExcludeDelegate { get;internal set; }
        /// <summary>
        /// 是否是排除的列
        /// </summary>
        public bool IsExcludeColumn { get; internal set; }
        public Type FieldType { get; internal set; }
        public string ColName
        {
            get
            {
                if (Column == null) { return Name; }
                return Column.ColName(Name);
            }
        }
        internal Column Column { get; set; }
        internal MethodInfo GetMethod { get; set; }
        internal MethodInfo SetMethod { get; set; }
        internal Type PropertyType { get; set; }
    }
}
