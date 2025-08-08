using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;

using Dapper;

namespace DapperExt
{
    public interface IDapperDbContext
    {
        IDbConnection DbConnection { get; }
        IDbTransaction DbTransaction { get; set; }

        UnitOfWork CreateUnitOfWork(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        IEnumerable<dynamic> Query(string sql, dynamic param = null, int? commandTimeout = null, CommandType? commandType = null);

        void Dispose();

        //IEnumerable<IDictionary<string, object>> Query(string sql, object param);

        //  IEnumerable<IDictionary<string, object>> Query(string sql, object param, CommandType? commandType);

        // IEnumerable<IDictionary<string, object>> Query(string sql, object param, int? commandTimeout, CommandType? commandType);

        // int Execute(string sql, object param, CommandType commandType);

        // IEnumerable<T> Query<T>(string sql, object param);

        // IEnumerable<T> Query<T>(string sql, object param, CommandType commandType);

        //  SqlMapper.GridReader QueryMultiple(string sql, object param);

        // SqlMapper.GridReader QueryMultiple(string sql, object param, CommandType commandType);

        IEnumerable<T> Query<T>(string sql, object param, int? commandTimeout, CommandType? commandType);

        IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null);

        IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null);

        IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null);

        IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null);

        IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null);

        SqlMapper.GridReader QueryMultiple(string sql, dynamic param = null, int? commandTimeout = null, CommandType? commandType = null);

        int Execute(string sql, dynamic param = null, int? commandTimeout = null, CommandType? commandType = null);

        string GetSQL();

        T ExecuteStoredProcedure<T>(string spName, dynamic param = null);

        IEnumerable<T> Query<T>(Expression<Func<T, bool>> where, int? commandTimeout = null);

        IEnumerable<T> GetList<T>();

        IEnumerable<T> GetList<T>(object whereConditions, int? commandTimeout = null);

        // IEnumerable<T> ExecuteQuery<T>(this IDbConnection connection, string where, object parameters = null, IDbTransaction transaction = null, int? commandTimeout = null);

        T Get<T>(Expression<Func<T, bool>> where);

        T Get<T>(object id, int? commandTimeout = null);

        int? Insert<TEntity>(TEntity entityToInsert, string sequenceName="", int? commandTimeout = null);

        string GetConnectionString();

        string CurrentSchema { get; }
    }
}
