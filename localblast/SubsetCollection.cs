using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LocalBlast
{
    public class SubsetCollection<T> : IList<T>, IReadOnlyCollection<T>, IList
    {
        private readonly IReadOnlyList<T> collection;
        private readonly int startIndex;

        public SubsetCollection(IReadOnlyList<T> collection, int startIndex, int count)
        {
            if (startIndex < 0 || (startIndex >= collection.Count && count > 0))
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            if (count < 0 || startIndex + count > collection.Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            this.collection = collection;
            this.startIndex = startIndex;
            Count = count;
        }

        public T this[int index] => collection[startIndex + index];

        public int Count { get; }

        public bool IsReadOnly => true;

        public IEnumerator<T> GetEnumerator()
        {
            return Count == 0 ? Enumerable.Empty<T>().GetEnumerator() : collection.Take(startIndex..(startIndex + Count - 1)).GetEnumerator();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
                array[arrayIndex] = collection[startIndex + i];
        }

        public bool Contains(T item) => IndexOf(item) >= 0;

        public int IndexOf(T item)
        {
            var comp = EqualityComparer<T>.Default;

            for (int i = 0; i < Count; i++)
            {
                if (comp.Equals(collection[startIndex + i], item))
                    return i;
            }
            return -1;
        }

        #region Explicit implementations

        T IList<T>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        bool IList.IsFixedSize => true;

        bool IList.IsReadOnly => true;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => collection is ICollection coll ? coll.SyncRoot : this;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array is T[] array2)
                CopyTo(array2, index);
            else
            {
                for (int i = 0; i < Count; i++)
                    array.SetValue(collection[startIndex + i], index + i);
            }
        }

        bool IList.Contains(object? value)
        {
            return value is T item && Contains(item);
        }

        int IList.IndexOf(object? value)
        {
            return value is T item ? IndexOf(item) : -1;
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        int IList.Add(object? value)
        {
            throw new NotSupportedException();
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList.Insert(int index, object? value)
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(object? value)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}