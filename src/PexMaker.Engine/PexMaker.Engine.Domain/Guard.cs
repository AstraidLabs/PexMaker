namespace PexMaker.Engine.Domain;

public static class Guard
{
    public static T NotNull<T>(T? value, string name) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }

        return value;
    }

    public static string NotNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} cannot be null or whitespace", name);
        }

        return value;
    }

    public static int InRange(int value, int min, int max, string name)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be between {min} and {max}");
        }

        return value;
    }

    public static double InRange(double value, double min, double max, string name)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be between {min} and {max}");
        }

        return value;
    }
}
