using System.IO;
using System.Text;

namespace Eocron.Serialization.Helpers
{
    internal sealed class StringStreamWriter : StreamWriter
    {
        public StringStreamWriter(StringBuilder sb) : base(new ErrorStubStream())
        {
            _sb = sb;
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

        public override void WriteLine()
        {
            _sb.AppendLine();
        }

        private readonly StringBuilder _sb;
    }
}