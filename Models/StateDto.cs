using System.Collections.Generic;

namespace Kuiz.Models
{
    public class StateDto
    {
        public int questionIndex { get; set; }
        public string? revealed { get; set; }
        public List<string>? buzzOrder { get; set; }
        public Dictionary<string,int>? scores { get; set; }
        public Dictionary<string,int>? mistakes { get; set; }
        public int sessionId { get; set; }
    }
}
