using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Metadata.Edm;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Client.DAO.Extensions
{
    public static class EFExtensions
    {
        public static EntitySingleOp<TEntity> Upsert<TEntity>(this DbContext context, TEntity entity) where TEntity : class
        {
            return new UpsertOp<TEntity>(context, entity);
        }

        public static EntityOp<TEntity, TRes> UpsertMany<TEntity, TRes>(this DbContext context, IEnumerable<TEntity> entities) where TEntity : class
        {
            return new UpsertManyOp<TEntity, TRes>(context, entities);
        }
    }

    public abstract class EntityOp<TEntity, TRet> where TEntity : class
    {
        public readonly DbContext Context;
        public readonly string TableName;

        private readonly List<string> _keyNames = new List<string>();
        private readonly List<string> _identityNames = new List<string>();
        private readonly Dictionary<string, Tuple<string, string>> _foreignKey = new Dictionary<string, Tuple<string, string>>();
        public IEnumerable<string> KeyNames { get { return _keyNames.Union(_identityNames); } }
        public IEnumerable<string> IdentityNames { get { return _identityNames; } }

        private readonly List<string> _excludeProperties = new List<string>();
        private EntitySet set;

        private static string GetMemberName<T>(Expression<Func<TEntity, T>> selectMemberLambda)
        {
            var member = selectMemberLambda.Body as MemberExpression;
            if (member == null)
            {
                throw new ArgumentException("The parameter selectMemberLambda must be a member accessing labda such as x => x.Id", "selectMemberLambda");
            }
            return member.Member.Name;
        }

        protected EntityOp(DbContext context)
        {
            Context = context;

            TableName = GetTableName<TEntity>();
        }

        private string GetTableName<TType>() where TType : class
        {
            object[] mappingAttrs = typeof(TType).GetCustomAttributes(typeof(TableAttribute), false);
            TableAttribute tableAttr = null;
            if (mappingAttrs.Length > 0)
            {
                tableAttr = mappingAttrs[0] as TableAttribute;
            }

            if (tableAttr == null)
            {
              
                set=(Context as System.Data.Entity.Infrastructure.IObjectContextAdapter).ObjectContext
                    .CreateObjectSet<TType>().EntitySet;
                return set.Name;
                
            }
            return tableAttr.Name;
        }


        public static EntitySet GetEntitySet(DbContext context, Type type)
        {
            const BindingFlags propBinding = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty;
            var objectContext = (context as System.Data.Entity.Infrastructure.IObjectContextAdapter).ObjectContext;
            var objectSet = objectContext.GetType().GetMethod("CreateObjectSet", new Type[0]).MakeGenericMethod(type).Invoke(objectContext, null);
            return ((EntitySet)objectSet.GetType().GetProperty("EntitySet", propBinding).GetValue(objectSet, null));
        }

        public abstract TRet Execute();
        public abstract IEnumerable<TRet> Query();
        public void Run()
        {
            Execute();
        }

        public EntityOp<TEntity, TRet> Key<TKey>(Expression<Func<TEntity, TKey>> selectKey)
        {
            _keyNames.Add(GetMemberName(selectKey));
            return this;
        }

        public EntityOp<TEntity, TRet> ConstrainForeignKey<TEntityForeign, TKey>(Expression<Func<TEntity, TKey>> selectKey, Expression<Func<TEntityForeign, TKey>> foreignKey) where TEntityForeign : class
        {
            ForeignKeys[GetMemberName(selectKey)] = GetForeignKeyTableAndName(foreignKey);
            return this;
        }

        private Tuple<string, string> GetForeignKeyTableAndName<TEntityForeign, TForeignKey>(Expression<Func<TEntityForeign, TForeignKey>> foreignKey) where TEntityForeign : class
        {
            var member = foreignKey.Body as MemberExpression;
            if (member == null)
            {
                throw new ArgumentException("The parameter foreignKey must be a member accessing labda such as x => x.Id", "foreignKey");
            }
            return new Tuple<string, string>(GetTableName<TEntityForeign>(), member.Member.Name);
        }

        public EntityOp<TEntity, TRet> Identity<TKey>(Expression<Func<TEntity, TKey>> selectIdentity)
        {
            _identityNames.Add(GetMemberName(selectIdentity));
            return this;
        }

        public EntityOp<TEntity, TRet> ExcludeField<TField>(Expression<Func<TEntity, TField>> selectField)
        {
            _excludeProperties.Add(GetMemberName(selectField));
            return this;
        }

        public IEnumerable<PropertyInfo> ColumnProperties
        {
            get
            {
                return typeof(TEntity).GetProperties().Where(pr => !_excludeProperties.Contains(pr.Name));
            }
        }

        public Dictionary<string, Tuple<string, string>> ForeignKeys
        {
            get { return _foreignKey; }
        }
    }

    public abstract class EntitySingleOp<TEntity> : EntityOp<TEntity, int> where TEntity : class
    {
        protected EntitySingleOp(DbContext context) : base(context) { }

    }

    public class UpsertOp<TEntity> : EntitySingleOp<TEntity> where TEntity : class
    {
        private readonly TEntity _entity;

        public UpsertOp(DbContext context, TEntity entity)
            : base(context)
        {
            _entity = entity;
        }

        public override int Execute()
        {
            List<object> valueList;
            var sql = GenerateSql(out valueList);
            return Context.Database.ExecuteSqlCommand(sql, valueList.ToArray());
           
        }

        public override IEnumerable<int> Query()
        {
            return new[] {Execute()};
        }

        private string GenerateSql(out List<object> valueList)
        {
            var sql = new StringBuilder();

            int notNullFields = 0;
            var valueKeyList = new List<string>();
            var columnList = new List<string>();
            valueList = new List<object>();
            foreach (var p in ColumnProperties)
            {
                columnList.Add(p.Name);
                var val = p.GetValue(_entity, null);
                if (val != null)
                {
                    valueKeyList.Add("{" + (notNullFields++) + "}");
                    valueList.Add(val);
                }
                else
                {
                    valueKeyList.Add("null");
                }
            }
            if (IdentityNames.Any())
            {
                sql.AppendFormat("SET IDENTITY_INSERT {0} ON{1}", TableName, Environment.NewLine);
            }
            sql.Append("merge into ");
            sql.Append(TableName);
            sql.Append(" as T ");

            sql.Append("using (values (");
            sql.Append(string.Join(",", valueKeyList));
            sql.Append(")) as S (");
            sql.Append(string.Join(",", columnList));
            sql.Append(") ");

            sql.Append("on (");
            var mergeCond = string.Join(" and ", KeyNames.Select(kn => "T." + kn + "=S." + kn));
            sql.Append(mergeCond);
            sql.Append(") ");

            var columsWithoutIdentity = columnList.Where(x => !IdentityNames.Contains(x)).ToList();
            sql.Append("when matched then update set ");
            sql.Append(string.Join(",", columsWithoutIdentity.Select(c => "T." + c + "=S." + c).ToArray()));
            sql.Append(" when not matched then insert (");
            sql.Append(string.Join(",", columnList));
            sql.Append(") values (S.");
            sql.Append(string.Join(",S.", columnList));
            sql.Append(") output $action;");
            if (IdentityNames.Any())
            {
                sql.AppendFormat("{1}SET IDENTITY_INSERT {0} OFF", TableName, Environment.NewLine);
            }
            return sql.ToString();
        }
    }

    public class UpsertManyOp<TEntity,TRet> : EntityOp<TEntity,TRet> where TEntity : class
    {
        private readonly IEnumerable<TEntity> _entities;

        public UpsertManyOp(DbContext context, IEnumerable<TEntity> entities)
            : base(context)
        {
            _entities = entities.ToList();
        }


        public override TRet Execute()
        {
            List<object> valueList;
            var sql = GenerateSql(out valueList);
            Context.Database.ExecuteSqlCommand(sql, valueList.ToArray());
            return default(TRet);
        }

        public override IEnumerable<TRet> Query()
        {
            List<object> valueList;
            var sql = GenerateSql(out valueList);
            return Context.Database.SqlQuery<TRet>(sql, valueList.ToArray());
        }

        private string GenerateSql(out List<object> valueList)
        {
            //TODO: Needed splitting in multiple records if valueList is to big and start transaction
            var sql = new StringBuilder();

            int notNullFields = 0;
            var valueKeyList = new List<string>();
            var columnList = new HashSet<string>();
            valueList = new List<object>();
            foreach (var entity in _entities)
            {
                foreach (var p in ColumnProperties)
                {
                    columnList.Add(p.Name);

                    var val = p.GetValue(entity, null);
                    if (val != null)
                    {
                        valueKeyList.Add("{" + (notNullFields++) + "}");
                        valueList.Add(val);
                    }
                    else
                    {
                        valueKeyList.Add("null");
                    }
                }
            }

            if (IdentityNames.Any())
            {
                sql.AppendFormat("SET IDENTITY_INSERT {0} ON{1}", TableName,Environment.NewLine);
            }
            sql.Append("merge into ");
            sql.Append(TableName);
            sql.Append(" as T ");

            sql.Append("using (values ");
            var columnValues = new List<string>();
            for (int i = 0; i < valueKeyList.Count; i += columnList.Count)
            {
                columnValues.Add("(" + string.Join(",", valueKeyList.GetRange(i, columnList.Count)) + ")");
            }
            sql.Append(string.Join(",", columnValues));
            sql.Append(") as S (");
            sql.Append(string.Join(",", columnList));
            sql.Append(") ");

            sql.Append("on (");
            var mergeCond = string.Join(" and ", KeyNames.Select(kn => "T." + kn + "=S." + kn));
            sql.Append(mergeCond);
            sql.Append(") ");

            var foreignCheck = string.Empty;
            if (ForeignKeys.Any())
            {
                foreignCheck = " and " +
                               string.Join(" and ",
                                   ForeignKeys.Select(
                                       x =>
                                           string.Format("exists (SELECT * FROM {1} WHERE S.{0} = {2})", x.Key,
                                               x.Value.Item1, x.Value.Item2)));
            }

            var columsWithoutIdentity = columnList.Where(x => !IdentityNames.Contains(x)).ToList();
            sql.AppendFormat("when matched{0} then update set ",foreignCheck);
            sql.Append(string.Join(",", columsWithoutIdentity.Select(c => "T." + c + "=S." + c).ToArray()));
            sql.AppendFormat(" when not matched{0} then insert (", foreignCheck);
            sql.Append(string.Join(",", columnList));
            sql.Append(") values (S.");
            sql.Append(string.Join(",S.", columnList));
            sql.AppendFormat(") output {0}, $action as Action;", string.Join(",", KeyNames.Select(x => "S." + x)));
            if (IdentityNames.Any())
            {
                sql.AppendFormat("{1}SET IDENTITY_INSERT {0} OFF", TableName, Environment.NewLine);
            }
            return sql.ToString();
        }
    }
}