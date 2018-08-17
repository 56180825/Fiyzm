using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection.Emit;
using System.Reflection;
using AutoORMCore.DbExpress;

namespace AutoORMCore.DbExpress
{
    internal static class Emit
    {
        internal static ConcurrentDictionary<string, object> dic;
        static Emit()
        {
            dic = new ConcurrentDictionary<string, object>();
        }
        internal static object Convert(object obj, Type t)
        {
            if (obj == DBNull.Value || obj == null)
            {
                return t.IsValueType ? Activator.CreateInstance(t) : null;
            }
            if (t == typeof(string)) { return obj.ToString(); }
            if (t.Equals(typeof(Guid)))
            {
                return Guid.Parse(obj.ToString());
            }
            else if (t.IsClass)
            {
                return obj;
            }
            return System.Convert.ChangeType(obj, t);
        }
        internal static Func<IDataReader, Dictionary<string, int>, T> CreateBind<T>() where T:new()
        {
            //产生如下代码
            //var t = new T();
            //int a = 0;
            //if (hs.TryGetValue("name",out a))
            //{
            //    t.Name = (int)Convert(rd[a], typeof(int));
            //}
            var type = typeof(T);
            var fields = DelegateExpr.GetFieldInfo<T>();
            object obj = null;
            if (dic.TryGetValue(type.FullName, out obj)) { return obj as Func<IDataReader, Dictionary<string, int>, T>; }
            var d = new DynamicMethod("FUNC_"+Guid.NewGuid().ToString(), type, new Type[2] { typeof(System.Data.IDataReader), typeof(Dictionary<string, int>) });
            var g = d.GetILGenerator();
            var r1 = g.DeclareLocal(type);//局部变量必须定义，不然会报错
            var r2 = g.DeclareLocal(typeof(int));
            //反编译代码--  .locals init(
            //             [0] class OrmTest.Program/MES_DailyTransMap,
            //	        [1] int32
            //          )
            g.Emit(OpCodes.Newobj, type.GetConstructors()[0]);
            g.Emit(OpCodes.Stloc_0);
            g.Emit(OpCodes.Ldc_I4_0);
            g.Emit(OpCodes.Stloc_1);
            Func<Object, Type, Object> cg =Emit.Convert;
            var GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);//获取类型
            var TryGetValue = typeof(Dictionary<string, int>).GetMethod("TryGetValue");
            var Item = typeof(System.Data.IDataRecord).GetMethod("get_Item", new Type[1] { typeof(int) });
            var lt = new List<string>(2);
            foreach (var n in fields.Values)
            {
                if (n.IsExcludeColumn || n.IsExcludeDelegate || n.SetMethod == null) { continue; }
                lt.Clear();
                lt.Add(n.ColName.ToLowerInvariant());
                if (n.ColName != n.Name) { lt.Add(n.Name.ToLowerInvariant()); }
                var tp = n.FieldType;
                foreach (var a in lt)
                {
                    var name = a;
                    var dl = g.DefineLabel();
                    g.Emit(OpCodes.Ldarg_1);
                    g.Emit(OpCodes.Ldstr, name);
                    g.Emit(OpCodes.Ldloca_S, 1);
                    g.Emit(OpCodes.Callvirt, TryGetValue);
                    g.Emit(OpCodes.Brfalse_S, dl);

                    g.Emit(OpCodes.Ldloc_0);
                    g.Emit(OpCodes.Ldarg_0);
                    g.Emit(OpCodes.Ldloc_1);
                    g.Emit(OpCodes.Callvirt, Item);

                    g.Emit(OpCodes.Ldtoken, tp);
                    g.Emit(OpCodes.Call, GetTypeFromHandle);
                    g.Emit(OpCodes.Call, cg.Method);
                    g.Emit(OpCodes.Unbox_Any, tp);
                    g.Emit(OpCodes.Callvirt, n.SetMethod);
                    g.MarkLabel(dl);
                }
            }
            g.Emit(OpCodes.Ldloc_0);
            g.Emit(OpCodes.Ret);
            var f = d.CreateDelegate(typeof(Func<IDataReader, Dictionary<string, int>, T>)) as Func<IDataReader, Dictionary<string, int>, T>;
            dic.TryAdd(type.FullName, f);
            return f;
        }
    }
}
