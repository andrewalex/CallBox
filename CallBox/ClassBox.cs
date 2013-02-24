using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CallBox.Internal;

namespace CallBox
{
    public class ClassBox
    {
        #region Static
        private static readonly ConcurrentDictionary<Type, ClassBox> Cache = new ConcurrentDictionary<Type,ClassBox>();


        public static ClassBox Get(Type t)
        {
            ClassBox cb;
            if (Cache.TryGetValue(t, out cb))
                return cb;
            lock (Cache)
            {
                if (Cache.TryGetValue(t, out cb))
                    return cb;

                cb = new ClassBox(t);
                Cache.TryAdd(t, cb);
                return cb;
            }
        }
        #endregion

        public Type BoxedType { get; private set; }

        private Dictionary<string, CallInfoCacheItem> CallMap;

        private ClassBox(Type T)
        {
            BoxedType = T;
            CallMap = new Dictionary<string,CallInfoCacheItem>();

            MapMethods();
            MapEvents();
        }

        private void MapMethods()
        {
            var methods = BoxedType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where((x) => 
                    x.GetCustomAttributes(typeof(CallBoxBindAttribute), true).Length > 0    
                );

            foreach (var mi in methods)
                this.CallMap.Add(mi.Name, new CallInfoCacheItem(mi));

        }

        private void MapEvents()
        {
            var Events = BoxedType.GetEvents()
                .Where( (x) => x
                    .GetCustomAttributes(typeof(CallBoxBindAttribute), true).Length > 0
                );

            foreach (var ev in Events)
                this.CallMap.Add(ev.Name, new CallInfoCacheItem(ev));
        }

        public object Call(object hostObject, string methodName, params object[] arguments)
        {
            CallInfoCacheItem exmi;
            if (CallMap.TryGetValue(methodName, out exmi) == false)
                throw new Exception("MethodName: " + methodName + " not found");

            return exmi.Call(hostObject, arguments);
        }


        public byte[] SerializedCall(object hostObject, ICallBoxDecoder decoder, byte[] data)
        {
            try
            {
                decoder.Parse(data);
            }
            catch(Exception ex)
            {
                return decoder.GetExceptionResponse(ex);
            }
            return SerializedCall(hostObject, decoder);
        }

        public byte[] SerializedCall(object hostObject, ICallBoxDecoder decoder)
        {
            try
            {
                string methodName = decoder.GetMethodName();
                CallInfoCacheItem mici;
                if (CallMap.TryGetValue(methodName, out mici) == false)
                    throw new Exception("MethodName: " + methodName + " not found");

                // build parameters
                object[] callparams = BuildSerializedParameters(decoder, mici);


                object ret = mici.Call(hostObject, callparams);
                //object ret = methodinfo.Invoke(hostObject, BindingFlags.ExactBinding, null, callparams, null);

                if (mici.HasReturn)
                    return decoder.GetOKResponse(ret);
                return null;
            }
            catch (Exception ex)
            {
                return decoder.GetExceptionResponse(ex);
            }
        }

        internal static object[] BuildSerializedParameters(ICallBoxDecoder decoder, CallInfoCacheItem mici)
        {

            MethodInfo methodinfo = mici.MI;
            var paramnames = mici.ParameterNames;
            var paramtypes = mici.ParameterTypes;

            object[] callparams = null;

            if (paramnames.Length != 0)
            {
                callparams = new object[paramnames.Length];
                for (int i = 0; i < callparams.Length; i++)
                {
                    if (decoder.TryGetArgument(i, paramnames[i], paramtypes[i], out callparams[i]) == false)
                    {
                        throw new Exception("Argument missing: " + paramnames[i]);
                    }
                }
            }
            return callparams;
        }

    }
}
