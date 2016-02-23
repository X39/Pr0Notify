using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pr0Notify2
{
    public class EventedList<T> : List<T>
    {
        public class ItemEventArgs : EventArgs
        {
            public T Value { get; internal set; }
            public ItemEventArgs(T item)
            {
                this.Value = item;
            }
        }
        public class RangeEventArgs : EventArgs
        {
            public IEnumerable<T> Value { get; internal set; }
            public RangeEventArgs(IEnumerable<T> item)
            {
                this.Value = item;
            }
        }
        public event EventHandler<ItemEventArgs> ItemAdded;
        public event EventHandler<RangeEventArgs> RangeAdded;
        new public void Add(T item)
        {
            base.Add(item);
            var ItemAdded = this.ItemAdded;
            if (ItemAdded != null)
                ItemAdded(this, new ItemEventArgs(item));
        }
        new public void AddRange(IEnumerable<T> items)
        {
            base.AddRange(items);
            var RangeAdded = this.RangeAdded;
            if (RangeAdded != null)
                RangeAdded(this, new RangeEventArgs(items));
            var ItemAdded = this.ItemAdded;
            if (ItemAdded != null)
                foreach (var it in items)
                    ItemAdded(this, new ItemEventArgs(it));
        }
        new public void Insert(int index, T item)
        {
            base.Insert(index, item);
            var ItemAdded = this.ItemAdded;
            if (ItemAdded != null)
                ItemAdded(this, new ItemEventArgs(item));
        }
        new public void InsertRange(int index, IEnumerable<T> items)
        {
            base.InsertRange(index, items);
            var RangeAdded = this.RangeAdded;
            if (RangeAdded != null)
                RangeAdded(this, new RangeEventArgs(items));
            var ItemAdded = this.ItemAdded;
            if (ItemAdded != null)
                foreach (var it in items)
                    ItemAdded(this, new ItemEventArgs(it));
        }
    }
}
