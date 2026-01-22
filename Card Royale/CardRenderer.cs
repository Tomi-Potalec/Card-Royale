using System.Drawing;

public class CardRenderer
{
    public static Bitmap DrawCard(string rank, string suit, bool darkTheme = false)
    {
        int width = 100, height = 150;
        Bitmap bmp = new Bitmap(width, height);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            // Pozadina
            Color bgColor = darkTheme ? Color.FromArgb(30, 30, 30) : Color.White;
            Color borderColor = darkTheme ? Color.LightGray : Color.Black;
            g.FillRectangle(new SolidBrush(bgColor), 0, 0, width, height);
            g.DrawRectangle(new Pen(borderColor, 2), 0, 0, width - 1, height - 1);

            // Oblikovanje teksta
            Font rankFont = new Font("Arial", 20, FontStyle.Bold);
            Font suitFont = new Font("Arial", 30, FontStyle.Regular);

            // Suit color
            Brush suitBrush;
            if (suit == "♥" || suit == "♦")
                suitBrush = Brushes.Red;
            else
                suitBrush = darkTheme ? Brushes.White : Brushes.Black;

            // Poredak za izvlačenje (gore lijevo)
            g.DrawString(rank, rankFont, suitBrush, new PointF(5, 5));
            g.DrawString(suit, suitFont, suitBrush, new PointF(5, 30));

            // Nacrtaj rang (dolje desno, rotirano)
            g.RotateTransform(180);
            g.DrawString(rank, rankFont, suitBrush, new PointF(-width + 5, -height + 5));
            g.DrawString(suit, suitFont, suitBrush, new PointF(-width + 5, -height + 30));
            g.ResetTransform();

            // Veliko središnje odijelo
            g.DrawString(suit, new Font("Arial", 50), suitBrush, new PointF(width / 2 - 25, height / 2 - 30));
        }
        return bmp;
    }
}
