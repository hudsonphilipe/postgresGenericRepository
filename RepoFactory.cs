using EFGenericRepository.Attributes;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFGenericRepository
{
    public class RepoFactory<C> : IBaseRepoFactory where C : DbContext
    {
        private object lockObject = new object();
        Dictionary<string, object> repositoryContainer = new Dictionary<string, object>();

        public object CreateRepository<T>() where T : class, new()
        {
            var attr = typeof(T).GetCustomAttributes(typeof(TableNameAttribute), true).FirstOrDefault() as TableNameAttribute;
            if (attr == null)
            {
                throw new Exception($"Table name should be set for object. => {typeof(T).Name}");
            }
            string tableName = attr.Value;

            return Activator.CreateInstance(typeof(EFRepository<,>).MakeGenericType(new Type[] { typeof(C), typeof(T) }));
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                if (repositoryContainer == null)
                {
                    return;
                }
                foreach (var item in repositoryContainer)
                {
                    if (item.Value.GetType().IsAssignableFrom(typeof(IDisposable)))
                    {
                        IDisposable disposable = (IDisposable)item.Value;
                        disposable.Dispose();
                    }
                }
                repositoryContainer.Clear();
                repositoryContainer = null;
            }
        }

        public IBaseRepo<T> GetRepo<T>() where T : class, new()
        {
            lock (lockObject)
            {
                if (repositoryContainer == null)
                {
                    repositoryContainer = new Dictionary<string, object>();
                }
                if (!repositoryContainer.ContainsKey(typeof(T).Name))
                {
                    repositoryContainer.Add(typeof(T).Name, CreateRepository<T>());
                }
            }
            return repositoryContainer[typeof(T).Name] as IBaseRepo<T>;
        }
    }
}
