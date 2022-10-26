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

/*
   This is raw form. Do not use in production env. Be wary when learning.

   This is me understanding DI and IoC.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace IoCContainer_net4_sharp5
{
    /// <summary>Thrown when <c>"constructors.select.count() &lt;= 0"</c>.</summary>
    public sealed class NoSuitableConstructorException : Exception
    {
        public NoSuitableConstructorException(string message) : base (message: message) { }
    }

    /// <summary>Thrown when <c>"constructors.select.distinct()"</c> fails.</summary>
    public sealed class AmbigousConstructorException : Exception
    {
        public AmbigousConstructorException(string message) : base (message: message) { }
    }

    // There is no Build*, and the ServiceProvider == ServiceCollection.
    // You can Register() services at any time. You can Deregister() them at anytime <=> it could modify
    // the constructor selection on the next Get*. You should decide how your binding handles ConstructorChanged.
    // Deregistering a running service won't stop it. All dependent services will continue running and referencing it.
    //TODO add an Unregistered event.
    /// <summary>Hosts services as in f(DI, IoC). Turning this into DI service itself is a very tempting idea.</summary>
    public static class ServiceCollection
    {
        /// <summary>When ambiguity arises, mark the constructor you want called with this attribute.</summary>
        public class CallMeAttribute : Attribute { }

        /// <summary>Mutex - sync r/w, r/r, and w/w - one thread at a time.</summary>
        private static Mutex _the_way_is_shut = new Mutex (initiallyOwned: false, name: "ServiceCollectionMutex");

        /// <summary>Has all the <c>Set=false</c> bindings.</summary>
        private static Dictionary<Type, ServiceCollectionBinding> _b = new Dictionary<Type, ServiceCollectionBinding> ();
        /// <summary>Has all the <c>Set=true</c> bindings.</summary>
        private static Dictionary<IEnumerable<Type>, ServiceCollectionBinding> _s = new Dictionary<IEnumerable<Type>, ServiceCollectionBinding> ();

        /// <summary>Applies <c>"ServiceCollection"</c> requirements to a <c>"binding"</c>.</summary>
        private static void ValidateBinding(ServiceCollectionBinding binding)
        {
            if (null == binding)
                throw new ArgumentException (Res.NO_NULLS, "binding");
            if (null == binding.Interface || binding.Interface.Count () <= 0
                || null == binding.Implementation || binding.Implementation.Count () <= 0)
                throw new ArgumentException (Res.BINDING_INCOMPLETE, "binding");
        }

        /// <summary>Register your service(s).</summary>
        public static void Register(ServiceCollectionBinding binding)
        {
            if (null == Builder)
                throw new ArgumentException (Res.MEANINGFUL_BUILDER, "Builder");
            ValidateBinding (binding);

            _the_way_is_shut.WaitOne ();
            try
            {
                if (!binding.Set)
                {
                    if (_b.ContainsKey (binding.Interface.First ()))
                        throw new ArgumentException (Res.BINDING_DUPLICATE, "binding.Interface");
                    _b[binding.Interface.First ()] = binding;
                }
                else
                {
                    if (_s.ContainsKey (binding.Interface))
                        throw new ArgumentException (Res.BINDING_DUPLICATE, "binding.Interface");
                    else _s[binding.Interface] = binding;
                }
            }
            finally
            {
                _the_way_is_shut.ReleaseMutex ();
            }
        }// public static void Register(ServiceCollectionBinding binding)

        /// <summary>Remove a binding from the ServiceCollection - this won't de-instantiate any services instantiated by it.</summary>
        public static void Deregister(ServiceCollectionBinding binding)
        {
            ValidateBinding (binding);

            _the_way_is_shut.WaitOne ();
            try
            {
                if (!binding.Set)
                {
                    if (!_b.ContainsKey (binding.Interface.First ()))
                        throw new ArgumentException (Res.BINDING_UNKNOWN, "binding");
                    _b.Remove (binding.Interface.First ());
                }
                else
                {
                    if (!_s.ContainsKey (binding.Interface))
                        throw new ArgumentException (Res.BINDING_UNKNOWN, "binding");
                    _s.Remove (binding.Interface);
                }
            }
            finally
            {
                _the_way_is_shut.ReleaseMutex ();
            }
        }// public static void Deregister(ServiceCollectionBinding binding)

        // Maps binding to service interface type, to anything else that might come into play someday:
        // who knows puting something else here could reduce the code size in half - everything is possible ...
        private sealed class NodeData : IServiceTreeNodeDataModel
        {
            private Type _t = null; // interface type
            private ServiceCollectionBinding _b = null;
            public NodeData(Type t, ServiceCollectionBinding b) { _t = t; _b = b; }
            public override bool Equals(object obj) { var o = (NodeData)obj; return o._b == _b && o._t == _t; }
            public override int GetHashCode() //LATER verify collision rate or just use a hash function generator
            {
                return ((_b.GetHashCode () * 234567891) >> 16) | ((_t.GetHashCode () * 123456791) << 16);
            }

            object IServiceTreeNodeDataModel.Create(object[] args)
            {
                return _b.Create (ServiceCollection.Builder, _b.GetImplementationType (_t), args);
                // create via impl type: return _b.Create (ServiceCollection.Builder, _t, args);
            }
        }

        /// <summary>Returns a non-empty set of public constructors for the implemetation associated with "iface". Or throws exceptions.</summary>
        private static ConstructorInfo[] GetConstructors(Type iface, ServiceCollectionBinding details)
        {
            Type to_construct = details.GetImplementationType (iface);
            var constructors = to_construct.GetConstructors (bindingAttr: BindingFlags.Instance | BindingFlags.Public);
            if (null == constructors || constructors.Length <= 0)
                throw new ArgumentException (Res.FMT_NO_CONSTRUCTORS (to_construct.Name, iface.Name), "details");
            const int TOO_MANY_CONSTRUCTORS = 1 << 3;
            if (constructors.Length > TOO_MANY_CONSTRUCTORS)
                throw new ArgumentException (Res.FMT_UPPER_LIMIT_CONSIDERATION (string.Format (
                    "constructors for \"{0}: {1}\"", to_construct.Name, iface.Name), "TOO_MANY_CONSTRUCTORS"), "details");
            var c_m = constructors.Where (x =>
                {
                    var tmp = x.GetCustomAttributes (typeof (CallMeAttribute), inherit: false);
                    return null != tmp && 1 == tmp.Length;
                });
            if (c_m.Count () > 1)
                throw new ArgumentException (Res.ONE_CALLME_CONSTRUCTOR, "details");
            return c_m.Count () == 1 ? c_m.ToArray () : constructors.ToArray ();
        }

        /// <summary>Does the tree build.</summary>
        private static void Resolve_InvTreeBuild(Type t, ServiceCollectionBinding b, ServiceTreeNode<NodeData> r)
        {
            ServiceTreeNode<NodeData> node = new ServiceTreeNode<NodeData> (new NodeData (t, b));
            node.Insert (r); // tmp_root depends on node
            Resolve (t, b, node);
        }

        private static int _resolve_so_protection = 0;
        private const int _RESOLVE_MAX_REENTRY = 1 << 12;
        //TODO42 resolve by interface only?
        //TODONT make the tree a state; it will add tons of complexity and code, and events, and bugs.
        /// <summary>Resolves the constructor mess: selects the more appropriate ones, detects dependency loops, etc. Throws exceptions.</summary>
        private static ServiceTreeNode<NodeData> Resolve(Type iface, ServiceCollectionBinding details, ServiceTreeNode<NodeData> tmp_root)
        {
            if (_resolve_so_protection++ >= _RESOLVE_MAX_REENTRY)
                throw new Exception (Res.FMT_INFINITE_LOOP_PROBABLY ("RESOLVE_MAX_REENTRY"));
            List<ParameterInfo[]> constructor_list = new List<ParameterInfo[]> ();
            foreach (var ci in GetConstructors (iface, details).OrderByDescending (c => c.GetParameters ().Length))
            {
                var constructor_params = ci.GetParameters ();
                int resolved_params = 0;
                foreach (var itm in constructor_params)
                {
                    // look at the set 1st; see TODO42 if (details.Set && details.Implementation.Contains (itm.ParameterType)) { r++; continue; }
                    if (details.Set && details.Interface.Contains (itm.ParameterType))
                    {
                        Resolve_InvTreeBuild (itm.ParameterType, details, tmp_root);
                        resolved_params++;
                        continue;
                    }
                    //TODO is there at least one objective reason setX services to be allowed to depend on setY one(s) given that setX has them not?
#if CROSS_SET_LOOKUP
                foreach (var kv in _s.Where (kv => kv.Value != details))
                    if (kv.Value.Implementation.Contains (itm.ParameterType)) { r++; continue; }
#endif
                    // look at the 1:1 table; see TODO42 foreach (var kv in _b) if (kv.Value.Interface.First () == itm.ParameterType)
                    ServiceCollectionBinding binding = null;
                    if (_b.TryGetValue (itm.ParameterType, out binding))
                    {
                        Resolve_InvTreeBuild (itm.ParameterType, binding, tmp_root);
                        resolved_params++;
                        continue;
                    }
                }// foreach (var itm in ci.GetParameters ())
                if (resolved_params == constructor_params.Count ()) constructor_list.Add (constructor_params); else tmp_root.Remove ();
            }// foreach (var ci in GetConstructors (iface, details))
            if (constructor_list.Count < 1)
                throw new NoSuitableConstructorException (Res.FMT_NO_SUITABLE_CONSTRUCTOR (iface.Name));
            if (constructor_list.Count > 1 && constructor_list[0].Length == constructor_list[1].Length)
                throw new AmbigousConstructorException (Res.FMT_AMBIGUOUS_CONSTRCUTORS (iface.Name));
            return tmp_root;
        }// Resolve()

        /// <summary>
        /// A function that creates a service, creating all its dependencies as needed honoring all bindings involved.
        /// Throws exceptions for bad arguments, wrong service configs, ambiguity situations, and some things I haven't noticed yet.
        /// </summary>
        private static object Create(Type iface, ServiceCollectionBinding details)
        {
            if (null == details) throw new ArgumentException (Res.NO_NULLS, "details");
            _resolve_so_protection = 0;
            return Resolve (iface, details, new ServiceTreeNode<NodeData> (
                new NodeData (iface, details))).Create ();
        }

        /// <summary>Creates your service(s) on demand.</summary>
        public static Interface Get<Interface>() where Interface : class
        {
            _the_way_is_shut.WaitOne ();
            try
            {
                // lets try with create on demand
                if (_b.ContainsKey (typeof (Interface))) return (Interface)Create (typeof (Interface), _b[typeof (Interface)]);
                if (_s.Where (kv => kv.Key.Contains (typeof (Interface))).Count () > 0)
                    throw new Exception ("Please use GetBinding() for binding that contains type set");
                else
                    throw new ArgumentException (Res.FMT_SERVICE_UNKNOWN (typeof (Interface).FullName), "Interface");
            }
            finally
            {
                _the_way_is_shut.ReleaseMutex ();
            }
        }

        /// <summary>
        /// Creates your service(s) on demand - the demand being governed by yield return.
        /// Use this when your binding contain a set of services itself.
        /// </summary>
        public static IEnumerable<ServiceCollectionBinding> GetBinding<Interface>() where Interface : class
        {
            _the_way_is_shut.WaitOne ();
            try
            {
                if (_b.ContainsKey (typeof (Interface)))
                    throw new Exception ("Please use Get() for bindings that contains no type set");
                List<ServiceCollectionBinding> result = new List<ServiceCollectionBinding> ();
                foreach (var kv in _s)
                    if (kv.Key.Contains (typeof (Interface)))
                    {
                        Create (typeof (Interface), kv.Value);
                        result.Add (kv.Value);
                    }
                if (result.Count <= 0)
                    throw new ArgumentException (Res.FMT_SERVICE_UNKNOWN (typeof (Interface).FullName), "Interface");
                else return result;
            }
            finally
            {
                _the_way_is_shut.ReleaseMutex ();
            }
        }// public static IEnumerable<ServiceCollectionBinding> GetBinding<Interface>()

        /// <summary>True when the service is registered with the service collection.</summary>
        public static bool Has<Interface>()
        {
            _the_way_is_shut.WaitOne ();
            try
            {
                return _b.ContainsKey (typeof (Interface)) || _s.Where (x => x.Key.Contains (typeof (Interface))).Count () > 0;
            }
            finally
            {
                _the_way_is_shut.ReleaseMutex ();
            }
        }

        /// <summary>The "service" that actually does the object instantiation.</summary>
        private static IInstanceBuilder _builder = null;
        public static IInstanceBuilder Builder
        {
            get { return _builder; }
            set
            {
                if (object.ReferenceEquals (_builder, value)) return;
                _the_way_is_shut.WaitOne ();
                try
                {
                    _builder = value;
                }
                finally
                {
                    _the_way_is_shut.ReleaseMutex ();
                }
            }
        }
    }// public sealed class ServiceCollection
}
