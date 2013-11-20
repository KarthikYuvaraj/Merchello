﻿using System;
using Merchello.Core.Services;
using Umbraco.Core;

namespace Merchello.Core
{
    /// <summary>
    /// Defines the MerchelloContext
    /// </summary>
    public interface IMerchelloContext : IDisposable
    {
        /// <summary>
        /// Umbraco's <see cref="CacheHelper"/>
        /// </summary>
        CacheHelper Cache { get; }

        /// <summary>
        /// The Merchello <see cref="IServiceContext"/>
        /// </summary>
        IServiceContext Services { get; }


        /// <summary>
        /// True/false indicating whether or not <see cref="CoreBootManager"/> has successfully booted
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Compares the binary version to that listed in the Merchello configuration to determine if the 
        /// package was upgraded
        /// </summary>
        bool IsConfigured { get; }

    }
}