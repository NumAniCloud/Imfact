﻿using NUnit.Framework.Constraints;

namespace Imfact.Test.Suite;

internal static class FluentAssertion
{
    public static FluentAssertionContext<T?> OnObject<T>(T? context)
        where T : class
    {
        return new FluentAssertionContext<T?> { Context = context };
    }

    public static FluentAssertionContext<T> NotNull<T>(this FluentAssertionContext<T?> context)
        where T : class
    {
        Assert.That(context.Context, Is.Not.Null);
        if (context.Context is not { } value) throw new Exception();

        return new FluentAssertionContext<T> { Context = value };
    }

	public static void IsNull<T>(this FluentAssertionContext<T?> context)
	    where T : class
	{
        Assert.That(context.Context, Is.Null);
	}
}

internal class FluentAssertionContext<T>
{
    public required T Context { get; init; }

    public FluentAssertionContext<T> AssertThat<TActual>(
        Func<T, TActual> selector,
        Constraint constraint)
    {
        Assert.That(selector(Context), constraint);
        return this;
    }


    public FluentAssertionContext<T> OnObject<TNext>(
        Func<T, TNext> selector,
        Action<FluentAssertionContext<TNext>> assertion)
    {
        assertion(new FluentAssertionContext<TNext> { Context = selector(Context) });
        return this;
    }
}