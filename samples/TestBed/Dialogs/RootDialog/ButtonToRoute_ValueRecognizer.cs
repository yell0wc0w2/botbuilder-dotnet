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
    public class ButtonToRoute_ValueRecognizer : ComponentDialog
    {
        private Templates _lgFile;

        public ButtonToRoute_ValueRecognizer()
            : base(nameof(ButtonToRoute_ValueRecognizer))
        {
            _lgFile = Templates.ParseFile(Path.Join(".", "Dialogs", "RootDialog", "ButtonToRoute_ValuerRecognizer.lg"));
            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Recognizer = new RecognizerSet()
                {
                    Recognizers = new List<Recognizer>()
                    {
                        //new ValueRecognizer(),
                        new RegexRecognizer()
                        {
                            Intents = new List<IntentPattern>()
                            {
                                new IntentPattern()
                                {
                                    Intent = "BookFlight",
                                    Pattern = "flight"
                                },
                                new IntentPattern()
                                {
                                    Intent = "GetWeather",
                                    Pattern = "weather"
                                }
                            }
                        }
                    }
                },
                Generator = new TemplateEngineLanguageGenerator(_lgFile),
                Triggers = new List<OnCondition>()
                {
                    new OnConversationUpdateActivity()
                    {
                        Actions = new List<Dialog>()
                        {
                            new Foreach()
                            {
                                ItemsProperty = "turn.activity.membersAdded",
                                Actions = new List<Dialog>()
                                {
                                    new IfCondition()
                                    {
                                        Condition = "dialog.foreach.value.name != turn.activity.recipient.name",
                                        Actions = new List<Dialog>()
                                        {
                                            new SendActivity("Hello, I'm a demo bot"),
                                            new SendActivity("${ButtonForIntent()}")
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "BookFlight",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Picked up book flight intent.. Booking flight ...")
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "GetWeather",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Picked up get weather intent.. Getting you weather ....")
                        }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("I'm a demo bot. I can book a flight or get weather information"),
                            new SendActivity("${ButtonForIntent()}")
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
