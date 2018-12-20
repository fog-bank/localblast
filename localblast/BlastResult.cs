using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LocalBlast
{
    public class Hit
    {
        public Hit(TabPage owner, XNamespace ns, XElement hitElement, int queryLength)
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

                var querySpan = new Span(value.QueryFrom, value.QueryTo);

                if (!queryLoc.IsSupersetOf(querySpan))
                    value.IsVisible = true;

                queryLoc.UnionWith(querySpan);
                hitLoc.UnionWith(new Span(value.HitFrom, value.HitTo));

                bitScore = Math.Max(bitScore, value.BitScore);
                minEValue = Math.Min(minEValue, value.EValue);
            }
            MaxBitScore = bitScore;
            MinEValue = minEValue;
            QueryCover = TruncateRatio(queryLoc.TotalLength, queryLength);
            HitCover = TruncateRatio(hitLoc.TotalLength, Length);
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

        public static double TruncateRatio(long numerator, long denominator) => (numerator * 100 / denominator) / 100.0;
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
            Positive = (int?)hspElement.Element(ns + "positive") ?? 0;
            QueryFrom = (int)hspElement.Element(ns + "query-from");
            QueryTo = (int)hspElement.Element(ns + "query-to");
            QueryStrand = (string)hspElement.Element(ns + "query-strand");
            QueryFrame = (string)hspElement.Element(ns + "query-frame");
            HitFrom = (int)hspElement.Element(ns + "hit-from");
            HitTo = (int)hspElement.Element(ns + "hit-to");
            HitStrand = (string)hspElement.Element(ns + "hit-strand");
            HitFrame = (string)hspElement.Element(ns + "hit-frame");
            AlignLength = (int)hspElement.Element(ns + "align-len");
            Gaps = (int)hspElement.Element(ns + "gaps");
            QuerySeq = (string)hspElement.Element(ns + "qseq");
            HitSeq = (string)hspElement.Element(ns + "hseq");
            Alignment = (string)hspElement.Element(ns + "midline");

            if (QueryFrame != null && !QueryFrame.StartsWith("-"))
                QueryFrame = "+" + QueryFrame;

            if (HitFrame != null && !HitFrame.StartsWith("-"))
                HitFrame = "+" + HitFrame;

            var sb = new StringBuilder(AlignLength + 10);

            if (QuerySeq != null)
                QueryScaleMark = CreateScaleMark(sb, QuerySeq, QueryFrom, QueryTo, QueryStrand, QueryFrame);

            if (HitSeq != null)
                HitScaleMark = CreateScaleMark(sb, HitSeq, HitFrom, HitTo, HitStrand, HitFrame);

            string CreateScaleMark(StringBuilder buffer, string alignment, int from, int to, string strand, string frame)
            {
                buffer.Clear();

                int incr = 1;
                int seqPos = from - 1;

                if (strand == "Minus")
                {
                    incr = -1;
                    seqPos = from + 1;
                }
                else if (frame != null)
                {
                    int frameValue = int.Parse(frame);

                    if (frame.StartsWith("+"))
                    {
                        incr = 3;
                        seqPos = from - 3;
                    }
                    else
                    {
                        incr = -3;
                        seqPos = to + 3;
                    }
                }

                for (int alignPos = 0; alignPos < alignment.Length; alignPos++)
                {
                    if (alignment[alignPos] != '-')
                        seqPos += incr;

                    if (sb.Length == alignPos)
                        buffer.Append(seqPos % 100 == 1 ? "|" + seqPos : " ");
                }
                return buffer.ToString();
            }
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

        public string QueryStrand { get; }

        public string QueryFrame { get; }

        public int HitFrom { get; }

        public int HitTo { get; }

        public string HitStrand { get; }

        public string HitFrame { get; }

        public int AlignLength { get; }

        public string QuerySeq { get; }

        public string HitSeq { get; }

        public string Alignment { get; }

        public string QueryScaleMark { get; }

        public string HitScaleMark { get; }

        // for view
        public int ZIndex => -Index;

        public int QueryWidth => QueryTo - QueryFrom + 1;

        public double IdentityRatio => Hit.TruncateRatio(Identity, AlignLength);

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

        public bool IsVisible { get; set; }
    }
}
