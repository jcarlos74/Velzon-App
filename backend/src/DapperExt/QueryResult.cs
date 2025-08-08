using System;
using System.Collections.Generic;
using System.Text;

namespace Dapper
{
    public class QueryResult
    {
        private readonly Tuple<string, dynamic> _result;

        /// <summary>
        /// Retorna o SQL.
        /// </summary>
        /// <value>
        /// The SQL.
        /// </value>
        public string Sql
        {
            get
            {
                return _result.Item1;
            }
        }

        public dynamic Param
        {
            get
            {
                return _result.Item2;
            }
        }

        public QueryResult(string sql, dynamic param)
        {
            _result = new Tuple<string, dynamic>(sql, param);
        }
    }
}
