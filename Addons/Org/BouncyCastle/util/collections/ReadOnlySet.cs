using System;
using System.Collections;
using System.Collections.Generic;

namespace MultiServer.Addons.Org.BouncyCastle.Utilities.Collections
{
    internal abstract class ReadOnlySet<T>
#if NETCOREAPP1_0_OR_GREATER || NET40_OR_GREATER || NETSTANDARD1_0_OR_GREATER
        : ISet<T>
#else
        : ICollection<T>, IEnumerable<T>, IEnumerable
#endif
    {
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsReadOnly => true;

        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        public bool Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public void ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
        public void IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public bool SetEquals(IEnumerable<T> other) => throw new NotSupportedException();
        public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
        public void UnionWith(IEnumerable<T> other) => throw new NotSupportedException();

        public abstract bool Contains(T item);
        public abstract void CopyTo(T[] array, int arrayIndex);
        public abstract int Count { get; }
        public abstract IEnumerator<T> GetEnumerator();
        public abstract bool IsProperSubsetOf(IEnumerable<T> other);
        public abstract bool IsProperSupersetOf(IEnumerable<T> other);
        public abstract bool IsSubsetOf(IEnumerable<T> other);
        public abstract bool IsSupersetOf(IEnumerable<T> other);
        public abstract bool Overlaps(IEnumerable<T> other);
    }

#if NETCOREAPP1_0_OR_GREATER || NET40_OR_GREATER || NETSTANDARD1_0_OR_GREATER
    internal class ReadOnlySetProxy<T>
        : ReadOnlySet<T>
    {
        private readonly ISet<T> m_target;

        internal ReadOnlySetProxy(ISet<T> target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            m_target = target;
        }

        public override bool Contains(T item) => m_target.Contains(item);
        public override void CopyTo(T[] array, int arrayIndex) => m_target.CopyTo(array, arrayIndex);
        public override int Count => m_target.Count;
        public override IEnumerator<T> GetEnumerator() => m_target.GetEnumerator();
        public override bool IsProperSubsetOf(IEnumerable<T> other) => m_target.IsProperSubsetOf(other);
        public override bool IsProperSupersetOf(IEnumerable<T> other) => m_target.IsProperSupersetOf(other);
        public override bool IsSubsetOf(IEnumerable<T> other) => m_target.IsSubsetOf(other);
        public override bool IsSupersetOf(IEnumerable<T> other) => m_target.IsSupersetOf(other);
        public override bool Overlaps(IEnumerable<T> other) => m_target.Overlaps(other);
    }
#endif
}
