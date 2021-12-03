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
        private readonly object[] _requiredRemoteParams;
        private readonly object[] _optionalRemoteParams;

        public RpcInvocation(object target, string methodName, object[] requiredRemoteParams, object[] optionalRemoteParams)
        {
            if (target == null)
            {
                System.Console.WriteLine("null target");

                throw new ArgumentException("Cannot construct rpc with null target.");
            }

            if (methodName == null)
            {
                System.Console.WriteLine("null method name");

                throw new ArgumentException("Cannot construct rpc with null method.");
            }

            System.Console.WriteLine("getting method");

            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            System.Console.WriteLine("done getting method");

            if (method == null)
            {
                System.Console.WriteLine("null method");

                throw new NullReferenceException($"Could not find method with name: {methodName} on object {target}");
            }

            _target = target;
            _method = method;
            _requiredRemoteParams = requiredRemoteParams ?? new object[]{};
            _optionalRemoteParams = optionalRemoteParams ?? new object[]{};
        }

        public void Invoke()
        {
            var allLocalParams = _method.GetParameters();
            var requiredLocalParams = new List<ParameterInfo>();
            var optionalLocalParams = new List<ParameterInfo>();

            foreach (var p in allLocalParams)
            {
                var list = p.IsOptional ? optionalLocalParams : requiredLocalParams;
                list.Add(p);
            }

            var processedParameters = new List<object>();

            for (int i = 0; i < _requiredRemoteParams.Length; i++)
            {
                if (i >= requiredLocalParams.Count)
                {
                    continue;
                }

                var localParam  = requiredLocalParams[i];


                var remoteParam = _requiredRemoteParams[i];
                processedParameters.Add(ProcessRpcParameter(localParam, remoteParam));
            }

            for (int i = _requiredRemoteParams.Length; i < requiredLocalParams.Count; i++)
            {
                processedParameters.Add(null);
            }

            for (int i = 0; i < _optionalRemoteParams.Length; i++)
            {
                processedParameters.Add(ProcessRpcParameter(optionalLocalParams[i], _optionalRemoteParams[i]));
            }

            _method.Invoke(_target, processedParameters.ToArray());
        }

        private object ProcessRpcParameter(ParameterInfo localParam, object remoteParam)
        {
            // only used by local rpcs
            object processedLocalParam = remoteParam;

            var converter = GetImplicitConverter(baseType: remoteParam.GetType(), targetType: localParam.ParameterType);
            if (converter != null)
            {
                processedLocalParam = converter.Invoke(null, new[] {localParam});
            }

            bool serializedAsGenericDict = remoteParam is IDictionary<string,object>;
            bool rpcExpectGenericDict = localParam.ParameterType == typeof(IDictionary<string, object>);

            // tinyjson processes anonymous objects as dictionaries
            if (serializedAsGenericDict && !rpcExpectGenericDict)
            {
                processedLocalParam = ParamToObject(remoteParam as IDictionary<string, object>, localParam.ParameterType);
            }

            return processedLocalParam;
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
