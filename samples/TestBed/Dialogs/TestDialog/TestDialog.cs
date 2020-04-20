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
    public class TestDialog : ComponentDialog
    {
        private Templates _lgFile;

        public TestDialog()
            : base(nameof(TestDialog))
        {
            _lgFile = Templates.ParseFile(Path.Join(".", "Dialogs", "TestDialog", "TestDialog.lg"));
            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(_lgFile),
                Recognizer = new RegexRecognizer()
                {
                    Intents = new List<IntentPattern>()
                    {
                        new IntentPattern()
                        {
                            Intent = "test",
                            Pattern = "test"
                        }
                    }
                },
                Triggers = new List<OnCondition>()
                {
                    new OnIntent()
                    {
                        Intent = "test",
                        Actions = new List<Dialog>()
                        {
                            new BeginDialog()
                            {
                                Dialog = "TestDialog2"
                            },
                            new SendActivity("${welcome()}")
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);
            AddDialog(new TestDialog2());

            // AddDialog(child1);

            // The initial child dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}
