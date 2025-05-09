using System.Text;
using Markdig.Helpers;
using Markdig.Parsers;

namespace JoinRpg.Markdown;

internal class LinkerParser(string[] prefixes) : InlineParser
{
    private readonly string[] prefixes = prefixes;

    private TextMatchHelper? _textMatchHelper;

    public override void Initialize()
    {
        _textMatchHelper = new TextMatchHelper(new HashSet<string>(prefixes.Select(c => "%" + c)));

        OpeningCharacters = ['%'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        if (_textMatchHelper is null)
        {
            throw new InvalidOperationException("Parser should be initialized before using");
        }

        if (processor.Context is null)
        {
            return false;
        }

        if (slice.Start != 0 && !slice.PeekCharExtra(-1).IsWhitespace())
        {
            return false;
        }
        if (!_textMatchHelper.TryMatch(slice.Text, slice.Start, slice.Length, out var match))
        {
            return false;
        }

        if (match is null)
        {
            return false;
        }

        // Move the cursor to the character after the matched string
        slice.Start += match.Length;

        var builder = new StringBuilder();
        while (slice.CurrentChar.IsDigit() && !slice.IsEmpty)
        {
            _ = builder.Append(slice.CurrentChar);
            slice.Start++;
        }

        var index = builder.Length > 0 && int.TryParse(builder.ToString(), out var i) ? i : 0;

        if (index == 0)
        {
            //If we failed to parse index, abort
            slice.Start -= builder.Length;
            slice.Start -= match.Length;
            return false;
        }

        _ = builder.Clear();

        if (slice.CurrentChar == '(')
        {
            slice.Start++;

            while (slice.CurrentChar != ')' && !slice.IsEmpty)
            {
                _ = builder.Append(slice.CurrentChar);
                slice.Start++;
            }

            if (!slice.IsEmpty)
            {
                slice.Start++;
            }
        }

        var extra = builder.ToString();

        if (processor.Context.Properties.TryGetValue(nameof(ILinkRenderer), out var obj) && obj is ILinkRenderer renderer)
        {
            if (!renderer.LinkTypesToMatch.Contains(match[1..]))
            {
                return false;
            }
            processor.Inline = new EntityLinkInline(match, index, extra, renderer);
            return true;
        }

        return false;
    }
}
