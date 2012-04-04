using System;
using MSTParser.Extensions;

namespace MSTParser
{
    public class Parameters
    {

        public LossTypes LossType = LossTypes.Punc;
        public double[] parameters;
        public double[] Total;

        public Parameters(int size)
        {
            parameters = new double[size];
            Total = new double[size];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = 0.0;
                Total[i] = 0.0;
            }
            LossType = LossTypes.Punc;
        }

        public void SetLoss(LossTypes lt)
        {
            LossType = lt;
        }

        public void AverageParams(double avVal)
        {
            for (int j = 0; j < Total.Length; j++)
                Total[j] *= 1.0/(avVal);
            parameters = Total;
        }

        public void UpdateParamsMIRA(DependencyInstance inst, object[,] d, double upd)
        {
            string actParseTree = inst.ActParseTree;
            FeatureVector actFV = inst.Fv;

            int K = 0;
            for (int i = 0; i < d.GetLength(0) && d[i, 0] != null; i++)
            {
                K = i + 1;
            }

            var b = new double[K];
            var lamDist = new double[K];
            var dist = new FeatureVector[K];

            for (int k = 0; k < K; k++)
            {
                lamDist[k] = GetScore(actFV)
                              - GetScore((FeatureVector) d[k, 0]);
                b[k] = NumErrors(inst, (string) d[k, 1], actParseTree);
                b[k] -= lamDist[k];
                dist[k] = FeatureVector.GetDistVector(actFV, (FeatureVector) d[k, 0]);
            }

            double[] alpha = hildreth(dist, b);

            FeatureVector fv = null;
            int res = 0;
            for (int k = 0; k < K; k++)
            {
                fv = dist[k];
                foreach (Feature feature in fv.FVector)
                {
                    if (feature.Index < 0)
                        continue;
                    parameters[feature.Index] += alpha[k]*feature.Value;
                    Total[feature.Index] += upd*alpha[k]*feature.Value;
                }
            }
        }

        public double GetScore(FeatureVector fv)
        {
            double score = 0.0;
            foreach (Feature feature in fv.FVector)
            {
                if (feature.Index >= 0)
                    score += parameters[feature.Index]*feature.Value;
            }
            return score;
        }

        private double[] hildreth(FeatureVector[] a, double[] b)
        {
            int i;
            const int maxIter = 10000;
            const double eps = 0.00000001;
            const double zero = 0.000000000001;

            var alpha = new double[b.Length];

            var F = new double[b.Length];
            var kkt = new double[b.Length];
            double maxKkt = double.NegativeInfinity;

            int K = a.Length;

            var A = new double[K][];
            for (int j = 0; j < A.Length; j++)
            {
                A[j] = new double[K];
            }
            var isComputed = new bool[K];
            for (i = 0; i < K; i++)
            {
                A[i][i] = FeatureVector.DotProduct(a[i], a[i]);
                isComputed[i] = false;
            }

            int maxKktI = -1;


            for (i = 0; i < F.Length; i++)
            {
                F[i] = b[i];
                kkt[i] = F[i];
                if (kkt[i] > maxKkt)
                {
                    maxKkt = kkt[i];
                    maxKktI = i;
                }
            }

            int iter = 0;
            double diff_alpha;
            double try_alpha;
            double add_alpha;

            while (maxKkt >= eps && iter < maxIter)
            {
                diff_alpha = A[maxKktI][maxKktI] <= zero ? 0.0 : F[maxKktI]/A[maxKktI][maxKktI];
                try_alpha = alpha[maxKktI] + diff_alpha;
                add_alpha = 0.0;

                if (try_alpha < 0.0)
                    add_alpha = -1.0*alpha[maxKktI];
                else
                    add_alpha = diff_alpha;

                alpha[maxKktI] = alpha[maxKktI] + add_alpha;

                if (!isComputed[maxKktI])
                {
                    for (i = 0; i < K; i++)
                    {
                        A[i][maxKktI] = FeatureVector.DotProduct(a[i], a[maxKktI]); // for version 1
                        isComputed[maxKktI] = true;
                    }
                }

                for (i = 0; i < F.Length; i++)
                {
                    F[i] -= add_alpha*A[i][maxKktI];
                    kkt[i] = F[i];
                    if (alpha[i] > zero)
                        kkt[i] = Math.Abs(F[i]);
                }

                maxKkt = double.NegativeInfinity;
                maxKktI = -1;
                for (i = 0; i < F.Length; i++)
                    if (kkt[i] > maxKkt)
                    {
                        maxKkt = kkt[i];
                        maxKktI = i;
                    }

                iter++;
            }

            return alpha;
        }


        public double NumErrors(DependencyInstance inst, string pred, string act)
        {
            if (LossType==LossTypes.NoPunc)
                return NumErrorsDepNoPunc(inst, pred, act) + NumErrorsLabelNoPunc(inst, pred, act);
            return NumErrorsDep(inst, pred, act) + NumErrorsLabel(inst, pred, act);
        }

        public double NumErrorsDep(DependencyInstance inst, string pred, string act)
        {
            string[] actSpans = act.Split(' ');
            string[] predSpans = pred.Split(' ');

            int correct = 0;

            for (int i = 0; i < predSpans.Length; i++)
            {
                string p = predSpans[i].Split(':')[0];
                string a = actSpans[i].Split(':')[0];
                if (p.Equals(a))
                {
                    correct++;
                }
            }

            return ((double) actSpans.Length - correct);
        }

        public double NumErrorsLabel(DependencyInstance inst, string pred, string act)
        {
            string[] actSpans = act.Split(' ');
            string[] predSpans = pred.Split(' ');

            int correct = 0;

            for (int i = 0; i < predSpans.Length; i++)
            {
                string p = predSpans[i].Split(':')[1];
                string a = actSpans[i].Split(':')[1];
                if (p.Equals(a))
                {
                    correct++;
                }
            }

            return ((double) actSpans.Length - correct);
        }

        public double NumErrorsDepNoPunc(DependencyInstance inst, string pred, string act)
        {
            string[] actSpans = act.Split(' ');
            string[] predSpans = pred.Split(' ');

            string[] pos = inst.POS;

            int correct = 0;
            int numPunc = 0;

            for (int i = 0; i < predSpans.Length; i++)
            {
                string p = predSpans[i].Split(':')[0];
                string a = actSpans[i].Split(':')[0];
                if (pos[i + 1].Matches(@"[,:\.'`]+"))
                {
                    numPunc++;
                    continue;
                }
                if (p.Equals(a))
                {
                    correct++;
                }
            }

            return ((double) actSpans.Length - numPunc - correct);
        }

        public double NumErrorsLabelNoPunc(DependencyInstance inst, string pred, string act)
        {
            string[] actSpans = act.Split(' ');
            string[] predSpans = pred.Split(' ');

            string[] pos = inst.POS;

            int correct = 0;
            int numPunc = 0;

            for (int i = 0; i < predSpans.Length; i++)
            {
                string p = predSpans[i].Split(':')[1];
                string a = actSpans[i].Split(':')[1];
                if (pos[i + 1].Matches("[,:.'`]+"))
                {
                    numPunc++;
                    continue;
                }
                if (p.Equals(a))
                {
                    correct++;
                }
            }

            return ((double) actSpans.Length - numPunc - correct);
        }
    }
}