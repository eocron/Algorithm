using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.EncryptedStreams.Tests;

[TestFixture]
public class PolyvalTests
{
    [Test]
    public void SanityCheck()
    {
        var key = new byte[]{ 62, 23, 186, 150, 174, 4, 205, 59, 153, 134, 158, 86, 240, 173, 191, 58 };
        var msg = new byte[]{ 111, 183, 77, 37, 85, 23, 93, 204, 110, 139, 9, 20, 87, 154, 176, 54, 207, 214, 40, 11, 179, 199, 7, 219, 174, 242, 112, 220, 149, 5, 9, 110 };
        var acc = new byte[16];
        var expectedAcc = new byte[] { 0, 13, 39, 164, 37, 28, 0, 87, 105, 176, 111, 92, 100, 61, 144, 20 };
        Polyval128.Update(key, msg, acc);
        acc.Should().BeEquivalentTo(expectedAcc, Reason(acc));
    }
    
    [Test]
    public void UpdateCheck()
    {
        var key = new byte[]{ 62, 23, 186, 150, 174, 4, 205, 59, 153, 134, 158, 86, 240, 173, 191, 58 };
        var msg = new byte[]{ 111, 183, 77, 37, 85, 23, 93, 204, 110, 139, 9, 20, 87, 154, 176, 54, 207, 214, 40, 11, 179, 199, 7, 219, 174, 242, 112, 220, 149, 5, 9, 110 };
        var acc = new byte[] { 0, 13, 39, 164, 37, 28, 0, 87, 105, 176, 111, 92, 100, 61, 144, 20 };
        var expectedAcc = new byte[] { 0, 169, 49, 45, 114, 49, 1, 53, 121, 235, 225, 14, 115, 196, 71, 17 };
        Polyval128.Update(key, msg, acc);
        acc.Should().BeEquivalentTo(expectedAcc, Reason(acc));
    }

    private static string Reason(byte[] data)
    {
        return $"[{string.Join(", ", data)}]";
    }
}