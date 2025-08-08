using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using DapperExt;

namespace Dapper
{
    public static partial class DapperExt
    {
        private static Dialect _dialect = Dialect.Oracle;
        private static string _encapsulation;
        private static string _getIdentitySql;
        private static string _getPagedListSql;
        private static string _schema;

        private static readonly IDictionary<Type, string> TableNames = new Dictionary<Type, string>();
        private static readonly IDictionary<string, string> ColumnNames = new Dictionary<string, string>();

        private static ITableNameResolver _tableNameResolver = new TableNameResolver();
        private static IColumnNameResolver _columnNameResolver = new ColumnNameResolver();

        private static string _sql { get; set; }

        static DapperExt()
        {
            SetDialect(_dialect);
        }

        /// <summary>
        /// Retorna o último comando SQL que foi executado
        /// </summary>
        /// <returns></returns>
        public static string GetSQL()
        {
            return _sql;
        }

        public static string Encapsulate(string databaseword)
        {
            return string.Format(_encapsulation, databaseword);
        }


        public static Guid SequentialGuid()
        {
            var tempGuid = Guid.NewGuid();
            var bytes = tempGuid.ToByteArray();
            var time = DateTime.Now;
            bytes[3] = (byte)time.Year;
            bytes[2] = (byte)time.Month;
            bytes[1] = (byte)time.Day;
            bytes[0] = (byte)time.Hour;
            bytes[5] = (byte)time.Minute;
            bytes[4] = (byte)time.Second;
            return new Guid(bytes);
        }

        public static string GetDialect()
        {
            return _dialect.ToString();
        }

        public static void SetDialect(Dialect dialect)
        {
            switch (dialect)
            {
                case Dialect.Oracle:
                    _dialect = Dialect.Oracle;
                    _encapsulation = "\"{0}\"";
                    _getIdentitySql = "SELECT {0}.CURRVAL AS ID From Dual";
                    _getPagedListSql = "Select * From( Select topn.*,ROWNUM rnum From (Select {SelectColumns} From {TableName} {WhereClause} {OrderBy} ) topn Where ROWNUM <= {RowsPerPage}) Where  rnum  >= {StartRow}";
                    break;

                case Dialect.PostgreSQL:
                    _dialect = Dialect.PostgreSQL;
                    _encapsulation = "\"{0}\"";
                    _getIdentitySql = "SELECT LASTVAL() AS id";
                    _getPagedListSql = "Select {SelectColumns} from {TableName} {WhereClause} Order By {OrderBy} LIMIT {RowsPerPage} OFFSET ({PageNumber}-1) * {RowsPerPage}";
                    break;
                case Dialect.SQLite:
                    _dialect = Dialect.SQLite;
                    _encapsulation = "\"{0}\"";
                    _getIdentitySql = string.Format("SELECT LAST_INSERT_ROWID() AS id");
                    _getPagedListSql = "Select {SelectColumns} from {TableName} {WhereClause} Order By {OrderBy} LIMIT {RowsPerPage} OFFSET (({PageNumber}-1) * {RowsPerPage})";
                    break;
                case Dialect.MySQL:
                    _dialect = Dialect.MySQL;
                    _encapsulation = "`{0}`";
                    _getIdentitySql = string.Format("SELECT LAST_INSERT_ID() AS id");
                    _getPagedListSql = "Select {SelectColumns} from {TableName} {WhereClause} Order By {OrderBy} LIMIT {Offset},{RowsPerPage}";
                    break;
                default:
                    _dialect = Dialect.SQLServer;
                    _encapsulation = "[{0}]";
                    _getIdentitySql = string.Format("SELECT CAST(SCOPE_IDENTITY()  AS BIGINT) AS [id]");
                    _getPagedListSql = "SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY {OrderBy}) AS PagedNumber, {SelectColumns} FROM {TableName} {WhereClause}) AS u WHERE PagedNUMBER BETWEEN (({PageNumber}-1) * {RowsPerPage} + 1) AND ({PageNumber} * {RowsPerPage})";
                    break;
            }
        }

        /// <summary>
        /// Define o Schema que será usado para acessar os daodos, pode ser definido também no atributo [Table] ou aqui caso necessite mudar o schema dinamicamente
        /// </summary>
        /// <param name="schema"></param>
        public static void SetSchemaName(string schema)
        {
            _schema = schema;
        }

        public static void SetTableNameResolver(ITableNameResolver resolver)
        {
            _tableNameResolver = resolver;
        }

        public static void SetColumnNameResolver(IColumnNameResolver resolver)
        {
            _columnNameResolver = resolver;
        }


        public static T Get<T>(this IDbConnection connection, object id, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype).ToList();

            if (!idProps.Any())
                throw new ArgumentException("Get<T> suporta somente entidades que possuam o atributo [Key] ou uma propriedade de nome Id");

            var name = GetTableName(currenttype);
            var sb = new StringBuilder();

            sb.Append("Select ");

            //create a new empty instance of the type to get the base properties
            BuildSelect(sb, GetScaffoldableProperties<T>().ToArray());

            sb.AppendFormat(" from {0} where ", name);


            var dynParms = new DynamicParameters();

            if (_dialect == Dialect.Oracle)
            {
                for (var i = 0; i < idProps.Count; i++)
                {
                    if (i > 0)
                        sb.Append(" and ");
                    sb.AppendFormat("{0} = :{1}", GetColumnName(idProps[i]), idProps[i].Name);
                }

                if (idProps.Count == 1)
                    dynParms.Add(":" + idProps.First().Name, id);
                else
                {
                    foreach (var prop in idProps)
                        dynParms.Add(":" + prop.Name, id.GetType().GetProperty(prop.Name).GetValue(id, null));
                }
            }
            else
            {
                for (var i = 0; i < idProps.Count; i++)
                {
                    if (i > 0)
                        sb.Append(" and ");
                    sb.AppendFormat("{0} = @{1}", GetColumnName(idProps[i]), idProps[i].Name);
                }

                if (idProps.Count == 1)
                    dynParms.Add("@" + idProps.First().Name, id);
                else
                {
                    foreach (var prop in idProps)
                        dynParms.Add("@" + prop.Name, id.GetType().GetProperty(prop.Name).GetValue(id, null));
                }
            }


            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Get<{0}>: {1} with Id: {2}", currenttype, sb, id));

            _sql = sb.ToString();

            return connection.Query<T>(sb.ToString(), dynParms, transaction, true, commandTimeout).FirstOrDefault();
        }


        public static IEnumerable<T> GetList<T>(this IDbConnection connection, object whereConditions, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype).ToList();
            if (!idProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            var whereprops = GetAllProperties(whereConditions).ToArray();
            sb.Append("Select ");
            //create a new empty instance of the type to get the base properties
            BuildSelect(sb, GetScaffoldableProperties<T>().ToArray());
            sb.AppendFormat(" from {0}", name);

            if (whereprops.Any())
            {
                sb.Append(" where ");
                BuildWhere(sb, whereprops, (T)Activator.CreateInstance(typeof(T)), whereConditions);
            }

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("GetList<{0}>: {1}", currenttype, sb));

            _sql = sb.ToString();

            return connection.Query<T>(sb.ToString(), whereConditions, transaction, true, commandTimeout);
        }

        /// <summary>
        /// <para>By default queries the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>conditions is an SQL where clause and/or order by clause ex: "where name='bob'" or "where age>=@Age"</para>
        /// <para>parameters is an anonymous type to pass in named parameter values: new { Age = 15 }</para>
        /// <para>Supports transaction and command timeout</para>
        /// <para>Returns a list of entities that match where conditions</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="conditions"></param>
        /// <param name="parameters"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>Gets a list of entities with optional SQL where conditions</returns>
        public static IEnumerable<T> GetList<T>(this IDbConnection connection, string conditions, object parameters = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype).ToList();
            if (!idProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            sb.Append("Select ");
            //create a new empty instance of the type to get the base properties
            BuildSelect(sb, GetScaffoldableProperties<T>().ToArray());
            sb.AppendFormat(" from {0}", name);

            sb.Append(" " + conditions);

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("GetList<{0}>: {1}", currenttype, sb));

            _sql = sb.ToString();

            return connection.Query<T>(sb.ToString(), parameters, transaction, true, commandTimeout);
        }

        /// <summary>
        /// <para>By default queries the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>Returns a list of all entities</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <returns>Gets a list of all entities</returns>
        public static IEnumerable<T> GetList<T>(this IDbConnection connection)
        {
            return connection.GetList<T>(new { });
        }

        /// <summary>
        /// <para>By default queries the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>conditions is an SQL where clause ex: "where name='bob'" or "where age>=@Age" - not required </para>
        /// <para>orderby is a column or list of columns to order by ex: "lastname, age desc" - not required - default is by primary key</para>
        /// <para>parameters is an anonymous type to pass in named parameter values: new { Age = 15 }</para>
        /// <para>Supports transaction and command timeout</para>
        /// <para>Returns a list of entities that match where conditions</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="pageNumber"></param>
        /// <param name="rowsPerPage"></param>
        /// <param name="conditions"></param>
        /// <param name="orderby"></param>
        /// <param name="parameters"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>Gets a paged list of entities with optional exact match where conditions</returns>
        public static IEnumerable<T> GetListPaged<T>(this IDbConnection connection, int pageNumber, int rowsPerPage, string conditions, string orderby, object parameters = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (string.IsNullOrEmpty(_getPagedListSql))
                throw new Exception("GetListPage is not supported with the current SQL Dialect");

            if (pageNumber < 1)
                throw new Exception("Page must be greater than 0");

            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype).ToList();
            if (!idProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var name = GetTableName(currenttype);
            var sb = new StringBuilder();
            var query = _getPagedListSql;
            if (string.IsNullOrEmpty(orderby))
            {
                orderby = GetColumnName(idProps.First());
            }

            //create a new empty instance of the type to get the base properties
            BuildSelect(sb, GetScaffoldableProperties<T>().ToArray());
            query = query.Replace("{SelectColumns}", sb.ToString());
            query = query.Replace("{TableName}", name);
            query = query.Replace("{PageNumber}", pageNumber.ToString());
            query = query.Replace("{RowsPerPage}", rowsPerPage.ToString());
            query = query.Replace("{OrderBy}", orderby);
            query = query.Replace("{WhereClause}", conditions);
            query = query.Replace("{Offset}", ((pageNumber - 1) * rowsPerPage).ToString());

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("GetListPaged<{0}>: {1}", currenttype, query));

            _sql = sb.ToString();

            return connection.Query<T>(query, parameters, transaction, true, commandTimeout);
        }

        /// <summary>
        /// <para>Inserts a row into the database</para>
        /// <para>By default inserts into the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>Insert filters out Id column and any columns with the [Key] attribute</para>
        /// <para>Properties marked with attribute [Editable(false)] and complex types are ignored</para>
        /// <para>Supports transaction and command timeout</para>
        /// <para>Returns the ID (primary key) of the newly inserted record if it is identity using the int? type, otherwise null</para>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="entityToInsert"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>The ID (primary key) of the newly inserted record if it is identity using the int? type, otherwise null</returns>
        public static int? Insert<TEntity>(this IDbConnection connection, TEntity entityToInsert, string sequenceName, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return Insert<int?, TEntity>(connection, entityToInsert, sequenceName, transaction, commandTimeout);
        }

        public static int Inserir<TEntity>(this IDbConnection connection, TEntity entityToInsert, IDbTransaction transaction = null)
        {
            return Insert<int, TEntity>(connection, entityToInsert, transaction, null);
        }

        /// <summary>
        /// <para>Inserts a row into the database, using ONLY the properties defined by TEntity</para>
        /// <para>By default inserts into the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>Insert filters out Id column and any columns with the [Key] attribute</para>
        /// <para>Properties marked with attribute [Editable(false)] and complex types are ignored</para>
        /// <para>Supports transaction and command timeout</para>
        /// <para>Returns the ID (primary key) of the newly inserted record if it is identity using the defined type, otherwise null</para>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="entityToInsert"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>The ID (primary key) of the newly inserted record if it is identity using the defined type, otherwise null</returns>
        public static TKey Insert<TKey, TEntity>(this IDbConnection connection, TEntity entityToInsert, string sequenceName, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var idProps = GetIdProperties(entityToInsert).ToList();

            if (!idProps.Any())
                throw new ArgumentException("Insert<T> suporta somente classes que possuam ao menos uma propriedade decorada com [Key]");

            var keyHasPredefinedValue = false;
            var baseType = typeof(TKey);
            var underlyingType = Nullable.GetUnderlyingType(baseType);
            var keytype = underlyingType ?? baseType;

            if (keytype != typeof(int) && keytype != typeof(uint) && keytype != typeof(long) && keytype != typeof(ulong) && keytype != typeof(short) && keytype != typeof(ushort) && keytype != typeof(Guid) && keytype != typeof(string))
            {
                throw new Exception("Invalid return type");
            }

            var name = GetTableName(entityToInsert);

            if (string.IsNullOrEmpty(sequenceName) && _dialect == Dialect.PostgreSQL && idProps.Count == 1)
            {
                var tableName = name.Replace(_schema, "").Replace(@"\", "").Replace(".", "").ToLower();
                var columnKey = idProps[0].Name.ToUnderscoreCase(true);

                sequenceName = $"{tableName}_{columnKey}_seq";
            }

            var sb = new StringBuilder();

            sb.AppendFormat("insert into {0}", name);
            sb.Append(" (");
            BuildInsertParameters<TEntity>(sb);
            sb.Append(") ");
            sb.Append("values");
            sb.Append(" (");
            BuildInsertValues<TEntity>(sb);
            sb.Append(")");

            if (keytype == typeof(Guid))
            {
                var guidvalue = (Guid)idProps.First().GetValue(entityToInsert, null);
                if (guidvalue == Guid.Empty)
                {
                    var newguid = SequentialGuid();
                    idProps.First().SetValue(entityToInsert, newguid, null);
                }
                else
                {
                    keyHasPredefinedValue = true;
                }
                sb.Append(";select '" + idProps.First().GetValue(entityToInsert, null) + "' as id");
            }

            if ((keytype == typeof(int) || keytype == typeof(long)) && Convert.ToInt64(idProps.First().GetValue(entityToInsert, null)) == 0)
            {
                //sb.Append(";"); // + _getIdentitySql);
                keyHasPredefinedValue = false;
            }
            else
            {
                keyHasPredefinedValue = true;
            }

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Insert: {0}", sb));

            _sql = sb.ToString();

            var recordAffect = connection.Execute(sb.ToString(), entityToInsert, transaction, commandTimeout);

            if (recordAffect > 0 && string.IsNullOrEmpty(sequenceName) == false)
            {
                var r = connection.Query(_getIdentitySql, null, transaction);  // connection.Query(string.Format(_getIdentitySql, sequenceName), null, transaction); 

                if (r.Count() == 0) return default(TKey);

                if (keytype == typeof(Guid) || keyHasPredefinedValue)
                {
                    var value = idProps.First().GetValue(entityToInsert, null);

                    return (TKey)value; // (TKey)idProps.First().GetValue(entityToInsert, null);
                }

                return (TKey)r.First().ID;
            }
            else
            {
                return default(TKey);
            }

        }


        public static TKey Insert<TKey, TEntity>(this IDbConnection connection, TEntity entityToInsert, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var idProps = GetIdProperties(entityToInsert).ToList();

            if (!idProps.Any())
                throw new ArgumentException("Insert<T> suporta somente classes que possuam ao menos uma propriedade decorada com [Key]");

            var keyHasPredefinedValue = false;
            var baseType = typeof(TKey);
            var underlyingType = Nullable.GetUnderlyingType(baseType);
            var keytype = underlyingType ?? baseType;
            if (keytype != typeof(int) && keytype != typeof(uint) && keytype != typeof(long) && keytype != typeof(ulong) && keytype != typeof(short) && keytype != typeof(ushort) && keytype != typeof(Guid) && keytype != typeof(string))
            {
                throw new Exception("Invalid return type");
            }

            var properPk = idProps.FirstOrDefault(p => p.GetCustomAttributes(false).Any(a => a.GetType() == typeof(KeyAttribute)));
            var pk = properPk.GetCustomAttribute<ColumnAttribute>().Name;

            var name = GetTableName(entityToInsert);
            var sb = new StringBuilder();

            sb.AppendFormat("insert into {0}", name);
            sb.Append(" (");
            BuildInsertParameters<TEntity>(sb);
            sb.Append(") ");
            sb.Append("values");
            sb.Append(" (");            
            BuildInsertValues<TEntity>(sb);            
            sb.Append($") RETURNING {pk} as id");

            if (keytype == typeof(Guid))
            {
                var guidvalue = (Guid)idProps.First().GetValue(entityToInsert, null);

                if (guidvalue == Guid.Empty)
                {
                    var newguid = SequentialGuid();
                    idProps.First().SetValue(entityToInsert, newguid, null);
                }
                else
                {
                    keyHasPredefinedValue = true;
                }

                sb.Append(";select '" + idProps.First().GetValue(entityToInsert, null) + "' as id");
            }

            if ((keytype == typeof(int) || keytype == typeof(long)) && Convert.ToInt64(idProps.First().GetValue(entityToInsert, null)) == 0)
            {
                //sb.Append(";"); // + _getIdentitySql);
                keyHasPredefinedValue = false;
            }
            else
            {
                keyHasPredefinedValue = true;
            }

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Insert: {0}", sb));

            _sql = sb.ToString();

            var result = connection.QueryFirst<dynamic>(sb.ToString(), entityToInsert, transaction, commandTimeout);


            return (TKey)result.id;            
        }

        public static bool Insert<TEntity>(this IDbConnection connection, TEntity entityToInsert)
        {
            var idProps = GetIdProperties(entityToInsert).ToList();

            if (!idProps.Any())
                throw new ArgumentException("Insert<T> suporta apenas uma entidade com uma propriedade [Key]");

            //var keyHasPredefinedValue = false;


            var name = GetTableName(entityToInsert);
            var sb = new StringBuilder();

            sb.AppendFormat("insert into {0}", name);
            sb.Append(" (");
            BuildInsertParameters<TEntity>(sb);
            sb.Append(") ");
            sb.Append("values");
            sb.Append(" (");
            BuildInsertValues<TEntity>(sb);
            sb.Append(")");

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Insert: {0}", sb));

            _sql = sb.ToString();

            var recordAffect = connection.Execute(sb.ToString(), entityToInsert);

            return recordAffect > 0;
        }

        public static bool Insert<TEntity>(this IDbConnection connection, TEntity entityToInsert, IDbTransaction transaction = null)
        {
            var idProps = GetIdProperties(entityToInsert).ToList();

            if (!idProps.Any())
                throw new ArgumentException("Insert<T> suporta apenas uma entidade com uma propriedade [Key]");

            //var keyHasPredefinedValue = false;


            var name = GetTableName(entityToInsert);
            var sb = new StringBuilder();

            sb.AppendFormat("insert into {0}", name);
            sb.Append(" (");
            BuildInsertParameters<TEntity>(sb);
            sb.Append(") ");
            sb.Append("values");
            sb.Append(" (");
            BuildInsertValues<TEntity>(sb);
            sb.Append(")");

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Insert: {0}", sb));

            _sql = sb.ToString();

            var recordAffect = connection.Execute(sb.ToString(), entityToInsert);

            return recordAffect > 0;
        }


        /// <summary>
        /// <para>Updates a record or records in the database with only the properties of TEntity</para>
        /// <para>By default updates records in the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>Updates records where the Id property and properties with the [Key] attribute match those in the database.</para>
        /// <para>Properties marked with attribute [Editable(false)] and complex types are ignored</para>
        /// <para>Supports transaction and command timeout</para>
        /// <para>Returns number of rows effected</para>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="entityToUpdate"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>The number of effected records</returns>
        public static int Update<TEntity>(this IDbConnection connection, TEntity entityToUpdate, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var idProps = GetIdProperties(entityToUpdate).ToList();

            if (!idProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] or Id property");

            var name = GetTableName(entityToUpdate);

            var sb = new StringBuilder();
            sb.AppendFormat("update {0}", name);

            sb.AppendFormat(" set ");
            BuildUpdateSet(entityToUpdate, sb);
            sb.Append(" where ");
            BuildWhere(sb, idProps, entityToUpdate);

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Update: {0}", sb));

            _sql = sb.ToString();

            return connection.Execute(sb.ToString(), entityToUpdate, transaction, commandTimeout);
        }

        /// <summary>
        /// <para>Deletes a record or records in the database that match the object passed in</para>
        /// <para>-By default deletes records in the table matching the class name</para>
        /// <para>Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>Supports transaction and command timeout</para>
        /// <para>Returns the number of records effected</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="entityToDelete"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>The number of records effected</returns>
        public static int Delete<T>(this IDbConnection connection, T entityToDelete, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var idProps = GetIdProperties(entityToDelete).ToList();


            if (!idProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] or Id property");

            var name = GetTableName(entityToDelete);

            var sb = new StringBuilder();
            sb.AppendFormat("delete from {0}", name);

            sb.Append(" where ");
            BuildWhere(sb, idProps, entityToDelete);

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Delete: {0}", sb));

            _sql = sb.ToString();

            return connection.Execute(sb.ToString(), entityToDelete, transaction, commandTimeout);
        }

        /// <summary>
        /// <para>Deletes a record or records in the database by ID</para>
        /// <para>By default deletes records in the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>Deletes records where the Id property and properties with the [Key] attribute match those in the database</para>
        /// <para>The number of records effected</para>
        /// <para>Supports transaction and command timeout</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>The number of records effected</returns>
        public static int Delete<T>(this IDbConnection connection, object id, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype).ToList();


            if (!idProps.Any())
                throw new ArgumentException("Delete<T> only supports an entity with a [Key] or Id property");

            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            sb.AppendFormat("Delete from {0} where ", name);

            for (var i = 0; i < idProps.Count; i++)
            {
                if (i > 0)
                    sb.Append(" and ");
                sb.AppendFormat("{0} = @{1}", GetColumnName(idProps[i]), idProps[i].Name);
            }

            var dynParms = new DynamicParameters();
            if (idProps.Count == 1)
                dynParms.Add("@" + idProps.First().Name, id);
            else
            {
                foreach (var prop in idProps)
                    dynParms.Add("@" + prop.Name, id.GetType().GetProperty(prop.Name).GetValue(id, null));
            }

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("Delete<{0}> {1}", currenttype, sb));

            _sql = sb.ToString();

            return connection.Execute(sb.ToString(), dynParms, transaction, commandTimeout);
        }

        /// <summary>
        /// <para>Deletes a list of records in the database</para>
        /// <para>By default deletes records in the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>Deletes records where that match the where clause</para>
        /// <para>whereConditions is an anonymous type to filter the results ex: new {Category = 1, SubCategory=2}</para>
        /// <para>The number of records effected</para>
        /// <para>Supports transaction and command timeout</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="whereConditions"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>The number of records effected</returns>
        public static int DeleteList<T>(this IDbConnection connection, object whereConditions, IDbTransaction transaction = null, int? commandTimeout = null)
        {

            var currenttype = typeof(T);
            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            var whereprops = GetAllProperties(whereConditions).ToArray();
            sb.AppendFormat("Delete from {0}", name);
            if (whereprops.Any())
            {
                sb.Append(" where ");
                BuildWhere(sb, whereprops, (T)Activator.CreateInstance(typeof(T)));
            }

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("DeleteList<{0}> {1}", currenttype, sb));

            _sql = sb.ToString();

            return connection.Execute(sb.ToString(), whereConditions, transaction, commandTimeout);
        }

        /// <summary>
        /// <para>Deletes a list of records in the database</para>
        /// <para>By default deletes records in the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>Deletes records where that match the where clause</para>
        /// <para>conditions is an SQL where clause ex: "where name='bob'" or "where age>=@Age"</para>
        /// <para>parameters is an anonymous type to pass in named parameter values: new { Age = 15 }</para>
        /// <para>Supports transaction and command timeout</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="conditions"></param>
        /// <param name="parameters"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>The number of records effected</returns>
        public static int DeleteList<T>(this IDbConnection connection, string conditions, object parameters = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            if (string.IsNullOrEmpty(conditions))
                throw new ArgumentException("DeleteList<T> requires a where clause");
            if (!conditions.ToLower().Contains("where"))
                throw new ArgumentException("DeleteList<T> requires a where clause and must contain the WHERE keyword");

            var currenttype = typeof(T);
            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            sb.AppendFormat("Delete from {0}", name);
            sb.Append(" " + conditions);

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("DeleteList<{0}> {1}", currenttype, sb));

            _sql = sb.ToString();

            return connection.Execute(sb.ToString(), parameters, transaction, commandTimeout);
        }

        /// <summary>
        /// <para>By default queries the table matching the class name</para>
        /// <para>-Table name can be overridden by adding an attribute on your class [Table("YourTableName")]</para>
        /// <para>Returns a number of records entity by a single id from table T</para>
        /// <para>Supports transaction and command timeout</para>
        /// <para>conditions is an SQL where clause ex: "where name='bob'" or "where age>=@Age" - not required </para>
        /// <para>parameters is an anonymous type to pass in named parameter values: new { Age = 15 }</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="conditions"></param>
        /// <param name="parameters"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>Returns a count of records.</returns>
        public static int RecordCount<T>(this IDbConnection connection, string conditions = "", object parameters = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var currenttype = typeof(T);
            var name = GetTableName(currenttype);
            var sb = new StringBuilder();

            sb.Append("Select count(1)");
            sb.AppendFormat(" from {0}", name);
            sb.Append(" " + conditions);

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("RecordCount<{0}>: {1}", currenttype, sb));

            _sql = sb.ToString();

            return connection.ExecuteScalar<int>(sb.ToString(), parameters, transaction, commandTimeout);
        }

        /// <summary>
        /// <para>Por padrão, consulta a tabela que corresponde ao nome da classe</para>
        /// <para>-O nome da tabela pode ser sobrescrito adicionando um atributo em sua classe [Table ("NomeTabela")]</para>
        /// <para>Retorna um número de entidades de registros por um único id da tabela T</para>
        /// <para>whereConditions é um tipo anônimo para filtrar os resultados ex: new {Category = 1, SubCategory = 2}</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="whereConditions"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>Returns a count of records.</returns>
        public static int RecordCount<T>(this IDbConnection connection, object whereConditions, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var currenttype = typeof(T);
            var name = GetTableName(currenttype);

            var sb = new StringBuilder();
            var whereprops = GetAllProperties(whereConditions).ToArray();

            sb.Append("Select count(1)");
            sb.AppendFormat(" from {0}", name);

            if (whereprops.Any())
            {
                sb.Append(" where ");
                BuildWhere(sb, whereprops, (T)Activator.CreateInstance(typeof(T)));
            }

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("RecordCount<{0}>: {1}", currenttype, sb));

            _sql = sb.ToString();

            return connection.ExecuteScalar<int>(sb.ToString(), whereConditions, transaction, commandTimeout);
        }

        /// <summary>
        /// Executa um Select Max na tabela alvo
        /// </summary>
        /// <typeparam name="T">Tabela Alvo</typeparam>
        /// <param name="connection"></param>
        /// <param name="columnMax"></param>
        /// <param name="whereConditions"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public static int GetMax<T>(this IDbConnection connection, string columnMax, object whereConditions = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var currenttype = typeof(T);

            var name = GetTableName(currenttype);

            var sb = new StringBuilder();



            sb.AppendFormat("Select max({0})", columnMax);
            sb.AppendFormat(" from {0}", name);

            if (whereConditions != null)
            {
                var whereprops = GetAllProperties(whereConditions).ToArray();

                if (whereprops.Any())
                {
                    sb.Append(" where ");
                    BuildWhere(sb, whereprops, (T)Activator.CreateInstance(typeof(T)));
                }
            }


            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("GetMax<{0}>: {1}", currenttype, sb));

            _sql = sb.ToString();

            return connection.ExecuteScalar<int>(sb.ToString(), whereConditions, transaction, commandTimeout);
        }

        //build update statement based on list on an entity
        private static void BuildUpdateSet<T>(T entityToUpdate, StringBuilder sb)
        {
            var nonIdProps = GetUpdateableProperties(entityToUpdate).ToArray();

            for (var i = 0; i < nonIdProps.Length; i++)
            {
                var property = nonIdProps[i];

                if (_dialect != Dialect.Oracle)
                {
                    sb.AppendFormat("{0} = @{1}", GetColumnName(property), property.Name);
                }
                else
                {
                    sb.AppendFormat("{0} = :{1}", GetColumnName(property), property.Name);
                }



                if (i < nonIdProps.Length - 1)
                    sb.AppendFormat(", ");
            }
        }

        //build select clause based on list of properties skipping ones with the IgnoreSelect and NotMapped attribute
        public static void BuildSelect(StringBuilder sb, IEnumerable<PropertyInfo> props)
        {
            var propertyInfos = props as IList<PropertyInfo> ?? props.ToList();
            var addedAny = false;
            for (var i = 0; i < propertyInfos.Count(); i++)
            {
                if (propertyInfos.ElementAt(i).GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(IgnoreSelectAttribute).Name || attr.GetType().Name == typeof(NotMappedAttribute).Name)) continue;

                if (addedAny)
                    sb.Append(",");
                sb.Append(GetColumnName(propertyInfos.ElementAt(i)));
                //if there is a custom column name add an "as customcolumnname" to the item so it maps properly
                if (propertyInfos.ElementAt(i).GetCustomAttributes(true).SingleOrDefault(attr => attr.GetType().Name == typeof(ColumnAttribute).Name) != null)
                    sb.Append(" as " + Encapsulate(propertyInfos.ElementAt(i).Name));
                addedAny = true;

            }
        }

        private static void BuildWhere<TEntity>(StringBuilder sb, IEnumerable<PropertyInfo> idProps, TEntity sourceEntity, object whereConditions = null)
        {
            var propertyInfos = idProps.ToArray();
            for (var i = 0; i < propertyInfos.Count(); i++)
            {
                var useIsNull = false;

                //match up generic properties to source entity properties to allow fetching of the column attribute
                //the anonymous object used for search doesn't have the custom attributes attached to them so this allows us to build the correct where clause
                //by converting the model type to the database column name via the column attribute
                var propertyToUse = propertyInfos.ElementAt(i);
                var sourceProperties = GetScaffoldableProperties<TEntity>().ToArray();
                for (var x = 0; x < sourceProperties.Count(); x++)
                {
                    if (sourceProperties.ElementAt(x).Name == propertyInfos.ElementAt(i).Name)
                    {
                        propertyToUse = sourceProperties.ElementAt(x);

                        if (whereConditions != null && propertyInfos.ElementAt(i).CanRead && (propertyInfos.ElementAt(i).GetValue(whereConditions, null) == null || propertyInfos.ElementAt(i).GetValue(whereConditions, null) == DBNull.Value))
                        {
                            useIsNull = true;
                        }
                        break;
                    }
                }

                if (_dialect != Dialect.Oracle)
                {
                    sb.AppendFormat(
                   useIsNull ? "{0} is null" : "{0} = @{1}",
                   GetColumnName(propertyToUse),
                   propertyInfos.ElementAt(i).Name);
                }
                else
                {
                    sb.AppendFormat(
                    useIsNull ? "{0} is null" : "{0} = :{1}",
                    GetColumnName(propertyToUse),
                    propertyInfos.ElementAt(i).Name);
                }

                if (i < propertyInfos.Count() - 1)
                    sb.AppendFormat(" and ");
            }
        }

        public static bool IsPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }

        //build insert values which include all properties in the class that are:
        //Not named Id
        //Not marked with the Editable(false) attribute
        //Not marked with the [Key] attribute (without required attribute)
        //Not marked with [IgnoreInsert]
        //Not marked with [NotMapped]
        private static void BuildInsertValues<T>(StringBuilder sb)
        {
            var props = GetScaffoldableProperties<T>().ToArray();

            for (var i = 0; i < props.Count(); i++)
            {
                var property = props.ElementAt(i);

                //if (property.PropertyType != typeof(Guid) && property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(KeyAttribute).Name)
                //                                           && property.GetCustomAttributes(true).All(attr => attr.GetType().Name != typeof(RequiredAttribute).Name)
                //                                           && property.GetCustomAttributes(true).All(attr => attr.GetType().Name == typeof(AutoIdentityAttribute).Name))
                if (property.PropertyType != typeof(Guid) && property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(KeyAttribute).Name ))
                {
                    var attributes = property.GetCustomAttributes(false);

                    if (attributes.Length > 0)
                    {
                        dynamic attrDBGenerated = attributes.FirstOrDefault(x => x.GetType().Name == typeof(AutoIdentityAttribute).Name || x.GetType().Name == typeof(DatabaseGeneratedAttribute).Name);

                        if (attrDBGenerated != null)
                        {
                            continue;
                        }
                    }
                }
                else if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(KeyAttribute).Name) && property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(DatabaseGeneratedAttribute).Name))
                {
                    var attributes = property.GetCustomAttributes(false);

                    if (attributes.Length > 0)
                    {
                        dynamic attrDBGenerated = attributes.FirstOrDefault(x => x.GetType().Name == typeof(DatabaseGeneratedAttribute).Name);

                        var value = attrDBGenerated.GetType().GetProperty("DatabaseGeneratedOption").GetValue(attrDBGenerated, null);

                        if (attrDBGenerated != null && Convert.ToString(value) == "Identity")
                        {
                            continue;
                        }
                    }
                }

                if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(IgnoreInsertAttribute).Name)) continue;
                if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(NotMappedAttribute).Name)) continue;
                if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(ReadOnlyAttribute).Name && IsReadOnly(property))) continue;

                if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) && property.GetCustomAttributes(true).All(attr => attr.GetType().Name != typeof(RequiredAttribute).Name) && property.PropertyType != typeof(Guid)) continue;

                var prefixParam = _dialect == Dialect.Oracle ? ":" : "@";

                sb.AppendFormat(prefixParam + "{0}", property.Name);

                if (i < props.Count() - 1)
                    sb.Append(", ");
            }

            if (sb.ToString().EndsWith(", "))
                sb.Remove(sb.Length - 2, 2);

        }

        //build insert parameters which include all properties in the class that are not:
        //marked with the Editable(false) attribute
        //marked with the [Key] attribute
        //marked with [IgnoreInsert]
        //named Id
        //marked with [NotMapped]
        private static void BuildInsertParameters<T>(StringBuilder sb)
        {
            var props = GetScaffoldableProperties<T>().ToArray();

            for (var i = 0; i < props.Count(); i++)
            {
                var property = props.ElementAt(i);

                //if (property.PropertyType != typeof(Guid) && property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(KeyAttribute).Name)
                //                                           && property.GetCustomAttributes(true).All(attr => attr.GetType().Name != typeof(RequiredAttribute).Name)
                //                                           && property.GetCustomAttributes(true).All(attr => attr.GetType().Name == typeof(AutoIdentityAttribute).Name))
                if (property.PropertyType != typeof(Guid) && property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(KeyAttribute).Name))
                {
                    var attributes = property.GetCustomAttributes(false);

                    if (attributes.Length > 0)
                    {
                        dynamic attrDBGenerated = attributes.FirstOrDefault(x => x.GetType().Name == typeof(AutoIdentityAttribute).Name || x.GetType().Name == typeof(DatabaseGeneratedAttribute).Name);

                        if (attrDBGenerated != null )
                        {
                            continue;
                        }
                    }
                }
                else if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(KeyAttribute).Name) && property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(DatabaseGeneratedAttribute).Name))
                {
                    var attributes = property.GetCustomAttributes(false);

                    if (attributes.Length > 0)
                    {
                        dynamic attrDBGenerated = attributes.FirstOrDefault(x => x.GetType().Name == typeof(DatabaseGeneratedAttribute).Name);

                        var value = attrDBGenerated.GetType().GetProperty("DatabaseGeneratedOption").GetValue(attrDBGenerated, null);

                        if (attrDBGenerated != null && Convert.ToString(value) == "Identity")
                        {
                            continue;
                        }
                    }
                }

                if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(IgnoreInsertAttribute).Name)) continue;
                if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(NotMappedAttribute).Name)) continue;

                if (property.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(ReadOnlyAttribute).Name && IsReadOnly(property))) continue;
                if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) && property.GetCustomAttributes(true).All(attr => attr.GetType().Name != typeof(RequiredAttribute).Name) && property.PropertyType != typeof(Guid)) continue;

                sb.Append(GetColumnName(property));

                if (i < props.Count() - 1)
                    sb.Append(", ");
            }
            if (sb.ToString().EndsWith(", "))
                sb.Remove(sb.Length - 2, 2);
        }

        //Get all properties in an entity
        private static IEnumerable<PropertyInfo> GetAllProperties<T>(T entity) where T : class
        {
            if (entity == null) return new PropertyInfo[0];
            return entity.GetType().GetProperties();
        }

        //Obtenha todas as propriedades que não são decoradas com o atributo Editável (falso)
        public static IEnumerable<PropertyInfo> GetScaffoldableProperties<T>()
        {
            IEnumerable<PropertyInfo> props = typeof(T).GetProperties();

            props = props.Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(EditableAttribute).Name && !IsEditable(p)) == false);

            props = props.Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(ColumnAttribute).Name));

            return props.Where(p => p.PropertyType.IsSimpleType() || IsEditable(p));
        }

        //Determine if the Attribute has an AllowEdit key and return its boolean state
        //fake the funk and try to mimick EditableAttribute in System.ComponentModel.DataAnnotations 
        //This allows use of the DataAnnotations property in the model and have the SimpleCRUD engine just figure it out without a reference
        private static bool IsEditable(PropertyInfo pi)
        {
            var attributes = pi.GetCustomAttributes(false);
            if (attributes.Length > 0)
            {
                dynamic write = attributes.FirstOrDefault(x => x.GetType().Name == typeof(EditableAttribute).Name);
                if (write != null)
                {
                    return write.AllowEdit;
                }
            }
            return false;
        }

        //Determine if the Attribute has an IsReadOnly key and return its boolean state
        //fake the funk and try to mimick ReadOnlyAttribute in System.ComponentModel 
        //This allows use of the DataAnnotations property in the model and have the SimpleCRUD engine just figure it out without a reference
        private static bool IsReadOnly(PropertyInfo pi)
        {
            var attributes = pi.GetCustomAttributes(false);
            if (attributes.Length > 0)
            {
                dynamic write = attributes.FirstOrDefault(x => x.GetType().Name == typeof(ReadOnlyAttribute).Name);
                if (write != null)
                {
                    return write.IsReadOnly;
                }
            }
            return false;
        }

        //Get all properties that are:
        //Not named Id
        //Not marked with the Key attribute
        //Not marked ReadOnly
        //Not marked IgnoreInsert
        //Not marked NotMapped
        private static IEnumerable<PropertyInfo> GetUpdateableProperties<T>(T entity)
        {
            var updateableProperties = GetScaffoldableProperties<T>();
            //remove ones with ID
            updateableProperties = updateableProperties.Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
            //remove ones with key attribute
            updateableProperties = updateableProperties.Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(KeyAttribute).Name) == false);
            //remove ones that are readonly
            updateableProperties = updateableProperties.Where(p => p.GetCustomAttributes(true).Any(attr => (attr.GetType().Name == typeof(ReadOnlyAttribute).Name) && IsReadOnly(p)) == false);
            //remove ones with IgnoreUpdate attribute
            updateableProperties = updateableProperties.Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(IgnoreUpdateAttribute).Name) == false);
            //remove ones that are not mapped
            updateableProperties = updateableProperties.Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(NotMappedAttribute).Name) == false);

            return updateableProperties;
        }

        //Get all properties that are named Id or have the Key attribute
        //For Inserts and updates we have a whole entity so this method is used
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

        //Gets the table name for this entity
        //For Inserts and updates we have a whole entity so this method is used
        //Uses class name by default and overrides if the class has a Table attribute
        public static string GetTableName(object entity)
        {
            var type = entity.GetType();
            return GetTableName(type);
        }

        //Gets the table name for this type
        //For Get(id) and Delete(id) we don't have an entity, just the type so this method is used
        //Use dynamic type to be able to handle both our Table-attribute and the DataAnnotation
        //Uses class name by default and overrides if the class has a Table attribute
        public static string GetTableName(Type type)
        {
            string tableName;

            //if (TableNames.TryGetValue(type, out tableName))
            //    return tableName;

            if (TableNames.TryGetValue(type, out tableName))
            {
                if (!string.IsNullOrEmpty(_schema) && !tableName.Contains(_schema) && !tableName.Contains("public."))
                {
                    tableName = string.Format("{0}.{1}", _schema, tableName);
                }

                return tableName;
            }

            tableName = _tableNameResolver.ResolveTableName(type);
            TableNames[type] = tableName;

            return tableName;
        }

        private static string GetColumnName(PropertyInfo propertyInfo)
        {
            string columnName, key = string.Format("{0}.{1}", propertyInfo.DeclaringType, propertyInfo.Name);

            if (ColumnNames.TryGetValue(key, out columnName))
                return columnName;

            columnName = _columnNameResolver.ResolveColumnName(propertyInfo);
            ColumnNames[key] = columnName;


            return columnName.ToLower();
        }


        #region Atributos

        /// <summary>
        /// Atributo Column Opcional
        /// Pode ser usado a versão de System.ComponentModel.DataAnnotations
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class TableAttribute : Attribute
        {

            public TableAttribute(string tableName)
            {
                Name = tableName;
            }
            /// <summary>
            /// Nome da tabela
            /// </summary>
            public string Name { get; private set; }
            /// <summary>
            /// Nome do Schema
            /// </summary>
            public string Schema { get; set; }

            /// <summary>
            /// Indica que é a tabela informada é usada somente para leitura, Views por exemplo
            /// </summary>
            public bool ReadOnly { get; set; }
        }

        /// <summary>
        /// Atributo Column Opcional
        /// Pode ser usado a versão de System.ComponentModel.DataAnnotations
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class ColumnAttribute : Attribute
        {
            /// <summary>
            /// Optional Column attribute.
            /// </summary>
            /// <param name="columnName"></param>
            public ColumnAttribute(string columnName)
            {
                Name = columnName;
            }
            /// <summary>
            /// Name of the column
            /// </summary>
            public string Name { get; private set; }

            public static explicit operator ColumnAttribute(CustomAttributeData v)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Atributo Opcional
        /// Pode ser usado a versão de System.ComponentModel.DataAnnotations
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class KeyAttribute : Attribute
        {
            public KeyAttribute(bool autoGenerate = true)
            {
                AutoGenerate = autoGenerate;


            }
            /// <summary>
            /// Nome da tabela
            /// </summary>
            public bool AutoGenerate { get; set; }
        }

        /// <summary>
        /// Atributo Opcional
        /// Pode ser usado a versão de System.ComponentModel.DataAnnotations
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class NotMappedAttribute : Attribute
        {
        }

        /// <summary>
        /// Atributo Opcional
        /// Pode ser usado a versão de System.ComponentModel.DataAnnotations
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class RequiredAttribute : Attribute
        {
        }

        /// <summary>
        /// Atributo Opcional
        /// Pode ser usado a versão de System.ComponentModel.DataAnnotations
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class EditableAttribute : Attribute
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="iseditable"></param>
            public EditableAttribute(bool iseditable)
            {
                AllowEdit = iseditable;
            }
            /// <summary>
            /// 
            /// </summary>
            public bool AllowEdit { get; private set; }
        }

        /// <summary>
        /// Atributo Opcional
        /// Pode ser usado a versão de System.ComponentModel.DataAnnotations
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class ReadOnlyAttribute : Attribute
        {
            /// <summary>
            /// Optional ReadOnly attribute.
            /// </summary>
            /// <param name="isReadOnly"></param>
            public ReadOnlyAttribute(bool isReadOnly)
            {
                IsReadOnly = isReadOnly;
            }
            /// <summary>
            /// Does this property persist to the database?
            /// </summary>
            public bool IsReadOnly { get; private set; }
        }

        /// <summary>
        /// Opcional
        /// Atributo que remove a coluna de um Select
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class IgnoreSelectAttribute : Attribute
        {
        }

        /// <summary>
        /// Optional IgnoreInsert attribute.
        /// Atributo que remove a coluna de um Insert
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class IgnoreInsertAttribute : Attribute
        {
        }

        /// <summary>
        /// 
        /// Atributo que remove a coluna de um Update
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class IgnoreUpdateAttribute : Attribute
        {
        }

        /// <summary>
        /// 
        /// Atributo que indica que coluna gerada automaticamente pelo banco de dados
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class AutoIdentityAttribute : Attribute
        {

        }

        #endregion


        #region Enums
        public enum Dialect
        {
            Oracle,
            SQLServer,
            SQLite,
            PostgreSQL,
            MySQL,
        }

        #endregion


        #region Classes

        public class TableNameResolver : ITableNameResolver
        {
            public virtual string ResolveTableName(Type type)
            {
               
                var tableName = Encapsulate(type.Name);

                var tableattr = type.GetCustomAttributes(true).SingleOrDefault(attr => attr.GetType().Name == typeof(TableAttribute).Name) as dynamic;

                if (tableattr != null)
                {


                    try
                    {
                        var schemaName = string.Empty;

                        if (_dialect == Dialect.Oracle)
                        {
                            tableName = tableattr.Name;

                            if (!String.IsNullOrEmpty(tableattr.Schema))
                            {
                                schemaName = tableattr.Schema; // Encapsulate(tableattr.Schema);
                                tableName = String.Format("{0}.{1}", schemaName, tableName);
                            }
                            else if (!string.IsNullOrEmpty(_schema))
                            {
                                schemaName = _schema; // Encapsulate(_schema);
                                tableName = String.Format("{0}.{1}", schemaName, tableName);
                            }
                        }
                        else
                        {
                            tableName = Encapsulate(tableattr.Name);

                            if (!String.IsNullOrEmpty(tableattr.Schema))
                            {
                                schemaName = Encapsulate(tableattr.Schema);
                                tableName = String.Format("{0}.{1}", schemaName, tableName);
                            }
                            else if (!string.IsNullOrEmpty(_schema))
                            {
                                schemaName = Encapsulate(_schema);
                                tableName = String.Format("{0}.{1}", schemaName, tableName);
                            }
                        }

                    }
                    catch (RuntimeBinderException)
                    {
                        //Schema doesn't exist on this attribute.
                    }
                }

                return tableName;
            }
        }

        public class ColumnNameResolver : IColumnNameResolver
        {
            public virtual string ResolveColumnName(PropertyInfo propertyInfo)
            {
                var columnName =  Encapsulate(propertyInfo.Name);

                var columnattr = propertyInfo.GetCustomAttributes(true).SingleOrDefault(attr => attr.GetType().Name == typeof(ColumnAttribute).Name) as dynamic;
                if (columnattr != null)
                {
                    columnName = columnattr.Name; // Encapsulate(columnattr.Name); comentado parar pode user o canse insensitive do postgres
                    if (Debugger.IsAttached)
                        Trace.WriteLine(String.Format("Column name for type overridden from {0} to {1}", propertyInfo.Name, columnName));
                }
                return columnName;
            }
        }

        #endregion

        #region Extensões para query dynamicas

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

        private static QueryResult DynamicWhere(List<QueryParameter> queryProperties, Type currentType)
        {
            IDictionary<string, Object> expando = new ExpandoObject();

            QueryResult result = null;

            var builder = new StringBuilder();

            builder.Append(" where ");

            for (int i = 0; i < queryProperties.Count(); i++)
            {
                QueryParameter item = queryProperties[i];

                var fieldType = GePropertyInfo(currentType, item.PropertyName); // currenttype.GetField(item.PropertyName, BindingFlags.PutRefDispProperty  );

                var columnTable = string.Empty;
                var param = string.Empty;

                if (fieldType != null)
                {
                    var attr = GetCustomAttibute<ColumnAttribute>(fieldType); //metodo criado para ter compatibilidade com as versões 4,4.5 e .netcore (ColumnAttribute Dapper)

                    if (attr == null)
                    {
                        object[] attributes = fieldType.GetCustomAttributes(true);

                        foreach (dynamic attribute in attributes) //(ColumnAttribute DataAnnotations)
                        {
                            Type type = attribute.GetType();

                            if (type.ToString().Contains("ColumnAttribute"))
                            {
                                if (attribute != null)
                                {
                                    columnTable = attribute.Name;

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        columnTable = attr.Name;
                    }


                    param = columnTable;
                }

                var dialeto = DapperExt.GetDialect();

                var prefixParam = dialeto == "Oracle" ? ":" : "@";

                if (!string.IsNullOrEmpty(columnTable))
                {

                    if (!string.IsNullOrEmpty(item.LinkingOperator) && i > 0)
                    {
                        builder.Append(string.Format("{0} {1} {2} {3} ", item.LinkingOperator, DapperExt.Encapsulate(columnTable), item.QueryOperator, prefixParam + param));
                    }
                    else
                    {
                        builder.Append(string.Format("{0} {1} {2} ", DapperExt.Encapsulate(columnTable), item.QueryOperator, prefixParam + param));
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

        private static PropertyInfo GePropertyInfo(Type type, string propertyName)
        {
            var tp = type.GetProperties().Where(p => p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(ColumnAttribute).Name)).ToList();

            //tp.Any() ? tp.First() : type.GetProperties().Where(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)).First();

            return type.GetProperties().Where(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)).First();
        }

        private static T GetCustomAttibute<T>(PropertyInfo property) where T : Attribute
        {
            return property.GetCustomAttributes(typeof(T), true).Select(attr => (T)attr).FirstOrDefault();
        }

        private static string BuildWhere<T>(Expression<Func<T, bool>> where)
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

                    MemberExpression me;

                    var ue = body.Right as UnaryExpression;

                    me = ((ue != null) ? ue.Operand : null) as MemberExpression;


                    right = me;
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
                    return "<>";
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

        public static T ExecuteStoredProcedure<T>(this IDbConnection connection, string spName, dynamic param = null)
        {
            var result = (T)SqlMapper.Query(connection, spName, param, commandType: CommandType.StoredProcedure).First();

            return result;
        }


        public static object ExecuteFunction(this IDbConnection connection, string functionName, dynamic param = null)
        {
            var result = (object)SqlMapper.Query(connection, functionName, param, commandType: CommandType.Text).First();

            return result;
        }

        public static IEnumerable<T> ExecuteQuery<T>(this IDbConnection connection, string where, object parameters = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            var currenttype = typeof(T);
            var idProps = GetIdProperties(currenttype).ToList();
            if (!idProps.Any())
                throw new ArgumentException("A entidade deve ter pelo menos uma propriedade [Key]");

            var name = GetTableName(currenttype);

            var sb = new StringBuilder();

            sb.Append("select ");

            //create a new empty instance of the type to get the base properties
            BuildSelect(sb, GetScaffoldableProperties<T>().ToArray());

            sb.AppendFormat(" from {0}", name);

            if (!string.IsNullOrEmpty(where))
            {
                sb.Append(where);
            }

            if (Debugger.IsAttached)
                Trace.WriteLine(String.Format("GetList<{0}>: {1}", currenttype, sb));

            _sql = sb.ToString();

            return connection.Query<T>(sb.ToString(), parameters, transaction, true, commandTimeout);
        }

        public static IEnumerable<T> QueryFromSQL<T>(this IDbConnection connection, string sql)
        {
            IEnumerable<T> query = null;

            using (var reader = connection.ExecuteReader(sql))
            {
                query = ConvertReaderToListObjects<T>(reader);
            }

            return query;
        }


        public static IEnumerable<T> Query<T>(this IDbConnection connection, Expression<Func<T, bool>> where = null)
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

                return connection.Query<T>(sb.ToString(), parametros);
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

                return connection.Query<T>(sb.ToString());
            }

        }

        public static T FromDynamic<T>(IDictionary<string, object> dictionary)
        {
            var bindings = new List<MemberBinding>();

            foreach (var sourceProperty in typeof(T).GetProperties().Where(x => x.CanWrite))
            {
                var key = dictionary.Keys.SingleOrDefault(x => x.Equals(sourceProperty.Name, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(key)) continue;
                var propertyValue = dictionary[key];
                bindings.Add(Expression.Bind(sourceProperty, Expression.Constant(propertyValue)));
            }
            Expression memberInit = Expression.MemberInit(Expression.New(typeof(T)), bindings);

            return Expression.Lambda<Func<T>>(memberInit).Compile().Invoke();
        }

        public static T QueryFirstOrDefault<T>(this IDbConnection connection, Expression<Func<T, bool>> where = null)
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

                return connection.QueryFirstOrDefault<T>(sb.ToString(), parametros);

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


                _sql = sb.ToString();

                return connection.QueryFirstOrDefault<T>(sb.ToString());
            }

        }

        #endregion

        private static T VerificaNulos<T>(object value)
        {
            T ret;

            var type = default(T);


            if (value != System.DBNull.Value && !string.IsNullOrEmpty(value.ToString()))
            {
                if (typeof(T) == typeof(short))
                    return (T)(object)short.Parse(value.ToString());

                if (typeof(T) == typeof(Int32))
                    return (T)(object)Int32.Parse(value.ToString());

                if (typeof(T) == typeof(DateTime))
                {
                    return (T)(object)DateTime.Parse(value.ToString());
                }

                if (typeof(T) == typeof(DateTime?))
                {
                    if (value == null)
                    {
                        return (T)(object)null;
                    }
                    else
                    {
                        return (T)(object)DateTime.Parse(value.ToString());
                    }

                }

                if (typeof(T) == typeof(Int64))
                {
                    return (T)(object)Int64.Parse(value.ToString());
                }

                if (typeof(T) == typeof(Double))
                {
                    return (T)(object)Double.Parse(value.ToString());
                }

                if (typeof(T) == typeof(String))
                {
                    return (T)(object)value.ToString();
                }

                if (typeof(T) == typeof(Boolean))
                {
                    return (T)(object)Boolean.Parse(value.ToString());
                }

                if (typeof(T) == typeof(Int32?))
                {
                    return (T)(object)Int32.Parse(value.ToString());
                }

                if (typeof(T) == typeof(Int64?))
                {
                    return (T)(object)Int64.Parse(value.ToString());
                }

                if (typeof(T) == typeof(Decimal))
                {
                    return (T)(object)Decimal.Parse(value.ToString());
                }

            }

            return type;
        }

        public static T ConvertReaderToObject<T>(IDataReader reader) //where T : new()
        {
            //var entity = new T();
            var obj = default(T);

            try
            {

                while (reader.Read())
                {
                    obj = Activator.CreateInstance<T>();
                    object[] atributos;

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        //Obtem os atributos da propriedade
                        atributos = prop.GetCustomAttributes(true);

                        object valueToProp = null;
                        var boolValue = string.Empty;
                        object value = null;

                        foreach (var atr in atributos)
                        {
                            //Verifica se a propriedade é do tipo ColumnAttribute

                            var column = atr as ColumnAttribute;

                            if (column != null)
                            {
                                if (atr is ColumnAttribute)
                                {
                                    column = (ColumnAttribute)atr;

                                    switch (prop.PropertyType.ToString())
                                    {
                                        case "System.String":
                                            value = VerificaNulos<string>(reader[column.Name]); break;
                                        case "System.DateTime":
                                            value = VerificaNulos<DateTime>(reader[column.Name]); break;
                                        case "System.Nullable`1[System.DateTime]":
                                            value = VerificaNulos<DateTime?>(reader[column.Name]); break;
                                        case "System.Decimal":
                                            value = VerificaNulos<Decimal>(reader[column.Name]); break;
                                        case "System.Nullable`1[System.Decimal]":
                                            value = VerificaNulos<Decimal>(reader[column.Name]); break;
                                        case "System.Int32":
                                            value = VerificaNulos<Int32>(reader[column.Name]); break;
                                        case "System.Nullable`1[System.Int32]":
                                            value = VerificaNulos<Int32>(reader[column.Name]); break;
                                        case "System.Int64":
                                            value = VerificaNulos<Int64>(reader[column.Name]); break;
                                        case "System.Nullable`1[System.Int64]":
                                            value = VerificaNulos<Int64?>(reader[column.Name]); break;
                                        case "System.Boolean":

                                            value = VerificaNulos<string>(reader[column.Name]); break;
                                    }

                                }

                                if (!string.IsNullOrEmpty(boolValue) && value != null)
                                {
                                    valueToProp = value.ToString() == boolValue;
                                }
                                else if (value != null)
                                {
                                    valueToProp = value;
                                }

                                prop.SetValue(obj, valueToProp, null);
                            }
                            else
                            {
                                 var columnDa = atr as System.ComponentModel.DataAnnotations.Schema.ColumnAttribute;

                                if (columnDa != null)
                                {
                                    if (atr is System.ComponentModel.DataAnnotations.Schema.ColumnAttribute)
                                    {
                                        columnDa = (System.ComponentModel.DataAnnotations.Schema.ColumnAttribute)atr;

                                        switch (prop.PropertyType.ToString())
                                        {
                                            case "System.String":
                                                value = VerificaNulos<string>(reader[columnDa.Name]); break;
                                            case "System.DateTime":
                                                value = VerificaNulos<DateTime>(reader[columnDa.Name]); break;
                                            case "System.Nullable`1[System.DateTime]":
                                                value = VerificaNulos<DateTime?>(reader[columnDa.Name]); break;
                                            case "System.Decimal":
                                                value = VerificaNulos<Decimal>(reader[columnDa.Name]); break;
                                            case "System.Nullable`1[System.Decimal]":
                                                value = VerificaNulos<Decimal>(reader[columnDa.Name]); break;
                                            case "System.Int32":
                                                value = VerificaNulos<Int32>(reader[columnDa.Name]); break;
                                            case "System.Nullable`1[System.Int32]":
                                                value = VerificaNulos<Int32>(reader[columnDa.Name]); break;
                                            case "System.Int64":
                                                value = VerificaNulos<Int64>(reader[columnDa.Name]); break;
                                            case "System.Nullable`1[System.Int64]":
                                                value = VerificaNulos<Int64?>(reader[columnDa.Name]); break;
                                            case "System.Boolean":

                                                value = VerificaNulos<string>(reader[columnDa.Name]); break;
                                        }

                                    }

                                    if (!string.IsNullOrEmpty(boolValue) && value != null)
                                    {
                                        valueToProp = value.ToString() == boolValue;
                                    }
                                    else if (value != null)
                                    {
                                        valueToProp = value;
                                    }

                                    prop.SetValue(obj, valueToProp, null);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }

            return obj;
        }


        public static IEnumerable<T> ConvertReaderToListObjects<T>(IDataReader reader, string columnIgnore = "") //where T : new()
        {
            var list = new List<T>();

            try
            {

                var obj = default(T);


                while (reader.Read())
                {
                    obj = Activator.CreateInstance<T>();
                    object[] atributos;

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        //Obtem os atributos da propriedade
                        atributos = prop.GetCustomAttributes(true);

                        object valueToProp = null;
                        var boolValue = string.Empty;
                        object value = null;

                        foreach (var atr in atributos)
                        {

                            dynamic column = atr;

                            var type = column?.GetType();

                            if (column != null && type != null && type.Name == "ColumnAttribute" && column.Name != columnIgnore)
                            {
                                string columnName = column.Name;

                                switch (prop.PropertyType.ToString())
                                {
                                    case "System.String":
                                        value = VerificaNulos<string>(reader[columnName]);
                                        break;
                                    case "System.DateTime":
                                        value = VerificaNulos<DateTime>(reader[columnName]);
                                        break;
                                    case "System.Nullable`1[System.DateTime]":
                                        value = VerificaNulos<DateTime?>(reader[columnName]);
                                        break;
                                    case "System.Decimal":
                                        value = VerificaNulos<Decimal>(reader[columnName]);
                                        break;
                                    case "System.Nullable`1[System.Decimal]":
                                        value = VerificaNulos<Decimal>(reader[columnName]);
                                        break;
                                    case "System.Int32":
                                        value = VerificaNulos<Int32>(reader[columnName]);
                                        break;
                                    case "System.Nullable`1[System.Int32]":
                                        value = VerificaNulos<Int32>(reader[columnName]);
                                        break;
                                    case "System.Int64":
                                        value = VerificaNulos<Int64>(reader[columnName]);
                                        break;
                                    case "System.Nullable`1[System.Int64]":
                                        value = VerificaNulos<Int64?>(reader[columnName]);
                                        break;
                                    case "System.Boolean":

                                        value = VerificaNulos<string>(reader[columnName]);

                                        break;
                                }
                            }



                            //if (atr is ConvertTypeAttribute)
                            //{
                            //    convValue = (ConvertTypeAttribute)atr;

                            //    if (!string.IsNullOrEmpty(convValue.TrueValue))
                            //    {
                            //        boolValue = convValue.TrueValue;
                            //    }
                            //    else
                            //    {
                            //        boolValue = convValue.DefaultValue;
                            //    }
                            //}
                        }

                        if (!string.IsNullOrEmpty(boolValue) && value != null)
                        {
                            valueToProp = value.ToString() == boolValue;
                        }
                        else if (value != null)
                        {
                            valueToProp = value;
                        }

                        prop.SetValue(obj, valueToProp, null);
                    }

                    list.Add(obj);
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }

            return list;
        }
    }

}

internal static class TypeExtension
{
    //You can't insert or update complex types. Lets filter them out.
    public static bool IsSimpleType(this Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        type = underlyingType ?? type;
        var simpleTypes = new List<Type>
                               {
                                   typeof(byte),
                                   typeof(sbyte),
                                   typeof(short),
                                   typeof(ushort),
                                   typeof(int),
                                   typeof(uint),
                                   typeof(long),
                                   typeof(ulong),
                                   typeof(float),
                                   typeof(double),
                                   typeof(decimal),
                                   typeof(bool),
                                   typeof(string),
                                   typeof(char),
                                   typeof(Guid),
                                   typeof(DateTime),
                                   typeof(DateTimeOffset),
                                   typeof(byte[])
                               };
        return simpleTypes.Contains(type) || type.IsEnum;
    }
}

