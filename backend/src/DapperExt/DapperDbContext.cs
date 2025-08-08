using Dapper;
//using GodSharp.Data.Common.DbProvider;
//using GodSharp.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using static Dapper.DapperExt;


namespace DapperExt
{
    public class DapperDbContext : IDapperDbContext, IDisposable
    {
        private IDbConnection _connection;
        private readonly DbProviderFactory _connectionFactory;
        private readonly string _connectionString;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private readonly LinkedList<UnitOfWork> _workItems = new LinkedList<UnitOfWork>();

        private string _schema;
        private string _sql;
        private string _dataBaseDialect;
        private int _idTenant;

        public IDbConnection DbConnection { get; private set; }
        public IDbTransaction DbTransaction { get; set; }
        public string CurrentSchema { get { return _schema; } }

        /// <summary>
        /// Usar esse construtor quando a leitura da string de conexao não usuar o App.Config
        /// </summary>
        /// <param name="connectionStringName"></param>
        /// <param name="schema"></param>
        /// <param name="dataBaseDialect"></param>
        /// <param name="dataBaseProvider"></param>
        public DapperDbContext(string connectionString, DbProviderFactory dbProviderFactory, string schema = "siltec", string dataBaseDialect = "PostgreSQL",  int tenant = 0) //"Oracle.ManagedDataAccess.Client"
        {

            _connectionFactory = dbProviderFactory; // DbProviderFactories.GetFactory(dataBaseProvider);

             _connectionString = connectionString;

           
            _dataBaseDialect = dataBaseDialect;

            _schema = schema;

            _idTenant = tenant;

            var dialeto = dataBaseDialect;

            switch (dialeto)
            {
                case "PostgreSQL":
                    SetDialect(Dialect.PostgreSQL);
                    break;
                case "SQLite":
                    SetDialect(Dialect.SQLite);
                    break;
                case "MySQL":
                    SetDialect(Dialect.MySQL);
                    break;
                case "Oracle":
                    SetDialect(Dialect.Oracle);
                    break;
                case "SQLServer":
                    SetDialect(Dialect.SQLServer);
                    break;
                default:
                    SetDialect(Dialect.PostgreSQL);
                    break;
            }

            SetSchemaName(_schema);

        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        private void SetCurrentTenant()
        {
            if (_dataBaseDialect == "PostgreSQL" && _idTenant > 0)
            {
                var sql = $"SET search_path TO siltec; SET app.current_tenant = '{_idTenant}';";

                _connection.Execute(sql);
            }
        }

        // <summary>
        /// Cria uma conexão ou reusa uma já pronta
        /// </summary>
        /// <remarks></remarks>
        private void CreateOrReuseConnection()
        {
            if (_connection != null)
            {
                SetCurrentTenant();


                return;
            }

            _connection = _connectionFactory.CreateConnection(); 

            _connection.ConnectionString = _connectionString;
            _connection.Open();

            this.DbConnection = _connection;

            SetCurrentTenant();
        }

        public UnitOfWork CreateUnitOfWork(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            CreateOrReuseConnection();

            //Para criar uma transação a conexão precisa estar aberta.
            //Se precisarmos abrir a conexão, também estamos a cargo de fechá-la quando essa transação for comitada ou revertida.
            //Isso será feito por RemoveTransactionAndCloseConnection 
            bool wasClosed = _connection.State == ConnectionState.Closed;
            if (wasClosed) _connection.Open();

            try
            {
                UnitOfWork unit;
                IDbTransaction transaction = _connection.BeginTransaction(isolationLevel);

                this.DbTransaction = transaction;

                if (wasClosed)
                    unit = new UnitOfWork(transaction, RemoveTransactionAndCloseConnection, RemoveTransactionAndCloseConnection);
                else
                    unit = new UnitOfWork(transaction, RemoveTransaction, RemoveTransaction);

                _rwLock.EnterWriteLock();
                _workItems.AddLast(unit);
                _rwLock.ExitWriteLock();

                return unit;
            }
            catch
            {
                //Fecha a conexão se uma exceção for lançada ao criar a transação.
                if (wasClosed) _connection.Close();

                throw; //Rethrow para transação original
            }
        }

        private IDbTransaction GetCurrentTransaction()
        {
            IDbTransaction currentTransaction = null;

            _rwLock.EnterReadLock();

            if (_workItems.Any()) currentTransaction = _workItems.First.Value.Transaction;

            _rwLock.ExitReadLock();

            return currentTransaction;
        }


#if !CSHARP30
        /// <summary>
        /// Retornar uma lista de objetos dinâmicos, a conexão é fechada após a chamada
        /// </summary>
        public IEnumerable<dynamic> Query(string sql, dynamic param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            CreateOrReuseConnection();

            _sql = sql;

            //O Dapper irá abrir e fechar a conexão para nós, se necessário.
            return SqlMapper.Query<dynamic>(_connection, sql, param, GetCurrentTransaction(), true, commandTimeout, commandType);
        }
#else
        /// <summary>
        /// Retornar uma lista de objetos dinâmicos, a conexão é fechada após a chamada
        /// </summary>
        public IEnumerable<IDictionary<string, object>> Query(string sql, object param)
        {
            return Query(sql, param, null, null);
        }

        /// <summary>
        /// Retornar uma lista de objetos dinâmicos, a conexão é fechada após a chamada
        /// </summary>
        public IEnumerable<IDictionary<string, object>> Query(string sql, object param, CommandType? commandType)
        {
            return Query(sql, param, null, commandType);
        }

        /// <summary>
        /// Retornar uma lista de objetos dinâmicos, a conexão é fechada após a chamada
        /// </summary>
        public IEnumerable<IDictionary<string, object>> Query(string sql, object param, int? commandTimeout, CommandType? commandType)
        {
            CreateOrReuseConnection();
            //Dapper will open and close the connection for us if necessary.
            return _connection.Query(sql, param, GetCurrentTransaction(), true, commandTimeout, commandType);
        }
#endif

#if CSHARP30
        /// <summary>
        /// Executa SQL parametrizado
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public int Execute(string sql, object param)
        {
            return Execute(sql, param, null, null);
        }

        /// <summary>
        /// Executa SQL parametrizado
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public int Execute(string sql, object param, CommandType commandType)
        {
            return Execute(sql, param, null, commandType);
        }
        
        /// <summary>
        /// Executa uma consulta, retornando os dados conforme T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public IEnumerable<T> Query<T>(string sql, object param)
        {
            return Query<T>(sql, param, null, null);
        }
        
        /// <summary>
        ///  Executa uma consulta, retornando os dados conforme T
        /// </summary>
        /// <returns>A sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public IEnumerable<T> Query<T>(string sql, object param, CommandType commandType)
        {
            return Query<T>(sql, param, null, commandType);
        }
        
        /// <summary>
        /// Execute um comando que retorna vários conjuntos de resultados e acessa cada um deles
        /// </summary>
        public SqlMapper.GridReader QueryMultiple(string sql, object param)
        {
            return QueryMultiple(sql, param, null, null);
        }

        /// <summary>
        /// Execute um comando que retorna vários conjuntos de resultados e acessa cada um deles
        /// </summary>
        public SqlMapper.GridReader QueryMultiple(string sql, object param, CommandType commandType)
        {
            return QueryMultiple(sql, param, null, commandType);
        }
#endif

#if CSHARP30
        public IEnumerable<T> Query<T>(string sql, object param, int? commandTimeout, CommandType? commandType)
#else
        public IEnumerable<T> Query<T>(string sql, dynamic param = null, int? commandTimeout = null, CommandType? commandType = null)
#endif
        {
            CreateOrReuseConnection();

            _sql = sql;

            return SqlMapper.Query<T>(_connection, sql, param, GetCurrentTransaction(), true, commandTimeout, commandType);
        }

#if CSHARP30
        public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, object param, string splitOn, int? commandTimeout, CommandType? commandType)
#else
        public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
#endif
        {
            CreateOrReuseConnection();

            _sql = sql;

            return SqlMapper.Query(_connection, sql, map, param, GetCurrentTransaction(), true, splitOn, commandTimeout, commandType);
        }

#if CSHARP30
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param, string splitOn, int? commandTimeout, CommandType? commandType)
#else
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
#endif
        {
            CreateOrReuseConnection();

            _sql = sql;

            //Dapper will open and close the connection for us if necessary.
            return SqlMapper.Query(_connection, sql, map, param, GetCurrentTransaction(), true, splitOn, commandTimeout, commandType);
        }

#if CSHARP30
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param, string splitOn, int? commandTimeout, CommandType? commandType)
#else
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
#endif
        {
            CreateOrReuseConnection();

            _sql = sql;

            //Dapper will open and close the connection for us if necessary.
            return SqlMapper.Query(_connection, sql, map, param, GetCurrentTransaction(), true, splitOn, commandTimeout, commandType);
        }

#if !CSHARP30
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            CreateOrReuseConnection();

            _sql = sql;

            //Dapper will open and close the connection for us if necessary.
            return SqlMapper.Query(_connection, sql, map, param, GetCurrentTransaction(), true, splitOn, commandTimeout, commandType);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            CreateOrReuseConnection();

            _sql = sql;

            //Dapper will open and close the connection for us if necessary.
            return SqlMapper.Query(_connection, sql, map, param, GetCurrentTransaction(), true, splitOn, commandTimeout, commandType);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, dynamic param = null, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            CreateOrReuseConnection();

            _sql = sql;

            //Dapper will open and close the connection for us if necessary.
            return SqlMapper.Query(_connection, sql, map, param, GetCurrentTransaction(), true, splitOn, commandTimeout, commandType);
        }
#endif

#if CSHARP30
        public SqlMapper.GridReader QueryMultiple(string sql, object param, int? commandTimeout, CommandType? commandType)
#else
        public SqlMapper.GridReader QueryMultiple(string sql, dynamic param = null, int? commandTimeout = null, CommandType? commandType = null)
#endif
        {
            CreateOrReuseConnection();

            _sql = sql;

            //Dapper will open and close the connection for us if necessary.
            return SqlMapper.QueryMultiple(_connection, sql, param, GetCurrentTransaction(), commandTimeout, commandType);
        }


#if CSHARP30
        public int Execute(string sql, object param, int? commandTimeout, CommandType? commandType)
#else
        public int Execute(string sql, dynamic param = null, int? commandTimeout = null, CommandType? commandType = null)
#endif
        {
            CreateOrReuseConnection();

            //Dapper expects a connection to be open when calling Execute, so we'll have to open it.
            bool wasClosed = _connection.State == ConnectionState.Closed;

            if (wasClosed) _connection.Open();

            try
            {
                _sql = sql;

                return SqlMapper.Execute(_connection, sql, param, GetCurrentTransaction(), commandTimeout, commandType);
            }
            finally
            {
                if (wasClosed) _connection.Close();
            }
        }

        /// <summary>
        /// Retorna o último comando SQL que foi executado
        /// </summary>
        /// <returns></returns>
        public string GetSQL()
        {
            if (string.IsNullOrEmpty(_sql))
            {
                _sql = GetSQL();
            }

            return _sql;
        }


        private void RemoveTransaction(UnitOfWork workItem)
        {
            _rwLock.EnterWriteLock();
            _workItems.Remove(workItem);
            _rwLock.ExitWriteLock();
        }

        private void RemoveTransactionAndCloseConnection(UnitOfWork workItem)
        {
            _rwLock.EnterWriteLock();
            _workItems.Remove(workItem);
            _rwLock.ExitWriteLock();

            _connection.Close();
        }

        #region Metodos do DapperExt

        public T Get<T>(object id, int? commandTimeout = null)
        {
            return _connection.Get<T>(id, GetCurrentTransaction(), commandTimeout);
        }

        public T Get<T>(Expression<Func<T, bool>> where)
        {
            CreateOrReuseConnection();
            
            return QueryFirst<T>(where);
        }

        /// <summary>
        /// Retorna registros da Tabela representada por T
        /// </summary>
        /// <typeparam name="T">Classe que representa a tabela do banco de dados</typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetList<T>()
        {
            CreateOrReuseConnection();

            return _connection.GetList<T>(new { });
        }


        public IEnumerable<T> GetList<T>(object whereConditions, int? commandTimeout = null)
        {
            CreateOrReuseConnection();

            return _connection.GetList<T>(whereConditions, GetCurrentTransaction(), commandTimeout);
        }


        public IEnumerable<T> GetList<T>(string conditions, object parameters = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            CreateOrReuseConnection();

            return _connection.GetList<T>(conditions, parameters, GetCurrentTransaction(), commandTimeout);
        }


        public int? Insert<TEntity>(TEntity entityToInsert, string sequenceName, int? commandTimeout = null)
        {
            CreateOrReuseConnection();

            return _connection.Insert<int?, TEntity>(entityToInsert, sequenceName, GetCurrentTransaction(), commandTimeout);
        }

        public int Update<TEntity>(TEntity entityToUpdate, int? commandTimeout = null)
        {
            CreateOrReuseConnection();

            return _connection.Update<TEntity>(entityToUpdate, GetCurrentTransaction(), commandTimeout);
        }
        #endregion

        public T ExecuteStoredProcedure<T>(string spName, dynamic param = null)
        {
            var result = (T)SqlMapper.Query(_connection, spName, param, commandType: CommandType.StoredProcedure).First();

            return result;
        }


        private QueryResult DynamicWhere(List<QueryParameter> queryProperties, Type currentType)
        {
            IDictionary<string, Object> expando = new ExpandoObject();
            QueryResult result = null;
            var builder = new StringBuilder();

            if (queryProperties.Count() > 0)
            {
                builder.Append(" where ");
            }

            for (int i = 0; i < queryProperties.Count(); i++)
            {
                QueryParameter item = queryProperties[i];

                var fieldType = GePropertyInfo(currentType, item.PropertyName); // currenttype.GetField(item.PropertyName, BindingFlags.PutRefDispProperty  );

                var columnTable = string.Empty;
                var param = string.Empty;

                if (fieldType != null && fieldType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>() != null)
                {
                    columnTable = fieldType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>().Name.ToLower();
                    param = columnTable;
                }
                else if (fieldType != null && fieldType.GetCustomAttribute<Dapper.DapperExt.ColumnAttribute>() != null)
                {
                    columnTable = fieldType.GetCustomAttribute<Dapper.DapperExt.ColumnAttribute>().Name.ToLower();
                    param = columnTable;
                }
                else if (fieldType != null && fieldType.CustomAttributes.Count() > 0)
                {

                    foreach (var atrib in fieldType.GetCustomAttributesData())
                    {
                       
                        if (atrib.AttributeType == typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute) || atrib.AttributeType == typeof(Dapper.DapperExt.ColumnAttribute))
                        {

                            string attributeTypeName = atrib.AttributeType.Name;

                            columnTable = atrib.ConstructorArguments[0].Value?.ToString();
                            param = columnTable;

                        }
                        //else if (atrib.AttributeType == typeof(Dapper.DapperExt.ColumnAttribute))
                        //{
                        //    columnTable = (Dapper.DapperExt.ColumnAttribute)atrib).Name;
                        //    param = columnTable;
                        //}
                    }
                }

                var dialeto = GetDialect();

                var prefixParam = dialeto == "Oracle" ? ":" : "@";

                if (!string.IsNullOrEmpty(columnTable))
                {
                    if (!string.IsNullOrEmpty(item.LinkingOperator) && i > 0)
                    {
                        //builder.Append(string.Format("{0} {1} {2} @{1} ", item.LinkingOperator, item.PropertyName, item.QueryOperator));
                        builder.Append(string.Format("{0} {1} {2} {3} ", item.LinkingOperator, Encapsulate(columnTable), item.QueryOperator, prefixParam + param));
                    }
                    else
                    {
                        //builder.Append(string.Format("{0} {1} @{0} ", item.PropertyName, item.QueryOperator));

                        builder.Append(string.Format("{0} {1} {2} ", Encapsulate(columnTable), item.QueryOperator, prefixParam + param));
                    }

                    expando[columnTable] = item.PropertyValue;
                }
                else
                {
                    if (!string.IsNullOrEmpty(item.LinkingOperator) && i > 0)
                    {
                        builder.Append(string.Format("{0} {1} {2} {3} ", item.LinkingOperator, item.PropertyName, item.QueryOperator, prefixParam + item.PropertyName));
                    }
                    else
                    {
                        builder.Append(string.Format("{0} {1} {2} ", item.PropertyName, item.QueryOperator, prefixParam + item.PropertyName));
                    }

                    expando[item.PropertyName] = item.PropertyValue;
                }

            }

            result = new QueryResult(builder.ToString().TrimEnd(), expando);

            return result;

        }

        private string DynamicWhere2(List<QueryParameter> queryProperties, Type currentType)
        {

            var builder = new StringBuilder();

            builder.Append(" Where ");

            for (int i = 0; i < queryProperties.Count(); i++)
            {
                QueryParameter item = queryProperties[i];

                var fieldType = GePropertyInfo(currentType, item.PropertyName); // currenttype.GetField(item.PropertyName, BindingFlags.PutRefDispProperty  );

                var columnTable = string.Empty;
                var param = string.Empty;

                if (fieldType != null && fieldType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>() != null)
                {
                    columnTable = fieldType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>().Name;
                    param = columnTable;
                }
                else if (fieldType != null && fieldType.GetCustomAttribute<Dapper.DapperExt.ColumnAttribute>() != null)
                {
                    columnTable = fieldType.GetCustomAttribute<Dapper.DapperExt.ColumnAttribute>().Name;
                    param = columnTable;
                }
                var dialeto = GetDialect();

                var prefixParam = dialeto == "Oracle" ? ":" : "@";

                if (!string.IsNullOrEmpty(columnTable))
                {
                    if (!string.IsNullOrEmpty(item.LinkingOperator) && i > 0)
                    {
                        //builder.Append(string.Format("{0} {1} {2} @{1} ", item.LinkingOperator, item.PropertyName, item.QueryOperator));
                        builder.Append(string.Format("{0} {1} {2} {3} ", item.LinkingOperator, Encapsulate(columnTable), item.QueryOperator, item.PropertyValue));
                    }
                    else
                    {
                        //builder.Append(string.Format("{0} {1} @{0} ", item.PropertyName, item.QueryOperator));

                        builder.Append(string.Format("{0} {1} {2} ", Encapsulate(columnTable), item.QueryOperator, item.PropertyValue));
                    }

                }
                else
                {
                    if (!string.IsNullOrEmpty(item.LinkingOperator) && i > 0)
                    {
                        builder.Append(string.Format("{0} {1} {2} {3} ", item.LinkingOperator, item.PropertyName, item.QueryOperator, item.PropertyValue));
                    }
                    else
                    {
                        builder.Append(string.Format("{0} {1} {2} ", item.PropertyName, item.QueryOperator, item.PropertyValue));
                    }

                }

            }

            return builder.ToString();

        }

        public T QueryFirst<T>(Expression<Func<T, bool>> where, int? commandTimeout = null)
        {
            var builder = new StringBuilder();

            var currenttype = typeof(T);


            if (where != null)
            {
                builder.Append(" where ");

                var queryProperties = new List<QueryParameter>();

                QueryResult result = null;

                if (where.Body is BinaryExpression)
                {
                    var body = (BinaryExpression)where.Body;

                    // Percorre a arvore de expressão e devolve os parametros da consulta
                    WalkTree(body, ExpressionType.Default, ref queryProperties);

                    result = DynamicWhere(queryProperties, currenttype);
                }
                else if (where.Body is MethodCallExpression)
                {
                    var body = where.Body as MethodCallExpression;

                    var methodName = body.Method.Name;
                    var propertyName = GetPropertyName(body);
                    var propertyValue = ((body.Arguments[0] as ConstantExpression).Value);
                    var link = GetOperator(ExpressionType.Default);
                    var opr = string.Empty;

                    switch (methodName)
                    {
                        case "Contains":
                            opr = " LIKE ";
                            propertyValue = string.Format("%{0}%", propertyValue);
                            break;
                        case "StartsWith":
                            opr = " LIKE ";
                            propertyValue = string.Format("{0}%", propertyValue);
                            break;
                        case "EndsWith":
                            opr = " LIKE ";
                            propertyValue = string.Format("%{0}", propertyValue);
                            break;
                    }

                    queryProperties.Add(new QueryParameter(link, propertyName, propertyValue, opr));

                    result = DynamicWhere(queryProperties, currenttype);
                }

                //return DapperExt.ExecuteQuey<T>(connection, result.Sql, result.Param);

                var idProps = GetIdProperties(currenttype).ToList();

                if (!idProps.Any())
                    throw new ArgumentException("A entidade deve ter pelo menos uma propriedade [Key]");

                var tableName = GetTableName(currenttype);

                var sb = new StringBuilder();

                sb.Append("Select ");

                //create a new empty instance of the type to get the base properties
                BuildSelect(sb, GetScaffoldableProperties<T>().ToArray());

                sb.AppendFormat(" from {0}", tableName);

                sb.Append(result.Sql);

                var parametros = ((IDictionary<string, object>)result.Param).ToDictionary(nvp => nvp.Key, nvp => nvp.Value);

                return _connection.QueryFirst<T>(sb.ToString(), parametros);
            }
            else
            {
                var idProps = GetIdProperties(currenttype).ToList();

                if (!idProps.Any())
                    throw new ArgumentException("A entidade deve ter pelo menos uma propriedade [Key]");

                var name = GetTableName(currenttype);

                var sb = new StringBuilder();

                sb.Append("Select ");

                //create a new empty instance of the type to get the base properties
                BuildSelect(sb, GetScaffoldableProperties<T>().ToArray());

                sb.AppendFormat(" from {0}", name);

                //sb.Append(where);
                //// sb.Append(" " + conditions);

                //if (Debugger.IsAttached)
                //    Trace.WriteLine(String.Format("GetList<{0}>: {1}", currenttype, sb));

                _sql = sb.ToString();

                return _connection.QueryFirst<T>(sb.ToString());
            }
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, bool>> where, int? commandTimeout = null)
        {
            var builder = new StringBuilder();

            var currenttype = typeof(T);

            CreateOrReuseConnection();

            if (where != null)
            {
                builder.Append(" where ");

                var queryProperties = new List<QueryParameter>();

                QueryResult result = null;

                if (where.Body is BinaryExpression)
                {
                    var body = (BinaryExpression)where.Body;

                    // Percorre a arvore de expressão e devolve os parametros da consulta
                    WalkTree(body, ExpressionType.Default, ref queryProperties);

                    result = DynamicWhere(queryProperties, currenttype);
                }
                else if (where.Body is MethodCallExpression)
                {
                    var body = where.Body as MethodCallExpression;

                    var methodName = body.Method.Name;
                    var propertyName = GetPropertyName(body);
                    var propertyValue = ((body.Arguments[0] as ConstantExpression).Value);
                    var link = GetOperator(ExpressionType.Default);
                    var opr = string.Empty;

                    switch (methodName)
                    {
                        case "Contains":
                            opr = " LIKE ";
                            propertyValue = string.Format("%{0}%", propertyValue);
                            break;
                        case "StartsWith":
                            opr = " LIKE ";
                            propertyValue = string.Format("{0}%", propertyValue);
                            break;
                        case "EndsWith":
                            opr = " LIKE ";
                            propertyValue = string.Format("%{0}", propertyValue);
                            break;
                    }

                    queryProperties.Add(new QueryParameter(link, propertyName, propertyValue, opr));

                    result = DynamicWhere(queryProperties, currenttype);
                }

                //return DapperExt.ExecuteQuey<T>(connection, result.Sql, result.Param);

                var idProps = GetIdProperties(currenttype).ToList();

                if (!idProps.Any())
                    throw new ArgumentException("A entidade deve ter pelo menos uma propriedade [Key]");

                var tableName = GetTableName(currenttype);

                var sb = new StringBuilder();

                sb.Append("Select ");

                //create a new empty instance of the type to get the base properties
                BuildSelect(sb, GetScaffoldableProperties<T>().ToArray());

                sb.AppendFormat(" from {0}", tableName);

                sb.Append(result.Sql);

                var parametros = ((IDictionary<string, object>)result.Param).ToDictionary(nvp => nvp.Key, nvp => nvp.Value);

                return _connection.Query<T>(sb.ToString(), parametros);
            }
            else
            {
                var idProps = GetIdProperties(currenttype).ToList();

                if (!idProps.Any())
                    throw new ArgumentException("A entidade deve ter pelo menos uma propriedade [Key]");

                var name = GetTableName(currenttype);

                var sb = new StringBuilder();

                sb.Append("Select ");

                //create a new empty instance of the type to get the base properties
                BuildSelect(sb, GetScaffoldableProperties<T>().ToArray());

                sb.AppendFormat(" from {0}", name);

                //sb.Append(where);
                //// sb.Append(" " + conditions);

                //if (Debugger.IsAttached)
                //    Trace.WriteLine(String.Format("GetList<{0}>: {1}", currenttype, sb));

                _sql = sb.ToString();

                return _connection.Query<T>(sb.ToString());
            }
        }

        
        private static PropertyInfo GePropertyInfo(Type type, string propertyName)
        {
            //if (type.GetProperties() GetCustomAttributes<Dapper.DapperExt.ColumnAttribute>() != null))

            var tp = type.GetProperties().Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(Dapper.DapperExt.ColumnAttribute).Name)).ToList();

            if (tp is null)
            {
                tp = type.GetProperties().Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute).Name)).ToList();

            }

            return type.GetProperties().Where(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)).First();
        }

        private string BuildWhere<T>(Expression<Func<T, bool>> where)
        {
            var queryProperties = new List<QueryParameter>();
            var body = (BinaryExpression)where.Body;

            IDictionary<string, Object> expando = new ExpandoObject();

            var buildWhere = new StringBuilder();

            // Percorre a arvore de expressão e devolve os parametros da consulta
            WalkTree(body, ExpressionType.Default, ref queryProperties);

            for (int i = 0; i < queryProperties.Count(); i++)
            {
                QueryParameter item = queryProperties[i];

                if (!string.IsNullOrEmpty(item.LinkingOperator) && i > 0)
                {
                    buildWhere.Append(string.Format("{0} {1} {2} @{1} ", item.LinkingOperator, item.PropertyName, item.QueryOperator));
                }
                else
                {
                    buildWhere.Append(string.Format("{0} {1} @{0} ", item.PropertyName, item.QueryOperator));
                }

                expando[item.PropertyName] = item.PropertyValue;
            }

            return buildWhere.ToString();
        }

        private static void WalkTree(BinaryExpression body, ExpressionType linkingType, ref List<QueryParameter> queryProperties)
        {
            if (body.NodeType != ExpressionType.AndAlso && body.NodeType != ExpressionType.OrElse)
            {
                var propertyName = GetPropertyName(body);

                dynamic right = null;

                if (body.Right.NodeType == ExpressionType.Constant)
                {
                    right = (ConstantExpression)body.Right;
                }
                else if (body.Right.NodeType == ExpressionType.Convert || body.Right.NodeType == ExpressionType.ConvertChecked)
                {

                    // MemberExpression me;

                    // var ue = body.Right as UnaryExpression;

                    // me = ((ue != null) ? ue.Operand : null) as MemberExpression;


                    // right = me == null ? ue : me;

                    right = (UnaryExpression)body.Right;
                }
                else
                {
                    right = (MemberExpression)body.Right;

                }

                var propertyValue = Expression.Lambda(right).Compile().DynamicInvoke();
                var opr = GetOperator(body.NodeType);
                var link = GetOperator(linkingType);

                if (propertyValue == null && opr == "=")
                {
                    opr = " Is null";
                    propertyValue = "";
                }
                else if (propertyValue == null && opr == "<>")
                {
                    opr = " Is Not null";
                    propertyValue = "";

                    queryProperties.Add(new QueryParameter(link, propertyName, propertyValue, opr));
                }


                queryProperties.Add(new QueryParameter(link, propertyName, propertyValue, opr));

            }
            else
            {
                if (body.Left is BinaryExpression)
                {
                    WalkTree((BinaryExpression)body.Left, body.NodeType, ref queryProperties);
                }
                else
                {
                    var bodyLeft = body.Left as MethodCallExpression;

                    var methodName = bodyLeft.Method.Name;
                    var propertyName = GetPropertyName(body);
                    var propertyValue = ((bodyLeft.Arguments[0] as ConstantExpression).Value);
                    var link = GetOperator(ExpressionType.Default);
                    var opr = string.Empty;

                    switch (methodName)
                    {
                        case "Contains":
                            opr = " LIKE ";
                            propertyValue = string.Format("%{0}%", propertyValue);
                            break;
                        case "StartsWith":
                            opr = " LIKE ";
                            propertyValue = string.Format("{0}%", propertyValue);
                            break;
                        case "EndsWith":
                            opr = " LIKE ";
                            propertyValue = string.Format("%{0}", propertyValue);
                            break;
                    }

                    queryProperties.Add(new QueryParameter(link, propertyName, propertyValue, opr));
                }

                if (body.Right is BinaryExpression)
                {
                    WalkTree((BinaryExpression)body.Right, body.NodeType, ref queryProperties);
                }
                else
                {
                    var bodyRight = body.Right as MethodCallExpression;

                    var methodName = bodyRight.Method.Name;
                    var propertyName = GetPropertyName(body);
                    var propertyValue = ((bodyRight.Arguments[0] as ConstantExpression).Value);
                    var link = GetOperator(ExpressionType.Default);
                    var opr = string.Empty;

                    switch (methodName)
                    {
                        case "Contains":
                            opr = " LIKE ";
                            propertyValue = string.Format("%{0}%", propertyValue);
                            break;
                        case "StartsWith":
                            opr = " LIKE ";
                            propertyValue = string.Format("{0}%", propertyValue);
                            break;
                        case "EndsWith":
                            opr = " LIKE ";
                            propertyValue = string.Format("%{0}", propertyValue);
                            break;
                    }

                    queryProperties.Add(new QueryParameter(link, propertyName, propertyValue, opr));
                }
            }
        }
       

        private static string GetPropertyName(BinaryExpression body)
        {
            string propertyName = body.Left.ToString().Split(new char[] { '.' })[1];

            if (body.Left.NodeType == ExpressionType.Convert)
            {
                // hack to remove the trailing ) when convering.
                propertyName = propertyName.Replace(")", string.Empty);
            }

            return propertyName;
        }

        private static string GetPropertyName(MethodCallExpression body)
        {
            var property = body.Object as MemberExpression;
            var propertyName = property.Member.Name;

            return propertyName;
        }

        private static string GetOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.AndAlso:
                case ExpressionType.And:
                    return "AND";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Default:
                    return string.Empty;
                default:
                    throw new NotImplementedException();
            }
        }

        public void Dispose()
        {

            _rwLock.EnterUpgradeableReadLock();

            try
            {
                while (_workItems.Any())
                {
                    var workItem = _workItems.First;
                    workItem.Value.Dispose(); //rollback, removerá o item da LinkedList porque ele chama RemoveTransaction ou RemoveTransactionAndCloseConnection
                }
            }
            finally
            {
                _rwLock.ExitUpgradeableReadLock();
            }

            if (_connection != null)
            {
                this.DbConnection.Dispose();
                this.DbTransaction?.Dispose();
                _connection.Dispose();
                _connection = null;
            }
        }


        private static IEnumerable<PropertyInfo> GetIdProperties(object entity)
        {
            var type = entity.GetType();
            return GetIdProperties(type);
        }

        //Get all properties that are named Id or have the Key attribute
        //For Get(id) and Delete(id) we don't have an entity, just the type so this method is used
        private static IEnumerable<PropertyInfo> GetIdProperties(Type type)
        {
            var tp = type.GetProperties().Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(KeyAttribute).Name)).ToList();
            return tp.Any() ? tp : type.GetProperties().Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        }

        
    }



    /// <summary>
    /// Classe que modela a estrutura de dados na conversão da árvore de expressão em SQL e Params.
    /// </summary>
    internal class QueryParameter
    {
        public string LinkingOperator { get; set; }
        public string PropertyName { get; set; }
        public object PropertyValue { get; set; }
        public string QueryOperator { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParameter" /> class.
        /// </summary>
        /// <param name="linkingOperator">The linking operator.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="queryOperator">The query operator.</param>
        internal QueryParameter(string linkingOperator, string propertyName, object propertyValue, string queryOperator)
        {
            this.LinkingOperator = linkingOperator;
            this.PropertyName = propertyName;
            this.PropertyValue = propertyValue;
            this.QueryOperator = queryOperator;
        }
    }
}
