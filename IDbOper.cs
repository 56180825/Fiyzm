using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Linq.Expressions;
using System.Data.Common;

namespace AutoORMCore
{
    public interface IDbOper<T> : IDisposable where T : new()
    {
        object Insert(T m);
        int Update(string str);
        int Update(T m);
        int Update(Dictionary<string, object> dic);
        int Delete();
        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="act"></param>
        void Import(DbDataReader dt, Action<object> act=null);
        IDbOper<T> Select(string sl);
        IDbOper<T> Select(Expression<Func<T, object>> sl);
        IDbOper<T> Where(Dictionary<string, object> dic);
        IDbOper<T> Where(string sl);
        IDbOper<T> Where(Expression<Func<T, bool>> sl);
        IDbOper<T> Orderby(Dictionary<string, string> dic);
        IDbOper<T> Orderby(string orby);
        /// <summary>
        /// 第i页
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        IDbOper<T> Index(int i);
        /// <summary>
        /// 每页行数
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        IDbOper<T> Size(int i);
        /// <summary>
        /// 添加参数组，需要添加前缀，比如@，?等
        /// </summary>
        /// <param name="ps1"></param>
        /// <returns></returns>
        IDbOper<T> WithPar(IEnumerable<IDataParameter> ps1);
        /// <summary>
        /// 添加参数组，需要添加前缀，比如@，?等
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        IDbOper<T> WithPar(Dictionary<string, object> dic);
        /// <summary>
        /// 获取第一个对象
        /// </summary>
        /// <returns></returns>
        T First();
        /// <summary>
        /// 获取第一个dic
        /// </summary>
        /// <returns></returns>
        Dictionary<string, object> FirstDic();
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="level"></param>
        void BegTran(IsolationLevel level = IsolationLevel.Unspecified);
        /// <summary>
        /// 回滚
        /// </summary>
        void RollBack();
        /// <summary>
        /// 提交事务
        /// </summary>
        void Commit();
        /// <summary>
        /// 自定义转换
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="func"></param>
        /// <param name="sql"></param>
        /// <param name="issp"></param>
        /// <returns></returns>
        M ToObj<M>(Func<IDataReader, M> func,string sql=null,bool issp=false);
        /// <summary>
        /// 获取为LIST
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="issp">是否为存储过程</param>
        /// <returns></returns>
        List<T> ToList(string sql = null, bool issp = false);
        /// <summary>
        /// 获取为Dic组
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="issp">是否为存储过程</param>
        /// <returns></returns>
        List<Dictionary<string, Object>> ToListDic(string sql = null, bool issp = false);
        /// <summary>
        /// 获取为DataTable
        /// </summary>
        /// <returns></returns>
        DataTable ToDataTable();
        /// <summary>
        /// 获取为DataTable
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="issp">是否为存储过程</param>
        /// <returns></returns>
        DataTable ToDataTable(string sql, bool issp = false);
        /// <summary>
        /// 添加单个参数
        /// </summary>
        /// <param name="name">参数名,需要添加前缀，比如@，?等</param>
        /// <param name="val">值</param>
        /// <returns></returns>
        IDbOper<T> AddPar(string name, object val);
        //string ToJson();
        //IDbOper<T> ToCacheDb(string name="");
        IDbOper<M> ToOper<M>() where M : new();
        int Count();
        int DoCommand(string sql, bool issp);
        string TableName { get; set; }
        /// <summary>
        /// 是否添加引用标记（用于保留字符）
        /// </summary>
        bool DbQuote { get; set; }
        /// <summary>
        /// 所有的字段
        /// </summary>
        IDictionary<string, Field> Fields { get; }
        /// <summary>
        /// 主键的列名
        /// </summary>
        string Key { get; }
        Action<object, string, object> SetMethod { get; }
        Func<object, string, object> GetMethod { get; }
        /// <summary>
        /// 将dic转换为T
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="convertCol">T对象属性与数据库字段的自动转换</param>
        /// <returns></returns>
        T Dic2Object(Dictionary<string, object> dic);
        /// <summary>
        /// T转换为dic
        /// </summary>
        /// <param name="t"></param>
        /// <param name="excludeNull">排除空，默认为排除</param>
        /// <returns></returns>
        Dictionary<string, object> Object2Dic(T t, bool excludeNull = true);
    }
}
