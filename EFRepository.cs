using EFGenericRepository.Attributes;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EFGenericRepository
{
    public class EFRepository<C, T> : IBaseRepo<T>
        where C : DbContext, new()
        where T : class, new()
       
    {
        public string TableName { get; private set; }

        internal List<BaseColumn> Columns { get; private set; }

        internal C Context = new C();

        DbSet<T> CurrentDbSet
        {
            get
            {
                return Context.Set<T>();
            }
        }


        public EFRepository()
        {
            Columns = new List<BaseColumn>();

            var attr = typeof(T).GetCustomAttributes(typeof(TableNameAttribute), true).FirstOrDefault() as TableNameAttribute;
            if (attr == null)
            {
                throw new Exception($"Table name should be set for object. => {typeof(T).Name}");
            }
            TableName = attr.Value;

            PropertyInfo[] props = typeof(T).GetProperties();
            foreach (var prop in props)
            {
                BaseColumn column = new BaseColumn();
                column.IsPrimaryKey = prop.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).FirstOrDefault() != null;

                ColumnNameAttribute columnNameAttr = prop.GetCustomAttributes(typeof(ColumnNameAttribute), true).FirstOrDefault() as ColumnNameAttribute;
                if (columnNameAttr != null)
                {
                    column.ColumnName = columnNameAttr.ColumnName;
                }
                else
                {
                    column.ColumnName = prop.Name;
                }
                column.PropertyName = prop.Name;
                Columns.Add(column);
            }

            if (Columns.FirstOrDefault(t => t.IsPrimaryKey) == null)
            {
                throw new Exception($"Primary key(s) should be set for object. => {typeof(T).Name}");
            }
        }

        #region Private Methods

        private T GetDBEntity(T filledKeys)
        {
            List<object> primaryKeyFields = new List<object>();
            foreach (var column in Columns)
            {
                if (column.IsPrimaryKey)
                {
                    primaryKeyFields.Add(filledKeys.GetType().GetProperty(column.PropertyName).GetValue(filledKeys));
                    column.Value = filledKeys.GetType().GetProperty(column.PropertyName).GetValue(filledKeys);
                }
            }
            T dbObject = CheckEntityExistance();
            if (dbObject != null)
            {
                return CurrentDbSet.Find(primaryKeyFields.ToArray());
            }
            return dbObject;
        }

        private T CheckEntityExistance()
        {
            string whereClause = "";
            IList<BaseColumn> primaryKeyColumns = Columns.Where(t => t.IsPrimaryKey).ToList();

            List<SqlParameter> sqlParameters = new List<SqlParameter>();

            for (int i = 0; i < primaryKeyColumns.Count(); i++)
            {
                if (i != 0)
                {
                    whereClause += " AND ";
                }
                whereClause += "\"" + primaryKeyColumns[i].ColumnName + "\"" + "=" + primaryKeyColumns[i].Value;
               //sqlParameters.Add(new SqlParameter("@" + primaryKeyColumns[i].ColumnName, primaryKeyColumns[i].Value));
            }
            string existanceSql = $"select * from \"{TableName}\" where {whereClause}";
            var query = Context.Database.SqlQuery<T>(existanceSql, sqlParameters.ToArray());
            return query.ToListAsync().Result.FirstOrDefault();
        }

        private IList<T> GetDBList(Expression<Func<T, bool>> predicate)
        {
            try
            {
                List<T> dbList = CurrentDbSet.Where(predicate).ToList();
                return dbList;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.GetBaseException();
            }
        }

        private async Task<IList<T>> GetDBListAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                List<T> dbList = await CurrentDbSet.Where(predicate).ToListAsync();
                return dbList;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.GetBaseException();
            }
        }


        private IList<TResult> GetDBList<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        {
            try
            {
                IQueryable<T> queryable = CurrentDbSet.Where(predicate);
                List<TResult> dbList = queryable.Select(selector).ToList();
                return dbList;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.GetBaseException();
            }
        }

        private T AddUpdateEntity(T entity)
        {
            T localDBEntity = GetDBEntity(entity);
            if (localDBEntity == null)
            {
                localDBEntity = CurrentDbSet.Add(entity);
            }
            else
            {
                Context.Entry(localDBEntity).State = EntityState.Detached;
                Context.Entry(entity).State = EntityState.Modified;
            }
            return localDBEntity;
        }

        private void CommitChanges()
        {
            bool saveFailed;
            do
            {
                saveFailed = false;

                try
                {
                    Context.SaveChanges();
                }
                // Client wins on concurrency
                catch (DbUpdateConcurrencyException ex)
                {
                    saveFailed = true;
                    var entry = ex.Entries.Single();
                    entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                    Thread.Sleep(1);
                }

            } while (saveFailed);
        }

        private Task CommitChangesAsync()
        {
            bool saveFailed;
            do
            {
                saveFailed = false;

                try
                {
                    return Context.SaveChangesAsync();
                }
                // Client wins on concurrency
                catch (DbUpdateConcurrencyException ex)
                {
                    saveFailed = true;
                    var entry = ex.Entries.Single();
                    entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                    Thread.Sleep(1);
                }

            } while (saveFailed);

            return null;
        }

        #endregion

        public int Count(Expression<Func<T, bool>> predicate)
        {
            IQueryable<T> queryable = CurrentDbSet.Where(predicate);
            return queryable.Count();
        }

        public void Dispose()
        {
            Context.Dispose();
            Context = null;
        }

        public T GetEntity(T filledKeys)
        {
            T dbObject = GetDBEntity(filledKeys);
            return dbObject;
        }

        public IList<T> GetList(Expression<Func<T, bool>> predicate)
        {
            IList<T> dbList = GetDBList(predicate);
            return dbList;
        }

        public IList<TResult> GetList<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        {
            return GetDBList(predicate, selector);
        }

        public async Task<IList<T>> GetListAsync(Expression<Func<T, bool>> predicate)
        {
            IList<T> dbList = await GetDBListAsync(predicate);
            return dbList;
        }

        public void Remove(Expression<Func<T, bool>> predicate)
        {
            CurrentDbSet.RemoveRange(GetDBList(predicate));
            CommitChanges();
        }

        public void Remove(ICollection<T> entities)
        {
            foreach (var entity in entities)
            {
                var dbEntity = GetDBEntity(entity);
                CurrentDbSet.Remove(dbEntity);
            }
            CommitChanges();
        }

        public void Remove(T removedEntity)
        {
            var dbEntity = GetDBEntity(removedEntity);
            Context.Entry(dbEntity).State = EntityState.Deleted;
            CurrentDbSet.Remove(dbEntity);
            CommitChanges();
        }

        public async Task RemoveAsync(ICollection<T> entities)
        {
            foreach (var entity in entities)
            {
                var dbEntity = GetDBEntity(entity);
                CurrentDbSet.Remove(dbEntity);
            }
            await CommitChangesAsync();
        }

        public async Task RemoveAsync(Expression<Func<T, bool>> predicate)
        {
            IList<T> dbList = await GetDBListAsync(predicate);
            await RemoveAsync(dbList);
        }

        public async Task RemoveAsync(T removedEntity)
        {
            var dbEntity = GetDBEntity(removedEntity);
            Context.Entry(dbEntity).State = EntityState.Deleted;
            CurrentDbSet.Remove(dbEntity);
            await CommitChangesAsync();
        }

        public void Save(ICollection<T> entities)
        {
            foreach (var entity in entities)
            {
                AddUpdateEntity(entity);
            }

            CommitChanges();
        }

        public T Save(T updatedEntity)
        {
            T dbEntity = AddUpdateEntity(updatedEntity);
            CommitChanges();
            return dbEntity;
        }

        public async Task SaveAsync(ICollection<T> entities)
        {
            foreach (var entity in entities)
            {
                AddUpdateEntity(entity);
            }

            await CommitChangesAsync();
        }

        public async Task<T> SaveAsync(T updatedEntity)
        {
            T dbEntity = AddUpdateEntity(updatedEntity);
            await CommitChangesAsync();
            return dbEntity;
        }

        public Tout Max<Tout>(Expression<Func<T, bool>> predicate, Expression<Func<T, Tout>> selector) where Tout : struct
        {
            IQueryable<T> queryable = CurrentDbSet.Where(predicate);
            return queryable.Max(selector);
        }

        public Tout Min<Tout>(Expression<Func<T, bool>> predicate, Expression<Func<T, Tout>> selector) where Tout : struct
        {
            IQueryable<T> queryable = CurrentDbSet.Where(predicate);
            return queryable.Min(selector);
        }
    }
}
