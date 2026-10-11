using System;
using System.Collections;
using System.Collections.Generic;

namespace AQ.Generators
{
    internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    {
        private readonly T[]? _items;

        public EquatableArray(T[] items)
        {
            _items = items;
        }

        public int Length => _items?.Length ?? 0;

        public T this[int index] => _items![index];

        public bool Equals(EquatableArray<T> other)
        {
            var left = _items ?? Array.Empty<T>();
            var right = other._items ?? Array.Empty<T>();
            if (left.Length != right.Length)
            {
                return false;
            }

            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < left.Length; i++)
            {
                if (!comparer.Equals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

        public override int GetHashCode()
        {
            var hash = 17;
            var comparer = EqualityComparer<T>.Default;
            foreach (var item in _items ?? Array.Empty<T>())
            {
                hash = unchecked(hash * 31 + (item is null ? 0 : comparer.GetHashCode(item)));
            }

            return hash;
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal static class EquatableArrayExtensions
    {
        public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source)
        {
            return new EquatableArray<T>(new List<T>(source).ToArray());
        }
    }
}
