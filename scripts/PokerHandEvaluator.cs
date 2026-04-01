using System;
using System.Collections.Generic;

public enum PokerHandType
{
    HighCard = 0,
    OnePair = 1,
    TwoPair = 2,
    ThreeOfAKind = 3,
    Straight = 4,
    Flush = 5,
    FullHouse = 6,
    FourOfAKind = 7,
    StraightFlush = 8
}

public readonly struct PokerHandResult
{
    public PokerHandType HandType { get; }
    public string HandName { get; }
    public float DamageMultiplier { get; }
    public int[] WinningCardIndices { get; }

    public PokerHandResult(PokerHandType handType, string handName, float damageMultiplier, int[] winningCardIndices)
    {
        HandType = handType;
        HandName = handName;
        DamageMultiplier = damageMultiplier;
        WinningCardIndices = winningCardIndices;
    }
}

public static class PokerHandEvaluator
{
    public static PokerHandResult Evaluate(IReadOnlyList<CardData> cards)
    {
        if (cards == null || cards.Count != 5)
        {
            return BuildResult(PokerHandType.HighCard, new[] { 0 });
        }

        var rankGroups = BuildRankGroups(cards);
        bool isFlush = IsFlush(cards);
        bool isStraight = IsStraight(cards);

        if (isFlush && isStraight)
        {
            return BuildResult(PokerHandType.StraightFlush, new[] { 0, 1, 2, 3, 4 });
        }

        int[] four = FindGroupWithCount(rankGroups, 4);
        if (four.Length == 4)
        {
            return BuildResult(PokerHandType.FourOfAKind, four);
        }

        int[] three = FindGroupWithCount(rankGroups, 3);
        int[] pair = FindGroupWithCount(rankGroups, 2);
        if (three.Length == 3 && pair.Length == 2)
        {
            return BuildResult(PokerHandType.FullHouse, new[] { 0, 1, 2, 3, 4 });
        }

        if (isFlush)
        {
            return BuildResult(PokerHandType.Flush, new[] { 0, 1, 2, 3, 4 });
        }

        if (isStraight)
        {
            return BuildResult(PokerHandType.Straight, new[] { 0, 1, 2, 3, 4 });
        }

        if (three.Length == 3)
        {
            return BuildResult(PokerHandType.ThreeOfAKind, three);
        }

        var pairs = FindAllPairs(rankGroups);
        if (pairs.Count >= 2)
        {
            var winning = new int[4];
            winning[0] = pairs[0][0];
            winning[1] = pairs[0][1];
            winning[2] = pairs[1][0];
            winning[3] = pairs[1][1];
            return BuildResult(PokerHandType.TwoPair, winning);
        }

        if (pairs.Count == 1)
        {
            return BuildResult(PokerHandType.OnePair, pairs[0]);
        }

        int highCardIndex = FindHighCardIndex(cards);
        return BuildResult(PokerHandType.HighCard, new[] { highCardIndex });
    }

    private static PokerHandResult BuildResult(PokerHandType type, int[] winningIndices)
    {
        string name = type switch
        {
            PokerHandType.StraightFlush => "同花顺",
            PokerHandType.FourOfAKind => "四条",
            PokerHandType.FullHouse => "葫芦",
            PokerHandType.Flush => "同花",
            PokerHandType.Straight => "顺子",
            PokerHandType.ThreeOfAKind => "三条",
            PokerHandType.TwoPair => "两对",
            PokerHandType.OnePair => "一对",
            _ => "高牌"
        };

        float multiplier = type switch
        {
            PokerHandType.StraightFlush => 4.5f,
            PokerHandType.FourOfAKind => 3.6f,
            PokerHandType.FullHouse => 2.9f,
            PokerHandType.Flush => 2.4f,
            PokerHandType.Straight => 2.1f,
            PokerHandType.ThreeOfAKind => 1.8f,
            PokerHandType.TwoPair => 1.45f,
            PokerHandType.OnePair => 1.2f,
            _ => 1.0f
        };

        return new PokerHandResult(type, name, multiplier, winningIndices);
    }

    private static Dictionary<int, List<int>> BuildRankGroups(IReadOnlyList<CardData> cards)
    {
        var groups = new Dictionary<int, List<int>>();
        for (int i = 0; i < cards.Count; i++)
        {
            int rank = RankToValue(cards[i].Rank);
            if (!groups.TryGetValue(rank, out var indices))
            {
                indices = new List<int>();
                groups[rank] = indices;
            }
            indices.Add(i);
        }
        return groups;
    }

    private static bool IsFlush(IReadOnlyList<CardData> cards)
    {
        CardSuit suit = cards[0].Suit;
        for (int i = 1; i < cards.Count; i++)
        {
            if (cards[i].Suit != suit)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsStraight(IReadOnlyList<CardData> cards)
    {
        var values = new int[5];
        for (int i = 0; i < cards.Count; i++)
        {
            values[i] = RankToValue(cards[i].Rank);
        }

        Array.Sort(values);

        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] == values[i - 1])
            {
                return false;
            }
        }

        bool normalStraight = true;
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] - values[i - 1] != 1)
            {
                normalStraight = false;
                break;
            }
        }

        if (normalStraight)
        {
            return true;
        }

        return values[0] == 2 && values[1] == 3 && values[2] == 4 && values[3] == 5 && values[4] == 14;
    }

    private static int[] FindGroupWithCount(Dictionary<int, List<int>> rankGroups, int count)
    {
        foreach (var kv in rankGroups)
        {
            if (kv.Value.Count == count)
            {
                return kv.Value.ToArray();
            }
        }
        return Array.Empty<int>();
    }

    private static List<int[]> FindAllPairs(Dictionary<int, List<int>> rankGroups)
    {
        var pairList = new List<int[]>();
        foreach (var kv in rankGroups)
        {
            if (kv.Value.Count == 2)
            {
                pairList.Add(kv.Value.ToArray());
            }
        }

        if (pairList.Count > 1)
        {
            pairList.Sort((a, b) => b[0].CompareTo(a[0]));
        }

        return pairList;
    }

    private static int FindHighCardIndex(IReadOnlyList<CardData> cards)
    {
        int bestIndex = 0;
        int bestRank = RankToValue(cards[0].Rank);
        for (int i = 1; i < cards.Count; i++)
        {
            int current = RankToValue(cards[i].Rank);
            if (current > bestRank)
            {
                bestRank = current;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private static int RankToValue(CardRank rank)
    {
        return rank switch
        {
            CardRank.Ace => 14,
            CardRank.Two => 2,
            CardRank.Three => 3,
            CardRank.Four => 4,
            CardRank.Five => 5,
            CardRank.Six => 6,
            CardRank.Seven => 7,
            CardRank.Eight => 8,
            CardRank.Nine => 9,
            CardRank.Ten => 10,
            CardRank.Jack => 11,
            CardRank.Queen => 12,
            CardRank.King => 13,
            _ => 0
        };
    }
}
