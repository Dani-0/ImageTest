using System.Collections.Generic;

namespace ImageTest
{
    public class Word
    {
        public List<double> boundingBox { get; set; }
        public string text { get; set; }
        public double confidence { get; set; }
    }
}
