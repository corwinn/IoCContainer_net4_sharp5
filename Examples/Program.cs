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
    A simple proof of concept program that shows it does what is supposed to do in a few use-cases.
*/

using IoCContainer_net4_sharp5;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
                {//VS_DEBUG_BREAKPOINT
                    Debug.WriteLine ("Fixme: UnhandledException.");
                };
            AppDomain.CurrentDomain.AssemblyResolve += (a, b) =>
                {//VS_DEBUG_BREAKPOINT
                    Debug.WriteLine ("Fixme: AssemblyResolve. ");
                    return null;
                };

            try
            {
                // global setup
                ServiceCollection.Builder = new DefaultInstanceBuilder ();

                Service1Impl.SimpleSingletonExample ();
                Service2Impl.SimpleSingletonMarkedConstructorExample ();
                Service3Impl.SimpleTransientExample ();
                Service4Impl.SimpleScopedExample ();
                Service5.SimpleAssemblySingletonExample ();
                Service6Impl.SimpleDepSingletonExample ();
                Service7Impl.SimpleDepSingletonExample2 ();
                Service8Impl.SimpleDepSingletonExampleDepth3 ();
                Service9Impl.SimpleConstructorSelectorSingletonExample ();

                // Simplicity ends here. I have a mostly working prototype. Time to write the unit tests.
            }
            catch (Exception e)
            {//VS_DEBUG_BREAKPOINT
                Console.WriteLine ("Fixme: Unhandled Exception.", e.ToString ());
            }
        }// static void Main(string[] args)
    }// class Program

    #region Service1
    interface IService1 { void foo(); }
    class Service1Impl : IService1
    {
        private static int uid = 0;

        public Service1Impl()
        {
            Console.WriteLine (string.Format ("Online: {0}{1} {2} no dependencies; one parameterless constructor;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }

        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
        }

        public static void SimpleSingletonExample()
        {
            Console.WriteLine ("----" + MethodBase.GetCurrentMethod ().Name + "----");

            // -- a.k.a configure -----------------
            // add singleton service known at compile time
            ServiceCollectionSingletonBinding b_Service1 = new ServiceCollectionSingletonBinding ();
            b_Service1.Bind<IService1, Service1Impl> ();
            ServiceCollection.Register (b_Service1);

            // -- a.k.a DI -----------------
            // instantiate on demand
            IService1 service1 = ServiceCollection.Get<IService1> ();
            // invoke the service
            service1.foo ();
            IService1 service1_ref2 = ServiceCollection.Get<IService1> ();
            service1_ref2.foo (); // should print exactly the same uid as service1.foo ();
            Debug.Assert (service1 == service1_ref2, "SingletonBinding failed");
        }
    }// class Service1Impl
    #endregion
    #region Service2
    interface IService2 { void foo(); }
    class Service2Impl : IService2
    {
        private static int uid = 0;

        public Service2Impl(char foo) { } // 1
        [ServiceCollection.CallMe]
        public Service2Impl()             // 2
        {
            Console.WriteLine (string.Format ("Online: {0}{1} {2} no dependencies; 1/3 ServiceCollection.CallMe parameterless constructor;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public Service2Impl(int bar) { }  // 3

        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
        }

        public static void SimpleSingletonMarkedConstructorExample()
        {
            Console.WriteLine ("----" + MethodBase.GetCurrentMethod ().Name + "----");

            // singleton with attribute-marked constructor
            ServiceCollectionSingletonBinding b_Service2 = new ServiceCollectionSingletonBinding ();
            b_Service2.Bind<IService2, Service2Impl> ();
            ServiceCollection.Register (b_Service2);
            IService2 service2 = ServiceCollection.Get<IService2> ();
            service2.foo ();
            IService2 service2_ref2 = ServiceCollection.Get<IService2> ();
            service2_ref2.foo ();
            Debug.Assert (service2 == service2_ref2, "SingletonBinding failed");
        }
    }// class Service2Impl
    #endregion
    #region Service3
    interface IService3 { void foo(); }
    class Service3Impl : IService3
    {
        private static int uid = 0;

        public Service3Impl()
        {
            Console.WriteLine (string.Format ("Online: {0}{1} {2} no dependencies; one parameterless constructor;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }

        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
        }

        public static void SimpleTransientExample()
        {
            Console.WriteLine ("----" + MethodBase.GetCurrentMethod ().Name + "----");

            // transient binding w/ single constructor
            ServiceCollectionTransientBinding b_Service3 = new ServiceCollectionTransientBinding ();
            b_Service3.Bind<IService3, Service3Impl> ();
            ServiceCollection.Register (b_Service3);
            IService3 service3 = ServiceCollection.Get<IService3> ();
            service3.foo ();
            IService3 service3_instance2 = ServiceCollection.Get<IService3> ();
            service3_instance2.foo ();
            Debug.Assert (service3 != service3_instance2, "TransientBinding failed");
        }
    }// class Service3Impl
    #endregion
    #region Service4
    interface IService4 { void foo(); }
    class Service4Impl : IService4
    {
        private static int uid = 0;

        public Service4Impl()
        {
            Console.WriteLine (string.Format ("Online: {0}{1} {2} no dependencies; one parameterless constructor;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }

        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
        }

        public static void SimpleScopedExample()
        {
            Console.WriteLine ("----" + MethodBase.GetCurrentMethod ().Name + "----");

            // scoped binding single constructor
            ServiceCollectionScopedBinding b_Service4_scope1 = new ServiceCollectionScopedBinding ();
            b_Service4_scope1.Bind<IService4, Service4Impl> ();
            ServiceCollection.Register (b_Service4_scope1);
            ServiceCollectionScopedBinding b_Service4_scope2 = new ServiceCollectionScopedBinding ();
            b_Service4_scope2.Bind<IService4, Service4Impl> ();
            ServiceCollection.Register (b_Service4_scope2);
            IService4 service4_scope1 = ServiceCollection.GetBinding<IService4> ()
                .Where (x => x is ServiceCollectionScopedBinding) // there could be other Set=true bindings
                .Cast<ServiceCollectionScopedBinding> () // for "ServiceCollectionScopedBinding.Get<Interface>()"
                .Where (b => b == b_Service4_scope1) // get the interested in scope only
                .First ()
                .Get<IService4> (); // get the service
            service4_scope1.foo ();
            IService4 service4_scope1_ref2 = ServiceCollection.GetBinding<IService4> ().Where (x => x is ServiceCollectionScopedBinding)
                .Cast<ServiceCollectionScopedBinding> ().Where (b => b == b_Service4_scope1).First ().Get<IService4> ();
            service4_scope1_ref2.foo ();
            Debug.Assert (service4_scope1 == service4_scope1_ref2, "ScopedBinding failed");
            IService4 service4_scope2 = ServiceCollection.GetBinding<IService4> ().Where (x => x is ServiceCollectionScopedBinding)
                .Cast<ServiceCollectionScopedBinding> ().Where (b => b == b_Service4_scope2).First ().Get<IService4> ();
            service4_scope2.foo ();
            IService4 service4_scope2_ref2 = ServiceCollection.GetBinding<IService4> ().Where (x => x is ServiceCollectionScopedBinding)
                .Cast<ServiceCollectionScopedBinding> ().Where (b => b == b_Service4_scope2).First ().Get<IService4> ();
            service4_scope2_ref2.foo ();
            Debug.Assert (service4_scope1 == service4_scope1_ref2, "ScopedBinding failed");      // one instance per scope
            Debug.Assert (service4_scope2 == service4_scope2_ref2, "ScopedBinding failed");      //
            Debug.Assert (service4_scope1 != service4_scope2, "ScopedBinding failed");           //
            Debug.Assert (service4_scope1_ref2 != service4_scope2_ref2, "ScopedBinding failed"); //
        }
    }// class Service4Impl
    #endregion
    #region Service5
    class Service5
    {
        public static void SimpleAssemblySingletonExample()
        {
            Console.WriteLine ("----" + MethodBase.GetCurrentMethod ().Name + "----");

            // add singleton service not known at compile time
            var startup_path = Assembly.GetExecutingAssembly ().Location;
            startup_path = Path.GetDirectoryName (startup_path);
            ServiceCollectionAssemblyBinding b_Service5 = new ServiceCollectionAssemblyBinding (
                startup_path + Path.DirectorySeparatorChar + "ExampleServiceAssembly.dll",
                new ServiceCollectionSingletonBinding ());
            b_Service5.Bind ("ExampleServiceAssembly.IService", "ExampleServiceAssembly.Service");
            ServiceCollection.Register (b_Service5);

            // instantiate on demand; you could just add the reference to where you're using the service or:
            //NOTE quick dirty code because proof of concept; never write code like this:
            //     always check the results prior dereferencing them and always handle the exceptions you can
            MethodInfo get_interface = typeof (ServiceCollection).GetMethod (
                name: "Get", bindingAttr: BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod (b_Service5.Interface.First ());

            // ExampleServiceAssembly.IService service5 = ServiceCollection.Get<IService> ();
            var service5 = get_interface.Invoke (null, null);

            // invoke the service
            // service5.foo ();
            service5.GetType ().GetMethod (name: "foo", bindingAttr: BindingFlags.Public | BindingFlags.Instance).Invoke (service5, null);
            var service5_ref2 = get_interface.Invoke (null, null);
            // service5_ref2.foo (); // should print exactly the same uid as service5.foo ();
            service5_ref2.GetType ().GetMethod (name: "foo", bindingAttr: BindingFlags.Public | BindingFlags.Instance).Invoke (service5_ref2, null);
            Debug.Assert (service5 == service5_ref2, "Assembly Singleton binding failed");
        }// public static void SimpleAssemblySingletonExample()
    }// class Service5
    #endregion
    #region Service6
    interface IService6_d1 { void foo(); }
    class Service6Impl_d1 : IService6_d1
    {
        private static int uid = 0;
        public Service6Impl_d1()
        {
            Console.WriteLine (string.Format ("Online: {0}{1} {2} no dependencies; one parameterless constructor;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
        }
    }
    interface IService6 { void foo(); }
    class Service6Impl : IService6
    {
        private static int uid = 0;
        private readonly IService6_d1 _d1;
        public Service6Impl(IService6_d1 d1)
        {
            _d1 = d1;
            Console.WriteLine (string.Format ("Online: {0}{1} {2} one dependency; one constructor with one parameter;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
            _d1.foo ();
        }
        public static void SimpleDepSingletonExample()
        {
            Console.WriteLine ("----" + MethodBase.GetCurrentMethod ().Name + "----");

            // setup
            ServiceCollectionSingletonBinding b_Service = new ServiceCollectionSingletonBinding ();
            b_Service.Bind<IService6, Service6Impl> ();
            ServiceCollection.Register (b_Service);
            b_Service = new ServiceCollectionSingletonBinding ();
            b_Service.Bind<IService6_d1, Service6Impl_d1> ();
            ServiceCollection.Register (b_Service);

            // instantiate on demand
            IService6 service6 = ServiceCollection.Get<IService6> ();
            // invoke the service
            service6.foo (); // should print Service6Impl1.foo and Service6Impl_d11
            IService6 service6_ref2 = ServiceCollection.Get<IService6> ();
            service6_ref2.foo (); // should print exactly the same uid as service6.foo ();
            Debug.Assert (service6 == service6_ref2, "SimpleDepSingletonExample failed");
        }
    }// class Service6Impl
    #endregion
    #region Service7
    interface IService7_d1 { void foo(); }
    class Service7Impl_d1 : IService7_d1
    {
        private static int uid = 0;
        public Service7Impl_d1()
        {
            Console.WriteLine (string.Format ("Online: {0}{1} {2} no dependencies; one parameterless constructor;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
        }
    }
    interface IService7_d2 { void foo(); }
    class Service7Impl_d2 : IService7_d2
    {
        private static int uid = 0;
        public Service7Impl_d2()
        {
            Console.WriteLine (string.Format ("Online: {0}{1} {2} no dependencies; one parameterless constructor;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
        }
    }
    interface IService7 { void foo(); }
    class Service7Impl : IService7
    {
        private static int uid = 0;
        private readonly IService7_d1 _d1;
        private readonly IService7_d2 _d2;
        public Service7Impl(IService7_d1 d1, IService7_d2 d2)
        {
            _d1 = d1;
            _d2 = d2;
            Console.WriteLine (string.Format ("Online: {0}{1} {2} two dependencies; one constructor with 2 distinct dep. parameters;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
            _d1.foo ();
            _d2.foo ();
        }
        public static void SimpleDepSingletonExample2()
        {
            Console.WriteLine ("----" + MethodBase.GetCurrentMethod ().Name + "----");

            // setup
            ServiceCollectionSingletonBinding b_Service = new ServiceCollectionSingletonBinding ();
            b_Service.Bind<IService7, Service7Impl> ();
            ServiceCollection.Register (b_Service);
            b_Service = new ServiceCollectionSingletonBinding ();
            b_Service.Bind<IService7_d1, Service7Impl_d1> ();
            ServiceCollection.Register (b_Service);
            b_Service = new ServiceCollectionSingletonBinding ();
            b_Service.Bind<IService7_d2, Service7Impl_d2> ();
            ServiceCollection.Register (b_Service);

            // instantiate on demand
            IService7 service7 = ServiceCollection.Get<IService7> ();
            // invoke the service
            service7.foo (); // should print Service7Impl1.foo and Service7Impl_d11 and Service7Impl_d21
            IService7 service7_ref2 = ServiceCollection.Get<IService7> ();
            service7_ref2.foo (); // should print exactly the same uid as service7.foo ();
            Debug.Assert (service7 == service7_ref2, "SimpleDepSingletonExample2 failed");
        }
    }// class Service7Impl
    #endregion
    #region Service8
    interface IService8_d1 { void foo(); }
    class Service8Impl_d1 : IService8_d1
    {
        private static int uid = 0;
        public Service8Impl_d1()
        {
            Console.WriteLine (string.Format ("Online: {0}{1} {2} no dependencies; one parameterless constructor;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
        }
    }
    interface IService8_d2 { void foo(); }
    class Service8Impl_d2 : IService8_d2
    {
        private static int uid = 0;
        private readonly IService8_d1 _d1;
        public Service8Impl_d2(IService8_d1 d1)
        {
            _d1 = d1;
            Console.WriteLine (string.Format ("Online: {0}{1} {2} one dependency; one constructor with one parameter;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
            _d1.foo ();
        }
    }
    interface IService8 { void foo(); }
    class Service8Impl : IService8
    {
        private static int uid = 0;
        private readonly IService8_d1 _d1;
        private readonly IService8_d2 _d2;
        public Service8Impl(IService8_d1 d1, IService8_d2 d2)
        {
            _d1 = d1;
            _d2 = d2;
            Console.WriteLine (string.Format ("Online: {0}{1} {2} two deps; one constructor with 2 distinct dep. parameters;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
            _d1.foo ();
            _d2.foo ();
        }
        public static void SimpleDepSingletonExampleDepth3()
        {
            Console.WriteLine ("----" + MethodBase.GetCurrentMethod ().Name + "----");
            /*      tree
              l0:     8      |   root
                     / \     |   /  \
              l1:   1   2    | leaf node
                         \   |        \
              l2:         1  |        leaf
            */

            // setup
            ServiceCollectionSingletonBinding b_Service = new ServiceCollectionSingletonBinding ();
            b_Service.Bind<IService8, Service8Impl> ();
            ServiceCollection.Register (b_Service);
            b_Service = new ServiceCollectionSingletonBinding ();
            b_Service.Bind<IService8_d1, Service8Impl_d1> ();
            ServiceCollection.Register (b_Service);
            b_Service = new ServiceCollectionSingletonBinding ();
            b_Service.Bind<IService8_d2, Service8Impl_d2> ();
            ServiceCollection.Register (b_Service);

            // instantiate on demand
            IService8 service8 = ServiceCollection.Get<IService8> ();
            // invoke the service
            service8.foo (); // should print Service8Impl1.foo and Service8Impl_d11 and Service8Impl_d21 Service8Impl_d11
            IService8 service8_ref2 = ServiceCollection.Get<IService8> ();
            service8_ref2.foo (); // should print exactly the same uid as service8.foo ();
            Debug.Assert (service8 == service8_ref2, "SimpleDepSingletonExampleDepth3 failed");
        }
    }// class Service8Impl
    #endregion
    #region Service9
    interface IService9_d1 { void foo(); }
    class Service9Impl_d1 : IService9_d1 { public void foo() { } }
    interface IService9 { void foo(); }
    class Service9Impl : IService9
    {
        private static int uid = 0;

        public Service9Impl() { throw new Exception ("I shouldn't be called"); }
        public Service9Impl(IService9_d1 d1) // this one should be chosen
        {
            Console.WriteLine (string.Format ("Online: {0}{1} {2} one dependency; 3 constructors;",
                this.GetType ().Name, (++uid).ToString (), Environment.NewLine));
        }
        public Service9Impl(IService9_d1 d1, int g) { throw new Exception ("I shouldn't be called"); }

        public void foo()
        {
            Console.WriteLine (string.Format ("{0}{1}.{2}",
                this.GetType ().Name, (uid).ToString (), MethodBase.GetCurrentMethod ().Name));
        }

        public static void SimpleConstructorSelectorSingletonExample()
        {
            Console.WriteLine ("----" + MethodBase.GetCurrentMethod ().Name + "----");

            ServiceCollectionSingletonBinding b_Service = new ServiceCollectionSingletonBinding ();
            ServiceCollection.Register (b_Service.Bind<IService9, Service9Impl> ());
            b_Service = new ServiceCollectionSingletonBinding ();
            ServiceCollection.Register (b_Service.Bind<IService9_d1, Service9Impl_d1> ());

            IService9 service9 = ServiceCollection.Get<IService9> (); // You should see "Online: "
            service9.foo ();
        }
    }// class Service9Impl
    #endregion
}
