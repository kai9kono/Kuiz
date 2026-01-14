using System.Windows.Media;

namespace Kuiz.Models
{
    public class PlayerState
    {
        public string Name { get; set; } = "";
        public int Score { get; set; }
        public int Correct { get; set; }
        public int Wrong { get; set; }
        public Brush? ColorBrush { get; set; }
        public bool IsDisabled { get; set; }
    }
}
