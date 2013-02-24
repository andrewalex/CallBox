using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using CallBox;

namespace BasicTests
{
    class Program
    {
        static void Debug(string s)
        {
            Console.WriteLine(s);
        }

        static void Main(string[] args)
        {

            PerformanceTest();

            //var hostObject = new RPCTest();
            //hostObject.add += (x, y) => x + y;
            //DoTests(hostObject);
            //DoWTests(hostObject);
            //DoJsonRPCTests(hostObject);

            Console.ReadLine();
        }

        static void WriteResult(byte[] result)
        {

            if (result != null)
                Console.WriteLine(Encoding.UTF8.GetString(result));
            else
                Console.WriteLine("Null Result");

        }

        static public void PerformanceTest()
        {
            int repeats = 10000000;
            TimeSpan baseTime = TimeSpan.MinValue;

            Action<string, Action> test = (n, t) =>
                {
                    Console.Write(n + ":");
                    DateTime startTime;
                    TimeSpan result;
                    startTime = DateTime.Now;
                    for (int j = 0; j < repeats; j++)
                        t();
                    result = DateTime.Now - startTime;
                    var spaces = new String(' ', 20 - n.Length);
                    if (baseTime == TimeSpan.MinValue)
                    {
                        baseTime = result;
                        Console.WriteLine(spaces + result.ToString());
                    }
                    else
                    {
                        Console.WriteLine(String.Format("{0}{1} {2}ns", 
                            spaces,
                            result.ToString(),
                            (result - baseTime).TotalMilliseconds / repeats * 1000000
                            ));
                    }
                };


            RPCTest tester = new RPCTest();
            EventTest et = new EventTest();
            et.ev1 += () => tester.IncI();
            byte[] bytes = Encoding.UTF8.GetBytes("[\"IncI\",null]");
            ClassBox callbox = ClassBox.Get(tester.GetType());
            JsonDecoder d = new JsonDecoder();
            d.Parse(bytes);

            var dynamicFunc = tester.GetType().GetMethod("IncI").CreateDynamicFunc();
            var eventFunc = et.GetType().GetEvent("ev1").CreateDynamicFunc();


            test("Direct", () => tester.IncI());
            test("DynamicFunc", () => dynamicFunc(tester, null));
            test("DynamicFunc(Event)", () => eventFunc(et, null));
            test("CallBox(Direct)", () => callbox.Call(tester, "IncI", null));
            test("CallBox", () => callbox.SerializedCall(tester, d));
            test("CallBox(Full)", () => callbox.SerializedCall(tester, d, bytes));

        }

        static public void DoTests(object hostObject)
        {
            var cb = ClassBox.Get(hostObject.GetType());

            Console.WriteLine("\n***Basic Tests***");
            var d = new JsonDecoder();
            // no param
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[\"IncI\", null]")));
            //return value
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[\"getI\", null]")));
            // param object
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[\"add\", {\"x\": 123, \"y\": 321}]")));
            // complex param
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[\"dofoo\", [{ \"a\": \"happy\", \"b\": 123 }, 7]]")));
            // bad method call
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[\"badCall\", null]")));

        }

        static public void DoWTests(object hostObject)
        {
            var cb = ClassBox.Get(hostObject.GetType());

            Console.WriteLine("\n***W Tests***");
            var d = new JsonWDecoder();

            // no param
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[2, 1, \"IncI\"]")));
            // return value
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[2, 1, \"getI\"]")));
            // param object
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[2, 1, \"add\", {\"x\": 123, \"y\": 321}]")));
            // complex param
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[2, 1, \"dofoo\", [{ \"a\": \"happy\", \"b\": 123 }, 2]]")));
            // bad method call
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[2, 1, \"badCall\"]")));
            // no callback
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "[2, null, \"add\", {\"x\": 123, \"y\": 321}]")));

        }


        static public void DoJsonRPCTests(object hostObject)
        {
            var cb = ClassBox.Get(hostObject.GetType());

            Console.WriteLine("\n***JsonRPC Tests***");
            var d = new JsonRPCDecoder();

            // no param
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "{\"method\": \"IncI\", \"id\": \"frogs\"}")));
            // return value
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "{\"method\": \"getI\", \"id\": 1}")));
            // param object
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "{\"method\": \"add\", \"params\": {\"x\": 123, \"y\": 321}, \"id\": 1}")));
            // complex param
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "{\"method\": \"dofoo\", \"params\": [{ \"a\": \"happy\", \"b\": 123 }, 2], \"id\": 1}")));
            // bad method call
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "{\"method\": \"badCall\", \"id\": 1 }")));
            // no callback
            WriteResult(cb.SerializedCall(hostObject, d, Encoding.UTF8.GetBytes(
                "{\"method\": \"add\", \"params\": {\"x\": 123, \"y\": 321}}")));

        }


        public class RPCTest
        {

            int i = 0;
            // a basic test
            [CallBoxBind]
            public void IncI ()
            {
                i++;
            }

            //public int add (int x, int y)
            //{
            //    return x + y;
            //}


            public delegate int addDelegate(int x, int y);
            [CallBoxBind]
            public event addDelegate add;

            [CallBoxBind]
            public int getI()
            {
                return i;
            }


            // complex return and recieve
            [CallBoxBind]
            public Foo dofoo(Foo foo, string s)
            {
                foo.a = s; 
                return foo;
            }


        }

        
        public class Foo
        {
            public string a { get; set; }
            public string b { get; set; }
        }

        public class EventTest
        {
            public event Action ev1;
        }

    }
}
