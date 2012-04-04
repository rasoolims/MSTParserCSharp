namespace MSTParser
{
    public class ParseForestItem
    {
        public int Comp;
        public int Dir;
        public FeatureVector FV;
        public ParseForestItem Left;
        public int Length;
        public double Prob;
        public int R;
        public ParseForestItem Right;
        public int S;
        public int T;
        public int Type;

        // productions
        public ParseForestItem(int i, int k, int j, int type,
                               int dir, int comp,
                               double prob, FeatureVector fv,
                               ParseForestItem left, ParseForestItem right)
        {
            S = i;
            R = k;
            T = j;
            Dir = dir;
            Comp = comp;
            Type = type;
            Length = 6;

            Prob = prob;
            FV = fv;

            Left = left;
            Right = right;
        }

        // preproductions
        public ParseForestItem(int s, int type, int dir, double prob, FeatureVector fv)
        {
            S = s;
            Dir = dir;
            Type = type;
            Length = 2;

            Prob = prob;
            FV = fv;

            Left = null;
            Right = null;
        }

        public ParseForestItem()
        {
        }

        public void CopyValues(ParseForestItem p)
        {
            p.S = S;
            p.R = R;
            p.T = T;
            p.Dir = Dir;
            p.Comp = Comp;
            p.Prob = Prob;
            p.FV = FV;
            p.Length = Length;
            p.Left = Left;
            p.Right = Right;
            p.Type = Type;
        }

        // way forest works, only have to check rule and indeces
        // for equality.
        public bool Equals(ParseForestItem p)
        {
            return S == p.S && T == p.T && R == p.R
                   && Dir == p.Dir && Comp == p.Comp
                   && Type == p.Type;
        }

        public bool IsPre()
        {
            return Length == 2;
        }
    }
}