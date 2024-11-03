using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Eocron.Algorithms.HashCode.Algorithms;

namespace Eocron.Algorithms.HashCode;

public sealed class HashBytesStream : Stream, IHashProvider
{
    public HashBytesStream(Stream targetStream, IHashAlgorithmFactory factory, bool leaveOpen)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _name = factory.Name;
        _target = targetStream ?? throw new ArgumentNullException(nameof(targetStream));
        _leaveOpen = leaveOpen;
        _hash = new Lazy<HashAlgorithm>(factory.Create, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override void Flush()
    {
        _target.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var ret = _target.Read(buffer, offset, count);
        _hash.Value.TransformBlock(buffer, offset, ret, buffer, offset);
        return ret;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _target.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _target.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _target.Write(buffer, offset, count);
        _hash.Value.TransformBlock(buffer, offset, count, buffer, offset);
    }

    public override void Close()
    {
        if (!_leaveOpen)
        {
            _target.Close();
        }

        base.Close();
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_leaveOpen)
        {
            await _target.DisposeAsync();
        }

        await base.DisposeAsync();
    }
    
    public HashBytes GetOrCreateHash()
    {
        return InternalGetOrCreateHash();
    }

    public HashBytes GetOrCreateHash(byte[] password)
    {
        return InternalGetOrCreateHash(password);
    }

    public HashBytes GetOrCreateHash(byte[] password, int offset, int count)
    {
        return InternalGetOrCreateHash(password, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _hash.IsValueCreated && !_hashDisposed)
        {
            _hash.Value.Dispose();
            _hashDisposed = true;
        }
        base.Dispose(disposing);
    }

    private HashBytes InternalGetOrCreateHash(byte[] password = null, int? offset = null, int? length = null)
    {
        if (_calculatedHash != null)
            return _calculatedHash;

        password ??= [];
        offset ??= 0;
        length ??= password.Length;

        lock (_sync)
        {
            if (_calculatedHash != null)
                return _calculatedHash;
            _hash.Value.TransformFinalBlock(password, offset.Value, length.Value);
            _calculatedHash = new HashBytes() { Source = _name, Value = _hash.Value.Hash };
        }

        return _calculatedHash;
    }

    public override bool CanRead => _target.CanRead;
    public override bool CanSeek => _target.CanSeek;
    public override bool CanWrite => _target.CanWrite;
    public override long Length => _target.Length;

    public override long Position
    {
        get => _target.Position;
        set => _target.Position = value;
    }

    private bool _hashDisposed;
    private HashBytes _calculatedHash;
    private readonly object _sync = new object();
    private readonly Stream _target;
    private readonly Lazy<HashAlgorithm> _hash;
    private readonly bool _leaveOpen;
    private readonly string _name;

}