using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoORMCore.DbAttributes;
using System.Collections;

namespace AutoORMCore
{
    public static class OrmGlobal
    {
        static Dictionary<string, object> alldb;
        static object defaultDb = null;
        static string defauleKey = string.Empty;
        static object ck;
        //internal const string dbstr = "DbChe_";
        public static int PageSize { get; set; }
        static OrmGlobal()
        {
            alldb = new Dictionary<string, object>();
            ck = new object();
            DbQuote = false;
            PageSize = 25;
        }
        /// <summary>
        /// 注册连接
        /// </summary>
        /// <param name="key">id</param>
        /// <param name="info">连接信息,包含数据库类型和连接字符</param>
        public static void RegeditDbInfo(string key, DbInfo info)
        {
            lock (ck)
            {
                if (alldb.ContainsKey(key)) { return; }
                if (defaultDb == null) { defaultDb = info; defauleKey = key; }
                alldb.Add(key, info);
            }
        }
        public static void RegeditDbInfo(string key, IDbFunc info)
        {
            lock (ck)
            {
                if (alldb.ContainsKey(key)) { return; }
                if (defaultDb == null) { defaultDb = info; defauleKey = key; }
                alldb.Add(key, info);
            }
        }
        /// <summary>
        /// 删除连接
        /// </summary>
        /// <param name="key"></param>
        public static void DeleteDbInfo(string key)
        {
            lock (ck)
            {
                if (alldb.ContainsKey(key)) { alldb.Remove(key); }
            }
        }
        /// <summary>
        /// 刷新表的所有缓存
        /// </summary>
        /// <param name="tbname"></param>
        //public static void RefreshTable(string tbname)
        //{
        //    CacheDic.Refresh(tbname);
        //}
        ///// <summary>
        ///// 刷新缓存,T为表对象
        ///// </summary>
        ///// <param name="key"></param>
        //public static void RefreshName<T>(string key)
        //{
        //    CacheDic.RefreshName(dbstr+typeof(T).Name + "_"+key);
        //}
        /// <summary>
        /// 获取连接信息
        /// </summary>
        /// <param name="key">id</param>
        /// <returns></returns>
        //public static DbInfo GetDbInfo(string key) 
        //{
        //    if (alldb.ContainsKey(key))
        //    {
        //        return alldb[key];
        //    }
        //    return null;
        //}
        //public static string GetDbInfoKey(DbInfo db)
        //{
        //    var rs=alldb.Where(n => n.Value == db);
        //    if (rs.Count() > 0) { return rs.First().Key; }
        //    return string.Empty;
        //}
        /// <summary>
        /// 默认连接,为添加的第一个连接
        /// </summary>
        public static object DefaultDb
        {
            get
            {
                if (defaultDb == null && alldb.Count > 0) { defaultDb = alldb.First().Value; } 
                return defaultDb;
            }
        }
        public static string DefaultKey { get { return defauleKey; } }
        public static IDbOper<T> Create<T>(string name="") where T:new()
        {
            object df = name.Length == 0 ? defaultDb : alldb[name];
            if (df is DbInfo)
            {
                return new DbOper<T>(df as DbInfo);
            }
            else if (df is IDbFunc)
            {
                return new DbOper<T>(df as IDbFunc);
            }
            return null;
        }
        internal static bool isrecord = false;
        /// <summary>
        /// 是否记录执行信息
        /// </summary>
        public static bool RecordLog { get { return isrecord; } set { isrecord = value; } }
        /// <summary>
        /// 发生错误时候记录执行信息(在RecordLog为true时才会记录信息)
        /// </summary>
        public static event Action<string> OnErr;
        internal static void DoErr(string o)
        {
            if (isrecord&&OnErr != null) { OnErr(o); }
        }
        /// <summary>
        /// 记录所有的执行信息,与onErr只能执行一个(在RecordLog为true时才会记录信息)
        /// </summary>
        public static event Action<string> OnDispose;
        internal static void DoDispose(string s)
        {
            if (isrecord && OnErr == null && OnDispose != null) { OnDispose(s); }
        }
        /// <summary>
        /// 自定义包装dic中的参数，默认将*转化为模糊查询>
        /// </summary>
        public static Func<Dictionary<string, object>, IPar, string> Wrap { get { return wrap; } set { wrap = value; } }
        public static bool DbQuote { get; set; }
        #region wrap
        static Func<Dictionary<string, object>,IPar,string> wrap =(dic,db) =>
        {
            StringBuilder sb = new StringBuilder();
            bool b = false;
            foreach (var n in dic)
            {
                sb.Append((b?" AND ":string.Empty)+db.Quote(n.Key));
                b = true;
                if (n.Value is string)
                {
                    var tp = n.Value.ToString();
                    if (tp[0] == '>' || tp[0] == '<' || tp[0] == '=')
                    {
                        sb.Append(tp[0] + db.AddParameter(n.Key, tp.Substring(1)));
                        continue;
                    }
                    else if (tp.Substring(tp.Length - 1, 1) == "*")
                    {
                        sb.Append(" LIKE " + db.AddParameter(n.Key, tp.Substring(0, tp.Length - 1) + "%"));
                        continue;
                    }
                }
                sb.Append("=" + db.AddParameter(n.Key, n.Value));
            }
            return sb.ToString();
        };
        #endregion
    }
}
