using System;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;

namespace CallBox
{

    /// <summary>
    /// A Basic Delegate Type that should be able to encapsulate most delegates and methods
    /// </summary>
    /// <param name="target">object instance to call this against</param>
    /// <param name="arguments">arguments to pass</param>
    /// <returns>the delegates return, or null if void</returns>
    public delegate object DynamicFunc(object target, params object[] arguments);

    public static class DynamicFuncExtensions
    {

        /// <summary>
        /// Creates a Dynamic Function from a delegate
        /// </summary>
        /// <param name="d"></param>
        /// <returns>Delegate of type DynamicFunc</returns>
        public static DynamicFunc CreateDynamicFunc(this Delegate d)
        {
            return d.Method.CreateDynamicFunc();
        }

        /// <summary>
        /// Creates a Dynamic Function from a MethodInfo for a specified Method
        /// </summary>
        /// <param name="mi"></param>
        /// <returns>DynamicFunc Delegate wrapping a call to the Method</returns>
        public static DynamicFunc CreateDynamicFunc(this MethodInfo mi)
        {
            var instanceEx = Expression.Parameter(typeof(object), "instance");
            var argumentsEx = Expression.Parameter(typeof(object[]), "arguments");

            var paramMapper = ParamMapper(mi, argumentsEx);

            MethodCallExpression call;
            if(mi.IsStatic)
                call = Expression.Call(mi, paramMapper);
            else
                call = Expression.Call(
                Expression.Convert(instanceEx, mi.DeclaringType),
                mi, paramMapper);

            Expression wrappedcall = WrapReturn(mi, call);
            return Expression.Lambda<DynamicFunc>(wrappedcall, instanceEx, argumentsEx).Compile();
        }

        public static DynamicFunc CreateDynamicFunc(this EventInfo ei)
        {
            LabelTarget returnTarget = Expression.Label();

            var fieldInfo = ei.DeclaringType.GetField(ei.Name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
                throw new Exception("Something went wrong processing this EventInfo");

            var instanceEx = Expression.Parameter(typeof(object), "instance");
            var argumentsEx = Expression.Parameter(typeof(object[]), "arguments");

            MethodInfo mi = ei.EventHandlerType.GetMethod("Invoke");
            var paramMapper = ParamMapper(mi, argumentsEx);

            var fieldvalue = MemberExpression.Field(Expression.Convert(instanceEx, ei.DeclaringType), fieldInfo);

            var call = Expression.Call(fieldvalue, mi, paramMapper);
            var ExitPoint = Expression.Label(mi.ReturnType);

            var fullCall = WrapReturn(mi, Expression.Block(
                Expression.IfThen(
                    Expression.Equal(fieldvalue, Expression.Constant(null)),
                    Expression.Goto(ExitPoint, Expression.Default(mi.ReturnType))
                    ),
                Expression.Label(ExitPoint, call)
            ));

//            Expression wrappedcall = WrapReturn(mi, fullCall);
            return Expression.Lambda<DynamicFunc>(fullCall, instanceEx, argumentsEx).Compile();
        }



        private static Expression[] ParamMapper(MethodInfo mi, Expression argumentsEx)
        {
            return mi.GetParameters().Select((p, i) =>
              Expression.Convert(
                Expression.ArrayIndex(argumentsEx, Expression.Constant(i)), p.ParameterType)).ToArray();

        }

        /// <summary>
        /// This coherse's the return of call into the appropriate return type. Handling void returns 
        /// and valuetypes (which require explicit casting to become objects)
        /// </summary>
        /// <param name="mi"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        private static Expression WrapReturn(MethodInfo mi, Expression call)
        {
            
            if (mi.ReturnType == typeof(void))
                return Expression.Block(call, Expression.Constant(null));
            else if (mi.ReturnType.IsValueType)
                return Expression.Convert(call, typeof(object));
            else
                return call;

        }
    }
}
