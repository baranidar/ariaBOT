using Microsoft.Cognitive.LUIS;
using Line.Messaging;
using Line.Messaging.Webhooks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ariabot.CloudStorage;
using ariabot.Models;
/*Test */
namespace ariabot
{
    internal class LineBotApp : WebhookApplication
    {
        private LineMessagingClient messagingClient { get; }
        private TableStorage<Review> reviewState { get; }
        private TableStorage<EventSourceState> sourceState { get; }
        private BlobStorage blobStorage { get; }

        private LuisClient luisClient = new LuisClient(
            appId: "40648804-3eda-4129-b8be-c215c25e2cf3",
            appKey: "9a18976793d64874b648ca12140597e2");

        public LineBotApp(LineMessagingClient lineMessagingClient, TableStorage<EventSourceState> tableStorage, BlobStorage blobStorage)
        {
            this.messagingClient = lineMessagingClient;
            this.sourceState = tableStorage;
            this.blobStorage = blobStorage;
        }

        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            switch (ev.Message.Type)
            {
                case EventMessageType.Text:
                    await HandleTextAsync(ev.ReplyToken, ((TextEventMessage)ev.Message).Text, ev.Source.UserId);
                    break;
            }
        }

        private async Task HandleLocationAsync(string replyToken, LocationEventMessage location, string userId)
        {
            // Get an order
            var review = await reviewState.FindAsync("review", userId);
            // Save the address first
            review.Name = location.Address;
            review.Text = location.Title;
            await reviewState.UpdateAsync(review);
            await messagingClient.ReplyMessageAsync(replyToken, new[] {
                        new TextMessage($"Thanks! We will add {review.Name}'s review and rating of {review.Rating} and {review.Text}!!")
                    });
        }

        private async Task HandleTextAsync(string replyToken, string userMessage, string userId)
        {
            ISendMessage replyMessage = new TextMessage("");
            // Analyze the input via LUIS
            var luisResult = await luisClient.Predict(userMessage);
            if (luisResult.TopScoringIntent.Name == "Greetings")
            {
                replyMessage = new TextMessage("Welcome to Aria's food adventures!");
            }
            else if (luisResult.TopScoringIntent.Name == "Recipes")
            {
                // If menu is specified.
                if (luisResult.Entities.ContainsKey("Recipes"))
                {
                    var name = luisResult.Entities["Name"].First().Value;
                    var order = new Review() { Name = name, SourceId = userId };
                    await reviewState.UpdateAsync(order);

                    replyMessage = new TemplateMessage("Review", new ButtonsTemplate(
                        title: "Review",
                        text: $"Thanks for the review.",
                        actions: new List<ITemplateAction>(){
                            new UriTemplateAction("Review","line://nv/location")
                            }));
                }
                else
                {
                    // If no recipes is specified, then shows them using buttons.
                    replyMessage = new TemplateMessage("Recipes", new ButtonsTemplate(
                title: "Recipes",
                text: "Which recipe would you like to choose?",
                actions: new List<ITemplateAction>(){
                    new MessageTemplateAction("Fetuccini Alfredo", "pasta"),
                    new MessageTemplateAction("Sphagetti with Spinach Alfredo Sauce","Sphagetti"),
                }));
                }
            }

            await messagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { replyMessage });
        }
    }
}