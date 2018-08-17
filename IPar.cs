using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoORMCore
{
    public interface IPar
    {
        /// <summary>
        /// 添加参数,无须加参数前缀
        /// </summary>
        /// <param name="name">带前缀的参数名</param>
        /// <param name="val"></param>
        /// <returns></returns>
        string AddParameter(string name, object val);
        string Quote(string name);
    }
}
