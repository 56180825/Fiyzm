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
        internal static object Convert(object obj, Type t,bool isGenericType)
        {
            if (obj == DBNull.Value || obj == null)
            {
                //if (isGenericType) { return null; }
                if (t.IsValueType&&!isGenericType) { return Activator.CreateInstance(t); }
                return null;
            }
            if (t == typeof(string)) { return obj.ToString(); }
            //if (t.IsGenericType) { t = t.GenericTypeArguments[0]; }
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
        internal static Temp<T> CreateBind<T>() where T:new()
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
            var dic1 = new Dictionary<string, int>(fields.Count);
            object obj = null;
            if (dic.TryGetValue(type.FullName, out obj)) { return obj as Temp<T>; }
            var d = new DynamicMethod("FUNC_"+Guid.NewGuid().ToString(), type, new Type[2] { typeof(System.Data.IDataReader), typeof(int[]) });
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
            Func<Object, Type, bool, Object> cg = Emit.Convert;
            var GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);//获取类型
            var TryGetValue = typeof(Dictionary<string, int>).GetMethod("TryGetValue");
            var Item = typeof(System.Data.IDataRecord).GetMethod("get_Item", new Type[1] { typeof(int) });
            var lt = new List<string>(2); int x = 0;
            foreach (var n in fields.Values)
            {
                if (n.IsExcludeColumn || n.IsExcludeDelegate || n.SetMethod == null) { continue; }
                lt.Clear();
                lt.Add(n.ColName.ToLowerInvariant());
                if (n.ColName != n.Name) { lt.Add(n.Name.ToLowerInvariant()); }
                var tp = n.FieldType;
                foreach (var a in lt)
                {
                    var dl = g.DefineLabel();
                    g.Emit(OpCodes.Ldarg_1);
                    g.Emit(OpCodes.Ldc_I4, x);
                    g.Emit(OpCodes.Ldelem_I4);
                    g.Emit(OpCodes.Ldc_I4_M1);
                    g.Emit(OpCodes.Ble_S, dl);

                    g.Emit(OpCodes.Ldloc_0);
                    g.Emit(OpCodes.Ldarg_0);
                    g.Emit(OpCodes.Ldarg_1);
                    g.Emit(OpCodes.Ldc_I4, x);
                    g.Emit(OpCodes.Ldelem_I4);
                    g.Emit(OpCodes.Callvirt, Item);

                    g.Emit(OpCodes.Ldtoken, n.FieldType);
                    g.Emit(OpCodes.Call, GetTypeFromHandle);
                    g.Emit(n.PropertyType.IsGenericType ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    g.Emit(OpCodes.Call, cg.Method);
                    g.Emit(OpCodes.Unbox_Any, n.PropertyType);
                    g.Emit(OpCodes.Callvirt, n.SetMethod);
                    g.MarkLabel(dl);
                    dic1.Add(a, x);
                    x++;
                }
            }
            g.Emit(OpCodes.Ldloc_0);
            g.Emit(OpCodes.Ret);
            var tp1 = new Temp<T>();
            tp1.Func= d.CreateDelegate(typeof(Func<IDataReader, int[], T>)) as Func<IDataReader, int[], T>;
            tp1.Dic = dic1;
            dic.TryAdd(type.FullName, tp1);
            return tp1;
        }
        internal class Temp<T>
        {
            internal Func<IDataReader, int[], T> Func { get; set; }
            internal Dictionary<string, int> Dic { get; set; }
        }
    }
}
