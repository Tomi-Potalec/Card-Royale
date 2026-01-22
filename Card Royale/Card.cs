using System.Drawing;

namespace Card_Royale
{
    public class Card
    {
        public SolitairePanel.Suit Suit { get; set; }   // use enum
        public string Rank { get; set; }   // "A", "2", ..., "K"
        public bool IsFaceUp { get; set; } = false;

        public Card(SolitairePanel.Suit suit, string rank)
        {
            Suit = suit;
            Rank = rank;
            IsFaceUp = false;
        }

        // Pretvara vrijednost ranga (Rank) u numeričku vrijednost za usporedbu
        public int GetRankValue()
        {
            switch (Rank)
            {
                case "A": return 1;
                case "2": return 2;
                case "3": return 3;
                case "4": return 4;
                case "5": return 5;
                case "6": return 6;
                case "7": return 7;
                case "8": return 8;
                case "9": return 9;
                case "10": return 10;
                case "J": return 11;
                case "Q": return 12;
                case "K": return 13;
                default: return 0;
            }
        }

       
    }
}
