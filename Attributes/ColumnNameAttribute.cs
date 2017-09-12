using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFGenericRepository.Attributes
{
    [System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ColumnNameAttribute : Attribute
    {
        readonly string columnName;

        public ColumnNameAttribute(string columnName)
        {
            this.columnName = columnName;
        }

        public string ColumnName
        {
            get { return columnName; }
        }
    }
}
