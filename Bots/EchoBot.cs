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

            string intent = orchestrationPrediction.GetProperty("topIntent").ToString();


            await turnContext.SendActivityAsync(MessageFactory.Text($"Recognized Intent: {intent}"), cancellationToken);
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