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
    public class ButtonToRoute_AsTrigger : ComponentDialog
    {
        private Templates _lgFile;

        public ButtonToRoute_AsTrigger()
            : base(nameof(ButtonToRoute_AsTrigger))
        {
            _lgFile = Templates.ParseFile(Path.Join(".", "Dialogs", "RootDialog", "ButtonToRoute_AsTrigger.lg"));
            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Recognizer = new RegexRecognizer()
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
                    },
                    new OnMessageActivity()
                    {
                        Condition = "turn.activity.text == null && turn.activity.value != null",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("I have ${turn.activity.value.intent}"),
                            
                            // This does not work. 
                            // Expecting a simple use case to use EmitEvent seems unnecessary. 
                            new EmitEvent()
                            {
                                EventName = AdaptiveEvents.RecognizedIntent,
                                EventValue = "=json(`{\"intent\", ${turn.activity.value.intent}}`)"
                            }
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
