//
// OptionValueCollection.cs  (derived from Mono.Options Options.cs)
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

using System;
using System.Collections;
using System.Collections.Generic;

namespace CodeBrix.ArgumentParser.Options; //was previously: Mono.Options;

/// <summary>
/// Ordered collection of values accumulated for a single <see cref="Option"/>
/// during command-line parsing. Implements both <see cref="IList"/> and
/// <see cref="IList{T}"/> of <see cref="string"/> for convenient indexed
/// and enumerated access from option callbacks.
/// </summary>
public class OptionValueCollection : IList, IList<string>
{
    List<string> values = new List<string>();
    OptionContext c;

    internal OptionValueCollection(OptionContext c)
    {
        this.c = c;
    }

    #region ICollection
    void ICollection.CopyTo(Array array, int index) { (values as ICollection).CopyTo(array, index); }
    bool ICollection.IsSynchronized { get { return (values as ICollection).IsSynchronized; } }
    object ICollection.SyncRoot { get { return (values as ICollection).SyncRoot; } }
    #endregion

    #region ICollection<T>
    /// <summary>Adds <paramref name="item"/> to the end of the collection.</summary>
    /// <param name="item">The value to add.</param>
    public void Add(string item) { values.Add(item); }

    /// <summary>Removes all values from the collection.</summary>
    public void Clear() { values.Clear(); }

    /// <summary>Determines whether the collection contains <paramref name="item"/>.</summary>
    /// <param name="item">The value to locate.</param>
    /// <returns><c>true</c> if the value is found; otherwise <c>false</c>.</returns>
    public bool Contains(string item) { return values.Contains(item); }

    /// <summary>Copies the contents of the collection to <paramref name="array"/>, starting at <paramref name="arrayIndex"/>.</summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The starting index in <paramref name="array"/>.</param>
    public void CopyTo(string[] array, int arrayIndex) { values.CopyTo(array, arrayIndex); }

    /// <summary>Removes the first occurrence of <paramref name="item"/> from the collection.</summary>
    /// <param name="item">The value to remove.</param>
    /// <returns><c>true</c> if removed; <c>false</c> if not found.</returns>
    public bool Remove(string item) { return values.Remove(item); }

    /// <summary>Gets the number of values in the collection.</summary>
    public int Count { get { return values.Count; } }

    /// <summary>Gets a value indicating whether the collection is read-only. Always <c>false</c>.</summary>
    public bool IsReadOnly { get { return false; } }
    #endregion

    #region IEnumerable
    IEnumerator IEnumerable.GetEnumerator() { return values.GetEnumerator(); }
    #endregion

    #region IEnumerable<T>
    /// <summary>Returns an enumerator over the values in the collection.</summary>
    /// <returns>A string enumerator.</returns>
    public IEnumerator<string> GetEnumerator() { return values.GetEnumerator(); }
    #endregion

    #region IList
    int IList.Add(object value) { return (values as IList).Add(value); }
    bool IList.Contains(object value) { return (values as IList).Contains(value); }
    int IList.IndexOf(object value) { return (values as IList).IndexOf(value); }
    void IList.Insert(int index, object value) { (values as IList).Insert(index, value); }
    void IList.Remove(object value) { (values as IList).Remove(value); }
    void IList.RemoveAt(int index) { (values as IList).RemoveAt(index); }
    bool IList.IsFixedSize { get { return false; } }
    object IList.this[int index] { get { return this[index]; } set { (values as IList)[index] = value; } }
    #endregion

    #region IList<T>
    /// <summary>Returns the zero-based index of <paramref name="item"/>, or -1 if not present.</summary>
    /// <param name="item">The value to locate.</param>
    /// <returns>The index of the value, or -1.</returns>
    public int IndexOf(string item) { return values.IndexOf(item); }

    /// <summary>Inserts <paramref name="item"/> at the specified <paramref name="index"/>.</summary>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="item">The value to insert.</param>
    public void Insert(int index, string item) { values.Insert(index, item); }

    /// <summary>Removes the value at the specified <paramref name="index"/>.</summary>
    /// <param name="index">The zero-based index.</param>
    public void RemoveAt(int index) { values.RemoveAt(index); }

    private void AssertValid(int index)
    {
        if (c.Option == null)
            throw new InvalidOperationException("OptionContext.Option is null.");
        if (index >= c.Option.MaxValueCount)
            throw new ArgumentOutOfRangeException("index");
        if (c.Option.OptionValueType == OptionValueType.Required &&
                index >= values.Count)
            throw new OptionException(string.Format(
                        c.OptionSet.MessageLocalizer("Missing required value for option '{0}'."), c.OptionName),
                    c.OptionName);
    }

    /// <summary>
    /// Gets or sets the value at the specified <paramref name="index"/>.
    /// When getting, returns <c>null</c> if the current option expects more
    /// values than have yet been supplied (and is not a required option).
    /// </summary>
    /// <param name="index">The zero-based value index.</param>
    /// <returns>The value at <paramref name="index"/>, or <c>null</c>.</returns>
    public string this[int index]
    {
        get
        {
            AssertValid(index);
            return index >= values.Count ? null : values[index];
        }
        set
        {
            values[index] = value;
        }
    }
    #endregion

    /// <summary>Returns a shallow copy of the values as a new <see cref="List{T}"/>.</summary>
    /// <returns>A new list containing the current values.</returns>
    public List<string> ToList()
    {
        return new List<string>(values);
    }

    /// <summary>Returns the values as a new array.</summary>
    /// <returns>A string array of the current values.</returns>
    public string[] ToArray()
    {
        return values.ToArray();
    }

    /// <summary>Returns a comma-separated string of all values.</summary>
    /// <returns>A diagnostic string joining the values with <c>", "</c>.</returns>
    public override string ToString()
    {
        return string.Join(", ", values.ToArray());
    }
}
