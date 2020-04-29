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
                AutoEndDialog = false,
                Generator = new TemplateEngineLanguageGenerator(_lgFile),
                Recognizer = new RegexRecognizer()
                {
                    Intents = new List<IntentPattern>()
                    {
                        new IntentPattern()
                        {
                            Intent = "quit",
                            Pattern = "quit"
                        }
                    }
                },
                Triggers = new List<OnCondition>()
                {
                    new OnBeginDialog()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Starting child dialog.. say 'back' to get back here. say 'quit' to quit"),
                            new BeginDialog()
                            {
                                Dialog = nameof(TestDialog2)
                            }
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "quit",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Ok. ending..."),
                            new EndDialog()
                        }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("In parent .. unknown..")
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
