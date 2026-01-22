using Card_Royale;
using System;
using System.Collections.Generic;

public class Deck
{
    private List<Card> cards;
    private Random random = new Random();

    public Deck(bool darkTheme = false)
    {
        cards = new List<Card>();
        string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        Card_Royale.SolitairePanel.Suit[] suits =
        {
            Card_Royale.SolitairePanel.Suit.Hearts,
            Card_Royale.SolitairePanel.Suit.Diamonds,
            Card_Royale.SolitairePanel.Suit.Clubs,
            Card_Royale.SolitairePanel.Suit.Spades
        };

        foreach (var suit in suits)
        {
            foreach (var rank in ranks)
            {
                cards.Add(new Card(suit, rank));
            }
        }
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
    }

    public List<Card> GetAllCards()
    {
        return new List<Card>(cards);
    }
}
