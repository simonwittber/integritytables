using System;

namespace IntegrityTables
{
    public class RowObjectAdapter
    {
        private readonly ITableMetadata _meta;
        
        public object row;
        
        public bool IsDirty { get; private set; }
        
        public int Count => _meta.Count;
        
        public int id => (int)_meta.Get(row, 0);

        public bool IsPending { get; set; }
        
        public RowObjectAdapter(ITableMetadata meta, object row)
        {
            _meta = meta;
            this.row = row;
            IsDirty = false;
        }
        
        public object this[string fieldName]
        {
            get => _meta.Get(row, _meta.IndexOf(fieldName));
            set
            {
                _meta.Set(ref row, _meta.IndexOf(fieldName), value);
                IsDirty = true;
            }
        }

        public object this[int index]
        {
            get => _meta.Get(row, index);
            set
            {
                _meta.Set(ref row, index, value);
                IsDirty = true;
            }
        }

        public (int index, string name, Type type, Type referencedType) GetInfo(int fieldIndex) => _meta.GetInfo(fieldIndex);

        public override string ToString()
        {
            return $"{_meta.GetType().Name} {id} {row.GetType().Name} {row} {IsDirty} {IsPending}";
        }

        public void UpdateInternalObject(object newRow)
        {
            if(newRow.GetType() != this.row.GetType())
                throw new InvalidOperationException($"Row type mismatch: {this.row.GetType()} != {newRow.GetType()}");
            this.row = newRow;
            IsDirty = true;
        }
    }
}