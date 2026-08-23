namespace TTMomentViewer.BLL.Helpers;

public sealed class NaturalComparer : IComparer<string>
{
    public static readonly NaturalComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var xIndex = 0;
        var yIndex = 0;

        while (xIndex < x.Length && yIndex < y.Length)
        {
            if (char.IsDigit(x[xIndex]) && char.IsDigit(y[yIndex]))
            {
                var xStart = xIndex;
                var yStart = yIndex;

                while (xIndex < x.Length && char.IsDigit(x[xIndex])) xIndex++;
                while (yIndex < y.Length && char.IsDigit(y[yIndex])) yIndex++;

                var xNumber = TrimLeadingZeros(x.AsSpan(xStart, xIndex - xStart));
                var yNumber = TrimLeadingZeros(y.AsSpan(yStart, yIndex - yStart));

                if (xNumber.Length != yNumber.Length) return xNumber.Length - yNumber.Length;

                var numberResult = xNumber.SequenceCompareTo(yNumber);
                if (numberResult != 0) return numberResult;

                continue;
            }

            var xChar = char.ToUpperInvariant(x[xIndex]);
            var yChar = char.ToUpperInvariant(y[yIndex]);
            if (xChar != yChar) return xChar.CompareTo(yChar);

            xIndex++;
            yIndex++;
        }

        var remainderResult = (x.Length - xIndex) - (y.Length - yIndex);
        return remainderResult != 0 ? remainderResult : string.CompareOrdinal(x, y);
    }

    private static ReadOnlySpan<char> TrimLeadingZeros(ReadOnlySpan<char> digits)
    {
        var start = 0;
        while (start < digits.Length - 1 && digits[start] == '0') start++;
        return digits[start..];
    }
}
