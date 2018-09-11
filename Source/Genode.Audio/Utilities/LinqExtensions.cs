using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides extra functions to Linq.
/// </summary>
public static class LinqExtensions
{
    /// <summary>
    /// Projects of each element with specified action.
    /// This function does not transform element into a new form.
    /// </summary>
    /// <typeparam name="T">Type of element of the collection.</typeparam>
    /// <param name="source">Source of collection to modify.</param>
    /// <param name="fun">A transform action to apply to each element.</param>
    /// <returns></returns>
    public static IEnumerable<T> Apply<T>(this IEnumerable<T> source, Action<T> fun)
    {
        foreach (var element in source) fun(element);
        return source;
    }
}