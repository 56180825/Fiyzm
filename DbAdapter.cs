using System;
using System.Data;
using System.Data.Common;

namespace AutoORMCore
{
     /// <summary>
    /// 数据填充器
    /// </summary>
    public class DbDataAdapter
    {
        private IDbCommand command;
        private string sql;
        private IDbConnection _sqlConnection;

        /// <summary>
        /// SqlDataAdapter
        /// </summary>
        /// <param name="command"></param>
        public DbDataAdapter(IDbCommand command)
        {
            this.command = command;
        }

        /// <summary>
        /// SqlDataAdapter
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="_sqlConnection"></param>
        public DbDataAdapter(string sql, IDbConnection _sqlConnection)
        {
            this.sql = sql;
            this._sqlConnection = _sqlConnection;
        }

        /// <summary>
        /// Fill
        /// </summary>
        /// <param name="dt"></param>
        public void Fill(DataTable dt)
        {
            if (dt == null)
            {
                dt = new DataTable();
            }
            var columns = dt.Columns;
            var rows = dt.Rows;
            using (IDataReader dr = command.ExecuteReader(CommandBehavior.CloseConnection))
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    string name = dr.GetName(i).Trim();
                    if (!columns.Contains(name))
                    {
                        columns.Add(new DataColumn(name, dr.GetFieldType(i)));
                    }
                }

                while (dr.Read())
                {
                    DataRow daRow =dt.NewRow();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        daRow[columns[i].ColumnName]=dr.GetValue(i);
                    }
                    dt.Rows.Add(daRow);
                }
                dr.Close();
            }
        }
    }

}
 