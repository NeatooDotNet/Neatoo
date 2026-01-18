// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file provides IsExternalInit to enable record types in netstandard2.0 projects.
// See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init

#if NETSTANDARD2_0

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    internal static class IsExternalInit { }
}

#endif
