using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace Microsoft.BotBuilderSamples
{
    public class TestDialog2 : ComponentDialog
    {
        private Templates _lgFile;

        public TestDialog2()
            : base(nameof(TestDialog2))
        {
            _lgFile = Templates.ParseFile(Path.Join(".", "Dialogs", "TestDialog", "TestDialog2.lg"));
            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(_lgFile),
                Triggers = new List<OnCondition>()
                {
                    new OnBeginDialog()
                    {
                        Actions = new List<Dialog>()
                        {
                            //new SendActivity("${welcome()}"),
                            new EndDialog()
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);

            // AddDialog(child1);

            // The initial child dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}
