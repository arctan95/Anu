using System.Text.RegularExpressions;

namespace Anu.Core.Utilities;

public class StringExtensions
{
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var snake = Regex.Replace(
            input,
            @"([a-z0-9])([A-Z])",
            "$1_$2"
        );

        return snake.ToLower();
    }
}