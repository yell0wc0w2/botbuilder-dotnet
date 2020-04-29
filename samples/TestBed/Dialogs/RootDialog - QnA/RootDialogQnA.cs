﻿using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;

//using Microsoft.Bot.Builder.Dialogs.Adaptive.QnA.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace Microsoft.BotBuilderSamples
{
    public class RootDialogQnA : ComponentDialog
    {
        private Templates _lgTemplates;

        public RootDialogQnA()
            : base(nameof(ButtonToRoute_ValueRecognizer))
        {
            var childDialog = new AdaptiveDialog("child")
            {
                Recognizer = new RegexRecognizer()
                {
                    Intents = new List<IntentPattern>()
                    {
                        new IntentPattern()
                        {
                            Intent = "cancel",
                            Pattern = "cancel"
                        }
                    }
                },
                Triggers = new List<OnCondition>()
                {
                    new OnBeginDialog()
                    {
                        Actions = new List<Dialog>()
                        {
                            // new TextInput()
                            // {
                            //     Prompt = new ActivityTemplate("what is your name?"),
                            //     Property = "user.name"
                            // },
                            new SendActivity("I have ${user.name}")
                        }
                    },
                    new OnEndOfActions()
                    {
                        Condition = "!turn.isRun",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("End of actions!"),
                            new SetProperty()
                            {
                                Property = "turn.isRun",
                                Value = "=true"
                            }
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "cancel",
                        Actions = new List<Dialog>()
                        {
                            new CancelAllDialogs()
                        }
                    }
                }
            };

            _lgTemplates = Templates.ParseFile(Path.Combine(".", "Dialogs", "RootDialog - QnA", "RootDialog.lg"));
            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Generator = new TemplateEngineLanguageGenerator(_lgTemplates),

                Recognizer = MultiRecognizer(),

                // Recognizer = MultiRecognizer(),
                Triggers = new List<OnCondition>()
                {
                    new OnDialogEvent()
                    {
                        Event = "cancel",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Ok")
                        }
                    },
                    new OnConversationUpdateActivity()
                    {
                        Actions = WelcomeUserAction()
                    },
                    new OnQnAMatch()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("${@answer}")
                        }
                    },
                    new OnQnAMatch()
                    {
                        Condition = "count(turn.recognized.answers[0].context.prompts) > 0",
                        Actions = new List<Dialog>()
                        {
                            new SetProperties()
                            {
                                Assignments = new List<PropertyAssignment>()
                                {
                                    new PropertyAssignment()
                                    {
                                        Property = "dialog.qnaContext",
                                        Value = "={}"
                                    },
                                    new PropertyAssignment()
                                    {
                                        Property = "dialog.qnaContext.PreviousQnAId",
                                        Value = "=turn.recognized.answers[0].id"
                                    },
                                    new PropertyAssignment()
                                    {
                                        Property = "dialog.qnaContext.previousUserQuery",
                                        Value = "=turn.recognized.text"
                                    },
                                    new PropertyAssignment()
                                    {
                                        Property = "dialog.qna.multiTurn.context",
                                        Value = "=turn.recognized.answers[0].context.prompts"
                                    }
                                }
                            },
                            new TextInput()
                            {
                                Prompt = new ActivityTemplate("${ShowMultiTurnAnswer()}"),
                                Property = "turn.qnaMultiTurnResponse",
                                AllowInterruptions = "false",
                                AlwaysPrompt = true
                            },
                            new SetProperty()
                            {
                                Property = "turn.qnaMatchFromContext",
                                Value = "=where(dialog.qna.multiTurn.context, item, item.displayText == turn.qnaMultiTurnResponse)"
                            },
                            
                            new IfCondition()
                            {
                                Condition = "turn.qnaMatchFromContext && count(turn.qnaMatchFromContext) > 0",
                                Actions = new List<Dialog>()
                                {
                                    new SetProperty()
                                    {
                                        Property = "turn.qnaIdFromPrompt",
                                        Value = "=turn.qnaMatchFromContext[0].qnaId"
                                    },
                                }
                            },
                            new EmitEvent()
                            {
                                EventName = AdaptiveEvents.ActivityReceived,
                                EventValue = "=turn.activity",
                            }
                        }
                    },
                    new OnChooseIntent()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SetProperties()
                            {
                                Assignments = new List<PropertyAssignment>()
                                {
                                    new PropertyAssignment()
                                    {
                                        Property = "dialog.luisResult",
                                        Value = "=jPath(turn.recognized, \"$.candidates[?(@.id == 'Root_LUIS')]\")"
                                    },
                                    new PropertyAssignment()
                                    {
                                        Property = "dialog.qnaResult",
                                        Value = "=jPath(turn.recognized, \"$.candidates[?(@.id == 'Root_QnA')]\")"
                                    },
                                }
                            },

                            // add rules to determine winner before disambiguation
                            // R1. L high (>0.9), Q low (<0.5) => LUIS
                            new IfCondition()
                            {
                                Condition = "dialog.luisResult.score >= 0.9 && dialog.qnaResult.score <= 0.5",
                                Actions = new List<Dialog>()
                                {
                                    new EmitEvent()
                                    {
                                        EventName = AdaptiveEvents.RecognizedIntent,
                                        EventValue = "=dialog.luisResult.result"
                                    },
                                    new BreakLoop()
                                }
                            },

                            // R2. Q high, L low => QnA
                            new IfCondition()
                            {
                                Condition = "dialog.luisResult.score <= 0.5 && dialog.qnaResult.score >= 0.9",
                                Actions = new List<Dialog>()
                                {
                                    new EmitEvent()
                                    {
                                        EventName = AdaptiveEvents.RecognizedIntent,
                                        EventValue = "=dialog.qnaResult.result"
                                    },
                                    new BreakLoop()
                                }
                            },

                            // R3. Q exact match (>=0.95) => QnA
                            new IfCondition()
                            {
                                Condition = "dialog.qnaResult.score >= 0.95",
                                Actions = new List<Dialog>()
                                {
                                    new EmitEvent()
                                    {
                                        EventName = AdaptiveEvents.RecognizedIntent,
                                        EventValue = "=dialog.qnaResult.result"
                                    },
                                    new BreakLoop()
                                }
                            },

                            // R4. Q no match => LUIS
                            new IfCondition()
                            {
                                Condition = "dialog.qnaResult.score <= 0.05",
                                Actions = new List<Dialog>()
                                {
                                    new EmitEvent()
                                    {
                                        EventName = AdaptiveEvents.RecognizedIntent,
                                        EventValue = "=dialog.luisResult.result"
                                    },
                                    new GotoAction("Exit")
                                }
                            },
                            new TextInput()
                            {
                                Property = "turn.intentChoice",
                                Prompt = new ActivityTemplate("${chooseIntentResponseWithCard()}"),
                                Value = "=@userChosenIntent",
                                AlwaysPrompt = true
                            },
                            new IfCondition()
                            {
                                Condition = "turn.intentChoice != 'none'",
                                Actions = new List<Dialog>()
                                {
                                    new EmitEvent()
                                    {
                                        EventName = AdaptiveEvents.RecognizedIntent,
                                        EventValue = "=dialog[turn.intentChoice].result"
                                    }
                                }, 
                                ElseActions = new List<Dialog>()
                                {
                                    new SendActivity()
                                    {
                                        Activity = new ActivityTemplate("Sure, no worries.")
                                    }
                                }
                            }
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "Greeting",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("I'm greeting you. LUIS recognizer won!")
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "UserProfile",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Let's get your user profile. LUIS recognizer won!")
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "Help",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("Getting you to a human!")
                        }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("I do not know how to do that!")
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);
            AddDialog(childDialog);

            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }

        private static Recognizer GetLUISApp()
        {
            return new LuisAdaptiveRecognizer()
            {
                Id = "Root_LUIS",
                ApplicationId = "063e7f98-fef5-4b60-a740-39a6d933dd09",
                EndpointKey = "a95d07785b374f0a9d7d40700e28a285",
                Endpoint = "https://westus.api.cognitive.microsoft.com"
            };
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
                                new SendActivity("I have ${coalesce(settings.key, 'error')}"),
                                new SendActivity("${WelcomeUser()}"),
                                new BeginDialog("child")
                            }
                        }
                    }
                }
            };
        }

        // private static Recognizer QnARecognizer()
        // {
        //     return new QnAMakerRecognizer()
        //     {
        //         Id = "Root_QnA",
        //         HostName = "https://vk-test-qna.azurewebsites.net/qnamaker",
        //         EndpointKey = "8e744f2e-2f80-4c16-bb68-7eb2a088726f",
        //         KnowledgeBaseId = "206eab69-6573-4a8d-939b-63a1a2511d11",
        //         Top = 10,
        //         Context = "dialog.qnaContext",
        //         QnAId = "turn.qnaIdFromPrompt"
        //     };
        // }

        private static Recognizer MultiRecognizer()
        {
            return new RecognizerSet()
            {
                Recognizers = new List<Recognizer>()
                {
                    //new ValueRecognizer(),
                    new CrossTrainedRecognizerSet()
                    {
                        Recognizers = new List<Recognizer>()
                        {
                            //QnARecognizer(),
                            GetLUISApp()
                        }
                    }
                }
            };
        }
    }
}
