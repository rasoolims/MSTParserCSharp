using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MSTParser;

namespace MSTParser
{
    public class DependencyDecoder
    {
        protected DependencyPipe m_pipe; 

        public DependencyDecoder(DependencyPipe pipe)
        {
            this.m_pipe = pipe;
        }

        protected int[,] GetTypes(double[,,,] ntProbs, int len)
        {
            var staticTypes = new int[len,len];
            for (int i = 0; i < len; i++)
            {
                for (int j = 0; j < len; j++)
                {
                    if (i == j)
                    {
                        staticTypes[i, j] = 0;
                        continue;
                    }
                    int wh = -1;
                    double best = double.NegativeInfinity;
                    for (int t = 0; t < m_pipe.Types.Length; t++)
                    {
                        double score;
                        if (i < j)
                            score = ntProbs[i, t, 0, 1] + ntProbs[j, t, 0, 0];
                        else
                            score = ntProbs[i, t, 1, 1] + ntProbs[j, t, 1, 0];

                        if (score > best)
                        {
                            wh = t;
                            best = score;
                        }
                    }
                    staticTypes[i, j] = wh;
                }
            }
            return staticTypes;
        }

        // static Type for each edge: run time O(n^3 + Tn^2) T is number of Types
        public object[,] DecodeProjective(DependencyInstance inst,
                                          FeatureVector[,,] fvs,
                                          double[,,] probs,
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

            var pf = new KBestParseForest(0, toks.Length - 1, inst, K);

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

                    for (int r = s; r <= t; r++)
                    {
                        if (r != t)
                        {
                            ParseForestItem[] b1 = pf.GetItems(s, r, 0, 0);
                            ParseForestItem[] c1 = pf.GetItems(r + 1, t, 1, 0);

                            if (b1 != null && c1 != null)
                            {
                                int[,] pairs = pf.GetKBestPairs(b1, c1);
                                for (int k = 0; k < pairs.GetLength(0); k++)
                                {
                                    if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                        break;

                                    int comp1 = pairs[k, 0];
                                    int comp2 = pairs[k, 1];

                                    double bc = b1[comp1].Prob + c1[comp2].Prob;

                                    double probFin = bc + prodProbSt;
                                    FeatureVector fv_fin = prodFvSt;
                                    if (m_pipe.Labeled)
                                    {
                                        fv_fin = FeatureVector.Cat(ntFvS01, FeatureVector.Cat(ntFvT00, fv_fin));
                                        probFin += ntProbS01 + ntProbT00;
                                    }
                                    pf.Add(s, r, t, type1, 0, 1, probFin, fv_fin, b1[comp1], c1[comp2]);

                                    probFin = bc + prodProbTs;
                                    fv_fin = prodFvTs;
                                    if (m_pipe.Labeled)
                                    {
                                        fv_fin = FeatureVector.Cat(ntFvT11, FeatureVector.Cat(ntFvS10, fv_fin));
                                        probFin += ntProbT11 + ntProbS10;
                                    }
                                    pf.Add(s, r, t, type2, 1, 1, probFin, fv_fin, b1[comp1], c1[comp2]);
                                }
                            }
                        }
                    }


                    for (int r = s; r <= t; r++)
                    {
                        if (r != s)
                        {
                            ParseForestItem[] b1 = pf.GetItems(s, r, 0, 1);
                            ParseForestItem[] c1 = pf.GetItems(r, t, 0, 0);
                            if (b1 != null && c1 != null)
                            {
                                int[,] pairs = pf.GetKBestPairs(b1, c1);
                                for (int k = 0; k < pairs.GetLength(0); k++)
                                {
                                    if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                        break;

                                    int comp1 = pairs[k, 0];
                                    int comp2 = pairs[k, 1];

                                    double bc = b1[comp1].Prob + c1[comp2].Prob;

                                    if (!pf.Add(s, r, t, -1, 0, 0, bc,
                                                new FeatureVector(),
                                                b1[comp1], c1[comp2]))
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        if (r != t)
                        {
                            ParseForestItem[] b1 = pf.GetItems(s, r, 1, 0);
                            ParseForestItem[] c1 = pf.GetItems(r, t, 1, 1);
                            if (b1 != null && c1 != null)
                            {
                                int[,] pairs = pf.GetKBestPairs(b1, c1);
                                for (int k = 0; k < pairs.GetLength(0); k++)
                                {
                                    if (pairs[k, 0] == -1 || pairs[k, 1] == -1)
                                        break;

                                    int comp1 = pairs[k, 0];
                                    int comp2 = pairs[k, 1];

                                    double bc = b1[comp1].Prob + c1[comp2].Prob;

                                    if (!pf.Add(s, r, t, -1, 1, 0, bc,
                                                new FeatureVector(), b1[comp1], c1[comp2]))
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return pf.GetBestParses();
        }

        public object[,] decodeNonProjective(DependencyInstance inst,
                                             FeatureVector[,,] fvs,
                                             double[,,] probs,
                                             FeatureVector[,,,] nt_fvs,
                                             double[,,,] nt_probs, int K)
        {
            string[] pos = inst.POS;

            int numWords = inst.Sentence.Length;
            var oldI = new int[numWords,numWords];
            var oldO = new int[numWords,numWords];
            var scoreMatrix = new double[numWords,numWords];
            var orig_scoreMatrix = new double[numWords,numWords];
            var curr_nodes = new bool[numWords];
            var reps = new Dictionary<int, int>[numWords];

            int[,] static_types = null;
            if (m_pipe.Labeled)
            {
                static_types = GetTypes(nt_probs, pos.Length);
            }

            for (int i = 0; i < numWords; i++)
            {
                curr_nodes[i] = true;
                reps[i] = new Dictionary<int, int>();
                reps[i].Add(i, 0);
                for (int j = 0; j < numWords; j++)
                {
                    // score of edge (i,j) i --> j
                    scoreMatrix[i, j] = probs[i < j ? i : j, i < j ? j : i, i < j ? 0 : 1]
                                        + (m_pipe.Labeled
                                               ? nt_probs[i, static_types[i, j], i < j ? 0 : 1, 1]
                                                 + nt_probs[j, static_types[i, j], i < j ? 0 : 1, 0]
                                               : 0.0);
                    orig_scoreMatrix[i, j] = probs[i < j ? i : j, i < j ? j : i, i < j ? 0 : 1]
                                             + (m_pipe.Labeled
                                                    ? nt_probs[i, static_types[i, j], i < j ? 0 : 1, 1]
                                                      + nt_probs[j, static_types[i, j], i < j ? 0 : 1, 0]
                                                    : 0.0);
                    oldI[i, j] = i;
                    oldO[i, j] = j;

                    if (i == j || j == 0) 
                        continue; // no self loops of i --> 0
                }
            }

            Dictionary<int, int> final_edges = chuLiuEdmonds(scoreMatrix, curr_nodes, oldI, oldO, false,
                                                             new Dictionary<int, int>(), reps);
            var par = new int[numWords];
            int[] ns = final_edges.Keys.ToArray();
            for (int i = 0; i < ns.Length; i++)
            {
                int ch = ns[i];
                int pr = final_edges[ns[i]];
                par[ch] = pr;
            }

            int[] n_par = getKChanges(par, orig_scoreMatrix, Math.Min(K, par.Length));
            int new_k = 1;
            for (int i = 0; i < n_par.Length; i++)
                if (n_par[i] > -1) new_k++;

            // Create Feature Vectors;
            var fin_par = new int[new_k,numWords];
            int fin_parFirstLen = new_k;
            int fin_par_secondLen = numWords;
            var fin_fv = new FeatureVector[new_k,numWords];
            int len = fin_par.GetLength(1);
            for (int i = 0; i < len; i++)
            {
                fin_par[0, i] = par[i];
            }
            int c = 1;
            for (int i = 0; i < n_par.Length; i++)
            {
                if (n_par[i] > -1)
                {
                    var t_par = new int[par.Length];
                    for (int j = 0; j < t_par.Length; j++)
                        t_par[j] = par[j];
                    t_par[i] = n_par[i];
                    len = t_par.Length;
                    for (int ct = 0; ct < len; ct++)
                    {
                        fin_par[c, ct] = t_par[ct];
                    }
                    c++;
                }
            }
            for (int k = 0; k < fin_parFirstLen; k++)
            {
                for (int i = 0; i < fin_par_secondLen; i++)
                {
                    int ch = i;
                    int pr = fin_par[k, i];
                    if (pr != -1)
                    {
                        fin_fv[k, ch] = fvs[ch < pr ? ch : pr, ch < pr ? pr : ch, ch < pr ? 1 : 0];
                        if (m_pipe.Labeled)
                        {
                            fin_fv[k, ch] = FeatureVector.Cat(fin_fv[k, ch],
                                      nt_fvs[ch, static_types[pr, ch], ch < pr ? 1 : 0, 0]);
                            fin_fv[k, ch] = FeatureVector.Cat(fin_fv[k, ch],
                                      nt_fvs[pr, static_types[pr, ch], ch < pr ? 1 : 0, 1]);
                        }
                    }
                    else
                    {
                        fin_fv[k, ch] = new FeatureVector();
                    }
                }
            }


            var fin = new FeatureVector[new_k];
            var result = new string[new_k];
            for (int k = 0; k < fin.Length; k++)
            {
                fin[k] = new FeatureVector();
                for (int i = 1; i < fin_fv.GetLength(k); i++) //doubt of Index
                    fin[k] = FeatureVector.Cat(fin_fv[k, i], fin[k]);
                result[k] = "";
                for (int i = 1; i < par.Length; i++)
                    result[k] += fin_par[k, i] + "|" + i + (m_pipe.Labeled ? ":" + static_types[fin_par[k, i], i] : ":0") + " ";
            }

            // create d.
            var d = new object[new_k,2];

            for (int k = 0; k < new_k; k++)
            {
                d[k, 0] = fin[k];
                d[k, 1] = result[k].Trim();
            }

            return d;
        }

        private int[] getKChanges(int[] par, double[,] scoreMatrix, int K)
        {
            var result = new int[par.Length];
            var n_par = new int[par.Length];
            var n_score = new double[par.Length];
            for (int i = 0; i < par.Length; i++)
            {
                result[i] = -1;
                n_par[i] = -1;
                n_score[i] = double.NegativeInfinity;
            }

            bool[,] isChild = calcChilds(par);

            for (int i = 1; i < n_par.Length; i++)
            {
                double Max = double.NegativeInfinity;
                int wh = -1;
                for (int j = 0; j < n_par.Length; j++)
                {
                    if (i == j || par[i] == j || isChild[i, j]) 
                        continue;

                    if (scoreMatrix[j, i] > Max)
                    {
                        Max = scoreMatrix[j, i];
                        wh = j;
                    }
                }
                n_par[i] = wh;
                n_score[i] = Max;
            }

            for (int k = 0; k < K; k++)
            {
                double Max = double.NegativeInfinity;
                int wh = -1;
                int whI = -1;
                for (int i = 0; i < n_par.Length; i++)
                {
                    if (n_par[i] == -1) continue;
                    double score = scoreMatrix[n_par[i], i];
                    if (score > Max)
                    {
                        Max = score;
                        whI = i;
                        wh = n_par[i];
                    }
                }

                if (Max == double.NegativeInfinity)
                    break;
                result[whI] = wh;
                n_par[whI] = -1;
            }

            return result;
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

        private static Dictionary<int, int> chuLiuEdmonds(double[,] scoreMatrix, bool[] curr_nodes,
                                                          int[,] oldI, int[,] oldO, bool Write,
                                                          Dictionary<int, int> final_edges, Dictionary<int, int>[] reps)
        {
            // need to construct for each node list of nodes they represent (here only!)

            var par = new int[curr_nodes.Length];
            int numWords = curr_nodes.Length;

            // create best graph
            par[0] = -1;
            for (int i = 1; i < par.Length; i++)
            {
                // only interested in_ current nodes
                if (!curr_nodes[i]) continue;
                double maxScore = scoreMatrix[0, i];
                par[i] = 0;
                for (int j = 0; j < par.Length; j++)
                {
                    if (j == i) continue;
                    if (!curr_nodes[j]) continue;
                    double newScore = scoreMatrix[j, i];
                    if (newScore > maxScore)
                    {
                        maxScore = newScore;
                        par[i] = j;
                    }
                }
            }

            if (Write)
            {
                Console.WriteLine("After init");
                for (int i = 0; i < par.Length; i++)
                {
                    if (curr_nodes[i])
                        Console.Write(par[i] + "|" + i + " ");
                }
                Console.WriteLine();
            }

            //Find a cycle
            var cycles = new ArrayList();
            var added = new bool[numWords];
            for (int i = 0; i < numWords && cycles.Count == 0; i++)
            {
                // if I have already considered this or
                // This is not a valid node (i.e. has been contracted)
                if (added[i] || !curr_nodes[i]) continue;
                added[i] = true;
                var cycle = new Dictionary<int, int>();
                cycle.Add(i, 0);
                int l = i;
                while (true)
                {
                    if (par[l] == -1)
                    {
                        added[l] = true;
                        break;
                    }
                    if (cycle.ContainsKey(par[l]))
                    {
                        cycle = new Dictionary<int, int>();
                        int lorg = par[l];
                        cycle.Add(lorg, par[lorg]);
                        added[lorg] = true;
                        int l1 = par[lorg];
                        while (l1 != lorg)
                        {
                            cycle.Add(l1, par[l1]);
                            added[l1] = true;
                            l1 = par[l1];
                        }
                        cycles.Add(cycle);
                        break;
                    }

                    if (cycle.ContainsKey(l)) //Added by MSR
                        cycle[l] = 0;
                    else
                    cycle.Add(l, 0);
                    l = par[l];
                    if (added[l] && !cycle.ContainsKey(l))
                        break;
                    added[l] = true;
                }
            }

            // get all edges and return them
            if (cycles.Count == 0)
            {
                //Console.WriteLine("TREE:");
                for (int i = 0; i < par.Length; i++)
                {
                    if (!curr_nodes[i]) continue;
                    if (par[i] != -1)
                    {
                        int pr = oldI[par[i], i];
                        int ch = oldO[par[i], i];
                        final_edges.Add(ch, pr);
                        //Console.Write(pr+"|"+ch + " ");
                    }
                    else
                        final_edges.Add(0, -1);
                }
                //Console.WriteLine();
                return final_edges;
            }

            int max_cyc = 0;
            int wh_cyc = 0;
            for (int i = 0; i < cycles.Count; i++)
            {
                var cycle_ = (Dictionary<int, int>) cycles[i];
                if (cycle_.Count > max_cyc)
                {
                    max_cyc = cycle_.Count;
                    wh_cyc = i;
                }
            }

            var m_cycle = (Dictionary<int, int>) cycles[wh_cyc];
            int[] cyc_nodes = m_cycle.Keys.ToArray();
            int rep = cyc_nodes[0];

            if (Write)
            {
                Console.WriteLine("Found Cycle");
                for (int i = 0; i < cyc_nodes.Length; i++)
                    Console.Write(cyc_nodes[i] + " ");
                Console.WriteLine();
            }

            double cyc_weight = 0.0;
            for (int j = 0; j < cyc_nodes.Length; j++)
            {
                cyc_weight += scoreMatrix[par[cyc_nodes[j]], cyc_nodes[j]];
            }


            for (int i = 0; i < numWords; i++)
            {
                if (!curr_nodes[i] || m_cycle.ContainsKey(i)) 
                    continue;

                double max1 = double.NegativeInfinity;
                int wh1 = -1;
                double max2 = double.NegativeInfinity;
                int wh2 = -1;

                for (int j = 0; j < cyc_nodes.Length; j++)
                {
                    int j1 = cyc_nodes[j];

                    if (scoreMatrix[j1, i] > max1)
                    {
                        max1 = scoreMatrix[j1, i];
                        wh1 = j1; //oldI[j1,i];
                    }

                    // cycle weight + new edge - removal of old
                    double scr = cyc_weight + scoreMatrix[i, j1] - scoreMatrix[par[j1], j1];
                    if (scr > max2)
                    {
                        max2 = scr;
                        wh2 = j1; //oldO[i,j1];
                    }
                }

                scoreMatrix[rep, i] = max1;
                oldI[rep, i] = oldI[wh1, i]; //wh1;
                oldO[rep, i] = oldO[wh1, i]; //oldO[wh1,i];
                scoreMatrix[i, rep] = max2;
                oldO[i, rep] = oldO[i, wh2]; //wh2;
                oldI[i, rep] = oldI[i, wh2]; //oldI[i,wh2];
            }

            var rep_cons = new Dictionary<int, int>[cyc_nodes.Length];
            for (int i = 0; i < cyc_nodes.Length; i++)
            {
                rep_cons[i] = new Dictionary<int, int>();
                int[] keys = reps[cyc_nodes[i]].Keys.ToArray();
                Array.Sort(keys);
                if (Write) Console.Write(cyc_nodes[i] + ": ");
                for (int j = 0; j < keys.Length; j++)
                {
                    rep_cons[i].Add(keys[j], 0);
                    if (Write) Console.Write(keys[j] + " ");
                }
                if (Write) Console.WriteLine();
            }

            // don'T consider not representative nodes
            // these nodes have been folded
            for (int i = 1; i < cyc_nodes.Length; i++)
            {
                curr_nodes[cyc_nodes[i]] = false;
                int[] keys = reps[cyc_nodes[i]].Keys.ToArray();
                for (int j = 0; j < keys.Length; j++)
                    reps[rep].Add(keys[j], 0);
            }

            chuLiuEdmonds(scoreMatrix, curr_nodes, oldI, oldO, Write, final_edges, reps);

            // check each node in_ cycle, if one of its representatives
            // is a key in_ the final_edges, it is the one.
            int wh = -1;
            bool found = false;
            for (int i = 0; i < rep_cons.Length && !found; i++)
            {
                int[] keys = rep_cons[i].Keys.ToArray();
                for (int j = 0; j < keys.Length && !found; j++)
                {
                    if (final_edges.ContainsKey(keys[j]))
                    {
                        wh = cyc_nodes[i];
                        found = true;
                    }
                }
            }

            int m_l = par[wh];
            while (m_l != wh)
            {
                int ch = oldO[par[m_l], m_l];
                int pr = oldI[par[m_l], m_l];
                final_edges.Add(ch, pr);
                m_l = par[m_l];
            }

            if (Write)
            {
                int[] keys = final_edges.Keys.ToArray();
                Array.Sort(keys);
                for (int i = 0; i < keys.Length; i++)
                    Console.Write(final_edges[keys[i]] + "|" + keys[i] + " ");
                Console.WriteLine();
            }

            return final_edges;
        }
    }
}