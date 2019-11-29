using System;
using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Expression = System.Linq.Expressions.Expression;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public static class Log
{
	private const int MaxLengthBeforeLineSplitting = 175;
	private const string NameValueSeparator = "=";
	private const string NameStateSeparator = " state: ";
	private const string SingleRowSeparator = ", ";
	private const string MultiRowSeparator = "\n";
	private const bool colorize = true;
	private const string Null = colorize ? "<color=red>null</color>" : "null";
	private const BindingFlags DefaultInstanceBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
	private const BindingFlags DefaultStaticBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

	[Conditional("UNITY_EDITOR")]
	public static void Value([NotNull]Expression<Func<object>> delegateToMember, Object context = null)
	{
		Debug.Log(ToString(delegateToMember), context);
	}

	[Conditional("UNITY_EDITOR")]
	public static void Values([NotNull]params Expression<Func<object>>[] delegateToMembers)
	{
		int count = delegateToMembers.Length;
		var sb = new StringBuilder();
		if(count > 0)
		{
			ToString(delegateToMembers[0], sb);
			for(int n = 1; n < count; n++)
			{
				sb.Append(MultiRowSeparator);
				ToString(delegateToMembers[n], sb);
			}

			if(sb.Length <= MaxLengthBeforeLineSplitting)
			{
				sb.Replace(MultiRowSeparator, SingleRowSeparator);
			}
		}
		Debug.Log(sb.ToString());
	}

	[Conditional("UNITY_EDITOR")]
	public static void State([CanBeNull]object target, BindingFlags flags = DefaultInstanceBindingFlags)
	{
		if(target == null)
		{
			Debug.Log(Null);
			return;
		}
		State(target.GetType(), target, flags);
	}

	[Conditional("UNITY_EDITOR")]
	public static void State([CanBeNull]object target, bool includePrivate, bool includeStatic = false)
	{
		if(target == null)
		{
			Debug.Log(Null);
			return;
		}
		BindingFlags flags;
		if(includePrivate)
		{
			if(includeStatic)
			{
				flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			}
			else
			{
				flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			}
		}
		else if(includeStatic)
		{
			flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
		}
		else
		{
			flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
		}
		State(target.GetType(), target, flags);
	}

	[Conditional("UNITY_EDITOR")]
	public static void State([NotNull]Type classType, BindingFlags flags = DefaultStaticBindingFlags)
	{
		State(classType, null, flags);
	}

	[Conditional("UNITY_EDITOR")]
	private static void State([NotNull]Type classType, [CanBeNull]object target, BindingFlags flags)
	{
		var sb = new StringBuilder();

		sb.Append(classType.Name);
		sb.Append(NameStateSeparator);

		var type = classType;
		do
		{
			var fields = type.GetFields(flags);
			for(int n = 0, count = fields.Length; n < count; n++)
			{
				var field = fields[n];
				if(field.Name[0] != '<') //skip property backing fields
				{
					sb.Append(MultiRowSeparator);
					ToString(target, field, sb);
				}
			}

			var properties = type.GetProperties(flags);
			for(int n = 0, count = properties.Length; n < count; n++)
			{
				var property = properties[n];
				if(property.CanRead)
				{
					sb.Append(MultiRowSeparator);
					ToString(target, property, sb);
				}
			}

			if((flags & BindingFlags.DeclaredOnly) == 0)
			{
				break;
			}
				
			type = type.BaseType;
		}
		// avoid obsolete warnings and excessive number of results by skipping base types such as Component
		while(type != null && type != typeof(object) && (type.Namespace == null || (!string.Equals(type.Namespace, "UnityEngine", StringComparison.Ordinal) && !string.Equals(type.Namespace, "UnityEditor", StringComparison.Ordinal))));

		if(sb.Length <= MaxLengthBeforeLineSplitting)
		{
			sb.Replace(NameStateSeparator + MultiRowSeparator, NameStateSeparator);
			sb.Replace(MultiRowSeparator, SingleRowSeparator);
		}

		Debug.Log(sb.ToString());
	}

	private static void ToString(object fieldOwner, [NotNull]FieldInfo field, StringBuilder sb)
	{
		sb.Append(field.Name);
		sb.Append(NameValueSeparator);
		var value = field.GetValue(fieldOwner);
		ToString(value, sb);
	}

	private static void ToString(object fieldOwner, [NotNull]PropertyInfo property, StringBuilder sb)
	{
		sb.Append(property.Name);
		sb.Append(NameValueSeparator);
		var value = property.GetValue(fieldOwner, null);
		ToString(value, sb);
	}

	private static string ToString([NotNull]Expression<Func<object>> delegateToMember)
	{
		var sb = new StringBuilder();
		ToString(delegateToMember, sb);
		return sb.ToString();
	}

	private static void ToString([NotNull]Expression<Func<object>> delegateToMember, StringBuilder sb)
	{
		MemberExpression memberExpression;
		var body = delegateToMember.Body;
		var unaryExpression = body as UnaryExpression;
		if(unaryExpression != null)
		{
			var operand = unaryExpression.Operand;
			memberExpression = operand as MemberExpression;
			if(memberExpression == null)
			{
				var constantExpression = (ConstantExpression)operand;
				sb.Append("const");
				sb.Append(NameValueSeparator);
				sb.Append(constantExpression.Value);
				return;
			}
		}
		else
		{
			memberExpression = body as MemberExpression;
			if(memberExpression == null)
			{
				var constantExpression = (ConstantExpression)body;
				sb.Append("const");
				sb.Append(NameValueSeparator);
				sb.Append(constantExpression.Value);
				return;
			}
		}

		sb.Append(memberExpression.Member.Name);
		sb.Append(NameValueSeparator);
		ToString(GetValue(memberExpression), sb);
	}

	private static void ToString([CanBeNull]object value, StringBuilder sb)
	{
		if(value == null)
		{
			sb.Append(Null);
			return;
		}

		if(colorize)
		{
			if(value is bool)
			{
				sb.Append((bool)value ? "<color=green>True</color>" : "<color=red>False</color>");
				return;
			}
		}

		var list = value as IList;
		if(list != null)
		{
			int lastIndex = list.Count - 1;
			if(lastIndex == -1)
			{
				sb.Append("[]");
				return;
			}

			sb.Append('[');
			for(int n = 0; n < lastIndex; n++)
			{
				ToString(list[n], sb);
				sb.Append(SingleRowSeparator);
			}
			ToString(list[lastIndex], sb);
			sb.Append(']');
			return;
		}

		sb.Append(value.ToString());
	}

	[CanBeNull]
	private static object GetValue([NotNull]Expression expression)
	{
		if(expression == null)
		{
			Debug.LogError("Log.GetValue provided Expression had a missing argument.");
			return null;
		}

		switch(expression.NodeType)
		{
			case ExpressionType.Constant:
				var constant = (ConstantExpression)expression;
				return constant.Value;
			case ExpressionType.MemberAccess:
				var memberExpression = (MemberExpression)expression;
				var memberInfo = memberExpression.Member;
					
				var field = memberInfo as FieldInfo;
				if(field != null)
				{
					return field.GetValue(field.IsStatic ? null : GetValue(memberExpression.Expression));
				}
					
				var property = memberInfo as PropertyInfo;
				if(property != null)
				{
					if(!property.CanRead)
					{
						Debug.LogError("Log.GetValue expression contained property with no get accessor.");
						return null;
					}
					return property.GetValue(property.GetGetMethod().IsStatic ? null : GetValue(memberExpression.Expression), null);
				}
					
				var method = memberInfo as MethodInfo;
				if(method != null)
				{
					if(method.ReturnType == typeof(void))
					{
						Debug.LogError("Validate.Arguments given argument that was a method with no return type.");
						return typeof(void);
					}

					// TO DO: Support default arguments
					if(method.GetParameters().Length > 0)
					{
						Debug.LogError("Validate.Arguments given argument that was a method with parameters.");
						return method;
					}

					return method.Invoke(GetValue(memberExpression.Expression), null);
				}

				Debug.LogError("Validate.Arguments given argument was of type MemberAccess but was not a field, property or method.");
				return null;
			case ExpressionType.Lambda:

				Debug.Log("Lambda");

				var lambdaExpression = (LambdaExpression)expression;
				var compiled = lambdaExpression.Compile();
				return compiled.DynamicInvoke(null);
			case ExpressionType.Call:
				lambdaExpression = expression as LambdaExpression;
				if(lambdaExpression != null)
				{
					Debug.Log("Lambda");

					compiled = lambdaExpression.Compile();
					return compiled.DynamicInvoke(null);
				}

				var methodCallExpression = (MethodCallExpression)expression;
				method = methodCallExpression.Method;

				Debug.Log("Method " + method.Name);

				if(method == null)
				{
					Debug.LogError("Validate given argument was of type Call but MethodInfo was null.");
					return null;
				}

				if(method.GetParameters().Length > 0)
				{
					Debug.LogError("Validate given argument was of type Call but MethodInfo had parameters.");
					return null;
				}

				return method.Invoke(methodCallExpression.Object, null);
			default:
				Debug.LogError("Validate provided argument was not a constant, MemberAccess or Call: " + expression.NodeType);
				return null;
		}
	}
}