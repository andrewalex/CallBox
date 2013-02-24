using System;
using System.Reflection;
using System.Linq;

namespace CallBox.Internal
{
    internal class CallInfoCacheItem
    {
        public CallInfoCacheItem(MethodInfo mi)
        {
            MI = mi;
            this.Call = mi.CreateDynamicFunc();
            EI = null;
            SetParamInfo();
        }

        public CallInfoCacheItem(EventInfo ei)
        {
            this.Call = ei.CreateDynamicFunc();
            EI = ei;
            MI = ei.EventHandlerType.GetMethod("Invoke");
            SetParamInfo();
        }

        private void SetParamInfo()
        {
            var pi = MI.GetParameters();
            ParameterNames = pi.Select(p => p.Name).ToArray();
            ParameterTypes = pi.Select(p => p.ParameterType).ToArray();

            if (MI.ReturnType == typeof(void))
                HasReturn = false;
            else
                HasReturn = true;

        }

        public MethodInfo MI;
        public EventInfo EI;

        public string[] ParameterNames;
        public Type[] ParameterTypes;
        public bool HasReturn;
        public DynamicFunc Call;

    }

}
