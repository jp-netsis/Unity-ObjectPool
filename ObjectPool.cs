//#define HIDE_OBJECTPOOL

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace netsis.jp.Utility
{
    public interface IPoolObject
    {
        /// <summary>
        /// Awake -> OnEnable -> OnSpawn -|> Start
        /// </summary>
        void OnSpawn();
        /// <summary>
        /// OnDespawn -> OnDisable -> OnDestroy
        /// </summary>
        void OnDespawn();
    }

    public class ObjectPool : MonoBehaviour
    {
        private static ObjectPool instance;

        // Singleton
        public static ObjectPool Instance {
            get {
                if (instance == null)
                {
                    instance = FindObjectOfType<ObjectPool> ();

                    if (instance == null)
                    {
                        var go = new GameObject("ObjectPool");
                        instance = go.AddComponent<ObjectPool> ();
                        instance.DontDestroyOnLoad();
                        #if HIDE_OBJECTPOOL
                        go.hideFlags = HideFlags.HideAndDontSave;
                        #else
                        go.hideFlags = HideFlags.DontSave;
                        #endif
                    }
                }
                return instance;
            }
        }

        // cached gameobjects
        private Dictionary<int, List<GameObject>> gameObjects = new Dictionary<int, List<GameObject>> ();

        public void CreateCacheGameObject(GameObject poolPrefab, int count)
        {
            // instance id is key
            int key = poolPrefab.GetInstanceID ();

            if (this.gameObjects.ContainsKey (key) == false)
            {
                this.gameObjects.Add (key, new List<GameObject> ());
            }

            var gameObjectList = this.gameObjects [key];
            var max = Mathf.Max(count, 1);

            for (int n = 0; n < max; ++n)
            {
                var go = InstantiateGameObject( poolPrefab, Vector3.zero, Quaternion.identity, this.transform );
                go.SetActive (false);
                gameObjectList.Add(go);
            }
        }

        private GameObject InstantiateGameObject(
            GameObject instantiatePrefab, 
            Vector3 position, 
            Quaternion rotation,
            Transform parent)
        {
            var go = Instantiate(instantiatePrefab, position, rotation);
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent);
            return go;
        }

        public GameObject SpawnGameObject (GameObject poolPrefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            // instance id is key
            int key = poolPrefab.GetInstanceID ();

            if (this.gameObjects.ContainsKey (key) == false)
            {
                this.gameObjects.Add (key, new List<GameObject> ());
            }

            var gameObjectList = this.gameObjects [key];
            GameObject go = null;

            for (int i = 0; i < gameObjectList.Count; i++)
            {
                go = gameObjectList[i];

                if (go.activeInHierarchy == false) {
                    go.transform.position = position;
                    go.transform.rotation = rotation;
                    go.transform.SetParent(parent);
                    go.SetActive (true);
                    var poolObjectComponentList = go.GetComponents<IPoolObject>();
                    foreach( var poolObjectComponent in poolObjectComponentList)
                        poolObjectComponent.OnSpawn();

                    return go;
                }
            }

            go = (GameObject)InstantiateGameObject(poolPrefab, position, rotation, parent);
            gameObjectList.Add (go);

            var recyclePoolObjectComponentList = go.GetComponents<IPoolObject>();
            foreach( var poolObjectComponent in recyclePoolObjectComponentList)
                poolObjectComponent.OnSpawn();
            
            return go;
        }

        public void DespawnGameObject (GameObject go)
        {
            var recyclePoolObjectComponentList = go.GetComponents<IPoolObject>();
            foreach( var poolObjectComponent in recyclePoolObjectComponentList)
                poolObjectComponent.OnDespawn();
            go.SetActive (false);
            go.transform.SetParent(this.transform);
        }

        public void Release(GameObject poolPrefab)
        {
            int key = poolPrefab.GetInstanceID ();
            Release(key);
        }

        public void Release(int key)
        {
            if (this.gameObjects.ContainsKey (key) == false) return;
            Release(this.gameObjects[key]);
            this.gameObjects.Remove(key);
        }

        private void Release(List<GameObject> goList)
        {
            foreach (var go in goList)
            {
                GameObject.Destroy(go);
            }
            goList.Clear();
        }

        public void ReleaseAll()
        {
            foreach(var vals in this.gameObjects.Values){
                Release(vals);
            }
            this.gameObjects.Clear();
        }

        public void DontDestroyOnLoad()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void DestroyOnLoad()
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        }
    }

}
