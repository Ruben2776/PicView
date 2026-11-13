namespace PicView.Core.FileSorting;

/// <summary>
/// Natural (alphanumeric) string comparison that sorts runs of digits by their
/// numeric value instead of by character code. This makes file names such as
/// "img2", "img10" or "968", "969", "9681" sort the way a human expects,
/// matching the behavior Windows already gets from StrCmpLogicalW.
/// </summary>
public static class NaturalStringComparer
{
    public static int Compare(string? a, string? b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }

        if (a is null)
        {
            return b is null ? 0 : -1;
        }

        if (b is null)
        {
            return 1;
        }

        int i = 0;
        int j = 0;
        while (i < a.Length && j < b.Length)
        {
            if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
            {
                int startA = i;
                while (i < a.Length && char.IsDigit(a[i]))
                {
                    i++;
                }

                int startB = j;
                while (j < b.Length && char.IsDigit(b[j]))
                {
                    j++;
                }

                // Strip leading zeros, then compare by digit count first
                // (fewer digits == smaller), then digit by digit.
                string numA = a.Substring(startA, i - startA).TrimStart('0');
                string numB = b.Substring(startB, j - startB).TrimStart('0');
                int lengthCompare = numA.Length.CompareTo(numB.Length);
                if (lengthCompare != 0)
                {
                    return lengthCompare;
                }

                for (int k = 0; k < numA.Length; k++)
                {
                    if (numA[k] != numB[k])
                    {
                        return numA[k] < numB[k] ? -1 : 1;
                    }
                }

                // Numeric parts are equal; continue with the remaining text.
            }
            else
            {
                char ca = a[i];
                char cb = b[j];

                // Case-insensitive comparison first, matching StrCmpLogicalW.
                int caseCompare = char.ToLowerInvariant(ca).CompareTo(char.ToLowerInvariant(cb));
                if (caseCompare != 0)
                {
                    return caseCompare;
                }

                if (ca != cb)
                {
                    int ordinalCompare = ca.CompareTo(cb);
                    if (ordinalCompare != 0)
                    {
                        return ordinalCompare;
                    }
                }

                i++;
                j++;
            }
        }

        return a.Length.CompareTo(b.Length);
    }
}
