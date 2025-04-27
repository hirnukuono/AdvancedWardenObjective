namespace AWO.Utils;

public static class NumberExtension
{
    public static bool IsPrime(this int num)
    {
        if (num < 2) return false;
        else if (num % 2 == 0) return num == 2;

        int rad = (int)Math.Sqrt(num);
        for (int i = 3; i <= rad; i += 2)
        {
            if (num % i == 0)
            {
                return false;
            }
        }

        return true;
    }
}
