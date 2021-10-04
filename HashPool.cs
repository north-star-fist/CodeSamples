using System.Collections.Generic;

using UnityEngine;

namespace Sergey.Safonov.Utility.Pool
{
    
    /**
    * <summary>Hash set based pool. Data is kept in two hashsets for busy and free items.</summary>
    */
    public class HashPool : Pool
    {
        GameObject prefab;

        HashSet<GameObject> busyItems = new HashSet<GameObject>();
        HashSet<GameObject> freeItems = new HashSet<GameObject>();

        int capacity;

        public HashPool(int capacity, GameObject prefab) {
            this.prefab = prefab;
            this.capacity = capacity;
        }
        
        /**
         * <summary>Borrows a game object</summary>
         */
        public GameObject Borrow() {
            return borrow(false, Vector3.zero, Quaternion.identity);
        }
        
        /**
         * <summary>Borrows and locates a game object</summary>
         */
        public GameObject Borrow(Vector3 pos, Quaternion rot) {
            return borrow(true, pos, rot);
        }

        /**
         * <summary>Releases the game object</summary>
         */
        public void Release(GameObject item) {
            if (!busyItems.Contains(item)) {
                return;
            }
            PoolItem pItem = item.GetComponent<PoolItem>();
            if (pItem != null) {
                pItem.OnRelease();
            }
            freeItems.Add(item);
            busyItems.Remove(item);
        }

        public GameObject borrow(bool setPositionNRotation, Vector3 pos, Quaternion rot) {
            if (freeItems.Count > 0) {
                HashSet<GameObject>.Enumerator iter = freeItems.GetEnumerator();
                iter.MoveNext();
                return prepareItem(iter.Current, setPositionNRotation, pos, rot);
            } else if (busyItems.Count < capacity) {
                GameObject obj = createItem(setPositionNRotation, pos, rot);
                return prepareItem(obj, false, pos, rot);
            }

            //Debug.Log("No free items in the pool");
            return null;
        }

        private GameObject prepareItem(GameObject obj, bool setPositionAndRotation, Vector3 pos, Quaternion rot) {
            if (setPositionAndRotation) {
                locateObject(obj, pos, rot);
            }
            busyItems.Add(obj);
            if (freeItems.Contains(obj)) {
                freeItems.Remove(obj);
            }

            PoolItem itemComp = obj.GetComponent<PoolItem>();
            if (itemComp != null) {
                itemComp.OnBorrow();
            }

            return obj;
        }


        private GameObject createItem(bool setPositionAndRotation, Vector3 pos, Quaternion rot) {
            GameObject item = null;
            if (setPositionAndRotation) {
                item = GameObject.Instantiate(prefab, pos, rot);
            } else {
                item = GameObject.Instantiate(prefab);
            }
            //Debug.LogFormat(item, "Pool item was spawned {0}", item) ;
            return item;
        }

        private void locateObject(GameObject obj, Vector3 pos, Quaternion rot) {
            if (obj.activeInHierarchy) {
                Rigidbody body = obj.GetComponent<Rigidbody>();
                if (body != null) {
                    body.gameObject.SetActive(true);
                    body.rotation = rot;
                    body.position = pos;
                } else {
                    obj.transform.SetPositionAndRotation(pos, rot);
                }
            } else {
                obj.transform.SetPositionAndRotation(pos, rot);
            }
        }

    }
}
