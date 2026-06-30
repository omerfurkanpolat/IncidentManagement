namespace IncidentManagement.Shared.Services;

public class DocumentChunker
{
    private const int ChunkSize = 500;
    private const int ChunkOverlap = 50;

    public List<string> Chunk(string text)
    {
        var chunks = new List<string>();
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        var current = new System.Text.StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            if (current.Length + paragraph.Length > ChunkSize && current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                var overlap = current.ToString();
                current.Clear();
                if (overlap.Length > ChunkOverlap)
                    current.Append(overlap[^ChunkOverlap..]);
            }
            current.AppendLine(paragraph);
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return chunks;
    }
}
