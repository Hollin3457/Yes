using System;

namespace NUHS.Common
{
    public static class SpanExtensions
    {
        /// <summary>
        /// Splits the span by the given separator, removing empty segments.
        /// </summary>
        /// <param name="span">The span to split.</param>
        /// <param name="separator">The separator to split the span on.</param>
        /// <returns>An enumerator over the span segments.</returns>
        public static StringSplitEnumerator Split(this ReadOnlySpan<char> span, ReadOnlySpan<char> separator) =>
            new StringSplitEnumerator(span, separator);

        public ref struct StringSplitEnumerator
        {
            private readonly ReadOnlySpan<char> _separator;
            private ReadOnlySpan<char> _span;

            public StringSplitEnumerator(ReadOnlySpan<char> span, ReadOnlySpan<char> separator)
            {
                _span = span;
                _separator = separator;
                Current = default;
            }

            public bool MoveNext()
            {
                if (_span.Length == 0)
                {
                    return false;
                }

                var index = _span.IndexOf(_separator, StringComparison.Ordinal);
                if (index < 0)
                {
                    Current = _span;
                    _span = default;
                }
                else
                {
                    //Current = _span[..index];
                    //_span = _span[(index + 1)..];
                    Current = _span.Slice(0, index);
                    _span = _span.Slice(index + 1, _span.Length - index - 1);
                }

                if (Current.Length == 0)
                {
                    return MoveNext();
                }

                return true;
            }

            public ReadOnlySpan<char> Current { get; private set; }

            public StringSplitEnumerator GetEnumerator() => this;
        }
    }
}
