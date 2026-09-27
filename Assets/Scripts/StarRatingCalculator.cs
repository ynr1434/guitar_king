using UnityEngine;

public static class StarRatingCalculator
{
    public static int Calculate(double accuracy,
        float twoStarThreshold = 50f,
        float threeStarThreshold = 65f,
        float fourStarThreshold = 80f,
        float fiveStarThreshold = 95f)
    {
        if (accuracy >= fiveStarThreshold)
            return 5;
        if (accuracy >= fourStarThreshold)
            return 4;
        if (accuracy >= threeStarThreshold)
            return 3;
        if (accuracy >= twoStarThreshold)
            return 2;
        return 1;
    }

    public static string Format(int stars)
    {
        return BestScoreManager.FormatStars(Mathf.Clamp(stars, 1, 5));
    }
}
