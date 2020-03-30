using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.LanguageGeneration
{
    public class BeginExpressionEvaluationArgs : ExpressionEventArgs
    {
        public string Type { get; } = ExpressionEventTypes.BeginExpressionEvaluation;

        public string Expression { get; set; }
    }
}
