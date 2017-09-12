using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFGenericRepository
{
    public interface IBaseRepoFactory : IDisposable
    {
        IBaseRepo<T> GetRepo<T>() where T : class, new();
    }
}
