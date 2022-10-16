using System;
using System.IO;

namespace Eocron.Serialization.Helpers
{
    internal sealed class ErrorStubStream : Stream
    {
        private readonly Exception _exception;

        public ErrorStubStream(Exception exception = null)
        {
            _exception = exception ?? new NotSupportedException("Stream write/read is not supported in this serialization mode.");
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw _exception;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw _exception;
        }

        public override void SetLength(long value)
        {
            throw _exception;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw _exception;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; }
    }
}