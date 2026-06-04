using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ToolWheel;
/// <summary>
/// Resolves a <see cref="System.Reflection.MethodInfo"/> from a lambda expression that wraps a delegate creation.
/// The resolver traverses <see cref="System.Linq.Expressions.LambdaExpression"/>,
/// <see cref="System.Linq.Expressions.UnaryExpression"/> and <see cref="System.Linq.Expressions.MethodCallExpression"/>
/// nodes until it reaches the underlying <see cref="System.Linq.Expressions.ConstantExpression"/> that holds the
/// <see cref="System.Reflection.MethodInfo"/>.
/// </summary>
internal class JobManagerMethodExpressionResolver
{
    /// <summary>
    /// Resolves the <see cref="System.Reflection.MethodInfo"/> referenced by the given expression.
    /// </summary>
    /// <param name="expression">The expression tree to resolve the method from.</param>
    /// <returns>The resolved <see cref="System.Reflection.MethodInfo"/>, or <c>null</c> when resolution fails.</returns>
    public static MethodInfo? ResolveMethodCall(Expression expression)
    {
        var currentExpression = expression;

        while (true)
        {
            // The C# compiler wraps lambda expressions in a LambdaExpression.
            if (currentExpression is LambdaExpression lambdaExpression)
            {
                // We are interested in the function body.
                currentExpression = lambdaExpression.Body;
                continue;
            }

            // The compiler may wrap the delegate creation in a UnaryExpression.
            if (currentExpression is UnaryExpression unaryExpression)
            {
                // The operand is expected to be the delegate creation call.
                currentExpression = unaryExpression.Operand;
                continue;
            }

            // If we hit a MethodCallExpression we inspect its Object which may be a constant containing the MethodInfo.
            if (currentExpression is MethodCallExpression methodCallExpression)
            {
                // The object is expected to be a ConstantExpression whose Value is a MethodInfo.
                currentExpression = methodCallExpression.Object;
                // continue;
            }

            // When we have a ConstantExpression containing a MethodInfo we can return it.
            if (currentExpression is ConstantExpression constantExpression && constantExpression.Value is MethodInfo method)
            {
                return method;
            }

            return null;
        }
    }
}
