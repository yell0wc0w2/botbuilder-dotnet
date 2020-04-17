using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples
{
    public class RootDialog : ComponentDialog
    {
        private Templates _lgFile;
        private Templates _lgText;

        public RootDialog()
            : base(nameof(RootDialog))
        {
            var lgContent = @"
# greetUser
- ${greeting()}, ${userName}

# greeting
- hi
- hello";
            _lgText = Templates.ParseText(lgContent, nameof(RootDialog));
            var data = new
            {
                userName = "vishwac"
            };
            var templateEval = _lgText.Evaluate("greetUser", data);
            Activity myActivity = ActivityFactory.FromObject(templateEval);
            var stringEval = _lgText.EvaluateText("Text evaluation: ${greeting()}, ${userName}", data);

            _lgFile = Templates.ParseFile(Path.Combine(".", "Dialogs", "RootDialog", "RootDialog.lg"));
            var rootDialog = new AdaptiveDialog("root")
            {
                Generator = new TemplateEngineLanguageGenerator(),
                Triggers = new List<OnCondition>()
                {
                    new OnBeginDialog()
                    {
                        Actions = new List<Dialog>()
                        {
                            new ChoiceInput()
                            {
                                Property = "user.choice",
                                Prompt = new ActivityTemplate("What do you want?"),
                                Choices = new ChoiceSet(new List<Choice>()
                                {
                                    new Choice()
                                    {
                                        Value = "foo",
                                        Action = new CardAction()
                                        {
                                            Type = "imBack",
                                            Title = "Amazing foo",
                                            DisplayText = "Amazing foo was clicked",
                                            Value = "foo"
                                        }
                                    },
                                    new Choice()
                                    {
                                        Value = "bar",
                                        Action = new CardAction()
                                        {
                                            Type = "imBack",
                                            Title = "Amazing bar",
                                            DisplayText = "Amazing bar was clicked",
                                            Value = "bar"
                                        }
                                    }
                                })
                            },
                            new SendActivity("I have ${user.choice}")
                        }
                    }
                }
            };

            // var child1 = new AdaptiveDialog("child1")
            // {
            //     Triggers = new List<OnCondition>()
            //     {
            //         new OnBeginDialog()
            //         {
            //             Actions = new List<Dialog>()
            //             {
            //                 new SendActivity("Dialog B : I have ${dialog.token} and ${dialog.token.token}")
            //             }
            //         }
            //     }
            // };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);
            
            // AddDialog(child1);

            // The initial child dialog to run.
            InitialDialogId = "root";
        }

        private static List<Dialog> WelcomeUserAction()
        {
            return new List<Dialog>()
            {
                // Iterate through membersAdded list and greet user added to the conversation.
                new Foreach()
                {
                    ItemsProperty = "turn.activity.membersAdded",
                    Actions = new List<Dialog>()
                    {
                        // Note: Some channels send two conversation update events - one for the Bot added to the conversation and another for user.
                        // Filter cases where the bot itself is the recipient of the message. 
                        new IfCondition()
                        {
                            Condition = "dialog.foreach.value.name != turn.activity.recipient.name",
                            Actions = new List<Dialog>()
                            {
                                new SendActivity("Hello! say something to get going!")
                            }
                        }
                    }
                }
            };
        }
    }
}
