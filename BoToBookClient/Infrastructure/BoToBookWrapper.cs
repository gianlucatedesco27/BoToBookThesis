using Azure;
using Azure.AI.OpenAI;
using Azure.AI.TextAnalytics;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using BoToBookClient.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace BoToBookClient.Infrastructure
{
    public class BoToBookWrapper : IBoToBookWrapper
    {
        private readonly ILogger<BoToBookWrapper> logger;
        private static IConfiguration _configuration;
        private readonly OpenAIClient openAIClient;
        private readonly TextAnalyticsClient textAnalyticsClient;

        public BoToBookWrapper(ILogger<BoToBookWrapper> logger, IConfiguration configuration)
        {
            this.logger = logger;
            _configuration = configuration;

            this.openAIClient = new OpenAIClient(new Uri(_configuration["OpenAISettings:AzureOpenAIEndpointUS"]),
                new AzureKeyCredential(_configuration["OpenAISettings:AzureOpenAIApiKeyUS"]));

            var options = new TextAnalyticsClientOptions();
            options.DefaultLanguage = "it";
            this.textAnalyticsClient = new TextAnalyticsClient(new Uri(_configuration["OpenAISettings:AzureTextAnalyticsEndpoint"]),
                new AzureKeyCredential(_configuration["OpenAISettings:AzureTextAnalyticsApiKey"]),
                options);
        }

        public async Task<(string, List<string>)> CreateRandomStory(string name)
        {
            var botTextResult = string.Empty;

            (string, List<string>) result = new(string.Empty, new List<string>());
            var storySummary = new StorySummary();
            int attempts = 5;
            int i = 1;

            try
            {
                storySummary.Hero = await GenerateRandomHero(textAnalyticsClient, name);
                storySummary.HeroId = await GetImageIdFromAzureDallE(storySummary.Hero);
                while (string.IsNullOrEmpty(storySummary.HeroId) && i <= attempts)
                {
                    storySummary.HeroId = await GetImageIdFromAzureDallE(storySummary.Hero);
                    i++;
                }

                i = 1;

                storySummary.Setting = GenerateRandomSetting();
                storySummary.SettingId = await GetImageIdFromAzureDallE(storySummary.Setting);
                while (string.IsNullOrEmpty(storySummary.SettingId) && i <= attempts)
                {
                    storySummary.SettingId = await GetImageIdFromAzureDallE(storySummary.Setting);
                    i++;
                }
                i = 1;

                storySummary.Antagonist = GenerateRandomAntagonist();
                storySummary.AntagonistId = await GetImageIdFromAzureDallE(storySummary.Antagonist);
                while (string.IsNullOrEmpty(storySummary.AntagonistId) && i < attempts)
                {
                    storySummary.AntagonistId = await GetImageIdFromAzureDallE(storySummary.Antagonist);
                    i++;
                }
                i = 1;

                storySummary.Friend = GenerateRandomFriend();
                storySummary.FriendId = await GetImageIdFromAzureDallE(storySummary.Friend);
                while (string.IsNullOrEmpty(storySummary.FriendId) && i < attempts)
                {
                    storySummary.FriendId = await GetImageIdFromAzureDallE(storySummary.Friend);
                    i++;
                }

                var chatCompletionsOptions = await SetChatbot(storySummary);

                while (string.IsNullOrEmpty(botTextResult))
                {
                    botTextResult = await PerformChatbot(chatCompletionsOptions);

                    result.Item1 = botTextResult;
                    var heroUrl = await GetUrlmageFromAzureDallE(storySummary.HeroId, _configuration["OpenAISettings:AzureOpenAIApiKeyUS"]);
                    var friendUrl = await GetUrlmageFromAzureDallE(storySummary.FriendId, _configuration["OpenAISettings:AzureOpenAIApiKeyUS"]);
                    var antagonistUrl = await GetUrlmageFromAzureDallE(storySummary.AntagonistId, _configuration["OpenAISettings:AzureOpenAIApiKeyUS"]);
                    var settingUrl = await GetUrlmageFromAzureDallE(storySummary.SettingId, _configuration["OpenAISettings:AzureOpenAIApiKeyUS"]);

                    result.Item2 = new List<string>
                    {
                        heroUrl,
                        friendUrl,
                        antagonistUrl,
                        settingUrl
                    };
                }

            }
            catch (Exception ex)
            {
                logger.LogInformation($"Exception {ex.Message}");
                Console.WriteLine(ex.Message);

                return result;
            }

            return result;
        }

        public async Task<(string, List<string>)> CreateCustomStory(string name, string friend, string setting, string antagonist)
        {
            var botTextResult = string.Empty;
            (string, List<string>) result = new(string.Empty, new List<string>());

            var storySummary = new StorySummary()
            {
                Hero = name,
                Antagonist = antagonist,
                Friend = friend,
                Setting = setting
            };

            var chatCompletionsOptions = await SetChatbot(storySummary);

            Console.WriteLine("SetChatbot OK");

            while (string.IsNullOrEmpty(botTextResult))
            {
                Console.WriteLine("Sto iniziando a scrivere");
                botTextResult = await PerformChatbot(chatCompletionsOptions);
                Console.WriteLine("Finito");
                result.Item1 = botTextResult;
                var heroUrl = await GetUrlmageFromAzureDallE(storySummary.HeroId, _configuration["OpenAISettings:AzureOpenAIApiKeyUS"]);
                var friendUrl = await GetUrlmageFromAzureDallE(storySummary.FriendId, _configuration["OpenAISettings:AzureOpenAIApiKeyUS"]);
                var antagonistUrl = await GetUrlmageFromAzureDallE(storySummary.AntagonistId, _configuration["OpenAISettings:AzureOpenAIApiKeyUS"]);
                var settingUrl = await GetUrlmageFromAzureDallE(storySummary.SettingId, _configuration["OpenAISettings:AzureOpenAIApiKeyUS"]);

                result.Item2 = new List<string>
                {
                    heroUrl,
                    friendUrl,
                    antagonistUrl,
                    settingUrl
                };
            }

            return result;
        }

        private async Task<ChatCompletionsOptions> SetChatbot(StorySummary storySummary)
        {
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, "Sei un cantastorie che crea storie con lieto fine con eroi, amici, ambientazioni ed antagonisti."),
                    new ChatMessage(ChatRole.System, "Il nostro protagonista o eroe ha sempre una fidata spalla che lo aiuta nei momenti difficili"),
                    new ChatMessage(ChatRole.System, "Le storie devono essere adatte ad un pubblico di minori"),
                    new ChatMessage(ChatRole.System, "A volte l'antagonista si pente delle sue azioni e con il protagonista e la spalla aiutano a sistemare le cose per il meglio"),
                    new ChatMessage(ChatRole.System, "Usa sempre toni carini e adatti ai bambini"),
                    new ChatMessage(ChatRole.System, "I personaggi, eroe, amici, ambientazioni ed antagonisti devono sempre avere una piccola descrizione"),
                    new ChatMessage(ChatRole.User, $"Crea una storia con un Protagonista {storySummary.Hero}, un compagno di avventura {storySummary.Friend} che vivono in {storySummary.Setting}. C'è sempre un'antagonista {storySummary.Antagonist}"),
                    //new ChatMessage(ChatRole.User, $"C'era una volta un {storySummary.Hero} che aveva una fidata spalla {storySummary.Friend}"),
                    //new ChatMessage(ChatRole.User, $"La storia è ambientata in {storySummary.Setting}"),
                    //new ChatMessage(ChatRole.User, $"{storySummary.Hero} e {storySummary.Friend} devono affrontare un antagonista di nome {storySummary.Antagonist}"),
                }
            };
            return chatCompletionsOptions;
        }

        private async Task<string> PerformChatbot(ChatCompletionsOptions options)
        {
            var chatCompletionsResponse = await openAIClient.GetChatCompletionsStreamingAsync(
              _configuration["OpenAISettings:AzureOpenAIModel"],
              options
          );

            var chatResponseBuilder = new StringBuilder();
            await foreach (var chatChoice in chatCompletionsResponse.Value.GetChoicesStreaming())
            {
                await foreach (var chatMessage in chatChoice.GetMessageStreaming())
                {
                    chatResponseBuilder.AppendLine(chatMessage.Content);
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
            }
            #region fix text
            var formattedText = chatResponseBuilder.ToString().Replace("\r\n", "");
            formattedText = formattedText.Replace("\n", "");
            #endregion

            options.Messages.Add(new ChatMessage(ChatRole.Assistant, formattedText));

            #region text analytics
            // Analyze sentiment
            var sentimentResponse = await AnalyzeSentiment(textAnalyticsClient, formattedText, "it");

            var sentiment = sentimentResponse?.FirstOrDefault()?.DocumentSentiment?.Sentiment ?? TextSentiment.Neutral;
            logger.LogInformation($"Sentiment of story: {sentiment}");

            // Extract key phrases
            var keyPhrasesResponse = await ExtractKeyPhrases(textAnalyticsClient, new List<string> { formattedText }, "it");


            foreach (var phrase in keyPhrasesResponse)
            {
                logger.LogInformation($"Frase: {phrase}");
            }
            #endregion

            return formattedText;
        }

        #region Info Storia
        static string GenerateRandomSetting()
        {
            string[] settings = { "Bosco Incantato", "Villaggio delle caramelle", "Regno delle nuvole", "Isola dei Delfini Sorriso",
            "Montagne Danzanti", "Spiaggia delle Fate Brillanti", "Città dei Giganti Gentili", "Prateria dei Fiori Sorridenti", "Arcipelago delle Farfalle Magiche",
            "Deserto dei racconti infiniti", "Giardino delle Meraviglie Floreali", "Cascate dell'Arcobaleno", "Palazzo delle Bolle Luminose", "Luna del Sorriso" };
            Random random = new Random();
            int index = random.Next(settings.Length);
            return settings[index];
        }

        static string GenerateRandomAntagonist()
        {
            string[] antagonists = { "Burattino Bugiardo", "Gatto Nero Birichino", "Maga Dispettosa", "Folletto Confuso",
            "Troll Guastafeste", "Koala Golosa", "Orco Farlocco", "Dinosauro Astronauta",
            "Elefante Dispettina", "Fantasma Timoroso", "Spaventapasseri Amichevole", "Goblin Birichino", "Orsetto Testardo" };
            Random random = new Random();
            int index = random.Next(antagonists.Length);
            return antagonists[index];
        }

        static string GenerateRandomFriend()
        {
            string[] friends = {"Ridolini il Bradipo Sbuffante", "Lucio la Lucertola Birichina", "Samantah la Cagnolina Gioiosa",
            "Riccardo il Riccio Divertente", "Ciufciuffo il Pollo Birichino", "Balzella l'Orsa Saltellante", "Allegro il Gorilla Gioioso",
            "Squittina la Scoiattola Stramba", "Trilli la Tartaruga Velocea", "Giggia la Giraffa Goliardica", "Furbetta la Fenicottera Impicciona",
            "Ripetina la Pappagalla Scherzosa", "Ridolina l'Ippopotamo Risoluta"
        };
            Random random = new Random();
            int index = random.Next(friends.Length);
            return friends[index];
        }

        static async Task<string> GenerateRandomHero(TextAnalyticsClient textAnalyticsClient, string name)
        {
            string[] maleRoles = { "il Topo Ingenioso", "il Coraggioso Cavaliere", "il Lupo Gentile", "il Benefico Albero Magico", "il Maestro degli Incantesimi", "il Pesce Parlante" };
            string[] femaleRoles = { "la Dolce Principessa", "la Fata delle Lucciole", "la Gattina Sognatrice", "l'Aquilotta Audace", "la Coniglietta Saltellante", "la Capra Avventurosa" };

            string detectedLanguage = DetectLanguage(textAnalyticsClient);
            string gender = await DetectGender(textAnalyticsClient, name, detectedLanguage);

            string role;

            if (gender == "male")
            {
                Random random = new Random();
                int index = random.Next(maleRoles.Length);
                role = maleRoles[index];
            }
            else if (gender == "female")
            {
                Random random = new Random();
                int index = random.Next(femaleRoles.Length);
                role = femaleRoles[index];
            }
            else
            {
                role = "Eroe";
            }

            string heroWithRole = $"{role} {name}";

            return heroWithRole;
        }

        static string DetectLanguage(TextAnalyticsClient textAnalyticsClient)
        {
            var documents = new List<string>
        {
            "C'era una volta un eroe in una terra lontana.",
            "Questo è un altro testo in italiano."
        };
            var response = textAnalyticsClient.DetectLanguageBatch(documents, "it");

            var result = response.Value;

            if (response.HasValue)
            {
                return result.FirstOrDefault().PrimaryLanguage.Name;
            }

            // Default to empty string if language detection fails
            return string.Empty;
        }

        static async Task<string> DetectGender(TextAnalyticsClient textAnalyticsClient, string text, string language)
        {
            if (language == "Italian")
            {
                var document = new TextDocumentInput("1", text);
                var documents = new List<TextDocumentInput> { document };
                var result = textAnalyticsClient.RecognizeEntitiesBatch(documents);

                if (result.Value.Count > 0 && result.Value[0].Entities.Count > 0)
                {
                    var genderEntity = result.Value[0].Entities.FirstOrDefault(e => e.Category == "Person");

                    if (genderEntity.Length > 0)
                    {
                        var name = genderEntity.Text;

                        // Call Genderize.io API to get gender information based on the name
                        using (var httpClient = new HttpClient())
                        {
                            var apiUrl = $"https://api.genderize.io/?name={name}";
                            var response = await httpClient.GetAsync(apiUrl);
                            var jsonString = await response.Content.ReadAsStringAsync();

                            // Parse the JSON response
                            var jsonDocument = JsonDocument.Parse(jsonString);

                            // Extract the gender from the JSON response
                            if (jsonDocument.RootElement.TryGetProperty("gender", out var genderElement))
                            {
                                var gender = genderElement.GetString();
                                return gender;
                            }
                        }
                    }
                }
            }

            // Default to neutral gender if unable to determine
            return "neutro";
        }

        static async Task<IReadOnlyCollection<AnalyzeSentimentResult>> AnalyzeSentiment(TextAnalyticsClient textAnalyticsClient, string text, string language)
        {
            var documents = new List<string> { text };

            var response = await textAnalyticsClient.AnalyzeSentimentBatchAsync(documents, language);

            if (response.Value.Count > 0)
            {
                return response.Value;
            }

            return Array.Empty<AnalyzeSentimentResult>();
        }

        static async Task<List<string>> ExtractKeyPhrases(TextAnalyticsClient textAnalyticsClient, List<string> texts, string language)
        {
            var response = await textAnalyticsClient.ExtractKeyPhrasesBatchAsync(texts, language);

            var keyPhrases = response.Value
                .SelectMany(result => result.KeyPhrases)
                .Distinct()
                .ToList();

            return keyPhrases;
        }

        public async Task<string> GetImageIdFromAzureDallE(string item)
        {
            string operationId = string.Empty;
            string apiBase = _configuration["OpenAISettings:AzureApiBase"];
            string token = _configuration["OpenAISettings:AzureOpenAIApiKeyUS"];
            string apiVersion = _configuration["OpenAISettings:AzureApiVersion"];

            string url = $"{apiBase}/openai/images/generations:submit?api-version={apiVersion}";

            var imageDetails = $"{item}, stile film di animazione";

            var imageObj = new DallERequest()
            {
                Prompt = imageDetails,
                N = 1,
                Size = "512x512"
            };
            string image = JsonConvert.SerializeObject(imageObj);

            using (HttpClient client = new HttpClient())
            {
                // Set the content type and data
                StringContent content = new StringContent(image, Encoding.UTF8, "application/json");

                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Headers.Add("api-key", token);

                // Send the POST request and get the response
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    string responseString = await response.Content.ReadAsStringAsync();

                    // Deserialize the JSON response
                    dynamic responseJson = JsonConvert.DeserializeObject<dynamic>(responseString);

                    // Retrieve the operation ID from the response
                    operationId = responseJson.id;
                }
                return operationId;
            }
        }

        public async Task<string> GetUrlmageFromAzureDallE(string imageId, string apiKey)
        {
            var urlImage = string.Empty;
            var baseUrl = _configuration["OpenAISettings:AzureApiBase"];

            using (HttpClient client = new HttpClient())
            {
                //Construct the URL for the GET request to retrieve images
                string getRequestUrl = $"{baseUrl}/openai/operations/images/{imageId}?api-version=2023-06-01-preview&api-key={apiKey}";

                // Send the GET request and get the response
                HttpResponseMessage getResponse = await client.GetAsync(getRequestUrl);

                // Check if the GET request was successful
                if (getResponse.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    string getResponseString = await getResponse.Content.ReadAsStringAsync();

                    // Deserialize the JSON response for image retrieval
                    dynamic getResponseJson = JsonConvert.DeserializeObject<dynamic>(getResponseString);

                    if (getResponseJson != null)
                    {
                        if (getResponseJson.result != null)
                        {
                            if (getResponseJson.result.data != null)
                            {
                                for (int i = 0; i < getResponseJson.result.data.Count; i++)
                                {
                                    urlImage = getResponseJson.result.data[i].url;
                                }
                            }
                        }
                    }
                }
                return urlImage;
            }
        }

        public async Task<byte[]> GeneratePDF(string text, List<string> imagesUrl)
        {
            byte[] pdfBytes;

            try
            {
                // Create a MemoryStream to write the PDF content
                using (var stream = new MemoryStream())
                {
                    // Create the document and set its size and margins
                    using (var document = new Document(PageSize.A4))
                    {
                        var writer = PdfWriter.GetInstance(document, stream);

                        // Open the document
                        document.Open();

                        // Add text to the document with the custom font

                        var paragraph = new Paragraph(" \" " + text.ToUpper() + " \" ");

                        document.Add(paragraph);

                        if (imagesUrl.Any())
                        {
                            using (var httpClient = new HttpClient())
                            {
                                var contentByte = writer.DirectContent;

                                // Calculate the available width and height for the images on the page
                                var availableWidth = PageSize.A4.Width - document.LeftMargin - document.RightMargin;
                                var availableHeight = PageSize.A4.Height - document.TopMargin - document.BottomMargin;

                                // Calculate the maximum image size that can fit within the available space
                                var maxImageWidth = availableWidth / imagesUrl.Count;
                                var maxImageHeight = availableHeight;

                                var currentX = document.LeftMargin;
                                var currentY = document.BottomMargin;

                                foreach (var item in imagesUrl)
                                {
                                    if (!string.IsNullOrEmpty(item))
                                    {
                                        byte[] imageData = await httpClient.GetByteArrayAsync(item);
                                        var image = Image.GetInstance(imageData);

                                        // Resize the image proportionally to fit within the available width and height
                                        image.ScaleToFit(maxImageWidth, maxImageHeight);

                                        // Set the image position
                                        image.SetAbsolutePosition(currentX, currentY);

                                        // Add the image to the contentByte
                                        contentByte.AddImage(image, true);

                                        // Update the current position for the next image
                                        currentX += maxImageWidth;
                                    }
                                }
                            }
                        }

                        // Close the document
                        document.Close();
                    }

                    // Get the byte array representation of the PDF from the MemoryStream
                    pdfBytes = stream.ToArray();
                    return pdfBytes;
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Exception {ex.Message}");
                return null;
            }
        }


        #endregion
    }
}
