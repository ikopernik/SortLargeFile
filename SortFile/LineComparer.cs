using System;
using System.Collections.Generic;

public class LineComparer : IComparer<(string Line, int ChunkIndex)>
{
    public int Compare((string Line, int ChunkIndex) x, (string Line, int ChunkIndex) y)
    {
        var xParts = x.Line.Split(new[] { '.' }, 2);
        var yParts = y.Line.Split(new[] { '.' }, 2);
        var xTrimmed = xParts[1].Trim();
        var yTrimmed = yParts[1].Trim();
        int textComparison = string.Compare(xParts[1].Trim(), yParts[1].Trim(), StringComparison.OrdinalIgnoreCase);
        if (textComparison == 0)
        {
            int xNumber = int.Parse(xParts[0]);
            int yNumber = int.Parse(yParts[0]);

            return xNumber.CompareTo(yNumber);
        }
        return textComparison;
    }
}