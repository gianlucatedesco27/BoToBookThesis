using BoToBook.Model;


namespace BoToBook
{
    public class Startup
    {
        public Startup()
        {
            var builder = new ConfigurationBuilder()
                      .SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false);

            IConfiguration config = builder.Build();

            OpenAISettings = config.GetSection("OpenAISettings").Get<OpenAISettings>();

        }

        public OpenAISettings OpenAISettings { get; private set; }
    }
}
