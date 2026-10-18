using System;
using System.Collections.Generic;

namespace TerminalEntry
{
	// Token: 0x02000002 RID: 2
	/// <summary>
	/// Fixed-size history list with a cursor. Put() appends (dropping the oldest entry when full),
	/// Backward()/Forward() move through the entries like shell history.
	/// </summary>
	public class DropOutList<T>
	{
		public DropOutList(int size)
		{
			this.m_capacity = Math.Max(1, size);
			this.m_items = new List<T>(this.m_capacity);
		}

		// Moves to an older entry. Returns false when already at the oldest one.
		public bool Backward(out T res)
		{
			if (this.m_pos > 0)
			{
				this.m_pos--;
				res = this.m_items[this.m_pos];
				return true;
			}
			res = default(T);
			return false;
		}

		// Moves to a newer entry. Returns false (and leaves the cursor "after newest") when there is none.
		public bool Forward(out T res)
		{
			if (this.m_pos < this.m_items.Count - 1)
			{
				this.m_pos++;
				res = this.m_items[this.m_pos];
				return true;
			}
			this.m_pos = this.m_items.Count;
			res = default(T);
			return false;
		}

		public bool Current(out T res)
		{
			if (this.m_pos >= 0 && this.m_pos < this.m_items.Count)
			{
				res = this.m_items[this.m_pos];
				return true;
			}
			res = default(T);
			return false;
		}

		public void Put(T value)
		{
			this.m_items.Add(value);
			if (this.m_items.Count > this.m_capacity)
			{
				this.m_items.RemoveAt(0);
			}
			this.m_pos = this.m_items.Count;
		}

		private readonly int m_capacity;

		private readonly List<T> m_items;

		// Index of the entry the cursor points at; == Count means "after the newest entry".
		private int m_pos;
	}
}
