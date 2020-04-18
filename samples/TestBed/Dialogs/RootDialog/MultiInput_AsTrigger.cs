using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace Microsoft.BotBuilderSamples
{
    public class MultiInput_AsTrigger : ComponentDialog
    {
        private Templates _lgFile;

        public MultiInput_AsTrigger()
            : base(nameof(MultiInput_AsTrigger))
        {
            _lgFile = Templates.ParseFile(Path.Join(".", "Dialogs", "RootDialog", "MultiInput.lg"));
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
                    },
                    Entities = new List<EntityRecognizer>()
                    {
                        new EmailEntityRecognizer(),
                        new NumberEntityRecognizer()
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
                    },
                    new OnIntent()
                    {
                        Intent = "BookFlight",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Picked up book flight intent.. Booking flight ..."),
                            new TextInput()
                            {
                                Prompt = new ActivityTemplate("${MultiProperty()}"),
                                Property = "user.email",
                                
                                // This enables you to rely on either the 'email' entity recognizer or the 'email' property from the card
                                Value = "=coalesce(@email, turn.activity.value.email)",
                                Validations = new List<string>()
                                {
                                    "isMatch(this.value, '^(([^<>()\\[\\]\\.,;:\\s@\"]+(\\.[^<>()\\[\\]\\.,;:\\s@\"]+)*)|(\".+ \"))@((\\[[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}])|(([a-zA-Z\\-0-9]+\\.)+[a-zA-Z]{2,}))$')",
                                },
                                InvalidPrompt = new ActivityTemplate("${MultiProperty.invalid('email')}")
                            },
                            new TextInput()
                            {
                                Prompt = new ActivityTemplate("${MultiProperty()}"),
                                Property = "user.age",
                                
                                // This enables you to rely on either the 'email' entity recognizer or the 'email' property from the card
                                Value = "=coalesce(@number,turn.activity.value.number)",
                                Validations = new List<string>()
                                {
                                    "int(this.value) >= 1",
                                    "int(this.value) <= 150"
                                },
                                InvalidPrompt = new ActivityTemplate("${MultiProperty.invalid('age')}")
                            },
                            new SendActivity("Sure, I have your email as ${user.email} and your age as ${user.age}.")
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
                    
                    // This is necessary to ground all captured entities so out of order entities are captured.
                    // Of course the ideal case here is to decouple properties and constraints on it away from dialog. 
                    // dialog should not manage these properties as called out in my DCR here https://github.com/microsoft/botbuilder-dotnet/issues/2322
                    // Adaptive card does not support the concept of 'disabled' so this still would not work if the user messes up email id while being prompted for age (after accepting a valid email).
                    new OnMessageActivity()
                    {
                        Condition = "turn.activity.text == null && turn.activity.value != null && turn.activity.value.intent == 'userProfileAdaptiveCardInput'"
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Setting values..."),
                            new SetProperties()
                            {
                                Assignments = new List<PropertyAssignment>()
                                {
                                    new PropertyAssignment()
                                    {
                                        Property = "user.email",
                                        Value = "=turn.activity.value.email"
                                    },
                                    new PropertyAssignment()
                                    {
                                        Property = "user.age",
                                        Value = "=turn.activity.value.number"
                                    }
                                }
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
