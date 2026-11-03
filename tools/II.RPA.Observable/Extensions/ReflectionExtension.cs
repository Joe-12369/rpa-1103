using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace II.RPA.Observable
{
    public static class ReflectionExtension
    {
        /// <summary>
        /// 使用泛型参数进行类型比较
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="type2"></param>
        /// <returns></returns>
        public static bool Equals<T1>(this Type type2)
        {
            return typeof(T1) == type2;
        }

        /// <summary>
        /// 判断提供的泛型参数是否是当前参数的基类
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="type2"></param>
        /// <returns></returns>
        public static bool HasAncestry<T1>(this Type type2)
        {
            return typeof(T1).IsAssignableFrom(type2);
        }

        /// <summary>
        /// 过滤一个类型的集合，筛选出指定类型的子类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="types"></param>
        /// <returns></returns>
        public static IEnumerable<Type> OfParent<T>(this IEnumerable<Type> types)
        {
            return types.Where(i => i.HasAncestry<T>());
        }

        /// <summary>
        /// 判断指定类型是否为可空类型
        /// </summary>
        /// <param name="targeType"></param>
        /// <returns></returns>
        public static bool IsNullable(this Type targeType)
        {
            return targeType.IsGenericType &&
                   targeType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// 当类型是可空类型时，使用其泛型参数类型作为比较
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static bool EqualsWhenNotNullable<T>(this Type targetType)
        {
            if (targetType.IsNullable())
            {
                var args = targetType.GetGenericArguments();
                return args.Length == 1 && args.First() == typeof(T);
            }

            return targetType.Equals<T>();
        }

        /// <summary>
        /// 如果是一个可空类型，则返回其对应的泛型参数类型
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static Type WhenNotNullable(this Type targetType)
        {
            return targetType.IsNullable()
                ? targetType.GetGenericArguments().First()
                : targetType;
        }

        /// <summary>
        /// 判断类型是否具有指定特性的声明
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool HasAnnotation<T>(this Type type) where T : Attribute
        {
            return type.GetCustomAttributes<T>().Any();
        }
    }
}
