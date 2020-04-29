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
            var testDialog2 = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                AutoEndDialog = false,
                Generator = new TemplateEngineLanguageGenerator(_lgFile),
                Recognizer = new RegexRecognizer()
                {
                    Intents = new List<IntentPattern>()
                    {
                        new IntentPattern()
                        {
                            Intent = "goBack",
                            Pattern = "back"
                        }
                    }
                },
                Triggers = new List<OnCondition>()
                {
                    new OnIntent()
                    {
                        Intent = "goBack",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Sure, going back to master.."),
                            new EndDialog()
                        }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("In child .. unknown..You said '${turn.activity.text}'")
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(testDialog2);

            // The initial child dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }
    }
}
