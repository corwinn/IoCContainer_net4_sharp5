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

    /// <summary>Hosts services as in f(DI, IoC). Turning this into DI service itself is a very tempting idea.</summary>
    public sealed class ServiceCollection
    {
        /// <summary>When ambiguity arises, mark the constructor you want called with this attribute.</summary>
        public class CallMeAttribute : Attribute { }

        // Per domain Singleton.
        // private static Mutex _the_way_is_shut = new Mutex (initiallyOwned: false, name: "ServiceCollectionMutex" + new Random ().Next ().ToString ());
        //TODO1 Recall how "yield return" plays with synchronization objects.
        /// <summary>Mutex - sync r/w, r/r, and w/w - one thread at a time. Perhaps a bad idea due to a yield return.</summary>
        private static Mutex _the_way_is_shut = new Mutex (initiallyOwned: false, name: "ServiceCollectionMutex");

        private static int _n = 0;
        private ServiceCollection() { if (_n++ > 0) throw new ArgumentException (this.GetType ().Name + ": create me twice: shame on you."); }

        /// <summary>Has all the <c>Set=false</c> bindings.</summary>
        private static Dictionary<Type, ServiceCollectionBinding> _b = new Dictionary<Type, ServiceCollectionBinding> ();
        /// <summary>Has all the <c>Set=true</c> bindings.</summary>
        private static Dictionary<IEnumerable<Type>, ServiceCollectionBinding> _s = new Dictionary<IEnumerable<Type>, ServiceCollectionBinding> ();

        /// <summary>Applies <c>"ServiceCollection"</c> requirements to a <c>"binding"</c>.</summary>
        private static void ValidateBinding(ServiceCollectionBinding binding)
        {
            if (null == binding)
                throw new ArgumentException ("null binding - can't help you.", "binding");
            if (null == binding.Interface || binding.Interface.Count () <= 0
                || null == binding.Implementation || binding.Implementation.Count () <= 0)
                throw new ArgumentException ("Incomplete binding - can't help you.", "binding");
        }

        /// <summary>Register your service(s).</summary>
        public static void Register(ServiceCollectionBinding binding)
        {
            if (null == Builder)
                throw new ArgumentException ("Please set ServiceCollection.Builder to something meaningful.", "Builder");
            ValidateBinding (binding);

            _the_way_is_shut.WaitOne ();
            try
            {
                if (!binding.Set)
                {
                    if (_b.ContainsKey (binding.Interface.First ()))
                        throw new ArgumentException ("Replacing an implementations is off limits.", "binding.Interface");
                    _b[binding.Interface.First ()] = binding;
                }
                else
                {
                    if (_s.ContainsKey (binding.Interface))
                        throw new ArgumentException ("Replacing an implementations is off limits.", "binding.Interface");
                    else _s[binding.Interface] = binding;
                }
            }
            finally
            {
                _the_way_is_shut.ReleaseMutex ();
            }
        }

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
                        throw new ArgumentException ("Unknown binding.", "binding");
                    _b.Remove (binding.Interface.First ());
                }
                else
                {
                    if (!_s.ContainsKey (binding.Interface))
                        throw new ArgumentException ("Unknown binding.", "binding");
                    _s.Remove (binding.Interface);
                }
            }
            finally
            {
                _the_way_is_shut.ReleaseMutex ();
            }
        }

        /// <summary>Returns true given the current level of constructor parameter types are present at the service collection.</summary>
        private static bool Resolvable(ParameterInfo[] p, ServiceCollectionBinding details)
        {
            //TODO42 shall I resolve by impl or iface or both?
            //     currently resolves by interface
            if (null == p) return false;
            if (0 == p.Length) return true;
            int r = 0;
            foreach (var itm in p)
            {
                //LATER perhaps its a good idea to update a dep. tree here: faster, dep. loop detection, more code - more bugs, etc.
                // look at the set 1st
                // see TODO42 if (details.Set && details.Implementation.Contains (itm.ParameterType)) { r++; continue; }
                if (details.Set && details.Interface.Contains (itm.ParameterType)) { r++; continue; }
                // Look at all sets?
                //TODO is there at least one objective reason setX services to be allowed to depend on setY one(s) given that setX has them not?
#if CROSS_SET_LOOKUP
                foreach (var kv in _s.Where (kv => kv.Value != details))
                    if (kv.Value.Implementation.Contains (itm.ParameterType)) { r++; continue; }
#endif
                // look at the 1:1 table
                foreach (var kv in _b)
                    // see TODO42 if (kv.Value.Implementation.First () == itm.ParameterType) { r++; continue; }
                    if (kv.Value.Interface.First () == itm.ParameterType) { r++; continue; }
            }
            return p.Length == r;
        }
        //TODO Resolvable() already has the info required by this one. Do not improve these functions because Resolvable() will
        //     build all the required info and these will be deleted.
        /*private static ServiceCollectionBinding BindingByImplementation(Type type, ServiceCollectionBinding details)
        {// see TODO42
            if (details.Set && details.Implementation.Contains (type)) return details;
#if CROSS_SET_LOOKUP
            foreach (var kv in _s.Where (kv => kv.Value != details))
                if (kv.Value.Implementation.Contains (type)) return kv.Value;
#endif
            foreach (var kv in _b)
                if (kv.Value.Implementation.Contains (type)) return kv.Value;
            throw new Exception (string.Format ("Can't find binding for Implementation \"{0}\"", type.FullName));
        }*/
        /// <summary>Returns the binding that bonds the <c>typeof(Interface)<c> as <c>"type"</c></summary>
        /// <param name="details">The <c>Set=true</c> binding to look for <c>typeof(Interface)</c></param>
        /// <remarks>It looks at details 1st if <c>details.Set</c>, and at the non-set ones 2nd.</remarks>
        private static ServiceCollectionBinding BindingByInterface(Type type, ServiceCollectionBinding details)
        {// see TODO42
            if (details.Set && details.Interface.Contains (type)) return details;
#if CROSS_SET_LOOKUP
            foreach (var kv in _s.Where (kv => kv.Value != details))
                if (kv.Value.Interface.Contains (type)) return kv.Value;
#endif
            foreach (var kv in _b)
                if (kv.Value.Interface.Contains (type)) return kv.Value;
            throw new Exception (string.Format ("Can't find binding for Interface \"{0}\"", type.FullName));
        }

        // reasons: less arguments for the recursive function; handles out of range access: less code at the said function;
        //          name - helps debug recursive function;
        /// <summary>A simple private <c>object[]</c> wrapper. Simplifies <c>Create()</c> and helps with debugging.</summary>
        [System.Diagnostics.DebuggerDisplay ("name={Name} index={Idx} arg_num={Args.Length}")]
        private class NamedArgArray
        {
            public NamedArgArray(string name = "Name me", int capacity = 1)
            {
                _a = new object[capacity];
                Idx = 0;
                Name = name;
            }
            private object[] _a = null;
            public object[] Args { get { return _a; } }
            public int Idx { get; set; } // state var bound to the Args[] array - last access index: foo.Args[foo.Idx]
            public string Name { get; private set; } // debug helper
            public object this[int index] // out of range access is allowed
            {
                get { return (index < 0 || index >= _a.Length) ? null : _a[index]; }
                set { if (index < 0 || index >= _a.Length) return; _a[index] = value; }
            }
            public static readonly NamedArgArray Empty = new NamedArgArray ();
        }

        private static int tmp_stack_overflow_protection = 0; // see TODO43
        //TODO Unspaghettify me.
        //     Don't write such functions: what follows has a lot of responsibilities within it making it hard to debug. maintain, and
        //     its probably riddled with bugs.
        //TODO It is supposed to choose a constructor with the biggest number of parameters whose service types are registered
        //     with the ServiceCollection; to be proved by the unit tests.
        /// <summary>
        /// A recursive function that creates a service, creating all its dependencies as needed honoring all bindings involved.
        /// Throws exceptions for bad arguments, wrong service configs, ambiguity situations, and some things I haven't noticed yet.
        /// </summary>
        private static object Create(Type iface, ServiceCollectionBinding details, NamedArgArray args)
        {
            if (tmp_stack_overflow_protection++ > (1 << 10)) // (1<<10)*3*(8 or 16 or who knows how much)
                throw new Exception ("Fixme: an endless loop probably");
            try
            {
                if (null == details)
                    throw new ArgumentException ("No null details this side of ...", "details");
                Type to_construct = details.GetImplementationType (iface);
                // select constructor
                ConstructorInfo ci = null;
                var constructors = to_construct.GetConstructors (bindingAttr: BindingFlags.Instance | BindingFlags.Public);
                if (null == constructors || constructors.Length <= 0)
                    throw new ArgumentException (string.Format ("No constructors found for \"{0}: {1}\"",
                        to_construct.Name, iface.Name), "details");
                const int TOO_MANY_CONSTRUCTORS = 1 << 3;
                if (constructors.Length > TOO_MANY_CONSTRUCTORS)
                    throw new ArgumentException (string.Format ("Too many constructors found for \"{0}: {1}\". Please consider a redesign.",
                        to_construct.Name, iface.Name), "details");
                var c_m = constructors.Where (x =>
                    {
                        var tmp = x.GetCustomAttributes (typeof (CallMeAttribute), inherit: false);
                        return null != tmp && 1 == tmp.Length;
                    });
                if (c_m.Count () > 1)
                    throw new ArgumentException ("Please mark exactly one public constructor with the CallMeAttribute", "details");
                if (c_m.Count () == 1) ci = c_m.First ();
                else // The short'n simple failed.
                {
                    // Select the one with the biggest number of parameters whose types can be resolved.
                    var bnp_set = constructors.Select (c =>
                        {
                            var p = c.GetParameters ();
                            return new { num = (null == p ? 0 : p.Length), ci = (Resolvable (p, details) ? c : null) };
                        }).Where (a => null != a.ci).OrderByDescending (a => a.num).Select (x => x.ci).ToArray ();
                    if (bnp_set.Length <= 0)
                        throw new NoSuitableConstructorException (string.Format (
                            "No suitable constructor found for service \"{0}\"", iface.Name));
                    if (bnp_set.Length > 1 && bnp_set[0].GetParameters ().Length == bnp_set[1].GetParameters ().Length)
                        throw new AmbigousConstructorException (string.Format (
                            "Ambigous constructors found for service \"{0}\"", iface.Name));
                    ci = bnp_set[0];
                }
                // Now that a constructor is selected, do employ the "divine" and be aware of stack overflows.
                var ci_params = ci.GetParameters ();
                if (0 == ci_params.Length)
                    args[args.Idx] = details.Create (Builder, to_construct, new object[] { });
                else
                {
                    NamedArgArray ci_args = new NamedArgArray (ci.ReflectedType.FullName, ci_params.Length);
                    ci_args.Idx = 0;
                    foreach (var param in ci_params)
                    {
                        //TODO43 replace with a stack
                        //see TODO42
                        //args[args.Idx] = Create (param.ParameterType, BindingByImplementation (param.ParameterType, details), ci_args);
                        ci_args[ci_args.Idx] = Create (param.ParameterType, BindingByInterface (param.ParameterType, details), ci_args);
                        ci_args.Idx++;
                    }
                    args[args.Idx] = details.Create (Builder, to_construct, ci_args.Args);
                }
                return args[args.Idx];
            }
            finally { tmp_stack_overflow_protection--; }
        }// private static object Create(Type iface, ServiceCollectionBinding details, NamedArgArray args)

        /// <summary>Creates your service(s) on demand.</summary>
        public static Interface Get<Interface>() where Interface : class
        {
            _the_way_is_shut.WaitOne ();
            try
            {
                // lets try with create on demand
                if (_b.ContainsKey (typeof (Interface))) return (Interface)Create (typeof (Interface), _b[typeof (Interface)], NamedArgArray.Empty);
                if (_s.Where (kv => kv.Key.Contains (typeof (Interface))).Count () > 0)
                    throw new Exception ("Please use GetBinding() for binding that contains type set");
                else
                    throw new ArgumentException (string.Format ("Unknown service: \"{0}\"", typeof (Interface).FullName), "Interface");
            }
            finally
            {
                _the_way_is_shut.ReleaseMutex ();
            }
        }

        //TODO perhaps yield return is not ok with WaitOne()
        /// <summary>
        /// Creates your service(s) on demand - the demand being governed by yield return.
        /// Use this when your binding contain a set of services itself. Just to be on the
        /// thread-safe side use your your own locking until the last yreturn is executed.
        /// </summary>
        public static IEnumerable<ServiceCollectionBinding> GetBinding<Interface>() where Interface : class
        {
            //TODO this threw AbandonedMutexException at least once when the test assembly was loaded by "testcentric"
            //     and "Examples" was ran. Reproduce: step after WaitOne; stop debugging; profit
            // OS object (after adding the random number above):
            //  Examples.exe: Mutant, \Sessions\1\BaseNamedObjects\ServiceCollectionMutex214940747, 0x5d8
            //  nunit-agent : Mutant, \Sessions\1\BaseNamedObjects\ServiceCollectionMutex1008353641, 0x5f8
            //TODO cross-domain singleton.
            //  Examples.exe: Mutant, \Sessions\1\BaseNamedObjects\ServiceCollectionMutex, 0x5f4
            //  nunit-agent : Mutant, \Sessions\1\BaseNamedObjects\ServiceCollectionMutex, 0x5e4
            _the_way_is_shut.WaitOne ();
            try
            {
                if (_b.ContainsKey (typeof (Interface)))
                    throw new Exception ("Please use Get() for bindings that contains no type set");
                foreach (var kv in _s)
                    if (kv.Key.Contains (typeof (Interface)))
                    {
                        Create (typeof (Interface), kv.Value, NamedArgArray.Empty);
                        yield return kv.Value;
                    }
                throw new ArgumentException (string.Format ("Unknown service: \"{0}\"", typeof (Interface).FullName), "Interface");
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

        //TODO Its strongly advisable to not change this during Get or GetBinding enumeration
        /// <summary>The "service" that actually does the object instantiation.</summary>
        public static IInstanceBuilder Builder { get; set; }
    }// public sealed class ServiceCollection
}
