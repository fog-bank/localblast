using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LocalBlast
{
	public class Hit
	{
		public Hit(TabPage owner, XElement hitElement, XNamespace ns)
		{
			Parent = owner;
			Index = (int)hitElement.Element(ns + "num");

			var hitDescr = hitElement.Descendants(ns + "HitDescr").FirstOrDefault();
			Id = (string)hitDescr?.Element(ns + "id");
			Accession = (string)hitDescr?.Element(ns + "accession");
			Title = (string)hitDescr?.Element(ns + "title");

			Length = (int)hitElement.Element(ns + "len");

			Segments = new List<SegmentPair>();

			var queryLoc = new Location();
			var hitLoc = new Location();
			double bitScore = double.MinValue;
			double minEValue = double.MaxValue;

			foreach (var hsp in hitElement.Descendants(ns + "Hsp"))
			{
				var value = new SegmentPair(this, hsp, ns);
				Segments.Add(value);

				queryLoc.UnionWith(new Span(value.QueryFrom, value.QueryTo));
				hitLoc.UnionWith(new Span(value.HitFrom, value.HitTo));

				bitScore = Math.Max(bitScore, value.BitScore);
				minEValue = Math.Min(minEValue, value.EValue);
			}
			MaxBitScore = bitScore;
			MinEValue = minEValue;
			QueryCover = (double)queryLoc.TotalLength / (owner as BlastpPage).QueryLength;
			HitCover = (double)hitLoc.TotalLength / Length;
			QueryFrom = queryLoc.From;
			QueryTo = queryLoc.To;
		}

		public TabPage Parent { get; }

		public int Index { get; }

		public string Id { get; }
		
		public string Accession { get; }
		
		public string Title { get; }
		
		public int Length { get; }
		
		public IList<SegmentPair> Segments { get; }

		public double MaxBitScore { get; }

		public double MinEValue { get; }

		public double QueryCover { get; }

		public double HitCover { get; }

		// for view
		public int QueryFrom { get; }

		public int QueryTo { get; }
	}

	public class SegmentPair
	{
		public SegmentPair(Hit owner, XElement hspElement, XNamespace ns)
		{
			Parent = owner;
			Index = (int)hspElement.Element(ns + "num");
			BitScore = (double)hspElement.Element(ns + "bit-score");
			Score = (int)hspElement.Element(ns + "score");
			EValue = (double)hspElement.Element(ns + "evalue");
			Identity = (int)hspElement.Element(ns + "identity");
			Positive = (int)hspElement.Element(ns + "positive");
			QueryFrom = (int)hspElement.Element(ns + "query-from");
			QueryTo = (int)hspElement.Element(ns + "query-to");
			HitFrom = (int)hspElement.Element(ns + "hit-from");
			HitTo = (int)hspElement.Element(ns + "hit-to");
			AlignLength = (int)hspElement.Element(ns + "align-len");
			Gaps = (int)hspElement.Element(ns + "gaps");
			QuerySeq = (string)hspElement.Element(ns + "qseq");
			HitSeq = (string)hspElement.Element(ns + "hseq");
			Alignment = (string)hspElement.Element(ns + "midline");

            var sb = new StringBuilder(AlignLength + 10);

            for (int i = QueryFrom; i <= QueryTo;)
            {
                if (i % 100 == 1)
                {
                    string mark = "|" + i;
                    sb.Append(mark);
                    i += mark.Length;
                }
                else
                {
                    sb.Append(' ');
                    i++;
                }
            }
            QueryScaleMark = sb.ToString();
            sb.Clear();

            for (int i = HitFrom; i <= HitTo;)
            {
                if (i % 100 == 1)
                {
                    string mark = "|" + i;
                    sb.Append(mark);
                    i += mark.Length;
                }
                else
                {
                    sb.Append(' ');
                    i++;
                }
            }
            HitScaleMark = sb.ToString();
        }

		public Hit Parent { get; }

		public int Index { get; }

		public double BitScore { get; }

		public int Score { get; }

		public double EValue { get; }

		public int Identity { get; }

		public int Positive { get; }

		public int Gaps { get; }

		public int QueryFrom { get; }

		public int QueryTo { get; }

		public int HitFrom { get; }

		public int HitTo { get; }

		public int AlignLength { get; }

		public string QuerySeq { get; }

		public string HitSeq { get; }

		public string Alignment { get; }

        public string QueryScaleMark { get; }

        public string HitScaleMark { get; }

        // for view
        public int ZIndex => -Index;

		public int QueryWidth => QueryTo - QueryFrom + 1;

		public double IdentityRatio => (double)Identity / AlignLength;

		public int BitScoreLevel
		{
			get
			{
				if (BitScore < 40)
					return 0;

				if (BitScore < 50)
					return 1;

				if (BitScore < 80)
					return 2;

				if (BitScore < 200)
					return 3;

				return 4;
			}
		}
	}
}
