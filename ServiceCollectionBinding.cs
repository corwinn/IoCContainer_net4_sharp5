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
    // Adapt your binding to the service collection. The idea is to enable you to extend the ServiceCollection.
    // Instead of .AddScoped(), .AddSingleton(), you .Register(Scoped), .Register(Singleton), ...
    //  * Lowers responsibilities per class
    //  * Create binding per type if needs be - you could do your private type setup at the binding
    // ServiceCollection is a singleton by design, so there is no explicit contract here. Access it via its public static interface if needs be.
    // Direct descendants of ServiceCollectionBinding that init the base decorator with null and then call the base methods will
    // be rewarded with CantBeImplementedException; just don't do that.
    /// <summary>
    /// Adapts your binding to the service collection. Employs Visitor (to abstract away the actual object construction) and
    /// Decorator (to simplify adapter code) as well.
    /// </summary>
    public abstract class ServiceCollectionBinding
    {
        // Avoids "if (null == _b)"
        private class ServiceCollectionBindingIfNull : ServiceCollectionBinding
        {
            private static readonly string DIRECTIONS = "Turn back.";
            public class CantBeImplementedException : NotImplementedException { public CantBeImplementedException() : base (DIRECTIONS) { } }
            public override object Create(IInstanceBuilder b, Type t, object[] a) { throw new CantBeImplementedException (); }
        }
        protected ServiceCollectionBinding _b = null;
        protected ServiceCollectionBinding() { }
        public ServiceCollectionBinding(ServiceCollectionBinding binding = null) { _b = binding ?? new ServiceCollectionBindingIfNull (); }

        /// <summary>Returns Implementation by Interface.</summary>
        public Type GetImplementationType(Type iface)
        {
            if (!Set)
            {
                if (iface != Interface.First ())
                    throw new ArgumentException ("Fixme: odd interface request", "iface");
                return Implementation.First ();
            }
            var tmp = Implementation.Where (t => iface.IsAssignableFrom (t));
            //TODO verify tmp vs tmp2; IsSubclassOf() works not
            //var tmp2 = Implementation.Where (t => null != t.GetInterface (iface.Name)).ToArray ();
            if (tmp.Count () <= 0)
                throw new ArgumentException (string.Format ("No implementor of \"{0}\" found", iface.FullName), "iface");
            if (tmp.Count () > 1)
                throw new ArgumentException (string.Format ("Too many implementors of \"{0}\" found", iface.FullName), "iface");
            return tmp.First ();
        }

        // These will be called by the ServiceCollection "build()" thread. Consider them a model.
        //TODO should this arg list become ServiceCollectionBindingCreateArguments? It should.
        /// <summary>
        /// Lets the <c>"builder"</c> create the service instance of <c>"impl_type"</c> using <c>"args"</c> constructor parameters.
        /// </summary>
        public virtual object Create(IInstanceBuilder builder, Type impl_type, object[] args) { return _b.Create (builder, impl_type, args); }
        
        /// <summary>The service Interface(s) associated with this binding</summary>
        public IEnumerable<Type> Interface { get; protected set; }

        /// <summary>The service Implementation(s) associated with this binding</summary>
        public IEnumerable<Type> Implementation { get; protected set; }

        // When this is true the IEnumerable becomes the key i.e. you state that your binding manages distinct "Interface"s -
        // not the ServiceCollection. The dependencies are looked for at the set 1st.
        /// <summary>Whether or not the binding encapsulates its own private service collection.</summary>
        public bool Set { get; protected set; }
    }// public abstract class ServiceCollectionBinding
}
