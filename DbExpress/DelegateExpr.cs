using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using System.Reflection;

namespace AutoORMCore.DbExpress
{
    public static class DelegateExpr
    {
        public static Action<object, string, object> SetMethod(Type t)
        {
            if (setList.ContainsKey(t.FullName)) { return setList[t.FullName]; }
            lock (ck)
            {
                if (setList.ContainsKey(t.FullName)) { return setList[t.FullName]; }
                Func<object, Type, object> func = DelegateExpr.Convert;
                List<SwitchCase> lt = new List<SwitchCase>();
                ParameterExpression val = Expression.Parameter(typeof(object));
                ParameterExpression instance = Expression.Parameter(typeof(object));
                ParameterExpression nameexp = Expression.Parameter(typeof(string));
                var ps = t.GetProperties();
                foreach (var n in ps)
                {
                    if (n.GetCustomAttributes(typeof(DbAttributes.ExcludeDelegate), false).Length > 0) { continue; }
                    if (!n.CanWrite) { continue; }
                    Expression tp = null;
                    if (n.PropertyType.IsValueType||n.PropertyType==typeof(string))
                    {
                        Expression pty = Expression.Constant(n.PropertyType.IsGenericType ? n.PropertyType.GetGenericArguments()[0] : n.PropertyType);
                        //4.5 Expression pty = Expression.Constant(Type.GetTypeCode(n.PropertyType.IsGenericType ? n.PropertyType.GenericTypeArguments[0] : n.PropertyType));
                        Expression val1 = Expression.Call(null, func.Method,val, pty);
                        tp = Expression.Convert(val1, n.PropertyType);
                    }
                    else { tp = Expression.Convert(val, n.PropertyType); }
                    lt.Add(Expression.SwitchCase(Expression.Call(Expression.Convert(instance, t), n.GetSetMethod(), tp), Expression.Constant(n.Name.ToLower())));
                    //4.5 lt.Add(Expression.SwitchCase(Expression.Call(Expression.Convert(instance, t), n.GetSetMethod, tp), Expression.Constant(n.Name.ToLower())));
                }
                Expression p1 = Expression.Switch(nameexp, lt.ToArray());
                LambdaExpression exp = Expression.Lambda(p1, instance, nameexp, val);
                Action<object, string, object> act = exp.Compile() as Action<object, string, object>;
                setList.TryAdd(t.FullName, act);
            }
            return setList[t.FullName];
        }
        public static Func<object, string, object> GetMethod(Type t)
        {
            if (getList.ContainsKey(t.FullName)) { return getList[t.FullName]; }
            lock (ck)
            {
                if (getList.ContainsKey(t.FullName)) { return getList[t.FullName]; }
                var ps = t.GetProperties();
                List<SwitchCase> lt = new List<SwitchCase>();
                ParameterExpression instance = Expression.Parameter(typeof(object));
                ParameterExpression nameexp = Expression.Parameter(typeof(string));
                foreach (var n in ps)
                {
                    if (n.GetCustomAttributes(typeof(DbAttributes.ExcludeDelegate), false).Length > 0) { continue; }
                    if (!n.CanRead) { continue; }
                    lt.Add(Expression.SwitchCase(Expression.Convert(Expression.Call(Expression.Convert(instance, t), n.GetGetMethod(), null), typeof(object)), Expression.Constant(n.Name.ToLower())));
                    //lt.Add(Expression.SwitchCase(Expression.Convert(Expression.Call(Expression.Convert(instance, t), n.GetSetMethod, null), typeof(object)), Expression.Constant(n.Name.ToLower())));
                }
                Expression p1 = Expression.Switch(nameexp, Expression.Constant(null), lt.ToArray());
                LambdaExpression exp = Expression.Lambda(p1, instance, nameexp);
                Func<object, string, object> func = exp.Compile() as Func<object, string, object>;
                getList.TryAdd(t.FullName, func);
            }
            return getList[t.FullName];
        }
        static ConcurrentDictionary<string, Func<object, string, object>> getList;
        static ConcurrentDictionary<string, Action<object, string, object>> setList;
        static ConcurrentDictionary<string, Dictionary<string, Field>> fList;
        static int Date { get; set; }
        static object ck;
        static DelegateExpr()
        {
            getList = new ConcurrentDictionary<string, Func<object, string, object>>();
            setList = new ConcurrentDictionary<string, Action<object, string, object>>();
            fList = new ConcurrentDictionary<string, Dictionary<string, Field>>();
            ck = new object();
        }
        public static object Get(object instan, string name)
        {
            string t=instan.GetType().Name;
            return GetMethod(instan.GetType())(instan, name);
        }
        public static void Set(object instan, string name, object value)
        {
            string t = instan.GetType().Name;
            SetMethod(instan.GetType())(instan, name.ToLower(), value);
        }
        public static void Remove(string fullName)
        {
            lock (ck)
            {
                Dictionary<string, Field> dc = null;
                fList.TryRemove(fullName, out dc);
                if (getList.ContainsKey(fullName))
                {
                    Func<Object, string, object> @out = null;
                    getList.TryRemove(fullName, out @out);
                }
                if (setList.ContainsKey(fullName))
                {
                    Action<Object, string, object> @out = null;
                    setList.TryRemove(fullName, out @out);
                }
                object obj = null;
                Emit.dic.TryRemove(fullName, out obj);
            }
        }
        public static void Remove(Type t)
        {
            Remove(t.FullName);
        }
        public static IDictionary<string,Field> GetFieldInfo<T>()
        {
            var name = typeof(T).Name;
            if (fList.ContainsKey(name)) { return fList[name]; }
            lock (ck)
            {
                if (fList.ContainsKey(name)) { return fList[name]; }
                var list = new Dictionary<string,Field>();object[] arr = null;
                foreach (var n in typeof(T).GetProperties())
                {
                    arr = n.GetCustomAttributes(typeof(DbAttributes.Column), false);
                    list.Add(n.Name, new Field()
                    {
                        IsExcludeDelegate = n.GetCustomAttributes(typeof(DbAttributes.ExcludeDelegate), false).Length > 0,
                        FieldType = n.PropertyType.IsGenericType ? n.PropertyType.GetGenericArguments()[0] : n.PropertyType,
                        Name = n.Name,
                        IsKey = n.GetCustomAttributes(typeof(DbAttributes.Key), false).Length > 0,
                        IsExcludeColumn = n.GetCustomAttributes(typeof(DbAttributes.ExcludeColumn), false).Length > 0,
                        Column = arr.Length > 0 ? (DbAttributes.Column)arr[0] : null,
                        GetMethod = n.GetMethod,
                        SetMethod = n.SetMethod,
                        PropertyType = n.PropertyType
                    });
                }
                fList.TryAdd(typeof(T).FullName, list);
                return list;
            }
        }
        internal static object Convert(object obj, Type t)
        {
            if (t == typeof(string)) { return obj.ToString(); }
            if (t.Equals(typeof(Guid)))
            {
                return Guid.Parse(obj.ToString());
            }
            else if(t.IsClass&&!t.Equals(typeof(String)))
            {
                return obj;
            }
            return System.Convert.ChangeType(obj, t);
        }
    }
}
