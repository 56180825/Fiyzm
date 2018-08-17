//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Data;
//using System.Data.SqlClient;
//using AutoORMCore;
//using System.Data.Common;

//namespace AutoORMCore.Helper
//{
//    /// <summary>
//    /// 支持mssql 自动开启mars,连接0为查询连接,连接1为执行无事务连接,事务连接为新连接
//    /// </summary>
//    class MsMars:IHelper
//    {
//        #region static
//        static SqlConnectionStringBuilder ssb;
//        static SqlConnection[] globalCon;
//        static object ck = new object();
//        public MsMars(string constr)
//        {
//            if (ssb == null)
//            {
//                ssb = new SqlConnectionStringBuilder(constr); ssb.MultipleActiveResultSets = true;
//            }
//        }
//        static SqlCommand GetCmd(int i = 3)
//        {
//            if (ssb == null) { throw new Exception("连接未初始化..."); }
//            if (i > 2) { return new SqlConnection(ssb.ConnectionString).CreateCommand(); }
//            if (globalCon != null) { return globalCon[i].CreateCommand(); }
//            lock (ck)
//            {
//                if (globalCon != null) { return globalCon[i].CreateCommand(); }
//                globalCon = new SqlConnection[2] { new SqlConnection(ssb.ConnectionString), new SqlConnection(ssb.ConnectionString) };
//                globalCon[0].Open(); globalCon[1].Open(); 
//            }
//            return globalCon[i].CreateCommand();
//        }
//        #endregion
//        SqlCommand cmd;
//        void SetCmd(string txt, IEnumerable<IDataParameter> ps, bool issp)
//        {
//            cmd.Parameters.Clear();
//            cmd.CommandText = txt;
//            cmd.CommandType = issp ? CommandType.StoredProcedure : CommandType.Text;
//            cmd.Parameters.AddRange(ps.ToArray());
//        }
//        public int ExecuteQuery(string txt, IEnumerable<IDataParameter> ps, bool issp)
//        {
//            cmd = cmd ?? GetCmd(1);
//            SetCmd(txt, ps, issp);
//            return cmd.ExecuteNonQuery();
//        }
//        public object ExectueScalar(string txt, IEnumerable<IDataParameter> ps, bool issp)
//        {
//            cmd = cmd ?? GetCmd(1);
//            SetCmd(txt, ps, issp);
//            return cmd.ExecuteScalar();
//        }
//        public IDataReader ExectueReader(string txt, IEnumerable<IDataParameter> ps, bool issp)
//        {
//            cmd =cmd??GetCmd(0);
//            SetCmd(txt, ps, issp);
//            return cmd.ExecuteReader();
//        }
//        public void BegTran()
//        {
//            cmd = GetCmd();
//            cmd.Connection.Open();
//            cmd.Transaction = cmd.Connection.BeginTransaction();
//        }
//        public void RollBack()
//        {
//            if (cmd.Transaction != null)
//            {
//                cmd.Transaction.Rollback();
//                cmd.Connection.Close(); cmd.Dispose(); cmd = null;
//            }
            
//        }
//        public void Commit()
//        {
//            if (cmd.Transaction != null)
//            {
//                cmd.Transaction.Commit();
//                cmd.Connection.Close(); cmd.Dispose(); cmd = null;
//            }
//        }
//        public void Import(DbDataReader rd, string tableName, Action<object> act)
//        {
//            var con = new SqlConnection(ssb.ConnectionString);
//            con.Open();
//            try
//            {
//                SqlBulkCopy bc = new SqlBulkCopy(con);
//                bc.DestinationTableName = tableName;
//                if (act == null)
//                {
//                    for (var i=0;i<rd.FieldCount;i++)
//                    {
//                        bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(rd.GetName(i), rd.GetName(i)));
//                    }
//                }
//                else
//                {
//                    act(bc);
//                }
//                bc.WriteToServer(rd);
//            }
//            finally { con.Close(); }
//        }
//        public DataTable GetDataTable(string txt, IEnumerable<IDataParameter> ps, bool issp)
//        {
//            cmd =cmd?? GetCmd(0);
//            SetCmd(txt,ps,issp);
//            var adapter = new DbDataAdapter(cmd);
//            var dt=new DataTable();
//            //adapter.FillSchema(dt, SchemaType.Mapped);
//            adapter.Fill(dt); return dt;
//        }
//        public string CreateSql(string select, string tbname, string where, string orderby, int size, int index)
//        {
//            StringBuilder sb = new StringBuilder();
//            sb.Append("SELECT ");
//            if (index == 1) { sb.Append("TOP " + size + " "); }
//            sb.Append(select + " FROM ");
//            if (index > 1) { sb.Append("(SELECT ROW_NUMBER() OVER(" + (!string.IsNullOrEmpty(orderby) ? "ORDER BY " + orderby : string.Empty) + ") AS ROWID," + select + " FROM "); }
//            sb.Append(tbname);
//            sb.Append(!string.IsNullOrEmpty(where) ? " WHERE " + where : string.Empty);
//            if (index > 1)
//            {
//                sb.Append(") AS " + tbname + " WHERE ROWID BETWEEN " + ((index - 1) * size + 1) + " AND " + size * index);
//            }
//            else
//            {
//                sb.Append(!string.IsNullOrEmpty(orderby) ? " ORDER BY " + orderby : string.Empty);
//            }
//            return sb.ToString();
//        }
//        public IDataParameter Cp(string name, object value)
//        {
//            return new SqlParameter(name, value);
//        }
//        public string GetIdStr { get { return "SELECT @@IDENTITY"; } }
//        public string ParStr(string name)
//        {
//            return "@" + name;
//        }
//        public void Dispose()
//        {
//            if (cmd != null) { cmd.Dispose(); cmd = null; }
//        }
//        public string Quote(string name) { return "[" + name + "]"; }
//    }
//}
