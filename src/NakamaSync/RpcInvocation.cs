/**
* Copyright 2021 The Nakama Authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NakamaSync
{
    internal class RpcInvocation
    {
        private readonly object _target;
        private readonly MethodInfo _method;
        private readonly object[] _parameters;

        public static RpcInvocation Create(object target, string methodName, object[] parameters)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (method == null)
            {
                throw new NullReferenceException($"Could not find method with name: {methodName} on object {target}");
            }

            return new RpcInvocation(target, method, parameters);
        }

        private RpcInvocation(object target, MethodInfo method, object[] parameters)
        {
            if (target == null)
            {
                throw new ArgumentException("Cannot construct rpc with null target.");
            }

            if (method == null)
            {
                throw new ArgumentException("Cannot construct rpc with null method.");
            }

            _target = target;
            _method = method;
            _parameters = parameters;
        }


        public void Invoke()
        {
            var processedParameters = new object[_parameters.Length];

            ParameterInfo[] methodParams = _method.GetParameters();

            for (int i = 0; i < methodParams.Length; i++)
            {
                ParameterInfo methodParam = methodParams[i];
                object param = _parameters[i];

                // only used by local rpcs

                var converter = GetImplicitConverter(baseType: param.GetType(), targetType: methodParam.ParameterType);

                if (converter != null)
                {
                    param = converter.Invoke(null, new[] {param});
                }

                bool serializedAsGenericDict = param is IDictionary<string,object>;
                bool rpcExpectGenericDict = methodParam.ParameterType == typeof(IDictionary<string, object>);

                // tinyjson processes anonymous objects as dictionaries
                if (serializedAsGenericDict && !rpcExpectGenericDict)
                {
                    param = ParamToObject(param as IDictionary<string, object>, methodParam.ParameterType);
                }

                processedParameters[i] = param;
            }

            System.Console.WriteLine("invoking rpc method with num params: " + processedParameters.Length);

            _method.Invoke(_target, processedParameters);
        }

        private static object ParamToObject(IDictionary<string, object> parameter, Type t)
        {
            var obj = System.Activator.CreateInstance(t);

            foreach (var item in parameter)
            {
                if (t.GetProperty(item.Key) != null)
                {
                    t.GetProperty(item.Key).SetValue(obj, item.Value, null);
                    continue;
                }

                if (t.GetField(item.Key) != null)
                {
                    try
                    {
                        // todo do this for properties too?
                        t.GetField(item.Key).SetValue(obj, System.Convert.ChangeType(item.Value, t.GetField(item.Key).FieldType));
                    }
                    catch
                    {
                        System.Console.WriteLine("Could not convert key " + item.Key);
                        throw;
                    }
                    continue;
                }
            }

            return obj;
        }

        public static MethodInfo GetImplicitConverter(Type baseType, Type targetType)
        {
            return baseType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "op_Implicit" && method.ReturnType == targetType)
                .FirstOrDefault(method => {
                    ParameterInfo param = method.GetParameters().FirstOrDefault();
                    return param != null && param.ParameterType == baseType;
                });
        }
    }
}
