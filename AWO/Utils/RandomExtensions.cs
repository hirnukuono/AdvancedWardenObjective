namespace AWO.Utils;

public static class RandomExtensions
{
    public static bool MeetProbability(this Random rand, float prob)
    {
        if (prob >= 1f)
            return true;

        if (prob <= 0f)
            return false;

        return prob >= rand.NextFloat();
    }

    public static float NextRange(this Random rand, float min, float max)
    {
        return (rand.NextFloat() * (max - min)) + min;
    }

    public static float NextFloat(this Random rand)
    {
        return rand.NextSingle();
    }
}
