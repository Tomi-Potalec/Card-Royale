namespace Card_Royale
{
    public class MoveRecord
    {
        // Kratki opis karte, npr. "7H" ili "AC"
        public string CardDescription { get; set; }

        // Oznaka izvora poteza (npr. "T3", "W", "A1")
        public string Source { get; set; }

        // Oznaka odredišta (npr. "A2", "T4")
        public string Destination { get; set; }

        // Opis karte koja je bila ispod, ako postoji
        public string BeneathDescription { get; set; }

        // Označava je li potez bio samo okretanje karte (flip)

        public int OriginalTop { get; set; }

        public bool IsFlip { get; set; }

        public MoveRecord(
            string cardDescription,
            string source,
            string destination,
            string beneathDescription = null,
            bool isFlip = false)
        {
            CardDescription = cardDescription;
            Source = source;
            Destination = destination;
            BeneathDescription = beneathDescription;
            IsFlip = isFlip;
        }

    }
}
