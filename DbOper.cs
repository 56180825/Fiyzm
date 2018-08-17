using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using AutoORMCore.DbExpress;
using AutoORMCore.DbAttributes;
using System.Web;
using System.Linq.Expressions;
using System.Data.Common;

namespace AutoORMCore
{
    internal class DbOper<T> :IDbPhysiceOper<T>,IPar,IDisposable where T : new()
    {
        internal IHelper db;
        internal StringBuilder where;
        internal StringBuilder select;
        internal StringBuilder orderby;
        internal List<IDataParameter> ps;
        internal StringBuilder sqlinfo;
        internal int index = 0;
        internal int size = OrmGlobal.PageSize;
        static Dictionary<Type, string> tables;
        IDictionary<string, Field> fd = null;
        string key = null;
        public IDictionary<string, Field> Fields
        {
            get
            {
                if (fd == null) { fd = DelegateExpr.GetFieldInfo<T>(); }
                return fd;
            }
        }
        /// <summary>
        /// 参数;对象,属性名（全大写）,设置值
        /// </summary>
        public Action<object,string,object> SetMethod { get { return DelegateExpr.SetMethod(typeof(T)); } }
        /// <summary>
        /// 参数;对象,属性名（全大写）,返回结果
        /// </summary>
        public Func<object, string, object> GetMethod { get { return DelegateExpr.GetMethod(typeof(T)); } }
        public string Key
        {
            get
            {
                if (key == null)
                {
                    var n = Fields.Values.Where(n1 => n1.IsKey).FirstOrDefault();
                    key = n != null ? n.ColName : string.Empty;
                }
                return key;
            }
        }
        string tname = string.Empty;
        bool qt = OrmGlobal.DbQuote;
        public bool DbQuote { get { return qt; } set { qt = value; lock (tables) { tables.Clear(); } } }
        public string TableName
        {
            get
            {
                if (!string.IsNullOrEmpty(tname)) { return Quote(tname); }
                var t = typeof(T);
                if (tables.ContainsKey(t)) { tname =tables[t]; return Quote(tname); }
                lock (tables)
                {
                    if (tables.ContainsKey(t)) { tname =tables[t]; return Quote(tname); }
                    var arr = typeof(T).GetCustomAttributes(typeof(Table), false);
                    tname = arr == null || arr.Length == 0 ? typeof(T).Name : (arr[0] as Table).Name;
                    tables.Add(t,tname);
                }
                return Quote(tname);
            }
            set
            {
                tname = value;
            }
        }
        static DbOper()
        {
            tables = new Dictionary<Type, string>();
        }
        private DbOper(IHelper h, StringBuilder w, StringBuilder s, StringBuilder or, List<IDataParameter> p,StringBuilder sql)
        {
            db = h;
            where = w;
            select = s;
            orderby = or;
            sqlinfo = sql;
            ps = p;
        }
        internal DbOper(DbInfo info)
        {
            if (info.DbType.Equals("mssql"))
            {
                db = new AutoORMCore.Helper.Mssql(info.DbConntion);
            }
            //else if (info.DbType.Equals("msmars"))
            //{
            //    db = new AutoORMCore.Helper.MsMars(info.DbConntion);
            //}
            else if (info.DbType.Equals("mysql"))
            {
                db = new AutoORMCore.Helper.Mysql(info.DbConntion);
            }
#if DEBUG
            else if (info.DbType.Equals("test"))
            {
                db = new AutoORMCore.Helper.Test(info.DbConntion);
            }
#endif
            where = new StringBuilder();
            select = new StringBuilder();
            orderby = new StringBuilder();
            sqlinfo = new StringBuilder();
            ps = new List<IDataParameter>();
        }
        internal DbOper(IDbFunc func)
        {
            where = new StringBuilder();
            select = new StringBuilder();
            orderby = new StringBuilder();
            sqlinfo = new StringBuilder();
            ps = new List<IDataParameter>();
            db = new AutoORMCore.Helper.CommHelper(func);
        }
        public object Insert(T m)
        {
            try
            {
                StringBuilder fields = new StringBuilder();
                StringBuilder values = new StringBuilder();
                Guid gd = Guid.Empty;
                string tp = string.Empty; object o = null; 
                var getmth = GetMethod;
                foreach (var n in Fields.Values)
                {
                    if (n.IsExcludeColumn||n.IsExcludeDelegate) { continue; }
                    if (n.IsKey)
                    {
                        if (n.FieldType.Equals(typeof(Guid)))
                        {
                            gd = Guid.NewGuid();
                            fields.Append(Quote(n.Name) + ",");
                            tp = db.ParStr(n.Name);
                            values.Append(tp + ",");
                            ps.Add(db.Cp(tp,gd));
                        }
                        continue;
                    }
                    o = getmth.Invoke(m, n.Name.ToLower());
                    if (o == null) { continue; }
                    fields.Append(Quote(n.Name) + ",");
                    tp = db.ParStr(n.Name);
                    values.Append(tp + ",");
                    ps.Add(db.Cp(tp, o));
                }
                if (fields.Length > 0) { fields.Length--; }
                if (values.Length > 0) { values.Length--; }
                tp = "INSERT INTO "+TableName+ "(" + fields.ToString() + ")VALUES(" + values.ToString() + ") " +(gd!=Guid.Empty?string.Empty:db.GetIdStr);
                if (OrmGlobal.isrecord) { Record(tp); }
                object a = db.ExectueScalar(tp, ps, false);
                Clear();
                return gd != Guid.Empty?gd:a;
            }
            catch
            {
                OrmGlobal.DoErr(sqlinfo.ToString()); throw;
            }
        }
        public int Update(string str)
        {
            try
            {
                string tp = "UPDATE " + TableName + " SET " + str + (where.Length > 0 ? " WHERE " + where : string.Empty);
                if (OrmGlobal.isrecord) { Record(tp); }
                int i = db.ExecuteQuery(tp, ps, false);
                Clear();
                return i;
            }
            catch
            {
                OrmGlobal.DoErr(sqlinfo.ToString()); throw;
            }
        }
        public int Update(T m)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("UPDATE " +TableName + " SET ");
                object o = null; int j = 0; var getmth = GetMethod;
                IDataParameter p = null;
                foreach (var n in Fields.Values)//为了保证参数顺序，参数用了Insert
                {
                    if (n.IsExcludeColumn||n.IsExcludeDelegate) { continue; }
                    o = getmth.Invoke(m, n.Name.ToLower());
                    if (o == null) { continue; }
                    if (n.IsKey)
                    {
                        where.Append((where.Length > 0 ? " AND " : string.Empty) + Quote(n.Name) + "=" + db.ParStr(n.Name));
                        p = db.Cp(db.ParStr(n.Name), o);
                        continue;
                    }
                    sb.Append(Quote(n.Name) + "=" + db.ParStr(n.Name) + ",");
                    ps.Insert(j, db.Cp(db.ParStr(n.Name), o)); j++;
                }
                if (p != null) { ps.Add(p); }
                if (sb.Length > 0) { sb.Length--; }
                if (where.Length > 0) { sb.Append(" WHERE " + where); }
                var sql = sb.ToString();
                if (OrmGlobal.isrecord) { Record(sql); }
                int i = db.ExecuteQuery(sql, ps, false);
                Clear();
                return i;
            }
            catch
            {
                OrmGlobal.DoErr(sqlinfo.ToString()); throw;
            }
        }
        public int Update(Dictionary<string,object> dic)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("UPDATE " + TableName + " SET ");
                int j = 0;IDataParameter p = null;
                foreach (var n in dic)
                {
                    if (n.Key==Key)
                    {
                        where.Append((where.Length > 0 ? " AND " : string.Empty) + Quote(n.Key) + "=" + db.ParStr(n.Key));
                        p = db.Cp(db.ParStr(n.Key), n.Value);
                        continue;
                    }
                    sb.Append(Quote(n.Key) + "=" + db.ParStr(n.Key) + ",");
                    ps.Insert(j, db.Cp(db.ParStr(n.Key), n.Value));
                    j++;
                }
                if (p != null) { ps.Add(p); }
                if (sb.Length > 0) { sb.Length--; }
                if (where.Length > 0) { sb.Append(" WHERE " + where); }
                var sql = sb.ToString();
                if (OrmGlobal.isrecord) { Record(sql); }
                int i = db.ExecuteQuery(sql, ps, false);
                Clear();
                return i;
            }
            catch
            {
                OrmGlobal.DoErr(sqlinfo.ToString()); throw;
            }
        }
        public int Delete()
        {
            try
            {
                string sql = "DELETE FROM " + TableName + (where.Length > 0 ? " WHERE " + where : string.Empty);
                if (OrmGlobal.isrecord) { Record(sql); }
                int i = db.ExecuteQuery(sql, ps, false);
                Clear();
                return i;
            }
            catch
            {
                OrmGlobal.DoErr(sqlinfo.ToString()); throw;
            }
        }
        public void Import(DbDataReader dt, Action<object> act=null)
        {
            try
            {
                db.Import(dt, TableName, act);
            }
            catch
            {
                OrmGlobal.DoErr(sqlinfo.ToString()); throw;
            }
        }
        public IDbOper<T> Select(string sl)
        {
            if (string.IsNullOrEmpty(sl)) { return this; }
            select.Append((select.Length > 0 ? "," : string.Empty) + sl); return this;
        }
        public IDbOper<T> Select(Expression<Func<T, object>> sl)
        {
            using (var tp1 = new LinqVisitor())
            {
                var tp=tp1.VisitNew(sl.Body as NewExpression);
                StringBuilder sb = new StringBuilder();
                foreach (var n in tp)
                {
                    sb.Append(Quote(n) + ",");
                }
                if (sb.Length > 0) { sb.Length--; }
                return Select(sb.ToString());
            }
        }
#region IPar
        string IPar.AddParameter(string name, object obj)
        {
            var t = Fields.ContainsKey(name) ? Fields[name] : null;
            if (t != null) { obj = DelegateExpr.Convert(obj, t.FieldType);name = t.ColName; }
            var n = db.ParStr(name);
            ps.Add(db.Cp(n, obj));
            return n;
        }
        public string Quote(string name) {
            var s = Fields.ContainsKey(name) ? Fields[name].ColName : name;
            return DbQuote?db.Quote(s):s;
        }
#endregion
        public IDbOper<T> Where(Dictionary<string, object> dic)
        {
            if (dic == null || dic.Count == 0) { return this; }
            Where(OrmGlobal.Wrap(dic, this));
            return this;
        }
        public IDbOper<T> Where(string sl)
        {
            if (string.IsNullOrEmpty(sl)) { return this; }
            where.Append((where.Length > 0 ? " AND " : string.Empty) + sl); return this;
        }
        public IDbOper<T> Where(Expression<Func<T, bool>> sl)
        {
            List<object> tp=null;var dc = Fields;
            using (var tp1 = new LinqVisitor())
            {
                tp = tp1.Visit(sl) as List<object>;
                StringBuilder sb = new StringBuilder(); string s = string.Empty;
                var a = ps.Count;
                for (int i = 0; i < tp.Count; i += 4)
                {
                    s = db.ParStr("Par" + (a + i));// db.ParStr(tp[i].ToString());
                    sb.Append(Quote(tp[i].ToString()) + tp[i + 1].ToString() + s);
                    if (i + 4 < tp.Count) { sb.Append(tp[i + 3]); }
                    ps.Add(db.Cp(s, tp[i + 2]));
                }
                Where(sb.ToString());
            }
            return this;
        }
        /// <summary>
        /// 若为最后须预防注入
        /// </summary>
        /// <param name="orby"></param>
        /// <returns></returns>
        public IDbOper<T> Orderby(string orby)
        {
            if (string.IsNullOrEmpty(orby)) { return this; }
            orderby.Append((orderby.Length > 0 ? "," : string.Empty) + orby); return this;
        }
        public IDbOper<T> Orderby(Dictionary<string, string> dic)
        {
            if (dic.Count == 0) { return this; }
            StringBuilder sb = new StringBuilder();
            foreach (var n in dic.Keys)
            {
                if(string.Compare("DESC",dic[n],true)!=0 && string.Compare("ASC",dic[n],true)!=0){continue;}
                sb.Append(n + " " +Quote(dic[n]) + ",");
            }
            if (sb.Length > 0) { sb.Length--; }
            Orderby(sb.ToString()); return this;
        }
        /// <summary>
        /// 分页的页码
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IDbOper<T> Index(int i) { if (i > 0) { index = i; } return this; }
        /// <summary>
        /// 每页行数，默认为100
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IDbOper<T> Size(int i) { if (i > 0) { size = i; } return this; }
        public IDbOper<T> AddPar(string name, object val) { ps.Add(db.Cp(name, val)); return this; }
        public IDbOper<T> WithPar(IEnumerable<IDataParameter> ps1) { if (ps1 != null) { ps.AddRange(ps1); } return this; }
        public IDbOper<T> WithPar(Dictionary<string, object> dic) { foreach (var n in dic) { AddPar(n.Key, n.Value); } return this; }
        public void BegTran(IsolationLevel level) { db.BegTran(level); }
        public void RollBack() { db.RollBack(); }
        public void Commit() { db.Commit(); }
        public void Clear()
        {
            where.Length = 0; select.Length = 0; orderby.Length = 0; ps.Clear(); index = 0; size = OrmGlobal.PageSize;
        }
        public M ToObj<M>(Func<IDataReader, M> func, string sql = null, bool issp = false)
        {
            try
            {
                if (string.IsNullOrEmpty(sql)) { sql = GetSql(); }
                if (OrmGlobal.isrecord) { Record(sql); }
                M t = default(M);
                using (var rd = db.ExectueReader(sql, ps, false))
                {
#if DEBUG
                if(rd==null){Clear();return default(M);}
#endif
                    t = func(rd);
                    rd.Close();
                }
                Clear();
                return t;
            }
            catch
            {
                OrmGlobal.DoErr(sqlinfo.ToString()); throw;
            }
        }
        public List<T> ToList(string sql=null,bool issp=false) 
        {
            sql =sql??GetSql();
            return ToObj<List<T>>(rd => ToList(rd), sql, issp);
        }
        public List<Dictionary<string, Object>> ToListDic(string sql = null, bool issp = false)
        {
            sql = sql ?? GetSql();
            return ToObj<List<Dictionary<string, Object>>>(rd => ToListDic(rd), sql, issp);
        }
        IEnumerable<Field> colfds = null;
        /// <summary>
        /// 获取列名和字段名不同的列集合
        /// </summary>
        IEnumerable<Field> ColFds
        {
            get
            {
                if (colfds == null) { colfds = Fields.Values.Where(n => n.Name != n.ColName); }
                return colfds;
            }
        }
        List<T> ToList(IDataReader rd)
        {
            var lt = new List<T>(size);
            //var set =SetMethod;
            //var lt1 = ColFds;
            //Field fd = null;string fieldname = null;
            var bind = Emit.CreateBind<T>();
            var dic = new Dictionary<string, int>(rd.FieldCount);
            for(var i = 0; i < rd.FieldCount; i++)
            {
                dic.Add(rd.GetName(i).ToLowerInvariant(), i);
            }
            while (rd.Read())
            {
                //var m = new T();
                //for (var i = 0; i < rd.FieldCount; i++)
                //{
                //    if (rd[i] == DBNull.Value || rd[i] == null) { continue; }
                //    fieldname = rd.GetName(i);
                //    fd = lt1.Where(n =>string.Compare(n.ColName,fieldname,true)==0).FirstOrDefault();
                //    set(m, (fd!=null?fd.Name:fieldname).ToLower(), rd[i]);
                //}
                lt.Add(bind(rd,dic));
            }
            return lt;
        }
        List<Dictionary<string,Object>> ToListDic(IDataReader rd)
        {
            var lt = new List<Dictionary<string, Object>>(size);
            string fieldname = null;
            Dictionary<string, Object> dc = null;
            while (rd.Read())
            {
                //var m = new T();
                dc = new Dictionary<string, object>(rd.FieldCount);
                for (var i = 0; i < rd.FieldCount; i++)
                {
                    if (rd[i] == DBNull.Value || rd[i] == null) { continue; }
                    fieldname = rd.GetName(i);
                    dc.Add(fieldname, rd[i]);
                }
                lt.Add(dc);
            }
            return lt;
        }
        public DataTable ToDataTable()
        {
            return ToDataTable(GetSql());
        }
        public DataTable ToDataTable(string sql,bool issp=false)
        {
            try
            {
                if (OrmGlobal.RecordLog) { Record(sql); }
                var tp = db.GetDataTable(sql, ps, issp);
                Clear(); return tp;
            }
            catch
            {
                OrmGlobal.DoErr(sqlinfo.ToString()); throw;
            }
        }
        public string GetSql()
        {
            var s = select.ToString();
            var r = orderby.ToString();
            if (index > 1 && string.IsNullOrEmpty(r))
            {
                r = Key;
            }
            return db.CreateSql(string.IsNullOrEmpty(s)?"*":s, TableName, where.ToString(), r, size, index);
        }
        public IDbOper<M> ToOper<M>() where M:new()
        {
            Clear();
            return new DbOper<M>(db,where,select,orderby,ps,sqlinfo);
        }
        public int Count()
        {
            try
            {
                string sql = "SELECT COUNT(*) FROM " + TableName + (where.Length > 0 ? " WHERE " + where : string.Empty);
                if (OrmGlobal.RecordLog) { Record(sql); }
                int i = Convert.ToInt32(db.ExectueScalar(sql, ps, false));
                Clear();
                return i;
            }
            catch
            {
                OrmGlobal.DoErr(sqlinfo.ToString()); throw;
            }
        }
        public int DoCommand(string sql,bool issp)
        {
            int i=db.ExecuteQuery(sql,ps,issp);
            Clear();
            return i;
        }
        void Record(string sql)
        {
            sqlinfo.Append(DateTime.Now.ToString() + "-Cmd:" + sql + ",");
            foreach (var n in ps)
            {
                sqlinfo.Append(n.ParameterName + ":" + n.Value + "("+n.DbType.ToString()+"),");
            }
            sqlinfo.Length--;
            sqlinfo.Append("\r\n");
        }
        public void Dispose() { Dispose(true); }
        void Dispose(bool f)
        {
            if (f) { db.Dispose(); }
            OrmGlobal.DoDispose(sqlinfo.ToString()); where = null; select = null; orderby = null; db = null; ps = null; sqlinfo = null; 
            GC.SuppressFinalize(this);
        }
        public T First()
        {
            var lt=Size(1).Index(1).ToList();
            if (lt.Count > 0) { return lt[0]; }
            return default(T);
        }
        public Dictionary<string,object> FirstDic()
        {
            var lt = Size(1).Index(1).ToListDic();
            if (lt.Count > 0) { return lt[0]; }
            return null;
        }
        public T Dic2Object(Dictionary<string,object> dic)
        {
            var t = new T();
            var set = SetMethod;
            var lt = ColFds;
            Field fd = null;
            foreach(var n in dic)
            {
                fd = lt.Where(n1 => string.Compare(n1.ColName,n.Key,true)==0).FirstOrDefault();
                set(t, n.Key.ToLower(), n.Value);
                if (fd != null)
                {
                    set(t, fd.Name.ToLower(), n.Value);
                }
            }
            return t;
        }
        public Dictionary<string, object> Object2Dic(T t,bool excludeNull=true)
        {
            var get = GetMethod;
            var dic = new Dictionary<string, object>(Fields.Count);
            object value = null;
            foreach(var n in Fields)
            {
                value = get(t, n.Key.ToLower());
                if (value == null && excludeNull) { continue; }
                dic.Add(n.Key, value);
            }
            return dic;
        }
        string IDbPhysiceOper<T>.PageWhere { get { return where.ToString(); } }
        string IDbPhysiceOper<T>.OrderBy { get { return orderby.ToString(); } }
        int IDbPhysiceOper<T>.PageIndex { get { return index; } }
        string IDbPhysiceOper<T>.SelectStr { get { return select.ToString(); } }
        List<IDataParameter> IDbPhysiceOper<T>.Pars { get { return ps; } }
        ~DbOper()
        {
            Dispose(false);
        }
    }
}
