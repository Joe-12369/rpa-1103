using System.Text;

namespace II.RPA.Core.Talker.Networking
{
    /// <summary>
    /// Accumulates UTF-8 data received from a stream and returns only complete CRLF-delimited records.
    /// The Decoder keeps an incomplete multibyte character for the next network read.
    /// </summary>
    internal sealed class LineDelimitedUtf8Buffer
    {
        private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
        private readonly StringBuilder _buffer = new StringBuilder();

        public IReadOnlyList<string> Append(ReadOnlySpan<byte> bytes)
        {
            if (!bytes.IsEmpty)
            {
                var charCount = _decoder.GetCharCount(bytes, flush: false);
                if (charCount > 0)
                {
                    var chars = new char[charCount];
                    _decoder.GetChars(bytes, chars, flush: false);
                    _buffer.Append(chars);
                }
            }

            var lines = new List<string>();
            while (true)
            {
                var newlineIndex = _buffer.ToString().IndexOf("\r\n", StringComparison.Ordinal);
                if (newlineIndex < 0)
                {
                    return lines;
                }

                lines.Add(_buffer.ToString(0, newlineIndex));
                _buffer.Remove(0, newlineIndex + 2);
            }
        }
    }
}
