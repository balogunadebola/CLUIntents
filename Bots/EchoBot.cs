// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.22.0

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using Azure.Core;
using System.Text.Json;
using Azure;
using Azure.AI.Language.Conversations;

namespace CLUIntentBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly string _cluProjectName;
        private readonly string _cluDeploymentName;
        private readonly ConversationAnalysisClient _conversationsClient;

        public EchoBot(IConfiguration configuration)
        {

            _cluProjectName = configuration["CluProjectName"];
            _cluDeploymentName = configuration["CluDeploymentName"];

            Uri endpoint = new Uri(configuration["cluEndpoint"]);
            AzureKeyCredential credential = new AzureKeyCredential(configuration["CluAPIKey"]);

            _conversationsClient = new ConversationAnalysisClient(
                endpoint,
                credential);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var data = new
                {
                    analysisInput = new
                    {
                        conversationItem = new
                        {
                            text = turnContext.Activity.Text,
                            id = "1",
                            participantId = "1",
                        }
                    },
                    parameters = new
                    {
                        projectName = _cluProjectName,
                        deploymentName = _cluDeploymentName,
                        stringIndexType = "Utf16CodeUnit",
                    },
                    kind = "Conversation",
                };
                Response response = _conversationsClient.AnalyzeConversation(RequestContent.Create(data));

                JsonDocument result = JsonDocument.Parse(response.ContentStream);
                JsonElement conversationalTaskResult = result.RootElement;
                JsonElement orchestrationPrediction = conversationalTaskResult.GetProperty("result").GetProperty("prediction");

                string topIntent = orchestrationPrediction.GetProperty("topIntent").ToString();

                JsonElement intents = orchestrationPrediction.GetProperty("intents");

                double confidenceScore = 0.0;

                foreach (JsonElement intentElement in intents.EnumerateArray())
                {
                    string intentName = intentElement.GetProperty("category").GetString();
                    if (intentName == topIntent)
                    {
                        confidenceScore = intentElement.GetProperty("confidenceScore").GetDouble();
                        break;
                    }
                }
                //Confidence Threshold
                double confidenceThreshold = 0.5;

                //Check if the confidence is below the threshold or the Intent is 'None'
                if (topIntent.Equals("None", StringComparison.OrdinalIgnoreCase) || confidenceScore < confidenceThreshold)
                {
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("Sorry, I didn't quite get your question. Please try asking in a different way (Intent was none)."),
                        cancellationToken
                        );
                    //Add a delay of 2 seconds before the next message
                    await Task.Delay(2000, cancellationToken);

                    //Send additional message after the apology
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("What else can I do for you?"), cancellationToken);
                }
                else
                {

                    await turnContext.SendActivityAsync(MessageFactory.Text($"Recognized Intent: {topIntent}"), cancellationToken);
                }
            }
            catch (RequestFailedException ex)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"Sorry, there was an error processing your request:{ex.Message}"),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                //Handle any other unexpected errors
                await turnContext.SendActivityAsync(MessageFactory.Text($"An unexpected error occurred:{ex.Message}"),
                    cancellationToken);
            }
        }




        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}