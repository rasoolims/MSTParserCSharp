using MSTParser;

namespace MSTParser
{
    public class KBestParseForest
    {
        public static int RootType;

        public ParseForestItem[,,,,] Chart;

        private readonly int m_end;
        private int K;
        private string[] m_pos;
        private string[] m_sent;
        private int m_start;

        public KBestParseForest(int start, int end, DependencyInstance inst, int K)
        {
            this.K = K;
            Chart = new ParseForestItem[end + 1,end + 1,2,2,K];
            m_start = start;
            m_end = end;
            m_sent = inst.Sentence;
            m_pos = inst.POS;
        }

        public bool Add(int s, int type, int dir, double score, FeatureVector fv)
        {
            bool added = false;

            if (Chart[s, s, dir, 0, 0] == null)
            {
                for (int i = 0; i < K; i++)
                    Chart[s, s, dir, 0, i] = new ParseForestItem(s, type, dir, double.NegativeInfinity, null);
            }

            if (Chart[s, s, dir, 0, K - 1].Prob > score)
                return false;

            for (int i = 0; i < K; i++)
            {
                if (Chart[s, s, dir, 0, i].Prob < score)
                {
                    ParseForestItem tmp = Chart[s, s, dir, 0, i];
                    Chart[s, s, dir, 0, i] = new ParseForestItem(s, type, dir, score, fv);
                    for (int j = i + 1; j < K && tmp.Prob != double.NegativeInfinity; j++)
                    {
                        ParseForestItem tmp1 = Chart[s, s, dir, 0, j];
                        Chart[s, s, dir, 0, j] = tmp;
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

            if (Chart[s, t, dir, comp, 0] == null)
            {
                for (int i = 0; i < K; i++)
                    Chart[s, t, dir, comp, i] =
                        new ParseForestItem(s, r, t, type, dir, comp, double.NegativeInfinity, null, null, null);
            }

            if (Chart[s, t, dir, comp, K - 1].Prob > score)
                return false;

            for (int i = 0; i < K; i++)
            {
                if (Chart[s, t, dir, comp, i].Prob < score)
                {
                    ParseForestItem tmp = Chart[s, t, dir, comp, i];
                    Chart[s, t, dir, comp, i] =
                        new ParseForestItem(s, r, t, type, dir, comp, score, fv, p1, p2);
                    for (int j = i + 1; j < K && tmp.Prob != double.NegativeInfinity; j++)
                    {
                        ParseForestItem tmp1 = Chart[s, t, dir, comp, j];
                        Chart[s, t, dir, comp, j] = tmp;
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
            if (Chart[s, t, dir, comp, i] != null)
                return Chart[s, t, dir, comp, i].Prob;
            return double.NegativeInfinity;
        }

        public double[] GetProbs(int s, int t, int dir, int comp)
        {
            var result = new double[K];
            for (int i = 0; i < K; i++)
                result[i] =
                    Chart[s, t, dir, comp, i] != null
                        ? Chart[s, t, dir, comp, i].Prob
                        : double.NegativeInfinity;
            return result;
        }

        public ParseForestItem GetItem(int s, int t, int dir, int comp)
        {
            return GetItem(s, t, dir, comp, 0);
        }

        public ParseForestItem GetItem(int s, int t, int dir, int comp, int k)
        {
            if (Chart[s, t, dir, comp, k] != null)
                return Chart[s, t, dir, comp, k];
            return null;
        }

        public ParseForestItem[] GetItems(int s, int t, int dir, int comp)
        {
            if (Chart[s, t, dir, comp, 0] != null)
            {
                int len = Chart.GetLength(4);
                var items = new ParseForestItem[len];
                for (int i = 0; i < len; i++)
                {
                    items[i] = Chart[s, t, dir, comp, i];
                }
                return items;
            }
            return null;
        }


        public object[] GetBestParse()
        {
            var d = new object[2];
            d[0] = GetFeatureVector(Chart[0, m_end, 0, 0, 0]);
            d[1] = GetDepString(Chart[0, m_end, 0, 0, 0]);
            return d;
        }

        public object[,] GetBestParses()
        {
            var d = new object[K,2];
            for (int k = 0; k < K; k++)
            {
                if (Chart[0, m_end, 0, 0, k].Prob != double.NegativeInfinity)
                {
                    d[k, 0] = GetFeatureVector(Chart[0, m_end, 0, 0, k]);
                    d[k, 1] = GetDepString(Chart[0, m_end, 0, 0, k]);
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

            if (pfi.Comp == 0)
            {
                return (GetDepString(pfi.Left) + " " + GetDepString(pfi.Right)).Trim();
            }
            if (pfi.Dir == 0)
            {
                return ((GetDepString(pfi.Left) + " " + GetDepString(pfi.Right)).Trim() + " "
                        + pfi.S + "|" + pfi.T + ":" + pfi.Type).Trim();
            }
            return (pfi.T + "|" + pfi.S + ":" + pfi.Type + " "
                    + (GetDepString(pfi.Left) + " " + GetDepString(pfi.Right)).Trim()).Trim();
        }

        public FeatureVector Cat(FeatureVector fv1, FeatureVector fv2)
        {
            return FeatureVector.Cat(fv1, fv2);
        }


        // returns pairs of indeces and -1,-1 if < K pairs
        public int[,] GetKBestPairs(ParseForestItem[] items1, ParseForestItem[] items2)
        {
            // in_ this case K = items1.Length

            var beenPushed = new bool[K,K];

            var result = new int[K,2];
            for (int i = 0; i < K; i++)
            {
                result[i, 0] = -1;
                result[i, 1] = -1;
            }

            if (items1 == null || items2 == null || items1[0] == null || items2[0] == null)
                return result;

            var heap = new BinaryHeap(K + 1);
            int n = 0;
            var vip = new ValueIndexPair(items1[0].Prob + items2[0].Prob, 0, 0);

            heap.Add(vip);
            beenPushed[0, 0] = true;

            while (n < K)
            {
                vip = heap.RemoveMax();

                if (vip.Val == double.NegativeInfinity)
                    break;

                result[n, 0] = vip.I1;
                result[n, 1] = vip.I2;

                n++;
                if (n >= K)
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

    internal class ValueIndexPair
    {
        public int I1, I2;
        public double Val;

        public ValueIndexPair(double val, int i1, int i2)
        {
            Val = val;
            I1 = i1;
            I2 = i2;
        }

        public int CompareTo(ValueIndexPair other)
        {
            if (Val < other.Val)
                return -1;
            if (Val > other.Val)
                return 1;
            return 0;
        }
    }

    // Max Heap
    // We know that never more than K elements on Heap
    internal class BinaryHeap
    {
        private int _currentSize;
        private readonly int _defaultCapacity;
        private readonly ValueIndexPair[] _theArray;

        public BinaryHeap(int defCap)
        {
            _defaultCapacity = defCap;
            _theArray = new ValueIndexPair[_defaultCapacity + 1];
            // _theArray[0] serves as dummy Parent for root (who is at 1) 
            // "largest" is guaranteed to be larger than all keys in_ heap
            _theArray[0] = new ValueIndexPair(double.PositiveInfinity, -1, -1);
            _currentSize = 0;
        }

        public ValueIndexPair GetMax()
        {
            return _theArray[1];
        }

        private static int Parent(int i)
        {
            return i/2;
        }

        private static int LeftChild(int i)
        {
            return 2*i;
        }

        private static int RightChild(int i)
        {
            return 2*i + 1;
        }

        public void Add(ValueIndexPair e)
        {
            // bubble up: 
            int where = _currentSize + 1; // new last place 
            while (e.CompareTo(_theArray[Parent(where)]) > 0)
            {
                _theArray[where] = _theArray[Parent(where)];
                where = Parent(where);
            }
            _theArray[where] = e;
            _currentSize++;
        }

        public ValueIndexPair RemoveMax()
        {
            ValueIndexPair min = _theArray[1];
            _theArray[1] = _theArray[_currentSize];
            _currentSize--;
            bool switched = true;
            // bubble down
            for (int parent = 1; switched && parent < _currentSize;)
            {
                switched = false;
                int mLeftChild = LeftChild(parent);
                int mRightChild = RightChild(parent);

                if (mLeftChild <= _currentSize)
                {
                    // if there is a Right child, see if we should bubble down there
                    int largerChild = mLeftChild;
                    if ((mRightChild <= _currentSize) &&
                        (_theArray[mRightChild].CompareTo(_theArray[mLeftChild])) > 0)
                    {
                        largerChild = mRightChild;
                    }
                    if (_theArray[largerChild].CompareTo(_theArray[parent]) > 0)
                    {
                        ValueIndexPair temp = _theArray[largerChild];
                        _theArray[largerChild] = _theArray[parent];
                        _theArray[parent] = temp;
                        parent = largerChild;
                        switched = true;
                    }
                }
            }
            return min;
        }
    }
}