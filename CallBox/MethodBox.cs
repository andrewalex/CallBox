using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using CallBox.Internal;

namespace CallBox
{
    public class MethodBox
    {

        class MethodInfoCacheItemEx : CallInfoCacheItem
        {

            public MethodInfoCacheItemEx(MethodInfo mi)
                : base(mi)
            {
                this.Instance = null;
            }

            public MethodInfoCacheItemEx(MethodInfo mi, object instance)
                :base(mi)
            {
                this.Instance = instance;
            }

            public object Instance;
        }

        private Dictionary<string, MethodInfoCacheItemEx> MethodMap;

        private MethodBox()
        {
            MethodMap = new Dictionary<string, MethodInfoCacheItemEx>();
        }

        public void Add(Type T)
        {
            Add(T, "");
        }


        public void Add(Type T, string prefix)
        {
            var methods = T.GetMethods(BindingFlags.Public |
                BindingFlags.Static)
                .Where((x) => x.IsSpecialName == false && 
                              x.IsGenericMethod == false &&
                              x.GetCustomAttributes(typeof(CallBoxBindAttribute), false).Length == 0 &&
                              x.DeclaringType != typeof(object)
                        );

            foreach (var mi in methods)
                this.MethodMap.Add(prefix + mi.Name, new MethodInfoCacheItemEx(mi));
        }

        public void Add(string name, Delegate d)
        {
            this.MethodMap.Add(name, new MethodInfoCacheItemEx(d.Method));
        }

        public void Add(object instance)
        {
            Add(instance, "");
        }

        public void Add<T>(T instance, string prefix)
        {
            var methods = typeof(T).GetMethods(BindingFlags.Public |
                BindingFlags.Instance)
                .Where((x) => x.IsSpecialName == false &&
                              x.IsGenericMethod == false 
                        );

            foreach (var mi in methods)
                this.MethodMap.Add(prefix + mi.Name, new MethodInfoCacheItemEx(mi, instance));

        }

        public bool Remove(string methodName)
        {
            return this.MethodMap.Remove(methodName);
        }

        public ICollection<string> GetMethodList()
        {

            return this.MethodMap.Keys;
        }

        public object Call(string methodName, params object[] arguments)
        {
            MethodInfoCacheItemEx mici;
            if (MethodMap.TryGetValue(methodName, out mici) == false)
                throw new Exception("MethodName: " + methodName + " not found");
            return mici.Call(mici.Instance, arguments);
        }

        public byte[] SerializedCall(ICallBoxDecoder decoder, byte[] data)
        {
            try
            {
                decoder.Parse(data);
            }
            catch (Exception ex)
            {
                return decoder.GetExceptionResponse(ex);
            }
            return SerializedCall(decoder);
            
        }

        public byte[] SerializedCall(ICallBoxDecoder decoder)
        {
            try
            {
                string methodName = decoder.GetMethodName();
                MethodInfoCacheItemEx mici;
                if (MethodMap.TryGetValue(methodName, out mici) == false)
                    throw new Exception("MethodName: " + methodName + " not found");

                // build parameters
                object[] callparams = ClassBox.BuildSerializedParameters(decoder, mici);

                object ret = mici.Call(mici.Instance, callparams);

                if (mici.HasReturn)
                    return decoder.GetOKResponse(ret);
                return null;
            }
            catch (Exception ex)
            {
                return decoder.GetExceptionResponse(ex);
            }
        }




    }
}
