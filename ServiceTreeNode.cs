/**** BEGIN LICENSE BLOCK ****

BSD 3-Clause License

Copyright (c) 2022, the wind.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

**** END LICENCE BLOCK ****/

using System;
using System.Collections.Generic;
#if SERVICE_TREE_NODE_INSERT_DATA
using System.Diagnostics;
#endif
using System.Linq;

namespace IoCContainer_net4_sharp5
{
    /// <summary>Thrown when a dependency loop is detected.</summary>
    public class ServiceTreeNodeLoopException : Exception { }

    /// <summary>A contract between the tree and its T.</summary>
    public interface IServiceTreeNodeDataModel
    {
        object Create(object[] args);
    }

    // Simplifies the ServiceCollection code.
    //
    // Given S1(S2); S6(S2, S7, S2); S7(S1); S2(S7), tree (the service deps are resolved on Get*):
    //  - ServiceTreeNode refers nodes it depends on:
    //       1 6    | (6 has 2, 7, 2 as sub-nodes)
    //        2 7
    //       7   1  (all sub-nodes of 7 will have to be verified against all new parents of 7; 1 tree walk on insert())
    //      1       |
    //  - ServiceTreeNode refers nodes that do depend on it:
    //         2    |
    //        1 6   | (6 has 2 parent links to 2)
    //       7      |
    //      2 6     (only 2 has to be verified against all parents of 7; 1 tree walk on insert() for each dependency)
    //
    // The short and simple: an exception guides you on ServiceCollection.Register(). As far as I understand however DI
    // enforces the other neither-short-nor-simple problem+solution: put everything regardless of order (problem), and
    // let the IoC resolve the dependency net (solution).
    /// <summary>Tree-like structure managing service relations.</summary>
    public class ServiceTreeNode<T> where T : IServiceTreeNodeDataModel
    {
        public ServiceTreeNode(T k) { Data = k; }

        //PERHAPS private static PriorityQueue<CompositeKey->ServiceTreeNode> should the need arise to speed up NodeByData;

        // Services that do depend on this one.
        private Dictionary<T, ServiceTreeNode<T>> _n = new Dictionary<T, ServiceTreeNode<T>> ();

        // Service constructor args.
        private List<ServiceTreeNode<T>> _p = new List<ServiceTreeNode<T>> (); // parent node(s)
        public const int MAX_CONSTRUCTOR_PARAMS = 1 << 3; // necessity_to_redesign = parameter_num / MAX_CONSTRUCTOR_PARAMS; [%];
        private void AddParentNode(ServiceTreeNode<T> parent_node)
        {
            if (_p.Count >= MAX_CONSTRUCTOR_PARAMS)
                throw new ArgumentException (Res.FMT_UPPER_LIMIT_CONSIDERATION ("parameters", "MAX_CONSTRUCTOR_PARAMS"), "parent_node");
            if (null == parent_node) throw new ArgumentException (Res.NO_NULLS, "parent_node");
            _p.Add (parent_node);
        }
        // Used to restore node state on ServiceTreeNodeLoopException.
        private void RemoveParentNode(ServiceTreeNode<T> parent_node)
        {
            int idx = -1;
            for (int i = _p.Count - 1; i >= 0; i--) if (_p[i] == parent_node) { idx = i; break; }
            if (idx >= 0 && idx < _p.Count) _p.RemoveAt (idx);
        }

#if SERVICE_TREE_NODE_INSERT_DATA
        // Use-case: foreach (var arg in svc.chosen_con.args) service.GetNode (arg.svc).Insert (svc, svc.impl_type);
        /// <summary>Add a service that depends on this one</summary>
        public ServiceTreeNode<T> Insert(T data)
        {
            var node = NodeByData (data) ?? new ServiceTreeNode<T> (data);
            Debug.Assert (data.Equals (node.Data), "Fixme: either NodeByData() or CompositeKey.Equals() is playing you");
            node.AddParentNode (this);
            node.LoopCheck (node);
            return _n[data] = node;
        }
#endif

        /// <summary>Add a service that depends on this one.</summary>
        public ServiceTreeNode<T> Insert(ServiceTreeNode<T> node)
        {
            node.AddParentNode (this);
            try { node.LoopCheck (node); }
            catch (ServiceTreeNodeLoopException)
            {
                node.RemoveParentNode (this); // undo AddParentNode
                throw;
            }
            return _n[node.Data] = node;
        }

        private static int loop_check_sentinel = 0;
        /// <summary>Looks for reasons to throw a ServiceTreeNodeLoopException.</summary>
        private void LoopCheck(ServiceTreeNode<T> node)
        {
            if (loop_check_sentinel++ >= WALK_MAX) throw new Exception (Res.FMT_INFINITE_LOOP_PROBABLY ("WALK_MAX"));
            try
            {
                if (null == node) return;
                foreach (var p in node._p)
                    if (object.ReferenceEquals (p, this)) throw new ServiceTreeNodeLoopException ();
                    else LoopCheck (p);
            }
            finally { loop_check_sentinel--; }
        }

        /// <summary>Find the node that has <c>".Data == value"</c>.</summary>
        protected ServiceTreeNode<T> NodeByData(T value) { return GetRootNode ().Find (value); }

        public delegate bool TreeWalkDelegate(ServiceTreeNode<T> node); // true: continue
        private static int tree_walk_sentinel = 0;
        private const int WALK_MAX = 1 << 17;
        /// <summary>Thread-unsafe tree walk with min. stack overflow protection.</summary>
        public static void Walk(ServiceTreeNode<T> node, TreeWalkDelegate todo)
        {
            if (tree_walk_sentinel++ >= WALK_MAX) throw new Exception (Res.FMT_INFINITE_LOOP_PROBABLY ("WALK_MAX"));
            try
            {
                if (null == todo || null == node || !todo (node)) return;
                foreach (var n in node.Nodes) Walk (n, todo);
            }
            finally { tree_walk_sentinel--; }
        }

        /// <summary>Services that depend on this one.</summary>
        public IEnumerable<ServiceTreeNode<T>> Nodes { get { return _n.Select (x => x.Value); } }

        /// <summary>Service constructor parameters.</summary>
        public IEnumerable<ServiceTreeNode<T>> ParentNodes { get { return _p; } }

        protected ServiceTreeNode<T> Find(T value) // not FindSubNode; finds (.Data == value) with root = this
        {
            ServiceTreeNode<T> result = null;
            Walk (this, (node) => { if (!value.Equals (node.Data)) return true; result = node; return false; });
            return result;
        }

        protected ServiceTreeNode<T> GetRootNode() // not "Root { get{} }" on purpose: not a state
        {
            //PERHAPS static ServiceTreeNode RootNode_cache; should the need arise; or ServiceTree.Root, etc.
            var node = this;
            while (0 != node._p.Count) node = node._p[0];
            if (null == node) throw new Exception (Res.NO_NULLS);
            return node;
        }

        /// <summary>Gets the data associated with this node.</summary>
        public T Data { get; private set; }

        /// <summary>Convenience: gets an instance via <c>Data.Create(object[])</c>.</summary>
        internal object Create()//TODO protect me against StackOverflow
        {
            return Data.Create (_p.Select (x => x.Create ()).ToArray ());
        }

        /// <summary>Remove this node from the tree. Sub-nodes referred by other parent nodes outside this subtree, are not removed.</summary>
        internal void Remove()
        {
            foreach (var parent_node in _p) parent_node.RemoveSubNode (this);
            _n.Clear ();
            _p.Clear ();
        }

        private void RemoveSubNode(ServiceTreeNode<T> sub_node) { _n.Remove (sub_node.Data); }
    }// public class ServiceTreeNode
}
