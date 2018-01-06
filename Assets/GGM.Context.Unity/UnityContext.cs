using GGM.Context.Unity.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using static System.Reflection.Emit.OpCodes;

namespace GGM.Context.Unity
{
    public class UnityContext : ManagedContext
    {
        public UnityContext(UnityEngine.GameObject gameObject)
        {
            GameObject = gameObject;
        }

        private delegate void UnityDependencyInjector(UnityEngine.Component unityObject, object[] parameters);
        private Dictionary<Type, UnityDependencyInjector> mDependencyInjectors = new Dictionary<Type, UnityDependencyInjector>();
        public UnityEngine.GameObject GameObject { get; }

        protected override ParameterInfo[] GetInjectedParameters(Type type)
        {
            if (!type.IsSubclassOf(typeof(UnityEngine.Component)))
                return base.GetInjectedParameters(type);
            
            var methodInfo = type.GetMethods().FirstOrDefault(info => info.IsDefined(typeof(AutoWiredMethodAttribute)));
            if (methodInfo == null)
                Console.WriteLine($"{type}의 AutoWired 생성자가 없어 기본 생성자를 사용합니다.");
            return methodInfo?.GetParameters() ?? new ParameterInfo[] { };
        }

        public override object Create(Type type, object[] parameters = null)
        {
            // Unity 객체가 아닌경우
            if (!type.IsSubclassOf(typeof(UnityEngine.Component)))
                return base.Create(type, parameters);

            // Unity 객체인경우
            var prefabPath = type.GetCustomAttribute<UnityManagedAttribute>().ResourcePath;
            UnityEngine.GameObject obj;
            UnityEngine.Component component;
            if (string.IsNullOrEmpty(prefabPath)) // Path가 없는 경우는 새로 생성하여 추가해줌
            {
                obj = new UnityEngine.GameObject(type.Name);
                component = obj.AddComponent(type);
            }
            else
            {
                var prefab = UnityEngine.Resources.Load(prefabPath); // Prefab을 불러와 컴포넌트를 가져옴
                obj = UnityEngine.GameObject.Instantiate(prefab) as UnityEngine.GameObject;
                component = obj.GetComponent(type);
            }

            obj.transform.parent = GameObject.transform;
            var dependencyInjector = GetCachedInjectorInternal(type);
            dependencyInjector(component, parameters);

            return component;
        }

        private UnityDependencyInjector GetCachedInjectorInternal(Type type)
        {
            if (mDependencyInjectors.ContainsKey(type))
                return mDependencyInjectors[type];

            var autoWiredMethodInfo = type.GetMethods().FirstOrDefault(info => info.IsDefined(typeof(AutoWiredMethodAttribute)));
            var parameterInfos = autoWiredMethodInfo?.GetParameters() ?? new ParameterInfo[] { };

            var dm = new DynamicMethod($"{type.Name}DependencyInjector+{Guid.NewGuid()}", null, new[] { typeof(UnityEngine.Component), typeof(object[]) });
            var il = dm.GetILGenerator();
            if (parameterInfos.Length != 0)
            {
                il.Emit(Ldarg_0); // [Component]
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    il.Emit(Ldarg_1); // [Component] [params]
                    il.Emit(Ldc_I4, i); // [Component] [params] [index]
                    il.Emit(Ldelem_Ref); // [Component] [params[index]]

                    var parameterType = parameterInfos[i].ParameterType;
                    if (parameterType.IsValueType)
                        il.Emit(Unbox_Any, parameterType);
                }
                il.Emit(Call, autoWiredMethodInfo);
            }
            il.Emit(Ret);

            return mDependencyInjectors[type] = dm.CreateDelegate(typeof(UnityDependencyInjector)) as UnityDependencyInjector;
        }
    }
}
