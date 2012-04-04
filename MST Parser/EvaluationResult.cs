using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSTParser
{
    public class EvaluationResult
    {
        /// <summary>
        /// The Accuracy For Unlabeled Dependency Parsing
        /// </summary>
        public double UnlabeledAccuracy { get; private set; }
        /// <summary>
        /// The Accuracy For Labeled Dependency Parsing
        /// </summary>
        public double LabeledAccuracy { get; private set; }
        /// <summary>
        /// The Accuracy For Unlabeled Dependency Parsing based on Sentence Parsing Completion 
        /// </summary>
        public double UnlabeledCompleteAccuracy { get; private set; }
        /// <summary>
        /// The Accuracy For Labeled Dependency Parsing based on Sentence Parsing Completion 
        /// </summary>
        public double LabeledCompleteAccuracy { get; private set; }
        /// <summary>
        /// To Construct an Evaluation Object
        /// </summary>
        /// <param name="ua"> UnlabeledAccuracy
        /// The Accuracy For Unlabeled Dependency Parsing</param>
        /// <param name="uca">UnlabeledCompleteAccuracy
        /// The Accuracy For Labeled Dependency Parsing based on Sentence Parsing Completion </param>
        /// <param name="la">LabeledAccuracy
        /// The Accuracy For Labeled Dependency Parsing</param>
        /// <param name="lca">LabeledCompleteAccuracy
        /// The Accuracy For Labeled Dependency Parsing based on Sentence Parsing Completion </param>
        public EvaluationResult(double ua, double uca, double la, double lca)
        {
            UnlabeledAccuracy = ua;
            LabeledAccuracy = uca;
            UnlabeledCompleteAccuracy = la;
            LabeledCompleteAccuracy = lca;
        }
    }
}
