//
// OptionAction.cs  (derived from Mono.Options Options.cs)
//
// Authors:
//  Jonathan Pryor <jpryor@novell.com>, <Jonathan.Pryor@microsoft.com>
//  Federico Di Gregorio <fog@initd.org>
//  Rolf Bjarne Kvinge <rolf@xamarin.com>
//
// Copyright (C) 2008 Novell (http://www.novell.com)
// Copyright (C) 2009 Federico Di Gregorio.
// Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)
// Copyright (C) 2017 Microsoft Corporation (http://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace CodeBrix.ArgumentParser; //was previously: Mono.Options;

/// <summary>
/// Callback signature for options that accept two typed values — typically
/// used for <c>key=value</c>-style options registered via
/// <c>OptionSet.Add&lt;TKey, TValue&gt;</c>.
/// </summary>
/// <typeparam name="TKey">The type of the first (key) value.</typeparam>
/// <typeparam name="TValue">The type of the second (value) value.</typeparam>
/// <param name="key">The converted first value supplied on the command line.</param>
/// <param name="value">The converted second value supplied on the command line.</param>
public delegate void OptionAction<TKey, TValue>(TKey key, TValue value);
