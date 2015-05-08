using System;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Autofac.Builder;
using Autofac.Features.Metadata;
using Autofac.Integration.Mvc;
using NUnit.Framework;

namespace Autofac.Tests.Integration.Mvc
{
    [TestFixture]
    public class RegistrationExtensionsFixture
    {
        [Test]
        public void RegisterModelBinderProviderThrowsExceptionForNullBuilder()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Autofac.Integration.Mvc.RegistrationExtensions.RegisterModelBinderProvider(null));
            Assert.That(exception.ParamName, Is.EqualTo("builder"));
        }

        [Test]
        public void RegisterModelBindersThrowsExceptionForNullBuilder()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Autofac.Integration.Mvc.RegistrationExtensions.RegisterModelBinders(null, Assembly.GetExecutingAssembly()));
            Assert.That(exception.ParamName, Is.EqualTo("builder"));
        }

        [Test]
        public void RegisterModelBindersThrowsExceptionForNullAssemblies()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ContainerBuilder().RegisterModelBinders(null));
            Assert.That(exception.ParamName, Is.EqualTo("modelBinderAssemblies"));
        }

        [Test]
        public void AsModelBinderForTypesThrowsExceptionWhenAllTypesNullInList()
        {
            var builder = new ContainerBuilder();
            var registration = builder.RegisterType<TestModelBinder>();
            Assert.Throws<ArgumentException>(() => registration.AsModelBinderForTypes(null, null, null));
        }

        [Test]
        public void AsModelBinderForTypesThrowsExceptionForEmptyTypeList()
        {
            var types = new Type[0];
            var builder = new ContainerBuilder();
            var registration = builder.RegisterType<TestModelBinder>();
            Assert.Throws<ArgumentException>(() => registration.AsModelBinderForTypes(types));
        }

        [Test]
        public void AsModelBinderForTypesRegistersInstanceModelBinder()
        {
            IDependencyResolver originalResolver = null;
            try
            {
                originalResolver = DependencyResolver.Current;
                var builder = new ContainerBuilder();
                var binder = new TestModelBinder();
                builder.RegisterInstance(binder).AsModelBinderForTypes(typeof(TestModel1));
                var container = builder.Build();
                DependencyResolver.SetResolver(new AutofacDependencyResolver(container, new StubLifetimeScopeProvider(container)));
                var provider = new AutofacModelBinderProvider();
                Assert.AreSame(binder, provider.GetBinder(typeof(TestModel1)));
            }
            finally
            {
                DependencyResolver.SetResolver(originalResolver);
            }
        }

        [Test]
        public void AsModelBinderForTypesRegistersTypeModelBinder()
        {
            IDependencyResolver originalResolver = null;
            try
            {
                originalResolver = DependencyResolver.Current;
                var builder = new ContainerBuilder();
                builder.RegisterType<TestModelBinder>().AsModelBinderForTypes(typeof(TestModel1), typeof(TestModel2));
                var container = builder.Build();
                DependencyResolver.SetResolver(new AutofacDependencyResolver(container, new StubLifetimeScopeProvider(container)));
                var provider = new AutofacModelBinderProvider();
                Assert.IsInstanceOf<TestModelBinder>(provider.GetBinder(typeof(TestModel1)), "The model binder was not registered for TestModel1");
                Assert.IsInstanceOf<TestModelBinder>(provider.GetBinder(typeof(TestModel2)), "The model binder was not registered for TestModel2");
            }
            finally
            {
                DependencyResolver.SetResolver(originalResolver);
            }
        }

        [Test]
        public void AsModelBinderForTypesThrowsExceptionForNullRegistration()
        {
            IRegistrationBuilder<RegistrationExtensionsFixture, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration = null;
            Assert.Throws<ArgumentNullException>(() => registration.AsModelBinderForTypes(typeof(TestModel1)));
        }

        [Test]
        public void AsModelBinderForTypesThrowsExceptionForNullTypeList()
        {
            Type[] types = null;
            var builder = new ContainerBuilder();
            var registration = builder.RegisterType<TestModelBinder>();
            Assert.Throws<ArgumentNullException>(() => registration.AsModelBinderForTypes(types));
        }

        [Test]
        public void CacheInSessionThrowsExceptionForNullRegistration()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Autofac.Integration.Mvc.RegistrationExtensions.CacheInSession<object, SimpleActivatorData, SingleRegistrationStyle>(null));
            Assert.That(exception.ParamName, Is.EqualTo("registration"));
        }

        [Test]
        public void InvokesCustomActivating()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(GetType().Assembly)
                .OnActivating(e => ((TestController)e.Instance).Dependency = new object());

            var container = builder.Build();

            var controller = container.Resolve<TestController>();
            Assert.IsNotNull(controller.Dependency);
        }

        [Test]
        public void InjectsInvoker()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(GetType().Assembly)
                .InjectActionInvoker();
            builder.RegisterType<TestActionInvoker>().As<IActionInvoker>();
            var container = builder.Build();

            var controller = container.Resolve<TestController>();
            Assert.IsInstanceOf<TestActionInvoker>(controller.ActionInvoker);
        }


        [Test]
        public void DoesNotRegisterControllerTypesThatDoNotEndWithControllerString()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(GetType().Assembly);

            var container = builder.Build();

            Assert.IsFalse(container.IsRegistered<IsAControllerNot>());
        }

        static void AssertFilterRegistration<TFilter, TService>(FilterScope filterScope, MethodInfo methodInfo,
            Action<IRegistrationBuilder<TFilter, SimpleActivatorData, SingleRegistrationStyle>> configure, string metadataKey)
                where TFilter : new()
        {
            var builder = new ContainerBuilder();
            configure(builder.Register(c => new TFilter()));
            var container = builder.Build();

            var service = container.Resolve<Meta<TService>>();

            var metadata = (FilterMetadata)service.Metadata[metadataKey];

            Assert.That(metadata.ControllerType, Is.EqualTo(typeof(TestController)));
            Assert.That(metadata.FilterScope, Is.EqualTo(filterScope));
            Assert.That(metadata.MethodInfo, Is.EqualTo(methodInfo));
            Assert.That(metadata.Order, Is.EqualTo(20));
            Assert.That(service.Value, Is.InstanceOf<TService>());
        }
    }
}