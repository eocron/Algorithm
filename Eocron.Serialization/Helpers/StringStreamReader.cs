using System;
using System.IO;

namespace Eocron.Serialization.Helpers
{
    internal sealed class StringStreamReader : StreamReader
    {
        private readonly string _input;
        private int _position;

        public StringStreamReader(string input) : base(new ErrorStubStream(new NotSupportedException()))
        {
            _input = input;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (_input == null)
                return 0;

            var len = Math.Min(count, _input.Length - _position);
            if (len <= 0)
                return 0;

            var endIndex = len + index;
            for (var i = index; i < endIndex; i++, _position++)
            {
                buffer[i] = _input[_position];
            }
            return len;
        }
    }
}