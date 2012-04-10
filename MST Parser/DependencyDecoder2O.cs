using MSTParser;

namespace MSTParser
{
    public class DependencyDecoder2O : DependencyDecoder
    {
        public DependencyDecoder2O(DependencyPipe pipe) : base(pipe)
        {
        }

        private void Rearrange(double[,,] probs, double[,,] probsTrips, 
            double[,,] probsSibs, double[,,,] ntProbs, 
            int[] par, int[] labs)
        {
            int[,] staticTypes = null;
            if (m_pipe.Labeled)
            {
                staticTypes = GetTypes(ntProbs, par.Length);
            }

            bool[,] isChild = calcChilds(par);
            bool[,] isCross = null;

            while (true)
            {
                int wh = -1;
                int nPar = -1;
                int nType = -1;
                double max = double.NegativeInfinity;
                var aSibs = new int[par.Length,par.Length];
                var bSibs = new int[par.Length,par.Length];
                for (int i = 1; i < par.Length; i++)
                {
                    for (int j = 0; j < par.Length; j++)
                    {
                        int oP = par[i];
                        par[i] = j;
                        int[] sibs = getSibs(i, par);
                        aSibs[i, j] = sibs[0];
                        bSibs[i, j] = sibs[1];
                        par[i] = oP;
                    }
                }
                for (int ch = 1; ch < par.Length; ch++)
                {
                    // Calculate change of removing edge
                    int aSib = aSibs[ch, par[ch]];
                    int bSib = bSibs[ch, par[ch]];
                    bool lDir = ch < par[ch];
                    double change = 0.0 - probs[lDir ? ch : par[ch], lDir ? par[ch] : ch, lDir ? 1 : 0]
                                    - probsTrips[par[ch], aSib, ch] - probsSibs[aSib, ch, aSib == par[ch] ? 0 : 1]
                                    - (bSib != ch ? probsTrips[par[ch], ch, bSib] + probsSibs[ch, bSib, 1] : 0.0)
                                    -
                                    (m_pipe.Labeled
                                         ? (ntProbs[ch, labs[ch], lDir ? 1 : 0, 0] +
                                            ntProbs[par[ch], labs[ch], lDir ? 1 : 0, 1])
                                         : 0.0)
                                    +
                                    (bSib != ch
                                         ? probsTrips[par[ch], aSib, bSib] +
                                           probsSibs[aSib, bSib, aSib == par[ch] ? 0 : 1]
                                         : 0.0);
                    for (int pa = 0; pa < par.Length; pa++)
                    {
                        if (ch == pa || pa == par[ch] || isChild[ch, pa]) continue;
                        aSib = aSibs[ch, pa];
                        bSib = bSibs[ch, pa];
                        bool lDir1 = ch < pa;
                        double change1 = 0.0 + probs[lDir1 ? ch : pa, lDir1 ? pa : ch, lDir1 ? 1 : 0]
                                         + probsTrips[pa, aSib, ch] + probsSibs[aSib, ch, aSib == pa ? 0 : 1]
                                         + (bSib != ch ? probsTrips[pa, ch, bSib] + probsSibs[ch, bSib, 1] : 0.0)
                                         +
                                         (m_pipe.Labeled
                                              ? (ntProbs[ch, staticTypes[pa, ch], lDir1 ? 1 : 0, 0] +
                                                 ntProbs[pa, staticTypes[pa, ch], lDir1 ? 1 : 0, 1])
                                              : 0.0)
                                         -
                                         (bSib != ch
                                              ? probsTrips[pa, aSib, bSib] + probsSibs[aSib, bSib, aSib == pa ? 0 : 1]
                                              : 0.0);
                        if (max < change + change1)
                        {
                            max = change + change1;
                            wh = ch;
                            nPar = pa;
                            nType = m_pipe.Labeled ? staticTypes[pa, ch] : 0;
                        }
                    }
                }
                if (max <= 0.0)
                    break;
                par[wh] = nPar;
                labs[wh] = nType;
                isChild = calcChilds(par);
                //Console.WriteLine(Max + " " + wh + " " + nPar + " " + nType);
            }
        }

        // same as decode, except return K best
        public object[,] DecodeNonProjective(DependencyInstance inst,
                                             FeatureVector[,,] fvs,
                                             double[,,] probs,
                                             FeatureVector[,,] fvsTrips,
                                             double[,,] probsTrips,
                                             FeatureVector[,,] fvsSibs,
                                             double[,,] probsSibs,
                                             FeatureVector[,,,] ntFvs,
                                             double[,,,] ntProbs, int K)
        {
            string[] toks = inst.Sentence;
            string[] pos = inst.POS;

            object[,] orig = DecodeProjective(inst, fvs, probs, fvsTrips, probsTrips, fvsSibs, probsSibs, ntFvs,
                                              ntProbs, 1);
            string[] o = ((string) orig[0, 1]).Split(' ');
            var par = new int[o.Length + 1];
            var labs = new int[o.Length + 1];
            labs[0] = 0;
            par[0] = -1;
            for (int i = 1; i < par.Length; i++)
            {
                par[i] = int.Parse(o[i - 1].Split("\\|".ToCharArray())[0]);
                labs[i] = m_pipe.Labeled ? int.Parse(o[i - 1].Split(':')[1]) : 0;
            }

            Rearrange(probs, probsTrips, probsSibs, ntProbs, par, labs);

            string pars = "";
            for (int i = 1; i < par.Length; i++)
                pars += par[i] + "|" + i + ":" + labs[i] + " ";

            orig[0, 0] = ((DependencyPipe2O) m_pipe).CreateFeatureVector(toks, pos, labs, par);
            orig[0, 1] = pars.Trim();


            return orig;
        }

        private bool[,] calcChilds(int[] par)
        {
            var isChild = new bool[par.Length,par.Length];

            for (int i = 1; i < par.Length; i++)
            {
                int l = par[i];
                while (l != -1)
                {
                    isChild[l, i] = true;
                    l = par[l];
                }
            }
            return isChild;
        }

        private int[] getSibs(int ch, int[] par)
        {
            int aSib = par[ch];
            if (par[ch] > ch)
                for (int i = ch + 1; i < par[ch]; i++)
                {
                    if (par[i] == par[ch])
                    {
                        aSib = i;
                        break;
                    }
                }
            else
                for (int i = ch - 1; i > par[ch]; i--)
                {
                    if (par[i] == par[ch])
                    {
                        aSib = i;
                        break;
                    }
                }
            int bSib = ch;
            if (par[ch] < ch)
                for (int i = ch + 1; i < par.Length; i++)
                {
                    if (par[i] == par[ch])
                    {
                        bSib = i;
                        break;
                    }
                }
            else
                for (int i = ch - 1; i >= 0; i--)
                {
                    if (par[i] == par[ch])
                    {
                        bSib = i;
                        break;
                    }
                }
            return new[] {aSib, bSib};
        }

        // same as decode, except return K best
        public object[,] DecodeProjective(DependencyInstance inst,
                                          FeatureVector[,,] fvs,
                                          double[,,] probs,
                                          FeatureVector[,,] fvsTrips,
                                          double[,,] probsTrips,
                                          FeatureVector[,,] fvsSibs,
                                          double[,,] probsSibs,
                                          FeatureVector[,,,] ntFvs,
                                          double[,,,] ntProbs, int K)
        {
            string[] toks = inst.Sentence;
            string[] pos = inst.POS;

            int[,] staticTypes = null;
            if (m_pipe.Labeled)
            {
                staticTypes = GetTypes(ntProbs, toks.Length);
            }

            var pf = new KBestParseForest2O(0, toks.Length - 1, inst, K);

            for (int s = 0; s < toks.Length; s++)
            {
                pf.Add(s, -1, 0, 0.0, new FeatureVector());
                pf.Add(s, -1, 1, 0.0, new FeatureVector());
            }

            for (int j = 1; j < toks.Length; j++)
            {
                for (int s = 0; s < toks.Length && s + j < toks.Length; s++)
                {
                    int t = s + j;

                    FeatureVector prodFvSt = fvs[s, t, 0];
                    FeatureVector prodFvTs = fvs[s, t, 1];
                    double prodProbSt = probs[s, t, 0];
                    double prodProbTs = probs[s, t, 1];

                    int type1 = m_pipe.Labeled ? staticTypes[s, t] : 0;
                    int type2 = m_pipe.Labeled ? staticTypes[t, s] : 0;

                    FeatureVector ntFvS01 = ntFvs[s, type1, 0, 1];
                    FeatureVector ntFvS10 = ntFvs[s, type2, 1, 0];
                    FeatureVector ntFvT00 = ntFvs[t, type1, 0, 0];
                    FeatureVector ntFvT11 = ntFvs[t, type2, 1, 1];
                    double ntProbS01 = ntProbs[s, type1, 0, 1];
                    double ntProbS10 = ntProbs[s, type2, 1, 0];
                    double ntProbT00 = ntProbs[t, type1, 0, 0];
                    double ntProbT11 = ntProbs[t, type2, 1, 1];
                    double prodProb = 0.0;

                    if (true)
                    {
                        // case when R == S
                        ParseForestItem[] b1 = pf.GetItems(s, s, 0, 0);
                        ParseForestItem[] c1 = pf.GetItems(s + 1, t, 1, 0);
                        if (!(b1 == null || c1 == null))
                        {
                            FeatureVector prodFvSst = pf.Cat(fvsTrips[s, s, t], fvsSibs[s, t, 0]);
                            double prodProbSst = probsTrips[s, s, t] + probsSibs[s, t, 0];

                            int[,] pairs = pf.GetKBestPairs(b1, c1);

                            for (int k = 0; k < K; k++)
                            {
                                if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                    break;

                                int comp1 = pairs[k, 0];
                                int comp2 = pairs[k, 1];

                                double bc = b1[comp1].Prob + c1[comp2].Prob;

                                // create sibling pair
                                // create parent pair: S->T and S->(start,T)
                                bc += prodProbSt + prodProbSst;

                                FeatureVector fvFin = pf.Cat(prodFvSt, prodFvSst);
                                if (m_pipe.Labeled)
                                {
                                    bc += ntProbS01 + ntProbT00;
                                    fvFin = FeatureVector.Cat(ntFvS01, FeatureVector.Cat(ntFvT00, fvFin));
                                }

                                pf.Add(s, s, t, type1, 0, 1, bc, fvFin, b1[comp1], c1[comp2]);
                            }
                        }

                        // case when R == T
                        b1 = pf.GetItems(s, t - 1, 0, 0);
                        c1 = pf.GetItems(t, t, 1, 0);
                        if (!(b1 == null || c1 == null))
                        {
                            FeatureVector prodFvStt = pf.Cat(fvsTrips[t, t, s], fvsSibs[t, s, 0]);
                            double prodProbStt = probsTrips[t, t, s] + probsSibs[t, s, 0];

                            int[,] pairs = pf.GetKBestPairs(b1, c1);

                            for (int k = 0; k < K; k++)
                            {
                                if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                    break;

                                int comp1 = pairs[k, 0];
                                int comp2 = pairs[k, 1];

                                double bc = b1[comp1].Prob + c1[comp2].Prob;

                                // create sibling pair
                                // create parent pair: S->T and S->(start,T)
                                bc += prodProbTs + prodProbStt;

                                FeatureVector fvFin = pf.Cat(prodFvTs, prodFvStt);
                                if (m_pipe.Labeled)
                                {
                                    bc += ntProbT11 + ntProbS10;
                                    fvFin = FeatureVector.Cat(ntFvT11, FeatureVector.Cat(ntFvS10, fvFin));
                                }

                                pf.Add(s, t, t, type2, 1, 1, bc, fvFin, b1[comp1], c1[comp2]);
                            }
                        }
                    }

                    for (int r = s; r < t; r++)
                    {
                        // First case - create sibling
                        ParseForestItem[] b1 = pf.GetItems(s, r, 0, 0);
                        ParseForestItem[] c1 = pf.GetItems(r + 1, t, 1, 0);

                        if (!(b1 == null || c1 == null))
                        {
                            int[,] pairs = pf.GetKBestPairs(b1, c1);

                            for (int k = 0; k < K; k++)
                            {
                                if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                    break;

                                int comp1 = pairs[k, 0];
                                int comp2 = pairs[k, 1];

                                double bc = b1[comp1].Prob + c1[comp2].Prob;

                                pf.Add(s, r, t, -1, 0, 2, bc, new FeatureVector(), b1[comp1], c1[comp2]);
                                pf.Add(s, r, t, -1, 1, 2, bc, new FeatureVector(), b1[comp1], c1[comp2]);
                            }
                        }
                    }

                    for (int r = s + 1; r < t; r++)
                    {
                        // S -> (R,T)
                        ParseForestItem[] b1 = pf.GetItems(s, r, 0, 1);
                        ParseForestItem[] c1 = pf.GetItems(r, t, 0, 2);

                        if (!(b1 == null || c1 == null))
                        {
                            int[,] pairs = pf.GetKBestPairs(b1, c1);

                            for (int k = 0; k < K; k++)
                            {
                                if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                    break;

                                int comp1 = pairs[k, 0];
                                int comp2 = pairs[k, 1];

                                double bc = b1[comp1].Prob + c1[comp2].Prob;

                                bc += prodProbSt + probsTrips[s, r, t] + probsSibs[r, t, 1];
                                FeatureVector fv_fin = pf.Cat(prodFvSt, pf.Cat(fvsTrips[s, r, t], fvsSibs[r, t, 1]));

                                if (m_pipe.Labeled)
                                {
                                    bc += ntProbS01 + ntProbT00;
                                    fv_fin = FeatureVector.Cat(ntFvS01, FeatureVector.Cat(ntFvT00, fv_fin));
                                }

                                pf.Add(s, r, t, type1, 0, 1, bc, fv_fin, b1[comp1], c1[comp2]);
                            }
                        }

                        // T -> (R,S)
                        b1 = pf.GetItems(s, r, 1, 2);
                        c1 = pf.GetItems(r, t, 1, 1);

                        if (!(b1 == null || c1 == null))
                        {
                            int[,] pairs = pf.GetKBestPairs(b1, c1);

                            for (int k = 0; k < K; k++)
                            {
                                if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                    break;

                                int comp1 = pairs[k, 0];
                                int comp2 = pairs[k, 1];

                                double bc = b1[comp1].Prob + c1[comp2].Prob;

                                bc += prodProbTs + probsTrips[t, r, s] + probsSibs[r, s, 1];

                                FeatureVector fvFin = pf.Cat(prodFvTs, pf.Cat(fvsTrips[t, r, s], fvsSibs[r, s, 1]));
                                if (m_pipe.Labeled)
                                {
                                    bc += ntProbT11 + ntProbS10;
                                    fvFin = FeatureVector.Cat(ntFvT11, FeatureVector.Cat(ntFvS10, fvFin));
                                }

                                pf.Add(s, r, t, type2, 1, 1, bc, fvFin, b1[comp1], c1[comp2]);
                            }
                        }
                    }


                    // Finish off pieces incom + Comp -> Comp
                    for (int r = s; r <= t; r++)
                    {
                        if (r != s)
                        {
                            ParseForestItem[] b1 = pf.GetItems(s, r, 0, 1);
                            ParseForestItem[] c1 = pf.GetItems(r, t, 0, 0);

                            if (!(b1 == null || c1 == null))
                            {
                                //continue;

                                int[,] pairs = pf.GetKBestPairs(b1, c1);
                                for (int k = 0; k < K; k++)
                                {
                                    if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                        break;

                                    int comp1 = pairs[k, 0];
                                    int comp2 = pairs[k, 1];

                                    double bc = b1[comp1].Prob + c1[comp2].Prob;

                                    if (
                                        !pf.Add(s, r, t, -1, 0, 0, bc, new FeatureVector(), b1[comp1],
                                                c1[comp2]))
                                        break;
                                }
                            }
                        }

                        if (r != t)
                        {
                            ParseForestItem[] b1 = pf.GetItems(s, r, 1, 0);
                            ParseForestItem[] c1 = pf.GetItems(r, t, 1, 1);

                            if (!(b1 == null || c1 == null))
                            {
                                //continue;

                                int[,] pairs = pf.GetKBestPairs(b1, c1);
                                for (int k = 0; k < K; k++)
                                {
                                    if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                        break;

                                    int comp1 = pairs[k, 0];
                                    int comp2 = pairs[k, 1];

                                    double bc = b1[comp1].Prob + c1[comp2].Prob;

                                    if (
                                        !pf.Add(s, r, t, -1, 1, 0, bc, new FeatureVector(), b1[comp1],
                                                c1[comp2]))
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return pf.GetBestParses();
        }
    }
}