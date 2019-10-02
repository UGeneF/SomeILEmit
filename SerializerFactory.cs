using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using GrEmit;

namespace Sandbox
{
    public class SerializerFactory
    {
        private readonly ConcurrentDictionary<MethodInfo, object> _serializers=new ConcurrentDictionary<MethodInfo, object>();
        private MethodInfo _dictSetItemMethod=typeof(Dictionary<string, object>).GetMethod("set_Item", BindingFlags.Public | BindingFlags.Instance);

        public object GetSerializer(MethodInfo methodInfo)
        {
            return _serializers.GetOrAdd(methodInfo, BuildSerializeDelegate);
        }

        private object BuildSerializeDelegate(MethodInfo methodInfo)
        {
            var returnParameter = methodInfo.ReturnParameter;
            var dynamicMethod = GetDynamicMethod(methodInfo, returnParameter);
            
            
            EmitMethod(dynamicMethod,  returnParameter);

            var delegateType = GetDelegateType(returnParameter);
            return dynamicMethod.CreateDelegate(delegateType);
        }

        private Type GetDelegateType(ParameterInfo returnParameter)
        {
            return Expression.GetFuncType(returnParameter.ParameterType, typeof(Dictionary<string, object>));
        }

        private void EmitMethod(DynamicMethod dynamicMethod, ParameterInfo returnParameter)
        {
            using (var il = new GroboIL(dynamicMethod))
            {
                var dict = EmitDictDeclaration(il);
                foreach (var tupleFiled in GetValueTupleFields(returnParameter))
                {
                    LoadDictVariable(il, dict);
                    LoadTupleField(il, tupleFiled);
                    EmitDictItemSet(il);
                }

                EmitReturn(il, dict);
            }
        }

        private void EmitDictItemSet(GroboIL il)
        {
            il.Callnonvirt(_dictSetItemMethod);
        }

        private void LoadDictVariable(GroboIL il, GroboIL.Local dict)
        {
            il.Ldloc(dict);
        }

        private void LoadTupleField(GroboIL il, ValueTupleField tupleFiled)
        {
            il.Ldstr(tupleFiled.NameInUserCode);
            il.Ldarga(0);
            il.Ldfld(tupleFiled.FieldInfo);
            if (tupleFiled.FieldInfo.FieldType.IsValueType)
                il.Box(tupleFiled.FieldInfo.FieldType);
        }


        private void EmitReturn(GroboIL il, GroboIL.Local dict)
        {
            il.Ldloc(dict);
            il.Ret();
        }

        private GroboIL.Local EmitDictDeclaration(GroboIL il)
        {
            il.Newobj(typeof(Dictionary<string, object>).GetConstructor(new Type[0]));
            var dict = il.DeclareLocal(typeof(Dictionary<string, object>), "dict");
            il.Stloc(dict);
            return dict;
        }

        private IList<string> GetTupleFieldNames(ParameterInfo returnParameter)
        {
            return returnParameter.GetCustomAttribute<TupleElementNamesAttribute>().TransformNames;
        }

        private DynamicMethod GetDynamicMethod(MethodInfo methodInfo, ParameterInfo returnParameter)
        {
            return new DynamicMethod(methodInfo.Name + methodInfo.GetHashCode(),
                typeof(Dictionary<string, object>),
                new[] {returnParameter.ParameterType});
        }

        private IEnumerable<ValueTupleField> GetValueTupleFields(ParameterInfo returnParameter)
        {
            var tupleFields = returnParameter.ParameterType.GetFields();
            var tupleFieldNames = GetTupleFieldNames(returnParameter);
            for (int i = 0; i < tupleFields.Length; i++)
            {
                yield return new ValueTupleField()
                {
                    FieldInfo = tupleFields[i],
                    NameInUserCode = tupleFieldNames[i]
                };
            }
        }
        private class ValueTupleField
        {
            public string NameInUserCode { get; set; }
            public FieldInfo FieldInfo { get; set; }
        }
    }
}