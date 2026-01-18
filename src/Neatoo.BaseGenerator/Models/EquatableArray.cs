using System.Collections;
using System.Collections.Immutable;

namespace Neatoo.BaseGenerator.Models;

/// <summary>
/// Wrapper for ImmutableArray that implements structural equality.
/// Required for incremental generator caching - Roslyn compares results
/// using equality and only regenerates if values change.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _items;

    public EquatableArray(ImmutableArray<T> items) => _items = items;

    public EquatableArray(IEnumerable<T>? items) =>
        _items = items?.ToImmutableArray() ?? ImmutableArray<T>.Empty;

    public bool IsDefault => _items.IsDefault;
    public bool IsDefaultOrEmpty => _items.IsDefaultOrEmpty;

    public bool Equals(EquatableArray<T> other)
    {
        // Both default or both empty are equal
        if (_items.IsDefault && other._items.IsDefault) return true;
        if (_items.IsDefault || other._items.IsDefault) return false;
        if (_items.Length != other._items.Length) return false;

        for (int i = 0; i < _items.Length; i++)
        {
            if (!_items[i].Equals(other._items[i])) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_items.IsDefault) return 0;

        unchecked
        {
            int hash = 17;
            foreach (var item in _items)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    public int Count => _items.IsDefaultOrEmpty ? 0 : _items.Length;

    public T this[int index] => _items[index];

    public IEnumerator<T> GetEnumerator() =>
        _items.IsDefault
            ? Enumerable.Empty<T>().GetEnumerator()
            : ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
