using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CallBox
{

    /// <summary>
    /// JsonRequest will handle the following message requests for 
    ///     [ 2, requestID, methodName, params ]
    /// Success response will look like
    ///     [ 3, requestID, result ]
    /// Exception response will look like
    ///     [ 4, requestID, errorData ]
    /// A null requestID prevents a response
    /// (This is based loosely on WAMP)
    /// </summary>
    public class JsonWDecoder : JsonDecoder
    {
        protected JToken requestId;

        public override void Reset()
        {
            requestId = null;
            base.Reset();
        }

        public override void Parse(byte[] data)
        {
            root = JToken.Parse(Encoding.UTF8.GetString(data));

            if (root.Type != JTokenType.Array)
                throw new Exception("Invalid Message Format");

            JArray a = (JArray)root;
            // requestID
            requestId = a[1];
            // methodName
            if (a.Count < 3)
                throw new Exception("Method Name field missing from message");
            methodName = (string)a[2];
            // arguments
            arguments = (a.Count > 3) ? a[3] : null;
        }


        public override byte[] GetOKResponse(object returnValue)
        {
            if (requestId == null || requestId.Type == JTokenType.Null)
                return null;

            JToken jresult = (returnValue == null) ? new JRaw("null") : JToken.FromObject(returnValue);

            JArray response = new JArray();
            response.Add(3);
            response.Add(requestId);
            response.Add(jresult);

            return Encoding.UTF8.GetBytes(response.ToString(Formatting.None));
        }

        public override byte[] GetExceptionResponse(Exception ex)
        {
            if (requestId == null || requestId.Type == JTokenType.Null)
                return null;

            JToken jresult = JToken.FromObject(ex.Message);
            JArray response = new JArray();
            response.Add(4);
            response.Add(requestId);
            response.Add(jresult);

            return Encoding.UTF8.GetBytes(response.ToString(Formatting.None));
        }

        static public byte[] CreateCallMessage(string callID, string methodName, params object[] args)
        {
            var message = new JArray();
            message.Add(2);
            message.Add(callID);
            message.Add(methodName);
            message.Add(JArray.FromObject(args));
            return Encoding.UTF8.GetBytes(message.ToString(Formatting.None));
        }


        static public byte[] CreateEventMessage(string eventType, object ev)
        {
            JToken jev = JToken.FromObject(ev);
            var message = new JArray();
            message.Add(8);
            message.Add(eventType);
            message.Add(jev);
            return Encoding.UTF8.GetBytes(message.ToString(Formatting.None));
        }
    }
}
