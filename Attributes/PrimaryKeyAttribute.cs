using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFGenericRepository.Attributes
{
    [System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
        public PrimaryKeyAttribute()
        {

        }
    }
}
