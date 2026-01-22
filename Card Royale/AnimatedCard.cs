using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Card_Royale
{
    internal class AnimatedCard
    {

        public Card Card;
        public RectangleF Position;   // Current position on the panel
        public Bitmap Image;          // Cached image
        public bool IsDragging;
        public PointF Velocity;       // For animations (win/fall)
        public Panel OriginalParent;  // Where it came from
        public int OriginalIndex;
    }
}
