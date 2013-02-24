using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace CallBox
{

    /// <summary>
    /// JsonRPCRequest will handle the following message requests for 
    ///     { "method" : methodName, "params" : params, "id": requestId }
    /// Success response will look like
    ///     { "result" : resultData, "id": requestID }
    /// Exception response will look like
    ///     { "error" : errorData, "id": requestID }
    /// A null requestID prevents a response
    /// </summary>
    public class JsonRPCDecoder : JsonDecoder
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

            if (root.Type != JTokenType.Object)
                throw new Exception("Invalid Message Format");

            JObject o = (JObject)root;
            requestId = o["id"];
            methodName = (string)o["method"];
            arguments = o["params"];
        }


        public override byte[] GetOKResponse(object returnValue)
        {
            if (requestId == null || requestId.Type == JTokenType.Null)
                return null;

            JToken jresult = (returnValue == null) ? new JRaw("null") : JToken.FromObject(returnValue);

            JObject response = new JObject();
            response["result"] = jresult;
            response["id"] = requestId;
            return Encoding.UTF8.GetBytes(response.ToString(Formatting.None));
        }

        public override byte[] GetExceptionResponse(Exception ex)
        {
            if (requestId == null || requestId.Type == JTokenType.Null)
                return null;

            JToken jresult = JToken.FromObject(ex.Message);

            JObject response = new JObject();
            response["error"] = jresult;
            response["id"] = requestId;
            return Encoding.UTF8.GetBytes(response.ToString(Formatting.None));
        }

    }
}
