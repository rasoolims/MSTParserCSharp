using System.Collections.Generic;
using System.Linq;

namespace MSTParser
{
    public class FeatureVector
    {
        public LinkedList<Feature> FVector;

        public FeatureVector()
        {
            FVector = new LinkedList<Feature>();
        }

        public static FeatureVector Cat(FeatureVector fv1, FeatureVector fv2)
        {
            var fv = new FeatureVector {FVector = new LinkedList<Feature>(fv1.FVector)};
            foreach (Feature feature in fv2.FVector)
            {
                fv.FVector.AddFirst(feature);
            }
            return fv;
        }

        // fv1 - fv2
        public static FeatureVector GetDistVector(FeatureVector fv1, FeatureVector fv2)
        {
            var fv = new FeatureVector {FVector = new LinkedList<Feature>(fv1.FVector)};
            foreach (Feature feature in fv2.FVector)
            {
                fv.FVector.AddFirst(new Feature(feature.Index, -feature.Value));
            }
            return fv;
        }

        public static double DotProduct(FeatureVector fv1, FeatureVector fv2)
        {
            double result = 0.0;
            var hm1 = new Dictionary<int, double>();
            var hm2 = new Dictionary<int, double>();
            foreach (Feature feature in fv1.FVector)
            {
                if (feature.Index < 0)
                    continue;
                if (hm1.ContainsKey(feature.Index))
                {
                    hm1[feature.Index] = hm1[feature.Index] + feature.Value;
                }
                else
                {
                    hm1.Add(feature.Index, feature.Value);
                }
            }
            if (ReferenceEquals(fv1, fv2))
            {
                hm2 = hm1;
            }
            else
            {
                foreach (Feature feature in fv2.FVector)
                {
                    if (feature.Index < 0)
                        continue;
                    if (hm2.ContainsKey(feature.Index))
                    {
                        hm2[feature.Index] =hm1.ContainsKey(feature.Index)? hm1[feature.Index]:0 + feature.Value;
                    }
                    else
                    {
                        hm2.Add(feature.Index, feature.Value);
                    }
                }
            }


            int[] keys = hm1.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                double v1 = hm1[keys[i]];
                double v2 =hm2.ContainsKey(keys[i])? hm2[keys[i]]:0;
                result += v1*v2;
            }

            return result;
        }
    }


}