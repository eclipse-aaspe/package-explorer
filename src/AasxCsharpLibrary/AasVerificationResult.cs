using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminShellNS
{
    public class AasVerificationResult
    {
        public string ErrorText { get; set; }
        public string PathSegment { get; set; }

        public AasVerificationResult(string errorText, string pathSegment)
        {
            ErrorText = errorText;
            PathSegment = pathSegment;
        }

        public string DisplayErrorText { get { return "" + ErrorText.ToString(); } }
        public string DisplayPathSegment { get { return "" + PathSegment.ToString(); } }
    }
}
