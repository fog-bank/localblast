using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LocalBlast
{
	public struct SeqSpan : IEquatable<SeqSpan>, IComparable<SeqSpan>
	{
		public SeqSpan(int from, int to)
		{
			From = Math.Min(from, to);
			To = Math.Max(from, to);
		}

		public int From { get; }

		public int To { get; }

		public int Length => To - From + 1;

		public bool Equals(SeqSpan other) => From == other.From && To == other.To;

		public override bool Equals(object? obj) => obj is SeqSpan span && Equals(span);

		public bool Contains(int point) => From <= point && point <= To;

		public bool Overlaps(SeqSpan other) => From <= other.To && other.From <= To;

		public bool IsNeighbor(SeqSpan other) => To + 1 == other.From || other.To + 1 == From;

		public bool IsSupersetOf(SeqSpan other) => From <= other.From && other.To <= To;

		public bool IsSubsetOf(SeqSpan other) => other.IsSupersetOf(this);

		public static SeqSpan Union(SeqSpan one, SeqSpan other)
		{
			return new SeqSpan(Math.Min(one.From, other.From), Math.Max(one.To, other.To));
		}

		public static SeqSpan Intersect(SeqSpan one, SeqSpan other)
		{
			if (!one.Overlaps(other))
				throw new ArgumentException();

			return new SeqSpan(Math.Max(one.From, other.From), Math.Min(one.To, other.To));
		}

		public int CompareTo(SeqSpan other)
		{
			int fromComp = From.CompareTo(other.From);
			return (fromComp != 0) ? fromComp : other.To.CompareTo(To);
		}

		public override int GetHashCode() => ((From & 0xffff) << 16) + (To & 0xffff);

		public override string ToString() => From == To ? From.ToString() : From + ".." + To;

		public static bool operator ==(SeqSpan one, SeqSpan other) => one.Equals(other);

		public static bool operator !=(SeqSpan one, SeqSpan other) => !one.Equals(other);

		public static bool operator >(SeqSpan one, SeqSpan other) => one.CompareTo(other) > 0;

		public static bool operator >=(SeqSpan one, SeqSpan other) => one.CompareTo(other) >= 0;

		public static bool operator <(SeqSpan one, SeqSpan other) => one.CompareTo(other) < 0;

		public static bool operator <=(SeqSpan one, SeqSpan other) => one.CompareTo(other) <= 0;
	}

	[DebuggerDisplay("{DebuggerToString}")]
	public class Location
	{
		private LinkedList<SeqSpan> segments = new();

		public Location()
		{
		}

		public int NumberOfSegments => segments.Count;

		public IEnumerable<SeqSpan> Segments => segments;

		public int TotalLength
		{
			get
			{
				int length = 0;

				if (segments != null)
				{
					foreach (var span in segments)
						length += span.Length;
				}
				return length;
			}
		}

		public int From
		{
			get
			{
				if (NumberOfSegments == 0)
					throw new InvalidOperationException();

				return segments.First.Value.From;
			}
		}

		public int To
		{
			get
			{
				if (NumberOfSegments == 0)
					throw new InvalidOperationException();

				return segments.Last.Value.To;
			}
		}

		private string DebuggerToString => NumberOfSegments > 3 ? "NumberOfSegments = " + NumberOfSegments : ToString();

		public bool Overlaps(SeqSpan other)
		{
			foreach (var one in segments)
			{
				if (one.Overlaps(other))
					return true;

				if (other.To < one.From)
					return false;
			}
			return false;
		}

		public bool Overlaps(Location location)
		{
			foreach (var other in location.segments)
			{
				if (Overlaps(other))
					return true;
			}
			return false;
		}

		public bool IsSupersetOf(SeqSpan other)
		{
			foreach (var seg in segments)
			{
				if (seg.IsSupersetOf(other))
					return true;

				if (seg.Overlaps(other))
					return false;
			}
			return false;
		}

		public bool IsSupersetOf(Location location)
		{
			foreach (var seg in location.segments)
			{
				if (!IsSupersetOf(seg))
					return false;
			}
			return true;
		}

		public void IntersectWith(SeqSpan other)
		{
			for (var node = segments.First; node != null; )
			{
				if (node.Value.Overlaps(other))
				{
					node.Value = SeqSpan.Intersect(node.Value, other);
					node = node.Next;
				}
				else
				{
					var nextNode = node.Next;
					segments.Remove(node);
					node = nextNode;
				}
			}
		}

		public void IntersectWith(Location location)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			if (NumberOfSegments == 0)
				return;

			if (location.NumberOfSegments == 0)
			{
				segments.Clear();
				return;
			}

			// A ∩ (∪{Bi}) = ∪{A ∩ Bi}
			var spans = new List<SeqSpan>();

			foreach (var one in segments)
			{
				foreach (var other in location.segments)
				{
					if (one.Overlaps(other))
						spans.Add(SeqSpan.Intersect(one, other));
				}
			}
			segments = new LinkedList<SeqSpan>(spans);
		}

		public void UnionWith(SeqSpan other)
		{
			bool added = false;

			for (var node = segments.First; node != null; node = node.Next)
			{
				if (!added)
				{
					if (CanUnion(node.Value, other))
					{
						node.Value = SeqSpan.Union(node.Value, other);
						added = true;
					}
					else if (other.To < node.Value.From)
					{
						segments.AddBefore(node, other);
						added = true;
						break;
					}
				}
				else
				{
					var prevNode = node.Previous;

					if (CanUnion(prevNode.Value, node.Value))
					{
						prevNode.Value = SeqSpan.Union(prevNode.Value, node.Value);
						segments.Remove(node);
						node = prevNode;
					}
					else if (prevNode.Value.To < node.Value.From)
						break;
				}
			}

			if (!added)
				segments.AddLast(other);
		}

		public override string ToString()
		{
			if (NumberOfSegments == 0)
				return "NumberOfSegments = 0";

			if (NumberOfSegments == 1)
				return segments.First.ToString();

			return "join(" + string.Join(",", segments) + ")";
		}

#if DEBUG
#pragma warning disable IDE0051 // Remove unused private members
        private bool Invalidate()
        {
			for (var node1 = segments.First; node1 != null; node1 = node1.Next)
			{
				if (node1.Previous != null && node1.Previous.Value.To + 1 >= node1.Value.From)
					return false;
			}
			return true;
		}
#pragma warning restore IDE0051
#endif

		private static bool CanUnion(SeqSpan one, SeqSpan other) => one.Overlaps(other) || one.IsNeighbor(other);
	}
}
