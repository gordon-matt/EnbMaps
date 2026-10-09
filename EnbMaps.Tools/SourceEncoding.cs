using System.Text;

namespace EnbMaps.Tools;

/// <summary>
/// The original pages declare ISO-8859-1 but are mostly UTF-8 (database content), with a few stray Latin-1 bytes
/// from PHP string literals (e.g. "NPC´s"). This decodes UTF-8 and maps any invalid byte to its Latin-1 character.
/// </summary>
public static class SourceEncoding
{
    public static readonly Encoding Instance = Encoding.GetEncoding("utf-8", EncoderFallback.ReplacementFallback, new Latin1DecoderFallback());

    private sealed class Latin1DecoderFallback : DecoderFallback
    {
        public override int MaxCharCount => 4;

        public override DecoderFallbackBuffer CreateFallbackBuffer() => new Latin1DecoderFallbackBuffer();
    }

    private sealed class Latin1DecoderFallbackBuffer : DecoderFallbackBuffer
    {
        private char[] chars = [];
        private int position;

        public override int Remaining => chars.Length - position;

        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            chars = bytesUnknown.Select(b => (char)b).ToArray();
            position = 0;
            return chars.Length > 0;
        }

        public override char GetNextChar() => position < chars.Length ? chars[position++] : '\0';

        public override bool MovePrevious()
        {
            if (position == 0)
            {
                return false;
            }

            position--;
            return true;
        }

        public override void Reset()
        {
            chars = [];
            position = 0;
        }
    }
}
