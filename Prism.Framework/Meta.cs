using System.Dynamic;
using System.Reflection;

namespace Prism.Framework;

/// <summary>
/// This tool class is designed for get reflection member information more conveniently.
/// </summary>
/// <typeparam name="TType">Target type.</typeparam>
public static class Meta<TType>
{
    private class PropertyMeta : DynamicObject
    {
        public override IEnumerable<string> GetDynamicMemberNames()
            => typeof(TType).GetProperties().Select(property => property.Name);

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = typeof(TType).GetProperty(binder.Name,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return result != null;
        }
    }
    
    private class MethodMeta : DynamicObject
    {
        public override IEnumerable<string> GetDynamicMemberNames()
        => typeof(TType).GetMethods(
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name);

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? arguments, out object? result)
        {
            if (arguments == null)
                result = typeof(TType).GetMethod(binder.Name);
            else
                result = typeof(TType).GetMethod(binder.Name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    arguments.Select(argument => argument as Type).ToArray()!);
            return result != null;
        }
    }

    private class FieldMeta : DynamicObject
    {
        public override IEnumerable<string> GetDynamicMemberNames()
            => typeof(TType).GetFields().Select(property => property.Name);

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = typeof(TType).GetField(binder.Name,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return result != null;
        }
    }

    private static readonly WeakReference<PropertyMeta> PropertyHolder = new (new PropertyMeta());
    /// <summary>
    /// Get a property of this class.
    /// </summary>
    public static dynamic Property 
    {
        get
        {
            if (PropertyHolder.TryGetTarget(out var target))
                return target;
            target = new PropertyMeta();
            PropertyHolder.SetTarget(target);
            return target;
        }
    }

    private static readonly WeakReference<MethodMeta> MethodHolder = new (new MethodMeta());
    /// <summary>
    /// Get a method of this class.
    /// </summary>
    public static dynamic Method
    {
        get
        {
            if (MethodHolder.TryGetTarget(out var target))
                return target;
            target = new MethodMeta();
            MethodHolder.SetTarget(target);
            return target;
        }
    }

    private static readonly WeakReference<FieldMeta> FieldHolder = new (new FieldMeta());
    /// <summary>
    /// Get a field of this class.
    /// </summary>
    public static dynamic Field 
    {
        get
        {
            if (FieldHolder.TryGetTarget(out var target))
                return target;
            target = new FieldMeta();
            FieldHolder.SetTarget(target);
            return target;
        }
    }
}