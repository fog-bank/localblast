using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LocalBlast
{
	public struct Span : IEquatable<Span>, IComparable<Span>
	{
		public Span(int from, int to)
		{
			From = Math.Min(from, to);
			To = Math.Max(from, to);
		}

		public int From { get; }

		public int To { get; }

		public int Length => To - From + 1;

		public bool Equals(Span other) => From == other.From && To == other.To;

		public override bool Equals(object obj) => obj is Span && Equals((Span)obj);

		public bool Contains(int point) => From <= point && point <= To;

		public bool Overlaps(Span other) => From <= other.To && other.From <= To;

		public bool IsNeighbor(Span other) => To + 1 == other.From || other.To + 1 == From;

		public bool IsSupersetOf(Span other) => From <= other.From && other.To <= To;

		public bool IsSubsetOf(Span other) => other.IsSupersetOf(this);

		public static Span Union(Span one, Span other)
		{
			return new Span(Math.Min(one.From, other.From), Math.Max(one.To, other.To));
		}

		public static Span Intersect(Span one, Span other)
		{
			if (!one.Overlaps(other))
				throw new ArgumentException();

			return new Span(Math.Max(one.From, other.From), Math.Min(one.To, other.To));
		}

		public int CompareTo(Span other)
		{
			int fromComp = From.CompareTo(other.From);
			return (fromComp != 0) ? fromComp : other.To.CompareTo(To);
		}

		public override int GetHashCode() => ((From & 0xffff) << 16) + (To & 0xffff);

		public override string ToString() => From == To ? From.ToString() : From + ".." + To;

		public static bool operator ==(Span one, Span other) => one.Equals(other);

		public static bool operator !=(Span one, Span other) => !one.Equals(other);

		public static bool operator >(Span one, Span other) => one.CompareTo(other) > 0;

		public static bool operator >=(Span one, Span other) => one.CompareTo(other) >= 0;

		public static bool operator <(Span one, Span other) => one.CompareTo(other) < 0;

		public static bool operator <=(Span one, Span other) => one.CompareTo(other) <= 0;
	}

	[DebuggerDisplay("{DebuggerToString}")]
	public class Location
	{
		private LinkedList<Span> segments = new LinkedList<Span>();

		public Location()
		{
		}

		public int NumberOfSegments => segments.Count;

		public IEnumerable<Span> Segments => segments;

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

		public bool Overlaps(Span other)
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

		public bool IsSupersetOf(Span other)
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

		public void IntersectWith(Span other)
		{
			for (var node = segments.First; node != null; )
			{
				if (node.Value.Overlaps(other))
				{
					node.Value = Span.Intersect(node.Value, other);
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
			var spans = new List<Span>();

			foreach (var one in segments)
			{
				foreach (var other in location.segments)
				{
					if (one.Overlaps(other))
						spans.Add(Span.Intersect(one, other));
				}
			}
			segments = new LinkedList<Span>(spans);
		}

		public void UnionWith(Span other)
		{
			bool added = false;

			for (var node = segments.First; node != null; node = node.Next)
			{
				if (!added)
				{
					if (CanUnion(node.Value, other))
					{
						node.Value = Span.Union(node.Value, other);
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
						prevNode.Value = Span.Union(prevNode.Value, node.Value);
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

		private bool Invalidate()
		{
			for (var node1 = segments.First; node1 != null; node1 = node1.Next)
			{
				if (node1.Previous != null && node1.Previous.Value.To + 1 >= node1.Value.From)
					return false;
			}
			return true;
		}

		private bool CanUnion(Span one, Span other) => one.Overlaps(other) || one.IsNeighbor(other);
	}
}
