using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CallBox
{
    public interface ICallBoxDecoder
    {

        /// <summary>
        /// Method to do the initial parsing of a message
        /// </summary>
        void Parse(byte[] data);

        /// <summary>
        /// Called by the Dispatcher when the method call executes successfully
        /// </summary>
        /// <param name="returnValue">the response from the method call</param>
        /// <returns>The returnValue encapsulated in a response</returns>
        byte[] GetOKResponse(object returnValue);

        /// <summary>
        /// Called by the Dispatcher when the method call throws an exception
        /// </summary>
        /// <param name="ex">Exception thrown during the method call</param>
        /// <returns>The Error Message response</returns>
        byte[] GetExceptionResponse(Exception ex);


        /// <summary>
        /// Returns the name of the method being called. The object passed in is the result from Parse
        /// </summary>
        /// <returns>name of the method being called</returns>
        string GetMethodName();

        /// <summary>
        /// Attempts to get specified argument as the specified Type.
        /// </summary>
        /// <param name="position">Ordered Position to return</param>
        /// <param name="name">name of the argument to return</param>
        /// <param name="type">Type to obtain</param>
        /// <param name="result">result if successful</param>
        /// <returns>true if successful, false if not</returns>
        bool TryGetArgument(int position, string name, Type type, out object result);


    }
}
