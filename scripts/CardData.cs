public enum CardSuit
{
    Spade = 0,
    Heart = 1,
    Club = 2,
    Diamond = 3
}

public enum CardRank
{
    Ace = 0,
    Two = 1,
    Three = 2,
    Four = 3,
    Five = 4,
    Six = 5,
    Seven = 6,
    Eight = 7,
    Nine = 8,
    Ten = 9,
    Jack = 10,
    Queen = 11,
    King = 12
}

public readonly struct CardData
{
    public CardSuit Suit { get; }
    public CardRank Rank { get; }

    public CardData(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public string Id => Suit.ToString() + "_" + Rank.ToString();

    public string DisplayText => SuitToShort(Suit) + RankToShort(Rank);

    private static string SuitToShort(CardSuit suit)
    {
        return suit switch
        {
            CardSuit.Spade => "S",
            CardSuit.Heart => "H",
            CardSuit.Club => "C",
            CardSuit.Diamond => "D",
            _ => "?"
        };
    }

    private static string RankToShort(CardRank rank)
    {
        return rank switch
        {
            CardRank.Ace => "A",
            CardRank.Two => "2",
            CardRank.Three => "3",
            CardRank.Four => "4",
            CardRank.Five => "5",
            CardRank.Six => "6",
            CardRank.Seven => "7",
            CardRank.Eight => "8",
            CardRank.Nine => "9",
            CardRank.Ten => "10",
            CardRank.Jack => "J",
            CardRank.Queen => "Q",
            CardRank.King => "K",
            _ => "?"
        };
    }
}
