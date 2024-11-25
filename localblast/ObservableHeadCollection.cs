using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LocalBlast;

public class ObservableHeadCollection<T>(IReadOnlyList<T> collection, int count)
    : IList<T>, IReadOnlyList<T>, IList, INotifyPropertyChanged, INotifyCollectionChanged
{
    private SubsetCollection<T> view = new(collection, 0, count);
    private static readonly PropertyChangedEventArgs CountChanged = new(nameof(Count));
    private static readonly PropertyChangedEventArgs IndexerChanged = new("Item[]");

    public T this[int index] => view[index];

    public int Count => view.Count;

    public bool IsReadOnly => true;

    public IEnumerator<T> GetEnumerator() => view.GetEnumerator();

    public void CopyTo(T[] array, int arrayIndex) => view.CopyTo(array, arrayIndex);

    public bool Contains(T item) => view.Contains(item);

    public int IndexOf(T item) => view.IndexOf(item);

    public void ViewMoreItems(int maxCount)
    {
        int currentCount = Count;
        int addCount = Math.Min(maxCount, collection.Count - currentCount);

        view = new SubsetCollection<T>(collection, 0, currentCount + addCount);

        PropertyChanged?.Invoke(this, CountChanged);
        PropertyChanged?.Invoke(this, IndexerChanged);

        for (int i = currentCount; i < view.Count; i++)
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, view[i], i));
    }

    public void ViewAllItems() => ViewMoreItems(collection.Count - Count);

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

    bool IList.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => (view as IList).SyncRoot;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection.CopyTo(Array array, int index) => (view as IList).CopyTo(array, index);

    bool IList.Contains(object? value) => (view as IList).Contains(value);

    int IList.IndexOf(object? value) => (view as IList).IndexOf(value);

    void ICollection<T>.Add(T item) => throw new NotSupportedException();

    int IList.Add(object? value) => throw new NotSupportedException();

    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

    void IList.Insert(int index, object? value) => throw new NotSupportedException();

    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

    void IList.Remove(object? value) => throw new NotSupportedException();

    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

    void IList.RemoveAt(int index) => throw new NotSupportedException();

    void ICollection<T>.Clear() => throw new NotSupportedException();

    void IList.Clear() => throw new NotSupportedException();

    #endregion

    public event PropertyChangedEventHandler? PropertyChanged;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
}
