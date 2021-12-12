using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Sergey.Safonov.Utility
{

    /// <summary>Quad Tree implementation for usage with Unity.
    /// Suitable for both static and moving objects</summary>
    public class QuadTree<T> : IEnumerable<T>
    {

        private const int DEFAULT_CELL_CAPACITY = 4;

        private readonly QuadTree<T> _parent;

        private Bounds _boundary;

        private readonly LinkedList<LocatedObject<T>> _objects = new LinkedList<LocatedObject<T>>();
        
        /// <summary> Exceeding this capacity leads to cell splitting </summary>
        private readonly int _cellCapacity;

        //Child quad trees (cells)
        //north-west; north-east; south-west; south-east
        readonly List<QuadTree<T>> _subTrees = new List<QuadTree<T>>(4);
        

        /// <summary> Creates instance of QuadTree. </summary>
        /// <param name="boundary">bounds of the tree box(cell)</param>
        /// <param name="cellCapacity">maximum capacity of each tree cell before it's splitting into four subcells</param>
        public QuadTree(Bounds boundary, int cellCapacity = DEFAULT_CELL_CAPACITY) : this(boundary, cellCapacity, null) {
        }


        /// <summary> Creates instance of QuadTree. </summary>
        /// <param name="center">center of bounds of the tree box(cell)</param>
        /// <param name="size">size of bounds of the tree box(cell)</param>
        /// <param name="cellCapacity">maximum capacity of each tree cell before it's splitting into four subcells</param>
        public QuadTree(Vector3 center, Vector3 size, int cellCapacity = DEFAULT_CELL_CAPACITY) 
            : this(new Bounds(center, size), cellCapacity) {
        }


        /// <summary> Creates instance of QuadTree. </summary>
        /// <param name="boundary">bounds of the tree box(cell)</param>
        /// <param name="cellCapacity">maximum capacity of each tree cell before it's splitting into four subcells</param>
        /// <param name="parent">parent tree(cell) of this one</param>
        private QuadTree(Bounds boundary, int cellCapacity, QuadTree<T> parent) {
            _boundary = boundary;
            _cellCapacity = cellCapacity;
            _parent = parent;
        }
        

        /// <summary> Adds an object to the tree. </summary>
        /// <param name="obj">new object</param>
        /// <param name="location">location of the object</param>
        /// <returns>true if the object was added and false if the object is out of bounds of the tree cell</returns>
        public bool Add(T obj, Vector3 location) {
            if (!_boundary.Contains(location)) return false; // out of cell

            if (_objects.Count < _cellCapacity && _subTrees.Count == 0) {
                _objects.AddLast(new LocatedObject<T>(obj, location));
                return true;
            }

            if (_subTrees.Count == 0) {
                Subdivide();
            }
            return _subTrees.Any(child => child.Add(obj, location));
        }


        /// <summary> Updates object that moves. </summary>
        /// <param name="obj">object that moves</param>
        /// <param name="from">old location (may be approximate)</param>
        /// <param name="to">new location</param>
        /// <returns>true if the object was moved and false if there was not such object in the tree
        /// or the object is out of bounds of the tree (it is removed in this case)</returns>
        public bool Move(T obj, Vector3 from, Vector3 to) => Remove(obj, from, true) ? Add(obj, to) : false;


        /// <summary> Finds all objects that are located at the specified bounds. </summary>
        public IEnumerable<T> GetAllFromRegion(Bounds boundBox) => new RegionObjects(this, boundBox);


         /// <summary> Checks if the object is inside specified box. </summary>
         public bool Contains(T obj, Bounds boundaryBox) =>
             obj != null && GetAllFromRegion(boundaryBox).Any(iterObj => ReferenceEquals(obj, iterObj));

         
         /// <summary> Removes specified object from the tree. </summary>
         /// <param name="obj">object to remove</param>
         /// <param name="location">location of the object (it may be approximate)</param>
         /// <param name="searchOutOfLocation">Check this flag if the object could 'ran out of the box'
         /// to look for it out of it's location</param>
         /// <returns>true if the object was found and removed. False otherwise</returns>
        public bool Remove(T obj, Vector3 location, bool searchOutOfLocation = false) 
             => RemoveNarrow(obj, location, searchOutOfLocation);


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator() => GetAllFromRegion(_boundary).GetEnumerator();


        private bool RemoveNarrow(T obj, Vector3 location, bool searchOutOfLocation = false)
        {
            if (!_boundary.Contains(location)) return false;

            if (RemoveTheCellObject(obj)) return true;

            if (_subTrees.Any(childTree => childTree.RemoveNarrow(obj, location, searchOutOfLocation))) {
                if (isReadyForCompression()) combine();
                return true;
            }

            //we are at the bottom. Now we can start to search widely up
            return (searchOutOfLocation && _parent != null) && _parent.RemoveWide(obj, this);
        }


        private bool RemoveWide(T obj, QuadTree<T> treeToExclude = null)
        {
            if (RemoveTheCellObject(obj)) return true;

            if (_subTrees.Where(subTree => subTree != treeToExclude).Any(childTree => childTree.RemoveWide(obj)))
            {
                if (isReadyForCompression()) combine();
                return true;
            }
            return false;
        }


        private bool RemoveTheCellObject(T obj)
        {
            var toRemove = _objects.FirstOrDefault(locObj => ReferenceEquals(locObj.Obj, obj));
            if (toRemove != null)
            {
                _objects.Remove(toRemove);
                return true;
            }
            return false;
        }


        // Subdivides the Quad Tree space into cells with separate Quade Trees
        private void Subdivide() {
            Vector3 cellSize = new Vector3(_boundary.extents.x, _boundary.size.y, _boundary.extents.z);
            Vector3 nwCenter = new Vector3(_boundary.center.x - _boundary.extents.x / 2, _boundary.center.y, _boundary.center.z + _boundary.extents.z / 2);
            Vector3 neCenter = new Vector3(_boundary.center.x + _boundary.extents.x / 2, _boundary.center.y, _boundary.center.z + _boundary.extents.z / 2);
            Vector3 swCenter = new Vector3(_boundary.center.x - _boundary.extents.x / 2, _boundary.center.y, _boundary.center.z - _boundary.extents.z / 2);
            Vector3 seCenter = new Vector3(_boundary.center.x + _boundary.extents.x / 2, _boundary.center.y, _boundary.center.z - _boundary.extents.z / 2);
            _subTrees.Add(new QuadTree<T>(new Bounds(nwCenter, cellSize), _cellCapacity, this));
            _subTrees.Add(new QuadTree<T>(new Bounds(neCenter, cellSize), _cellCapacity, this));
            _subTrees.Add(new QuadTree<T>(new Bounds(swCenter, cellSize), _cellCapacity, this));
            _subTrees.Add(new QuadTree<T>(new Bounds(seCenter, cellSize), _cellCapacity, this));

            foreach (var p in _objects)
            {
                if (_subTrees.Any(child => child.Add(p.Obj, p.Point))) continue;
            }
            _objects.Clear();
        }


        private bool isReadyForCompression()
        {
            return !_subTrees.Any(st => st._subTrees.Any()) && _subTrees.Aggregate(0, (c, st) => c + st._objects.Count) <= _cellCapacity;
        }


        private void combine()
        {
            _subTrees.Aggregate(_objects, (objs, st) => {
                    foreach (var o in st._objects) objs.AddLast(o);
                    return objs;
                });

            _subTrees.Clear();
        }



        /// <summary> Iterable subclass for objects iterating without keeping them in separate collection. </summary>
        private class RegionObjects : IEnumerable<T>
        {
            private readonly QuadTree<T> _tree;
            private Bounds _box;

            public RegionObjects(QuadTree<T> root, Bounds box)
            {
                _tree = root;
                _box = box;
            }
            
            public IEnumerator<T> GetEnumerator()
            {
                if (!_tree._boundary.Intersects(_box))
                    yield break;

                foreach (var locObj in _tree._objects) {
                    if (locObj.Obj != null && _box.Contains(locObj.Point)) yield return locObj.Obj;
                }

                foreach (var subTree in _tree._subTrees)
                {
                    var subItems = new RegionObjects(subTree, _box);
                    foreach (var subItem in subItems)
                    {
                        yield return subItem;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        


        private class LocatedObject<TO>
        {
            public readonly TO Obj;
            public readonly Vector3 Point;

            public LocatedObject(TO obj, Vector3 point) {
                Obj = obj;
                Point = point;
            }
        }

    }
}