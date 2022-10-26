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

using NUnit.Framework;
using System;
using System.Linq;

namespace IoCContainer_net4_sharp5.Tests
{
    [TestFixture]
    public class ServiceTreeNodeTest
    {
        class TestModel : IServiceTreeNodeDataModel
        {
            object IServiceTreeNodeDataModel.Create(object[] args) { return args; }
        }

        ServiceTreeNode<TestModel> _root = null;
        TestModel _root_data = null;
        static ServiceTreeNode<TestModel> NewNode() { return new ServiceTreeNode<TestModel> (new TestModel ()); }

        [SetUp]
        public void setup() { _root = new ServiceTreeNode<TestModel> (_root_data = new TestModel ()); }

        [Test, Category ("InitialState")]
        public void InitialState_Model() { Assert.AreEqual (_root_data, _root.Data); }
        [Test, Category ("InitialState")]
        public void InitialState_SubNodes() { Assert.IsEmpty (_root.Nodes); }
        [Test, Category ("InitialState")]
        public void InitialState_ParentNodes() { Assert.IsEmpty (_root.ParentNodes); }

        [Test, Category ("Insert")]
        public void Insert_One_node()
        {
            var node_data = new TestModel ();
            var node = new ServiceTreeNode<TestModel> (node_data);
            _root.Insert (node);
            Assert.AreEqual (_root.Data, _root_data);
            Assert.AreEqual (_root.Nodes.Count (), 1);
            Assert.AreEqual (_root.ParentNodes.Count (), 0);
            Assert.AreEqual (_root.Nodes.ElementAt (0), node);
            Assert.AreEqual (node.Data, node_data);
            Assert.AreEqual (node.Nodes.Count (), 0);
            Assert.AreEqual (node.ParentNodes.Count (), 1);
            Assert.AreEqual (node.ParentNodes.ElementAt (0), _root);
        }
#if SERVICE_TREE_NODE_INSERT_DATA
        [Test, Category ("Insert")]
        public void Insert_One_data()
        {
            var node_data = new TestModel ();
            _root.Insert (node_data);
            Assert.AreEqual (_root.Data, _root_data);
            Assert.AreEqual (_root.Nodes.Count (), 1);
            Assert.AreEqual (_root.ParentNodes.Count (), 0);
            var node = _root.Nodes.ElementAt (0);
            Assert.AreEqual (_root.Nodes.ElementAt (0), node);
            Assert.AreEqual (node.Data, node_data);
            Assert.AreEqual (node.Nodes.Count (), 0);
            Assert.AreEqual (node.ParentNodes.Count (), 1);
            Assert.AreEqual (node.ParentNodes.ElementAt (0), _root);
        }
#endif
        [Test, Category ("Insert")]
        public void Insert_MAX_CONSTRUCTOR_PARAMS()
        {
            for (int i = 0; i < ServiceTreeNode<TestModel>.MAX_CONSTRUCTOR_PARAMS; i++)
                new ServiceTreeNode<TestModel> (new TestModel ()).Insert (_root);
            Assert.AreEqual (expected: _root_data, actual: _root.Data);
            Assert.AreEqual (expected: 0, actual: _root.Nodes.Count ());
            Assert.AreEqual (expected: ServiceTreeNode<TestModel>.MAX_CONSTRUCTOR_PARAMS, actual: _root.ParentNodes.Count ());
            Assert.Catch (typeof (ArgumentException), () => new ServiceTreeNode<TestModel> (new TestModel ()).Insert (_root));
        }
#if SERVICE_TREE_NODE_INSERT_DATA
        [Test, Category ("Insert")]
        public void Insert_data_MAX_CONSTRUCTOR_PARAMS() // a.k.a. the difference vs Insert_MAX_CONSTRUCTOR_PARAMS
        {
            // required for NodeByData() to function, otherwise the next "new ServiceTreeNode<TestModel>" won't find
            // "_root" by "_root_data" because it isn't linked to _root
            var tmp_root = new ServiceTreeNode<TestModel> (new TestModel ());
            tmp_root.Insert (_root); // otherwise "node(_root_data)" won't actually be "_root"
            for (int i = 0; i < ServiceTreeNode<TestModel>.MAX_CONSTRUCTOR_PARAMS - 1; i++)
                tmp_root.Insert (new ServiceTreeNode<TestModel> (new TestModel ())).Insert (_root_data);
            Assert.AreEqual (_root.Data, _root_data);
            Assert.AreEqual (_root.Nodes.Count (), 0);
            Assert.AreEqual (_root.ParentNodes.Count (), ServiceTreeNode<TestModel>.MAX_CONSTRUCTOR_PARAMS);
            Assert.Catch (typeof (ArgumentException), () => tmp_root.Insert (new ServiceTreeNode<TestModel> (new TestModel ())).Insert (_root_data));
        }
#endif
        [Test, Category ("Loop Detection")]
        public void LoopCheck_InsertSelf()
        {
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => _root.Insert (_root));
        }
        [Test, Category ("Loop Detection")]
        public void LoopCheck_ABA()
        {
            var node = _root.Insert (NewNode ());
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => node.Insert (_root));
        }
        [Test, Category ("Loop Detection")]
        public void LoopCheck_many()
        {
            var node1 = _root.Insert (NewNode ());
            var node11 = node1.Insert (NewNode ());
            var node2 = _root.Insert (NewNode ());
            var node3 = node2.Insert (NewNode ());
            var nodeq = node3.Insert (node11);
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => nodeq.Insert (node3));
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => nodeq.Insert (node2));
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => nodeq.Insert (_root));
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => nodeq.Insert (node1));
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => node11.Insert (node1));
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => node11.Insert (_root));
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => node3.Insert (node2));
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => node3.Insert (_root));
            Assert.Catch (typeof (ServiceTreeNodeLoopException), () => node2.Insert (_root));
            // state shall remain unchanged
            Assert.AreEqual (expected: 2, actual: _root.Nodes.Count ());
            Assert.AreEqual (expected: node1, actual: _root.Nodes.ElementAt (0));
            Assert.AreEqual (expected: node2, actual: _root.Nodes.ElementAt (1));
            Assert.AreEqual (expected: 1, actual: node1.Nodes.Count ());
            Assert.AreEqual (expected: node11, actual: node1.Nodes.ElementAt (0));
            Assert.AreEqual (expected: 1, actual: node2.Nodes.Count ());
            Assert.AreEqual (expected: node3, actual: node2.Nodes.ElementAt (0));
            Assert.AreEqual (expected: 1, actual: node3.Nodes.Count ());
            Assert.AreEqual (expected: nodeq, actual: node3.Nodes.ElementAt (0));
            Assert.AreEqual (expected: 0, actual: nodeq.Nodes.Count ());
            Assert.AreEqual (expected: 2, actual: nodeq.ParentNodes.Count ());
            Assert.AreEqual (expected: node1, actual: nodeq.ParentNodes.ElementAt (0));
            Assert.AreEqual (expected: node3, actual: nodeq.ParentNodes.ElementAt (1));
            Assert.AreEqual (expected: nodeq, actual: node11);
            Assert.AreEqual (expected: 1, actual: node3.ParentNodes.Count ());
            Assert.AreEqual (expected: node2, actual: node3.ParentNodes.ElementAt (0));
            Assert.AreEqual (expected: 1, actual: node2.ParentNodes.Count ());
            Assert.AreEqual (expected: _root, actual: node2.ParentNodes.ElementAt (0));
            Assert.AreEqual (expected: 1, actual: node1.ParentNodes.Count ());
            Assert.AreEqual (expected: _root, actual: node1.ParentNodes.ElementAt (0));
            Assert.AreEqual (expected: 0, actual: _root.ParentNodes.Count ());
        }// public void LoopCheck_many

        //TODO Remove, random tree gen., etc.
    }// public class ServiceTreeNodeTest
}
