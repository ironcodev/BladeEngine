using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BladeEngine.Core.Utils
{
	public class OpenStack<T> : IEnumerable<T>
	{
		T[] items;

		public OpenStack()
		{
			items = new T[0];
		}
		public int Count
		{
			get
			{
				return items.Length;
			}
		}
		public void Push(T item)
		{
			var temp = new T[items.Length + 1];

			if (items.Length > 0)
			{
				Array.Copy(items, 0, temp, 1, items.Length);
			}

			temp[0] = item;

			items = temp;
		}
		private bool Pop(out T item, int index, bool throwOnEmptyStack)
        {
			var result = false;

			if (items.Length == 0 || index < 0 || index > items.Length - 1)
			{
				if (throwOnEmptyStack)
				{
					throw new IndexOutOfRangeException();
				}
				else
                {
					item = default;
                }
			}
			else
			{
				item = items[index];

				var temp = new T[items.Length - 1];

				if (temp.Length > 0)
				{
					if (index > 0)
					{
						Array.Copy(items, 0, temp, 0, index);
					}

					if (items.Length - index - 1 > 0)
					{
						Array.Copy(items, index + 1, temp, index, items.Length - index - 1);
					}
				}

				items = temp;

				result = true;
			}

			return result;
		}
		public T Pop(int index = 0)
		{
			T item;

			Pop(out item, index: index, throwOnEmptyStack: true);

			return item;
		}
		public bool TryPop(out T item, int index = 0)
        {
			return Pop(out item, index: index, throwOnEmptyStack: false);
        }
		private bool Peek(out T item, int index, bool throwOnEmptyStack)
		{
			var result = false;

			if (items.Length == 0 || index < 0 || index > items.Length - 1)
			{
				if (throwOnEmptyStack)
				{
					throw new IndexOutOfRangeException();
				}
				else
				{
					item = default;
				}
			}
			else
			{
				item = items[index];

				result = true;
			}

			return result;
		}
		public T Peek(int index = 0)
		{
			T item;

			Peek(out item, index, throwOnEmptyStack: true);

			return item;
		}
		public bool TryPeek(out T item, int index = 0)
        {
			return Peek(out item, index: index, throwOnEmptyStack: false);
        }
		public IEnumerator<T> GetEnumerator()
		{
			for (var i = 0; i < items.Length; i++)
			{
				yield return items[i];
			}
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		public T this[int index]
		{
			get
			{
				return items[index];
			}
			set
			{
				items[index] = value;
			}
		}
		public void Clear()
        {
			items = new T[0];
        }
		public bool Contains(T item)
        {
			return items.Contains(item);
        }
		public bool Contains(T item, IEqualityComparer<T> comparer)
		{
			return items.Contains(item, comparer);
		}
		public T[] ToArray(int? fromIndex = null, int? toIndex = null)
        {
			var from = 0;
			var to = items.Length - 1;

			if (fromIndex.HasValue)
            {
				from = fromIndex.Value;
            }

			if (toIndex.HasValue)
            {
				to = toIndex.Value;
            }

			if (from > to)
            {
				var t = from;
				from = to;
				to = t;
            }

			if (from < 0 || from > items.Length - 1 || to < 0 || to > items.Length - 1)
            {
				throw new IndexOutOfRangeException();
			}

			var temp = new T[to - from + 1];

			if (temp.Length > 0)
            {
				Array.Copy(items, from, temp, 0, temp.Length);
            }

			return temp;
		}
	}
}
