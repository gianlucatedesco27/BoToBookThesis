using BoToBookClient.Infrastructure;
using Microsoft.AspNetCore.Mvc;


namespace BoToBook.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StoryController : ControllerBase
    {
        private readonly ILogger<StoryController> _logger;
        private readonly IChatbotWrapper _boToBookWrapper;

        public StoryController(ILogger<StoryController> logger, IChatbotWrapper boToBookWrapper)
        {
            this._boToBookWrapper = boToBookWrapper;
            _logger = logger;
        }

        [HttpPost("RandomStory")]
        public async Task<IActionResult> CreateStory([FromHeader] string heroName)
        {
            try
            {
                var infoStory = await _boToBookWrapper.CreateRandomStory(heroName);
                byte[] pdf = await _boToBookWrapper.GeneratePDF(infoStory.Item1, infoStory.Item2);

                return File(pdf, "application/pdf", $"La storia di {heroName}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception {ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpPost("CustomStory")]
        public async Task<IActionResult> CreateCustomStory([FromBody] StorySummary storySummary)
        {
            try
            {
                var infoStory = await _boToBookWrapper.CreateCustomStory(storySummary.Hero, storySummary.Friend, storySummary.Setting, storySummary.Antagonist);
                byte[] pdf = await _boToBookWrapper.GeneratePDF(infoStory.Item1, infoStory.Item2);

                return File(pdf, "application/pdf", $"La storia di {storySummary.Hero}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception {ex.Message}");
                return StatusCode(500);
            }
        }
    }
}

public class StorySummary
{
    public string Hero { get; set; }
    public string? Antagonist { get; set; }
    public string? Friend { get; set; }
    public string? Setting { get; set; }
}

public class CustomResponse
{
    public string FileContent { get; set; }
    public string StoryText { get; set; }
}