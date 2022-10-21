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
using System.Reflection;

namespace IoCContainer_net4_sharp5
{
    // Lets say you want a singleton where the type bond comes from an assembly:
    // new ServiceCollectionAssemblyBinding ("org.bigcorp.bestcode", new ServiceCollectionSingletonBinding ())
    //LATER multi-domain/and_or AppDomain.CurrentDomain.SetupInformation.ShadowCopy*
    /// <summary>Extends the <c>"ServiceCollection"</c> with the ability to load services from assemblies not known at compile time.</summary>
    public class ServiceCollectionAssemblyBinding : ServiceCollectionBinding
    {
        private string _asm_fname = "";
        private Assembly _asm = null;
        private string _iface_name = "";
        private string _impl_name = "";

        /// <summary>This is a decorator that isn't hosting any services - a binding that does so is required.</summary>
        private ServiceCollectionAssemblyBinding(string assembly_file_name, Assembly assembly, ServiceCollectionBinding binding)
            : base (binding)
        {
            if (null == binding)
                throw new ArgumentException ("Can't decorate null. Perhaps IDecorate<decorated> or something?", "binding");
            if (null == assembly && string.IsNullOrEmpty (assembly_file_name))
                throw new ArgumentException ("Try specifying a valid assembly file name. Test it using Assembly.Load().", "assembly_file_name");
            if (string.IsNullOrEmpty (assembly_file_name) && null == assembly)
                throw new ArgumentException ("Try specifying a valid assembly. Use Assembly.Load().", "assembly");
            _asm_fname = assembly_file_name;
            _asm = assembly;
        }

        private void EnsureInitialStateOk()
        {
            if (string.IsNullOrEmpty (_asm_fname) && null == _asm)
                throw new Exception ("You forgot to call the big private constructor.");
        }

        /// <summary>Construct via assembly file name - full path could be required.</summary>
        public ServiceCollectionAssemblyBinding(string assembly_fname, ServiceCollectionBinding binding)
            : this (assembly_fname, assembly: null, binding: binding) { }
        /// <summary>Construct via assembly reference.</summary>
        public ServiceCollectionAssemblyBinding(Assembly assembly, ServiceCollectionBinding binding)
            : this (assembly_file_name: "", assembly: assembly, binding: binding) { }
        // etc. When adding new constructors, please do call the big private constructor above, or bad things will happen.

        public override object Create(IInstanceBuilder builder, Type type, object[] args)
        {
            LoadTypes ();
            return base.Create (builder, type, args);
        }

        // Returns not null or throws any exception you can imagine.
        // type_name: the complete type name: foo.bar.fancy_type for example
        private static Type LoadType(Assembly assembly, string type_name)
        {
            Type result = null;
            try
            {
                result = assembly.GetType (name: type_name, throwOnError: true, ignoreCase: false);
                if (null == result)
                    throw new Exception (".net framework bug: \"" + Environment.Version.ToString () + "\"");
            }
            catch (TypeLoadException e)
            {
                throw new ArgumentException (string.Format ("\"{0}\" not found at \"{1}\"", type_name, assembly.FullName), "Bind.iface", e);
            }
            catch (Exception e)
            {
                throw new Exception (string.Format ("Unhandled exception while looking for \"{0}\" at \"{1}\"", type_name, assembly.FullName), e);
            }
            return result;
        }

        private void LoadTypes()
        {
            EnsureInitialStateOk ();
            if (null != Interface && null != Implementation) return; // no Transient Assembly loading

            // This is a scary function - be aware! - loading assemblies is never simple. Assume (this time only) time consuming.
            //TODONT optimize; no point: you can't know what will happen during the next call or how much time it will take because:
            //       S.o. could employ custom missing dependencies resolution, where s.o. could ask the user, via an interactive UI,
            //       to locate the missing dependencies, who can go, to lets say dinner, leaving the file dialog open and locate them the
            //       next week if he decides to; or s.o. could invoke the CodeDOM to build something on the fly; or s.o. could invoke
            //       some network deployment wonder to deliver the required assemblies. Good enough?
            if (null == _asm) _asm = Assembly.LoadFile (_asm_fname);

            Interface = new Type[] { LoadType (assembly: _asm, type_name: _iface_name) };
            Implementation = new Type[] { LoadType (assembly: _asm, type_name: _impl_name) };
        }

        /// <summary>Bind via full (including all namespaces) type names as a string.</summary>
        public ServiceCollectionAssemblyBinding Bind(string iface, string impl)
        {
            _iface_name = iface;
            _impl_name = impl;
            LoadTypes ();
            return this;
        }
    }// public class ServiceCollectionAssemblyBinding
}
