namespace MSTParser
{
    public class DependencyInstance
    {
        public string ActParseTree ;
        public FeatureVector Fv ;
        public string[] Labs ;
        public int Length ;
        public string[] POS ;
        public string[] Sentence ;

        public DependencyInstance()
        {
        }

        public DependencyInstance(int length)
        {
            Length = length;
        }

        public DependencyInstance(string[] sentence, FeatureVector fv)
        {
            Sentence = sentence;
            Fv = fv;
            Length = sentence.Length;
        }

        public DependencyInstance(string[] sentence, string[] pos, FeatureVector fv)
        {
            Sentence = sentence;
            POS = pos;
            Fv = fv;
            Length = sentence.Length;
        }

        public DependencyInstance(string[] sentence, string[] pos, string[] labs, FeatureVector fv)
        {
            Sentence = sentence;
            POS = pos;
            Labs = labs;
            Fv = fv;
            Length = sentence.Length;
        }
    }
}