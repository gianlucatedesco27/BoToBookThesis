using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoToBookClient.Infrastructure
{
    public interface IChatbotWrapper
    {
        Task<(string, List<string>)> CreateRandomStory(string name);
        Task<(string, List<string>)> CreateCustomStory(string name, string friend, string setting, string antagonist);
        Task<byte[]> GeneratePDF(string text, List<string> imagesUrl);
    }
}
