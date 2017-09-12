namespace EFGenericRepository
{
    internal class BaseColumn
    {
        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
        public bool IsPrimaryKey { get; set; }
        public object Value { get; set; }
    }
}
