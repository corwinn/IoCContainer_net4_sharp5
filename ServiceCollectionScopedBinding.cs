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
using System.Linq;

namespace IoCContainer_net4_sharp5
{
    // The scope is ServiceCollectionScopedBinding (composite it where you see fit: connection, session, thread, etc):
    //   scope1 = new ServiceCollectionScopedBinding(); scope1.Bind<ifoo, foo>(); scope1.Bind<ibar, bar>();
    //   scope2 = new ServiceCollectionScopedBinding(); scope2.Bind<ifoo, foo>(); scope2.Bind<ibar, bar>();
    //   ServiceCollection.Register (scope1);
    //   ServiceCollection.Register (scope2);
    // The scope:instance relation is 1:*.
    // You shall use GetBinding() instead of Get():
    //    ifoo service = ServiceCollection.GetBinding<ifoo> ().Cast<ServiceCollectionScopedBinding> ().Where (scope => scope == myscope).First ().Get<ifoo> ();
    // Its scary but you write it once; also, your scope is whatever you like: even a multi-keyed fractal database for example.
    /// <summary>A set binding that hosts many distinct services per scope (itself).</summary>
    public class ServiceCollectionScopedBinding : ServiceCollectionBinding
    {
        private class DE { public Type type = null; public object instance = null; } // dictionary entry
        private Dictionary<Type, DE> _instances = new Dictionary<Type, DE> ();

        public ServiceCollectionScopedBinding(ServiceCollectionBinding binding = null)
            : base (binding)
        {
            Set = true;
            this.Interface = _instances.Select (kv => kv.Key);
            this.Implementation = _instances.Select (kv => kv.Value.type);
        }

        public override object Create(IInstanceBuilder builder, Type type, object[] args)
        {
            foreach (var kv in _instances)
                if (kv.Value.type == type)
                    return kv.Value.instance ?? (kv.Value.instance = builder.Create (type, args));
            throw new ArgumentException ("Unknown type", "type");
        }

        /// <summary>Bind a service to this binding via its Interface and Implementation.</summary>
        public ServiceCollectionScopedBinding Bind<Interface, Implementation>()
            where Interface : class
            where Implementation : class
        {
            if (_instances.ContainsKey (typeof (Interface)))
                throw new ArgumentException ("Replacing an implementations is off limits.", "Interface");
            _instances[typeof (Interface)] = new DE { type = typeof (Implementation) };
            return this;
        }

        /// <summary>Retrieves a service by its Interface.</summary>
        public Interface Get<Interface>() where Interface : class
        {
            DE result = null;
            if (!_instances.TryGetValue (typeof (Interface), out result))
                throw new ArgumentException ("Type not found.", "Interface");
            if (null == result.instance)
                throw new Exception ("Fixme: service not created.");
            return (Interface)result.instance; // should throw exception when ! (result.instance is Interface)
        }
    }// ServiceCollectionScopedBinding
}
