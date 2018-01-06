using GGM.Context.Attribute;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GGM.Context.Unity
{
    public class GGMUnityApplication : MonoBehaviour
    {
        private static GGMUnityApplication application;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod()
        {
            var gameObject = FindObjectOfType<GGMUnityApplication>()?.gameObject;
            if (gameObject == null)
            {
                gameObject = new GameObject(nameof(GGMUnityApplication));
                gameObject.AddComponent<GGMUnityApplication>();
            }

            application = gameObject.GetComponent<GGMUnityApplication>();
        }

        public T GetManaged<T>() where T : class
        {
            if (application == null)
                throw new System.Exception("application이 초기화 되기전에 기능을 사용했습니다.");
            return application.Context.GetManaged<T>();
        }

        public ManagedContext Context { get; set; }

        public void Awake()
        {
            Context = new UnityContext(gameObject);
            var managedTypes = GetType().Assembly.GetTypes()
                .Where(type => type.IsDefined(typeof(ManagedAttribute)))
                .ToDictionary(type => type, type => type.GetCustomAttribute<ManagedAttribute>());

            foreach (var managedType in managedTypes)
            {
                // Key : type
                // Value : ManagedAttribute
                if (managedType.Value.ManagedType == ManagedType.Singleton)
                    Context.GetManaged(managedType.Key);
            }
        }
    }
}
