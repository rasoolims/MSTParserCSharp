using MSTParser;

namespace MSTParser
{
    public class KBestParseForest2O
    {
        private readonly ParseForestItem[,,,,] m_chart;
        private readonly int m_end;
        private int m_K;
        private string[] m_pos;
        private string[] m_sent;
        private int m_start;

        public KBestParseForest2O(int start, int end, DependencyInstance inst, int K)
        {
            this.m_K = K;
            m_chart = new ParseForestItem[end + 1,end + 1,2,3,K];
            m_start = start;
            m_end = end;
            m_sent = inst.Sentence;
            m_pos = inst.POS;
        }

        public bool Add(int s, int type, int dir, double score, FeatureVector fv)
        {
            bool added = false;

            if (m_chart[s, s, dir, 0, 0] == null)
            {
                for (int i = 0; i < m_K; i++)
                    m_chart[s, s, dir, 0, i] = new ParseForestItem(s, type, dir, double.NegativeInfinity, null);
            }

            if (m_chart[s, s, dir, 0, m_K - 1].Prob > score)
                return false;

            for (int i = 0; i < m_K; i++)
            {
                if (m_chart[s, s, dir, 0, i].Prob < score)
                {
                    ParseForestItem tmp = m_chart[s, s, dir, 0, i];
                    m_chart[s, s, dir, 0, i] = new ParseForestItem(s, type, dir, score, fv);
                    for (int j = i + 1; j < m_K && tmp.Prob != double.NegativeInfinity; j++)
                    {
                        ParseForestItem tmp1 = m_chart[s, s, dir, 0, j];
                        m_chart[s, s, dir, 0, j] = tmp;
                        tmp = tmp1;
                    }
                    added = true;
                    break;
                }
            }

            return added;
        }

        public bool Add(int s, int r, int t, int type,
                        int dir, int comp, double score,
                        FeatureVector fv,
                        ParseForestItem p1, ParseForestItem p2)
        {
            bool added = false;

            if (m_chart[s, t, dir, comp, 0] == null)
            {
                for (int i = 0; i < m_K; i++)
                    m_chart[s, t, dir, comp, i] =
                        new ParseForestItem(s, r, t, type, dir, comp, double.NegativeInfinity, null, null, null);
            }

            if (m_chart[s, t, dir, comp, m_K - 1].Prob > score)
                return false;

            for (int i = 0; i < m_K; i++)
            {
                if (m_chart[s, t, dir, comp, i].Prob < score)
                {
                    ParseForestItem tmp = m_chart[s, t, dir, comp, i];
                    m_chart[s, t, dir, comp, i] = new ParseForestItem(s, r, t, type, dir, comp, score, fv, p1, p2);
                    for (int j = i + 1; j < m_K && tmp.Prob != double.NegativeInfinity; j++)
                    {
                        ParseForestItem tmp1 = m_chart[s, t, dir, comp, j];
                        m_chart[s, t, dir, comp, j] = tmp;
                        tmp = tmp1;
                    }
                    added = true;
                    break;
                }
            }

            return added;
        }

        public double GetProb(int s, int t, int dir, int comp)
        {
            return GetProb(s, t, dir, comp, 0);
        }

        public double GetProb(int s, int t, int dir, int comp, int i)
        {
            if (m_chart[s, t, dir, comp, i] != null)
                return m_chart[s, t, dir, comp, i].Prob;
            return double.NegativeInfinity;
        }

        public double[] GetProbs(int s, int t, int dir, int comp)
        {
            var result = new double[m_K];
            for (int i = 0; i < m_K; i++)
                result[i] =
                    m_chart[s, t, dir, comp, i] != null ? m_chart[s, t, dir, comp, i].Prob : double.NegativeInfinity;
            return result;
        }

        public ParseForestItem GetItem(int s, int t, int dir, int comp)
        {
            return GetItem(s, t, dir, comp, 0);
        }

        public ParseForestItem GetItem(int s, int t, int dir, int comp, int i)
        {
            if (m_chart[s, t, dir, comp, i] != null)
                return m_chart[s, t, dir, comp, i];
            return null;
        }

        public ParseForestItem[] GetItems(int s, int t, int dir, int comp)
        {
            if (m_chart[s, t, dir, comp, 0] != null)
            {
                int len = m_chart.GetLength(4);
                var items = new ParseForestItem[len];
                for (int i = 0; i < len; i++)
                {
                    items[i] = m_chart[s, t, dir, comp, i];
                }
                return items;
            }
            return null;
        }

        public object[] GetBestParse()
        {
            var d = new object[2];
            d[0] = GetFeatureVector(m_chart[0, m_end, 0, 0, 0]);
            d[1] = GetDepString(m_chart[0, m_end, 0, 0, 0]);
            return d;
        }

        public object[,] GetBestParses()
        {
            var d = new object[m_K,2];
            for (int k = 0; k < m_K; k++)
            {
                if (m_chart[0, m_end, 0, 0, k].Prob != double.NegativeInfinity)
                {
                    d[k, 0] = GetFeatureVector(m_chart[0, m_end, 0, 0, k]);
                    d[k, 1] = GetDepString(m_chart[0, m_end, 0, 0, k]);
                }
                else
                {
                    d[k, 0] = null;
                    d[k, 1] = null;
                }
            }
            return d;
        }

        public FeatureVector GetFeatureVector(ParseForestItem pfi)
        {
            if (pfi.Left == null)
                return pfi.FV;

            return Cat(pfi.FV, Cat(GetFeatureVector(pfi.Left), GetFeatureVector(pfi.Right)));
        }

        public string GetDepString(ParseForestItem pfi)
        {
            if (pfi.Left == null)
                return "";

            if (pfi.Dir == 0 && pfi.Comp == 1)
                return
                    ((GetDepString(pfi.Left) + " " + GetDepString(pfi.Right)).Trim() + " " + pfi.S + "|" + pfi.T + ":" +
                     pfi.Type).Trim();
            if (pfi.Dir == 1 && pfi.Comp == 1)
                return
                    (pfi.T + "|" + pfi.S + ":" + pfi.Type + " " +
                     (GetDepString(pfi.Left) + " " + GetDepString(pfi.Right)).Trim()).Trim();
            return (GetDepString(pfi.Left) + " " + GetDepString(pfi.Right)).Trim();
        }

        public FeatureVector Cat(FeatureVector fv1, FeatureVector fv2)
        {
            return FeatureVector.Cat(fv1, fv2);
        }


        // returns pairs of indeces and -1,-1 if < K pairs
        public int[,] GetKBestPairs(ParseForestItem[] items1, ParseForestItem[] items2)
        {
            // in_ this case K = items1.Length

            var beenPushed = new bool[m_K,m_K];

            var result = new int[m_K,2];
            for (int i = 0; i < m_K; i++)
            {
                result[i, 0] = -1;
                result[i, 1] = -1;
            }

            var heap = new BinaryHeap(m_K + 1);
            int n = 0;
            var vip = new ValueIndexPair(items1[0].Prob + items2[0].Prob, 0, 0);

            heap.Add(vip);
            beenPushed[0, 0] = true;

            while (n < m_K)
            {
                vip = heap.RemoveMax();

                if (vip.Val == double.NegativeInfinity)
                    break;

                result[n, 0] = vip.I1;
                result[n, 1] = vip.I2;

                n++;
                if (n >= m_K)
                    break;

                if (!beenPushed[vip.I1 + 1, vip.I2])
                {
                    heap.Add(new ValueIndexPair(items1[vip.I1 + 1].Prob + items2[vip.I2].Prob, vip.I1 + 1, vip.I2));
                    beenPushed[vip.I1 + 1, vip.I2] = true;
                }
                if (!beenPushed[vip.I1, vip.I2 + 1])
                {
                    heap.Add(new ValueIndexPair(items1[vip.I1].Prob + items2[vip.I2 + 1].Prob, vip.I1, vip.I2 + 1));
                    beenPushed[vip.I1, vip.I2 + 1] = true;
                }
            }

            return result;
        }
    }
}