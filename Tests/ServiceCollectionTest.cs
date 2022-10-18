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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
/*
    I'm not about to describe every test. It should be simple enough for you to quickly figure it out.
    Suffice to say: any code modification to the code being tested must keep this test suite happy.

    TODO Codegen: random service trees (.ctor dependencies) (CodeDOM); sync. with TODO43 prior loop gen.
*/
namespace IoCContainer_net4_sharp5.Tests
{
    [TestFixture]
    public class ServiceCollectionBaseState
    {
        [SetUp]
        public void setup() { ServiceCollection.Builder = null; }

        private interface IUnusedInterface { }
        private class UnusedClass : IUnusedInterface { }
        [Test, Category ("InitialState")]
        public void BuilderIsNull() { Assert.IsNull (ServiceCollection.Builder); }
        [Test, Category ("InitialState")]
        public void CantRegisterWithNullBuilder()
        {
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Register (new ServiceCollectionSingletonBinding ()));
        }
        [Test, Category ("InitialState")]
        public void CantDeregisterUnknownBinding()
        {
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Deregister (new ServiceCollectionSingletonBinding ()));
            var b = new ServiceCollectionSingletonBinding ();
            b.Bind<IUnusedInterface, UnusedClass> ();
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Deregister (b));
        }
        [Test, Category ("InitialState")]
        public void CantGetUnkownServices()
        {
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Get<IUnusedInterface> ());
        }
        [Test, Category ("InitialState")]
        public void CantGetUnkownBindings()
        {
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.GetBinding<IUnusedInterface> ().ToArray ());
        }
        [Test, Category ("InitialState")]
        public void CantGetUnkownBindingsYieldReturn()
        {
            Assert.IsNull (ServiceCollection.GetBinding<IUnusedInterface> ().GetEnumerator ().Current);
        }
        [Test, Category ("InitialState")]
        public void DoesNotHaveUnknownService()
        {
            Assert.IsFalse (ServiceCollection.Has<IUnusedInterface> ());
        }
    }// public class ServiceCollectionBaseState

    // Do cleanup after each test. Introducing Clear() or Reset() to ServiceCollection just for the sake of testing, won't happen.
    [TestFixture]
    public class ServiceCollectionExtendedTests
    {
        [SetUp]
        public void setup() { ServiceCollection.Builder = new DefaultInstanceBuilder (); }

        // Use to identify the distinct constructor that should have been called: set Trigger to true inside it.
        // Check _trigger for false prior each ServiceCollection.Get*
        private static bool _trigger = false;
        private static bool Trigger { get { var s = _trigger; _trigger = !_trigger; return s; } set { _trigger = value; } }

        #region Service1
        interface IService1 { bool foo(); }
        class Service1_PublicParameterless1 : IService1
        {
            public bool foo() { return Trigger; }
            public Service1_PublicParameterless1() { Trigger = true; } // mark the constructor that is expected to be called
            public Service1_PublicParameterless1(int bar) { }
        }
        class Service1_ProtectedParameterless1 : IService1
        {
            public bool foo() { return true; }
            protected Service1_ProtectedParameterless1() { }
            public Service1_ProtectedParameterless1(int bar) { }
        }
        class Service1_ImplicitParameterless1 : IService1 { public bool foo() { return true; } }
        class Service1_ImplicitParameterless2 : IService1 { public bool foo() { return true; } static Service1_ImplicitParameterless2() { } }
        #endregion

        [Test, Category (".Collection")]
        public void HasServiceRegisterDeregisterNotASet()
        {
            ServiceCollectionSingletonBinding b = new ServiceCollectionSingletonBinding ();
            b.Bind<IService1, Service1_PublicParameterless1> ();
            Assert.IsFalse (b.Set);
            Assert.IsFalse (ServiceCollection.Has<IService1> ());
            ServiceCollection.Register (b);
            Assert.IsTrue (ServiceCollection.Has<IService1> ());
            ServiceCollection.Deregister (b);
            Assert.IsFalse (ServiceCollection.Has<IService1> ());
        }
        [Test, Category (".Collection")]
        public void HasServiceRegisterDeregisterASet()
        {
            ServiceCollectionScopedBinding b = new ServiceCollectionScopedBinding ();
            b.Bind<IService1, Service1_PublicParameterless1> ();
            Assert.IsTrue (b.Set);
            Assert.IsFalse (ServiceCollection.Has<IService1> ());
            ServiceCollection.Register (b);
            Assert.IsTrue (ServiceCollection.Has<IService1> ());
            ServiceCollection.Deregister (b);
            Assert.IsFalse (ServiceCollection.Has<IService1> ());
        }
        [Test, Category (".Collection")]
        public void EmptyBindingRegistrationASetAndNotASet()
        {
            ServiceCollectionSingletonBinding foo = new ServiceCollectionSingletonBinding ();
            ServiceCollectionScopedBinding bar = new ServiceCollectionScopedBinding ();
            Assert.IsFalse (foo.Set);
            Assert.IsTrue (bar.Set);
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Register (new ServiceCollectionSingletonBinding ()));
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Register (new ServiceCollectionScopedBinding ()));
        }
        [Test, Category (".Collection")]
        public void EmptyBindingDeregistrationASetAndNotASet()
        {
            ServiceCollectionSingletonBinding foo = new ServiceCollectionSingletonBinding ();
            ServiceCollectionScopedBinding bar = new ServiceCollectionScopedBinding ();
            Assert.IsFalse (foo.Set);
            Assert.IsTrue (bar.Set);
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Deregister (new ServiceCollectionSingletonBinding ()));
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Deregister (new ServiceCollectionScopedBinding ()));
        }
        [Test, Category (".Collection")]
        public void AskingForUnknownService()
        {
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Get<IService1> ());
        }
        [Test, Category (".Collection")]
        public void AskingForUnknownBinding()
        {
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.GetBinding<IService1> ().ToArray ());
        }
        [Test, Category (".Collection")]
        public void AskingForUnknownBindingYieldReturn()
        {
            Assert.IsNull (ServiceCollection.GetBinding<IService1> ().GetEnumerator ().Current);
        }
        [Test, Category (".Collection")]
        public void DuplicateRegistrationNotASet()
        {
            ServiceCollectionSingletonBinding b = new ServiceCollectionSingletonBinding ();
            Assert.IsFalse (b.Set);
            ServiceCollection.Register (b.Bind<IService1, Service1_PublicParameterless1> ());
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Register (b));
            ServiceCollection.Deregister (b);
        }
        [Test, Category (".Collection")]
        public void DuplicateRegistrationASet()
        {
            ServiceCollectionScopedBinding b = new ServiceCollectionScopedBinding ();
            Assert.IsTrue (b.Set);
            ServiceCollection.Register (b.Bind<IService1, Service1_PublicParameterless1> ());
            Assert.Catch (typeof (ArgumentException), () => ServiceCollection.Register (b));
            ServiceCollection.Deregister (b);
        }

        [Test, Category (".Singleton")]
        public void IndependentService()
        {
            ServiceCollectionSingletonBinding b = new ServiceCollectionSingletonBinding ();
            ServiceCollection.Register (b.Bind<IService1, Service1_PublicParameterless1> ());

            Assert.IsFalse (_trigger);
            var s = ServiceCollection.Get<IService1> ();
            Assert.IsTrue (s.foo ());
            ServiceCollection.Deregister (b);
        }
        [Test, Category (".Singleton")]
        public void IndependentServiceProtectedConstructor()
        {
            ServiceCollectionSingletonBinding b = new ServiceCollectionSingletonBinding ();
            ServiceCollection.Register (b.Bind<IService1, Service1_ProtectedParameterless1> ());

            Assert.IsFalse (_trigger);
            Assert.Catch (typeof (NoSuitableConstructorException), () => ServiceCollection.Get<IService1> ());
            Assert.IsFalse (_trigger);
            ServiceCollection.Deregister (b);
        }
        [Test, Category (".Singleton")]
        public void IndependentServiceImplicitConstructor()
        {
            ServiceCollectionSingletonBinding b = new ServiceCollectionSingletonBinding ();
            ServiceCollection.Register (b.Bind<IService1, Service1_ImplicitParameterless1> ());

            Assert.IsFalse (_trigger);
            var s = ServiceCollection.Get<IService1> ();
            Assert.IsTrue (s.foo ());
            ServiceCollection.Deregister (b);

            b = new ServiceCollectionSingletonBinding ();
            ServiceCollection.Register (b.Bind<IService1, Service1_ImplicitParameterless2> ());
            Assert.IsFalse (_trigger);
            s = ServiceCollection.Get<IService1> ();
            Assert.IsTrue (s.foo ());
            ServiceCollection.Deregister (b);
        }

        private delegate void BinaryEqualityTest(object a, object b);
        private void IndependentServiceFromAssembly(ServiceCollectionBinding decorated, BinaryEqualityTest test)
        {
            Assert.NotNull (decorated, message: "'decorated' shan't be null", args: null);
            Assert.NotNull (test, message: "'test' shan't be null", args: null);
            ServiceCollectionAssemblyBinding b = new ServiceCollectionAssemblyBinding (
                Assembly.Load (ExampleServiceAssemblyAsByteArray.for_unit_testing), decorated);
            ServiceCollection.Register (b.Bind ("ExampleServiceAssembly.IService", "ExampleServiceAssembly.Service"));

            MethodInfo get_interface = typeof (ServiceCollection).GetMethod (
                name: "Get", bindingAttr: BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod (b.Interface.First ());

            // ExampleServiceAssembly.IService service = ServiceCollection.Get<IService> ();
            var service = get_interface.Invoke (null, null);

            // service.foo ();
            service.GetType ().GetMethod (name: "foo", bindingAttr: BindingFlags.Public | BindingFlags.Instance).Invoke (service, null);

            var service_ref2 = get_interface.Invoke (null, null);
            // service_ref2.foo ();
            service_ref2.GetType ().GetMethod (name: "foo", bindingAttr: BindingFlags.Public | BindingFlags.Instance)
                .Invoke (service_ref2, null);

            test (service, service_ref2);
            ServiceCollection.Deregister (b);
        }// IndependentServiceFromAssembly
        [Test, Category (".Assembly")]
        public void IndependentServiceFromAssemblySingleton()
        {
            IndependentServiceFromAssembly (new ServiceCollectionSingletonBinding (), (a, b) => Assert.AreEqual (a, b));
        }
        [Test, Category (".Assembly")]
        public void IndependentServiceFromAssemblyTransient() // diff --color-words IndependentServiceFromAssemblySingleton IndependentServiceFromAssemblyTransient
        {
            IndependentServiceFromAssembly (new ServiceCollectionTransientBinding (), (a, b) => Assert.AreNotEqual (a, b));
        }

        //TODO add some dependency resolution scenarios and not all bindings: each binding goes to its separate test unit
    }// public class ServiceCollectionExtendedTests
}
