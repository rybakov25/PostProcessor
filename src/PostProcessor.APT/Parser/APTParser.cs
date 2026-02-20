using PostProcessor.APT.Lexer;
using PostProcessor.Core.Context;
using PostProcessor.Macros.Interfaces;

namespace PostProcessor.APT.Parser;

public static class APTParser
{
    public static async Task ParseWithMacrosAsync(
        string inputPath,
        PostContext context,
        IMacroEngine macroEngine,
        CancellationToken cancellationToken = default)
    {
        await using var lexer = new StreamingAPTLexer(inputPath);

        await foreach (var command in lexer.ParseStreamAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await macroEngine.ExecuteAsync(context, command, cancellationToken).ConfigureAwait(false);
        }
    }
}
