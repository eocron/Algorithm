﻿using System;
using Microsoft.Extensions.Hosting;

namespace Eocron.ProxyHost;

public interface IProxyConnection : IHostedService, IDisposable
{
    bool IsHealthy();
}