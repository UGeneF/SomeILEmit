using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sandbox
{
    public class Demo
    {
        public static void IlSerialization()
        {
            var factory = new SerializerFactory();
            var s1 = (Func<ValueTuple<int, string>, Dictionary<string, object>>) factory.GetSerializer(Case1MethodInfo);
            var s2 =
                (Func<ValueTuple<bool, List<string>>, Dictionary<string, object>>) factory.GetSerializer(
                    Case2MethodInfo);

            var serializedOutput1 = s1(Case1(121));
            var serializedOutput2 = s2(Case2(42));
        }


        private static readonly MethodInfo Case1MethodInfo =
            typeof(Demo).GetMethod(nameof(Case1), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo Case2MethodInfo =
            typeof(Demo).GetMethod(nameof(Case2), BindingFlags.Static | BindingFlags.NonPublic);

        private static (int foo, string bar) Case1(int someInput)
        {
            return (someInput * 42, "Some string");
        }

        private static (bool notDeepSer, List<string> list) Case2(int someInput)
        {
            return (someInput == 42, new List<string>());
        }
    }
}