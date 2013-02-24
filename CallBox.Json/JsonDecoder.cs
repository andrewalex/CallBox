using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CallBox
{

    /// <summary>
    /// JsonRequest accepts a simple protocol 
    ///     [ methodName, params ]
    /// Success response will look like
    ///     [ "OK", result ]
    /// Exception response will look like
    ///     [ "ERR", errorData ]
    /// </summary>
    public class JsonDecoder : ICallBoxDecoder
    {

        protected JToken root;
        protected string methodName;
        protected JToken arguments;

        public virtual void Reset()
        {
            root = null;
            methodName = null;
            arguments = null;
        }

        #region ICallBoxRequest
        
        public virtual void Parse(byte[] data)
        {
            Reset();
            root = JToken.Parse(Encoding.UTF8.GetString(data));

            if (root.Type != JTokenType.Array)
                throw new Exception("Invalid Message Format");

            JArray a = (JArray)root;
            if (a.Count != 2)
                throw new Exception("Invalid Message Format.");

            methodName = (string)a[0];
            // arguments
            arguments = a[1];
        }

        public virtual byte[] GetOKResponse(object returnValue)
        {
            JToken jresult = (returnValue == null) ? new JRaw("null") : JToken.FromObject(returnValue);

            JArray response = new JArray();
            response.Add("OK");
            response.Add(jresult);
            return Encoding.UTF8.GetBytes(response.ToString(Formatting.None));
        }

        public virtual byte[] GetExceptionResponse(Exception ex)
        {
            JToken jresult = JToken.FromObject(ex.Message);
            JArray response = new JArray();
            response.Add("ERR");
            response.Add(jresult);
            return Encoding.UTF8.GetBytes(response.ToString(Formatting.None));
        }

        public string GetMethodName()
        {
            return methodName;
        }

        public bool TryGetArgument(int position, string name, Type type, out object result)
        {
            if (arguments == null)
            {
                result = null;
                return false;
            }

            if (arguments.Type == JTokenType.Array)
            {
                JArray argarray = (JArray)arguments;
                if (position < argarray.Count)
                {
                    result = argarray[position].ToObject(type);
                    return true;
                }
            }
            else if (arguments.Type == JTokenType.Object)
            {
                JObject argob = (JObject)arguments;
                result = argob[name].ToObject(type);
                return true;
            }

            result = null;
            return false;
        }


        #endregion


    }
}
