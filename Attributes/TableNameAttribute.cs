using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFGenericRepository.Attributes
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class TableNameAttribute : Attribute
    {
        readonly string value;

        // This is a positional argument
        public TableNameAttribute(string tableName)
        {
            value = tableName;
        }

        public string Value
        {
            get { return value; }
        }
    }
}
