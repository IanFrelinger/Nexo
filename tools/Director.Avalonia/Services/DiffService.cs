namespace Director.Avalonia.Services;

public class DiffService
{
    public string GenerateDiff(string original, string modified)
    {
        // Simple diff implementation - in a real app you'd use a proper diff library
        if (original == modified)
            return "No changes";

        var lines1 = original.Split('\n');
        var lines2 = modified.Split('\n');

        var diff = new System.Text.StringBuilder();
        
        for (int i = 0; i < Math.Max(lines1.Length, lines2.Length); i++)
        {
            var line1 = i < lines1.Length ? lines1[i] : "";
            var line2 = i < lines2.Length ? lines2[i] : "";

            if (line1 != line2)
            {
                if (i < lines1.Length)
                    diff.AppendLine($"- {line1}");
                if (i < lines2.Length)
                    diff.AppendLine($"+ {line2}");
            }
            else
            {
                diff.AppendLine($"  {line1}");
            }
        }

        return diff.ToString();
    }

    public bool HasChanges(string original, string modified)
    {
        return original != modified;
    }
}
