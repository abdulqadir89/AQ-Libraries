namespace AQ.Utilities.Search;

/// <summary>
/// Utilities for fuzzy string matching and similarity calculation
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// Calculates the Levenshtein distance between two strings
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <returns>The edit distance between the strings</returns>
    public static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return string.IsNullOrEmpty(target) ? 0 : target.Length;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;
        var distance = new int[sourceLength + 1, targetLength + 1];

        // Initialize first column and row
        for (var i = 1; i <= sourceLength; i++)
            distance[i, 0] = i;
        for (var j = 1; j <= targetLength; j++)
            distance[0, j] = j;

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[sourceLength, targetLength];
    }

    /// <summary>
    /// Calculates similarity ratio between two strings (0.0 to 1.0)
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <returns>Similarity ratio where 1.0 is identical and 0.0 is completely different</returns>
    public static double SimilarityRatio(string source, string target)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return 1.0;

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        var maxLength = Math.Max(source.Length, target.Length);
        var distance = LevenshteinDistance(source, target);

        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Checks if two strings are similar within the specified threshold
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <param name="threshold">Similarity threshold (0.0 to 1.0)</param>
    /// <param name="ignoreCase">Whether to ignore case</param>
    /// <returns>True if strings are similar above the threshold</returns>
    public static bool IsSimilar(string source, string target, double threshold = 0.6, bool ignoreCase = true)
    {
        if (ignoreCase)
        {
            source = source?.ToLowerInvariant() ?? string.Empty;
            target = target?.ToLowerInvariant() ?? string.Empty;
        }

        return SimilarityRatio(source, target) >= threshold;
    }

    /// <summary>
    /// Calculates Jaro similarity between two strings
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <returns>Jaro similarity score (0.0 to 1.0)</returns>
    public static double JaroSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return 1.0;

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        if (sourceLength == 0 && targetLength == 0)
            return 1.0;

        var matchWindow = Math.Max(sourceLength, targetLength) / 2 - 1;
        if (matchWindow < 0) matchWindow = 0;

        var sourceMatches = new bool[sourceLength];
        var targetMatches = new bool[targetLength];

        var matches = 0;
        var transpositions = 0;

        // Find matches
        for (var i = 0; i < sourceLength; i++)
        {
            var start = Math.Max(0, i - matchWindow);
            var end = Math.Min(i + matchWindow + 1, targetLength);

            for (var j = start; j < end; j++)
            {
                if (targetMatches[j] || source[i] != target[j])
                    continue;

                sourceMatches[i] = true;
                targetMatches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
            return 0.0;

        // Find transpositions
        var k = 0;
        for (var i = 0; i < sourceLength; i++)
        {
            if (!sourceMatches[i])
                continue;

            while (!targetMatches[k])
                k++;

            if (source[i] != target[k])
                transpositions++;

            k++;
        }

        return (matches / (double)sourceLength +
                matches / (double)targetLength +
                (matches - transpositions / 2.0) / matches) / 3.0;
    }

    /// <summary>
    /// Calculates Jaro-Winkler similarity between two strings
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <param name="threshold">Threshold for applying Winkler bonus</param>
    /// <returns>Jaro-Winkler similarity score (0.0 to 1.0)</returns>
    public static double JaroWinklerSimilarity(string source, string target, double threshold = 0.7)
    {
        var jaro = JaroSimilarity(source, target);

        if (jaro < threshold)
            return jaro;

        var prefixLength = 0;
        var maxPrefixLength = Math.Min(4, Math.Min(source.Length, target.Length));

        for (var i = 0; i < maxPrefixLength; i++)
        {
            if (source[i] == target[i])
                prefixLength++;
            else
                break;
        }

        return jaro + 0.1 * prefixLength * (1 - jaro);
    }

    /// <summary>
    /// Generates Soundex code for phonetic matching
    /// </summary>
    /// <param name="input">Input string</param>
    /// <returns>Soundex code</returns>
    public static string Soundex(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "0000";

        input = input.ToUpperInvariant();
        var soundex = input[0].ToString();

        var previousCode = GetSoundexCode(input[0]);

        for (var i = 1; i < input.Length && soundex.Length < 4; i++)
        {
            var currentCode = GetSoundexCode(input[i]);

            if (currentCode != "0" && currentCode != previousCode)
            {
                soundex += currentCode;
            }

            if (currentCode != "0")
                previousCode = currentCode;
        }

        return soundex.PadRight(4, '0');
    }

    private static string GetSoundexCode(char c)
    {
        return c switch
        {
            'B' or 'F' or 'P' or 'V' => "1",
            'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => "2",
            'D' or 'T' => "3",
            'L' => "4",
            'M' or 'N' => "5",
            'R' => "6",
            _ => "0"
        };
    }

    /// <summary>
    /// Calculates a combined fuzzy score using multiple algorithms
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <param name="weights">Weights for different algorithms (Levenshtein, Jaro, JaroWinkler, Soundex)</param>
    /// <returns>Combined fuzzy score (0.0 to 1.0)</returns>
    public static double CombinedFuzzyScore(string source, string target, double[]? weights = null)
    {
        weights ??= [0.3, 0.2, 0.3, 0.2]; // Default weights

        if (weights.Length != 4)
            throw new ArgumentException("Weights array must have exactly 4 elements");

        var levenshteinScore = SimilarityRatio(source, target);
        var jaroScore = JaroSimilarity(source, target);
        var jaroWinklerScore = JaroWinklerSimilarity(source, target);
        var soundexScore = Soundex(source) == Soundex(target) ? 1.0 : 0.0;

        return levenshteinScore * weights[0] +
               jaroScore * weights[1] +
               jaroWinklerScore * weights[2] +
               soundexScore * weights[3];
    }
}
