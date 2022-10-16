using System;
using System.IO;
using System.Text;

namespace Eocron.Serialization.Helpers
{
    internal sealed class StringStreamWriter : StreamWriter
    {
        private readonly StringBuilder _sb;

        public StringStreamWriter(StringBuilder sb) : base(new ErrorStubStream(new NotSupportedException()))
        {
            _sb = sb;
        }

        public override void WriteLine()
        {
            _sb.AppendLine();
        }

        public override void Write(char value)
        {
            _sb.Append(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _sb.Append(buffer, index, count);
        }

        public override void Write(string value)
        {
            _sb.Append(value);
        }
    }
}