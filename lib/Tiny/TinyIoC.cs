﻿//===============================================================================
// TinyIoC
//
// An easy to use, hassle free, Inversion of Control Container for small projects
// and beginners alike.
//
// http://hg.grumpydev.com/tinyioc
//===============================================================================
// Copyright © Steven Robbins.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================

#region Preprocessor Directives
// Uncomment this line if you want the container to automatically
// register the TinyMessenger messenger/event aggregator
//#define TINYMESSENGER

// Preprocessor directives for enabling/disabling functionality
// depending on platform features. If the platform has an appropriate
// #DEFINE then these should be set automatically below.
#define EXPRESSIONS                         // Platform supports System.Linq.Expressions
//#define APPDOMAIN_GETASSEMBLIES             // Platform supports getting all assemblies from the AppDomain object
#define UNBOUND_GENERICS_GETCONSTRUCTORS    // Platform supports GetConstructors on unbound generic types

// CompactFramework
// By default does not support System.Linq.Expressions.
// AppDomain object does not support enumerating all assemblies in the app domain.
#if PocketPC || WINDOWS_PHONE
#undef EXPRESSIONS
#undef APPDOMAIN_GETASSEMBLIES
#undef UNBOUND_GENERICS_GETCONSTRUCTORS
#endif

#if SILVERLIGHT
#undef APPDOMAIN_GETASSEMBLIES
#endif
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
#if EXPRESSIONS

#endif

namespace NBehave.Narrator.Framework.Tiny
{

    #region SafeDictionary
    public class SafeDictionary<TKey, TValue> : IDisposable
    {
        private readonly object _Padlock = new object();
        private readonly Dictionary<TKey, TValue> _Dictionary = new Dictionary<TKey, TValue>();

        public TValue this[TKey key]
        {
            set
            {
                lock (_Padlock)
                {
                    TValue current;
                    if (_Dictionary.TryGetValue(key, out current))
                    {
                        var disposable = current as IDisposable;

                        if (disposable != null)
                            disposable.Dispose();
                    }

                    _Dictionary[key] = value;
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_Padlock)
            {
                return _Dictionary.TryGetValue(key, out value);
            }
        }

        public bool Remove(TKey key)
        {
            lock (_Padlock)
            {
                return _Dictionary.Remove(key);
            }
        }

        public void Clear()
        {
            lock (_Padlock)
            {
                _Dictionary.Clear();
            }
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                return _Dictionary.Keys;
            }
        }
        #region IDisposable Members

        public void Dispose()
        {
            lock (_Padlock)
            {
                var disposableItems = from item in _Dictionary.Values
                                      where item is IDisposable
                                      select item as IDisposable;

                foreach (var item in disposableItems)
                {
                    item.Dispose();
                }

                this.Clear();
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
    #endregion

    #region TinyIoC Exception Types
    public class TinyIoCResolutionException : Exception
    {
        private const string ERROR_TEXT = "Unable to resolve type: {0}";

        public TinyIoCResolutionException(Type type)
            : base(String.Format(ERROR_TEXT, type.FullName))
        {
        }

        public TinyIoCResolutionException(Type type, Exception innerException)
            : base(String.Format(ERROR_TEXT, type.FullName), innerException)
        {
        }
    }

    public class TinyIoCRegistrationTypeException : Exception
    {
        private const string REGISTER_ERROR_TEXT = "Cannot register type {0} - abstract classes or interfaces are not valid implementation types for {1}.";

        public TinyIoCRegistrationTypeException(Type type, string factory)
            : base(String.Format(REGISTER_ERROR_TEXT, type.FullName, factory))
        {
        }

        public TinyIoCRegistrationTypeException(Type type, string factory, Exception innerException)
            : base(String.Format(REGISTER_ERROR_TEXT, type.FullName, factory), innerException)
        {
        }
    }

    public class TinyIoCRegistrationException : Exception
    {
        private const string CONVERT_ERROR_TEXT = "Cannot convert current registration of {0} to {1}";

        public TinyIoCRegistrationException(Type type, string method)
            : base(String.Format(CONVERT_ERROR_TEXT, type.FullName, method))
        {
        }

        public TinyIoCRegistrationException(Type type, string method, Exception innerException)
            : base(String.Format(CONVERT_ERROR_TEXT, type.FullName, method), innerException)
        {
        }
    }

    public class TinyIoCWeakReferenceException : Exception
    {
        private const string ERROR_TEXT = "Unable to instantiate {0} - referenced object has been reclaimed";

        public TinyIoCWeakReferenceException(Type type)
            : base(String.Format(ERROR_TEXT, type.FullName))
        {
        }

        public TinyIoCWeakReferenceException(Type type, Exception innerException)
            : base(String.Format(ERROR_TEXT, type.FullName), innerException)
        {
        }
    }

    public class TinyIoCConstructorResolutionException : Exception
    {
        private const string ERROR_TEXT = "Unable to resolve constructor for {0} using provided Expression.";

        public TinyIoCConstructorResolutionException(Type type)
            : base(String.Format(ERROR_TEXT, type.FullName))
        {
        }

        public TinyIoCConstructorResolutionException(Type type, Exception innerException)
            : base(String.Format(ERROR_TEXT, type.FullName), innerException)
        {
        }

        public TinyIoCConstructorResolutionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public TinyIoCConstructorResolutionException(string message)
            : base(message)
        {
        }
    }

    public class TinyIoCAutoRegistrationException : Exception
    {
        private const string ERROR_TEXT = "Duplicate implementation of type {0} found ({1}).";

        public TinyIoCAutoRegistrationException(Type registerType, IEnumerable<Type> types)
            : base(String.Format(ERROR_TEXT, registerType, GetTypesString(types)))
        {
        }

        public TinyIoCAutoRegistrationException(Type registerType, IEnumerable<Type> types, Exception innerException)
            : base(String.Format(ERROR_TEXT, registerType, GetTypesString(types)), innerException)
        {
        }

        private static string GetTypesString(IEnumerable<Type> types)
        {
            var typeNames = from type in types
                            select type.FullName;

            return string.Join(",", typeNames.ToArray());
        }
    }
    #endregion

    #region Public Setup / Settings Classes
    /// <summary>
    /// Name/Value pairs for specifying "user" parameters when resolving
    /// </summary>
    public sealed class NamedParameterOverloads : Dictionary<string, object>
    {
        public static NamedParameterOverloads FromIDictionary(IDictionary<string, object> data)
        {
            return data as NamedParameterOverloads ?? new NamedParameterOverloads(data);
        }

        public NamedParameterOverloads()
        {
        }

        public NamedParameterOverloads(IDictionary<string, object> data)
            : base(data)
        {
        }

        private static readonly NamedParameterOverloads _Default = new NamedParameterOverloads();

        public static NamedParameterOverloads Default
        {
            get
            {
                return _Default;
            }
        }
    }

    public enum UnregisteredResolutionActions
    {
        /// <summary>
        /// Attempt to resolve type, even if the type isn't registered.
        /// 
        /// Registered types/options will always take precedence.
        /// </summary>
        AttemptResolve,

        /// <summary>
        /// Fail resolution if type not explicitly registered
        /// </summary>
        Fail,

        /// <summary>
        /// Attempt to resolve unregistered type if requested type is generic
        /// and no registration exists for the specific generic parameters used.
        /// 
        /// Registered types/options will always take precedence.
        /// </summary>
        GenericsOnly
    }

    public enum NamedResolutionFailureActions
    {
        AttemptUnnamedResolution,
        Fail
    }

    /// <summary>
    /// Resolution settings
    /// </summary>
    public sealed class ResolveOptions
    {
        private static readonly ResolveOptions _Default = new ResolveOptions();

        private UnregisteredResolutionActions _UnregisteredResolutionAction = UnregisteredResolutionActions.AttemptResolve;
        public UnregisteredResolutionActions UnregisteredResolutionAction
        {
            get { return _UnregisteredResolutionAction; }
            set { _UnregisteredResolutionAction = value; }
        }

        private NamedResolutionFailureActions _NamedResolutionFailureAction = NamedResolutionFailureActions.Fail;
        public NamedResolutionFailureActions NamedResolutionFailureAction
        {
            get { return _NamedResolutionFailureAction; }
            set { _NamedResolutionFailureAction = value; }
        }

        public static ResolveOptions Default
        {
            get
            {
                return _Default;
            }
        }
    }
    #endregion

    public sealed class TinyIoCContainer : IDisposable
    {
        #region "Fluent" API
        /// <summary>
        /// Registration options for "fluent" API
        /// </summary>
        /// <typeparam name="RegisterType">Registered type</typeparam>
        /// <typeparam name="RegisterImplementation">Implementation type for construction of RegisteredType</typeparam>
        public sealed class RegisterOptions
        {
            private TinyIoCContainer _Container;
            private TypeRegistration _Registration;

            public RegisterOptions(TinyIoCContainer container, TypeRegistration registration)
            {
                _Container = container;
                _Registration = registration;
            }

            /// <summary>
            /// Make registration a singleton (single instance) if possible
            /// </summary>
            /// <returns>RegisterOptions</returns>
            /// <exception cref="TinyIoCInstantiationTypeException"></exception>
            public RegisterOptions AsSingleton()
            {
                var currentFactory = _Container.GetCurrentFactory(_Registration);

                if (currentFactory == null)
                    throw new TinyIoCRegistrationException(_Registration.Type, "singleton");

                return _Container.AddUpdateRegistration(_Registration, currentFactory.SingletonVariant);
            }

            /// <summary>
            /// Make registration multi-instance if possible
            /// </summary>
            /// <returns>RegisterOptions</returns>
            /// <exception cref="TinyIoCInstantiationTypeException"></exception>
            public RegisterOptions AsMultiInstance()
            {
                var currentFactory = _Container.GetCurrentFactory(_Registration);

                if (currentFactory == null)
                    throw new TinyIoCRegistrationException(_Registration.Type, "multi-instance");

                return _Container.AddUpdateRegistration(_Registration, currentFactory.MultiInstanceVariant);
            }

            /// <summary>
            /// Make registration hold a weak reference if possible
            /// </summary>
            /// <returns>RegisterOptions</returns>
            /// <exception cref="TinyIoCInstantiationTypeException"></exception>
            public RegisterOptions WithWeakReference()
            {
                var currentFactory = _Container.GetCurrentFactory(_Registration);

                if (currentFactory == null)
                    throw new TinyIoCRegistrationException(_Registration.Type, "weak reference");

                return _Container.AddUpdateRegistration(_Registration, currentFactory.WeakReferenceVariant);
            }

            /// <summary>
            /// Make registration hold a strong reference if possible
            /// </summary>
            /// <returns>RegisterOptions</returns>
            /// <exception cref="TinyIoCInstantiationTypeException"></exception>
            public RegisterOptions WithStrongReference()
            {
                var currentFactory = _Container.GetCurrentFactory(_Registration);

                if (currentFactory == null)
                    throw new TinyIoCRegistrationException(_Registration.Type, "strong reference");

                return _Container.AddUpdateRegistration(_Registration, currentFactory.StrongReferenceVariant);
            }

#if EXPRESSIONS
            public RegisterOptions UsingConstructor<RegisterType>(Expression<Func<RegisterType>> constructor)
            {
                var lambda = constructor as LambdaExpression;
                if (lambda == null)
                    throw new TinyIoCConstructorResolutionException(typeof(RegisterType));

                var newExpression = lambda.Body as NewExpression;
                if (newExpression == null)
                    throw new TinyIoCConstructorResolutionException(typeof(RegisterType));

                var constructorInfo = newExpression.Constructor;
                if (constructorInfo == null)
                    throw new TinyIoCConstructorResolutionException(typeof(RegisterType));

                var currentFactory = _Container.GetCurrentFactory(_Registration);
                if (currentFactory == null)
                    throw new TinyIoCConstructorResolutionException(typeof(RegisterType));

                currentFactory.SetConstructor(constructorInfo);

                return this;
            }
#endif
        }
        #endregion

        #region Public API
        #region Child Containers
        public TinyIoCContainer GetChildContainer()
        {
            return new TinyIoCContainer(this);
        }
        #endregion

        #region Registration
        /// <summary>
        /// Attempt to automatically register all non-generic classes and interfaces in the current app domain.
        /// 
        /// If more than one class implements an interface then only one implementation will be registered
        /// although no error will be thrown.
        /// </summary>
        public void AutoRegister()
        {
#if APPDOMAIN_GETASSEMBLIES
            AutoRegisterInternal(AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsIgnoredAssembly(a)), true);
#else
            AutoRegisterInternal(new Assembly[] {this.GetType().Assembly}, true);
#endif
        }

        /// <summary>
        /// Attempt to automatically register all non-generic classes and interfaces in the current app domain.
        /// </summary>
        /// <param name="ignoreDuplicateImplementations">Whether to ignore duplicate implementations of an interface/base class. False=throw an exception</param>
        /// <exception cref="TinyIoCAutoRegistrationException"/>
        public void AutoRegister(bool ignoreDuplicateImplementations)
        {
#if APPDOMAIN_GETASSEMBLIES
            AutoRegisterInternal(AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsIgnoredAssembly(a)), ignoreDuplicateImplementations);
#else
            AutoRegisterInternal(new Assembly[] { this.GetType().Assembly }, ignoreDuplicateImplementations);
#endif
        }

        /// <summary>
        /// Attempt to automatically register all non-generic classes and interfaces in the specified assembly
        /// 
        /// If more than one class implements an interface then only one implementation will be registered
        /// although no error will be thrown.
        /// </summary>
        /// <param name="assembly">Assembly to process</param>
        public void AutoRegister(Assembly assembly)
        {
            AutoRegisterInternal(new Assembly[] { assembly }, true);
        }

        /// <summary>
        /// Attempt to automatically register all non-generic classes and interfaces in the specified assembly
        /// </summary>
        /// <param name="assembly">Assembly to process</param>
        /// <param name="ignoreDuplicateImplementations">Whether to ignore duplicate implementations of an interface/base class. False=throw an exception</param>
        /// <exception cref="TinyIoCAutoRegistrationException"/>
        public void AutoRegister(Assembly assembly, bool ignoreDuplicateImplementations)
        {
            AutoRegisterInternal(new Assembly[] { assembly }, ignoreDuplicateImplementations);
        }

        /// <summary>
        /// Attempt to automatically register all non-generic classes and interfaces in the specified assemblies
        /// 
        /// If more than one class implements an interface then only one implementation will be registered
        /// although no error will be thrown.
        /// </summary>
        /// <param name="assemblies">Assemblies to process</param>
        public void AutoRegister(IEnumerable<Assembly> assemblies)
        {
            AutoRegisterInternal(assemblies, true);
        }

        /// <summary>
        /// Attempt to automatically register all non-generic classes and interfaces in the specified assemblies
        /// </summary>
        /// <param name="assemblies">Assemblies to process</param>
        /// <param name="ignoreDuplicateImplementations">Whether to ignore duplicate implementations of an interface/base class. False=throw an exception</param>
        /// <exception cref="TinyIoCAutoRegistrationException"/>
        public void AutoRegister(IEnumerable<Assembly> assemblies, bool ignoreDuplicateImplementations)
        {
            AutoRegisterInternal(assemblies, ignoreDuplicateImplementations);
        }

        /// <summary>
        /// Creates/replaces a container class registration with default options.
        /// </summary>
        /// <typeparam name="RegisterImplementation">Type to register</typeparam>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType>()
            where RegisterType : class
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterType), string.Empty, GetDefaultObjectFactory<RegisterType, RegisterType>());
        }

        /// <summary>
        /// Creates/replaces a named container class registration with default options.
        /// </summary>
        /// <typeparam name="RegisterImplementation">Type to register</typeparam>
        /// <param name="name">Name of registration</param>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType>(string name)
            where RegisterType : class
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterType), name, GetDefaultObjectFactory<RegisterType, RegisterType>());
        }

        /// <summary>
        /// Creates/replaces a container class registration with a given implementation and default options.
        /// </summary>
        /// <typeparam name="RegisterType">Type to register</typeparam>
        /// <typeparam name="RegisterImplementation">Type to instantiate that implements RegisterType</typeparam>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType, RegisterImplementation>()
            where RegisterType : class
            where RegisterImplementation : class, RegisterType
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterImplementation), string.Empty, GetDefaultObjectFactory<RegisterType, RegisterImplementation>());
        }

        /// <summary>
        /// Creates/replaces a named container class registration with a given implementation and default options.
        /// </summary>
        /// <typeparam name="RegisterType">Type to register</typeparam>
        /// <typeparam name="RegisterImplementation">Type to instantiate that implements RegisterType</typeparam>
        /// <param name="name">Name of registration</param>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType, RegisterImplementation>(string name)
            where RegisterType : class
            where RegisterImplementation : class, RegisterType
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterImplementation), name, GetDefaultObjectFactory<RegisterType, RegisterImplementation>());
        }

        /// <summary>
        /// Creates/replaces a container class registration with a specific, strong referenced, instance.
        /// </summary>
        /// <typeparam name="RegisterImplementation">Type to register</typeparam>
        /// <param name="instance">Instance of RegisterType to register</param>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType>(RegisterType instance)
           where RegisterType : class
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterType), string.Empty, new InstanceFactory<RegisterType, RegisterType>(instance));
        }

        /// <summary>
        /// Creates/replaces a named container class registration with a specific, strong referenced, instance.
        /// </summary>
        /// <typeparam name="RegisterImplementation">Type to register</typeparam>
        /// <param name="instance">Instance of RegisterType to register</param>
        /// <param name="name">Name of registration</param>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType>(RegisterType instance, string name)
            where RegisterType : class
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterType), name, new InstanceFactory<RegisterType, RegisterType>(instance));
        }

        /// <summary>
        /// Creates/replaces a container class registration with a specific, strong referenced, instance.
        /// </summary>
        /// <typeparam name="RegisterType">Type to register</typeparam>
        /// <typeparam name="RegisterImplementation">Type of instance to register that implements RegisterType</typeparam>
        /// <param name="instance">Instance of RegisterImplementation to register</param>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType, RegisterImplementation>(RegisterImplementation instance)
            where RegisterType : class
            where RegisterImplementation : class, RegisterType
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterImplementation), string.Empty, new InstanceFactory<RegisterType, RegisterImplementation>(instance));
        }

        /// <summary>
        /// Creates/replaces a named container class registration with a specific, strong referenced, instance.
        /// </summary>
        /// <typeparam name="RegisterType">Type to register</typeparam>
        /// <typeparam name="RegisterImplementation">Type of instance to register that implements RegisterType</typeparam>
        /// <param name="instance">Instance of RegisterImplementation to register</param>
        /// <param name="name">Name of registration</param>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType, RegisterImplementation>(RegisterImplementation instance, string name)
            where RegisterType : class
            where RegisterImplementation : class, RegisterType
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterImplementation), name, new InstanceFactory<RegisterType, RegisterImplementation>(instance));
        }

        /// <summary>
        /// Creates/replaces a container class registration with a user specified factory
        /// </summary>
        /// <typeparam name="RegisterType">Type to register</typeparam>
        /// <param name="factory">Factory/lambda that returns an instance of RegisterType</param>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType>(Func<TinyIoCContainer, NamedParameterOverloads, RegisterType> factory)
            where RegisterType : class
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterType), string.Empty, new DelegateFactory<RegisterType>(factory));
        }

        /// <summary>
        /// Creates/replaces a named container class registration with a user specified factory
        /// </summary>
        /// <typeparam name="RegisterType">Type to register</typeparam>
        /// <param name="factory">Factory/lambda that returns an instance of RegisterType</param>
        /// <param name="name">Name of registation</param>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions Register<RegisterType>(Func<TinyIoCContainer, NamedParameterOverloads, RegisterType> factory, string name)
            where RegisterType : class
        {
            return RegisterInternal(typeof(RegisterType), typeof(RegisterType), name, new DelegateFactory<RegisterType>(factory));
        }

        /// <summary>
        /// Creates/replaces a composable interface that has multiple implementations which are discovered at runtime
        /// </summary>
        /// <typeparam name="RegisterType">The composable interface that has multiple implementations</typeparam>
        /// <returns>RegisterOptions for fluent API</returns>
        public RegisterOptions RegisterMany<RegisterType>()
            where RegisterType : class
        {
            return RegisterManyInternal(typeof(RegisterType), new CompositionFactoryWrapper<RegisterType>(typeof(MultiInstanceFactory<,>)));
        }

        #endregion

        #region Resolution

        /// <summary>
        /// Attempts to resolve a type using default options.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <returns>Instance of type</returns>
        /// <exception cref="TinyIoCResolutionException">Unable to resolve the type.</exception>
        public ResolveType Resolve<ResolveType>()
            where ResolveType : class
        {
            return this.ResolveInternal(new TypeRegistration(typeof(ResolveType)), NamedParameterOverloads.Default, ResolveOptions.Default) as ResolveType;
        }

        /// <summary>
        /// Attempts to resolve a type using specified options.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="options">Resolution options</param>
        /// <returns>Instance of type</returns>
        /// <exception cref="TinyIoCResolutionException">Unable to resolve the type.</exception>
        public ResolveType Resolve<ResolveType>(ResolveOptions options)
            where ResolveType : class
        {
            return ResolveInternal(new TypeRegistration(typeof(ResolveType)), NamedParameterOverloads.Default, options) as ResolveType;
        }

        /// <summary>
        /// Attempts to resolve a type using default options and the supplied name.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <returns>Instance of type</returns>
        /// <exception cref="TinyIoCResolutionException">Unable to resolve the type.</exception>
        public ResolveType Resolve<ResolveType>(string name)
            where ResolveType : class
        {
            return ResolveInternal(new TypeRegistration(typeof(ResolveType), name), NamedParameterOverloads.Default, ResolveOptions.Default) as ResolveType;
        }

        /// <summary>
        /// Attempts to resolve a type using supplied options and  name.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="options">Resolution options</param>
        /// <returns>Instance of type</returns>
        /// <exception cref="TinyIoCResolutionException">Unable to resolve the type.</exception>
        public ResolveType Resolve<ResolveType>(string name, ResolveOptions options)
            where ResolveType : class
        {
            return ResolveInternal(new TypeRegistration(typeof(ResolveType), name), NamedParameterOverloads.Default, options) as ResolveType;
        }

        /// <summary>
        /// Attempts to resolve a type using default options and the supplied constructor parameters.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="parameters">User specified constructor parameters</param>
        /// <returns>Instance of type</returns>
        /// <exception cref="TinyIoCResolutionException">Unable to resolve the type.</exception>
        public ResolveType Resolve<ResolveType>(NamedParameterOverloads parameters)
            where ResolveType : class
        {
            return ResolveInternal(new TypeRegistration(typeof(ResolveType)), parameters, ResolveOptions.Default) as ResolveType;
        }

        /// <summary>
        /// Attempts to resolve a type using specified options and the supplied constructor parameters.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="parameters">User specified constructor parameters</param>
        /// <param name="options">Resolution options</param>
        /// <returns>Instance of type</returns>
        /// <exception cref="TinyIoCResolutionException">Unable to resolve the type.</exception>
        public ResolveType Resolve<ResolveType>(NamedParameterOverloads parameters, ResolveOptions options)
            where ResolveType : class
        {
            return ResolveInternal(new TypeRegistration(typeof(ResolveType)), parameters, options) as ResolveType;
        }

        /// <summary>
        /// Attempts to resolve a type using default options and the supplied constructor parameters and name.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="parameters">User specified constructor parameters</param>
        /// <param name="name">Name of registration</param>
        /// <returns>Instance of type</returns>
        /// <exception cref="TinyIoCResolutionException">Unable to resolve the type.</exception>
        public ResolveType Resolve<ResolveType>(string name, NamedParameterOverloads parameters)
            where ResolveType : class
        {
            return ResolveInternal(new TypeRegistration(typeof(ResolveType), name), parameters, ResolveOptions.Default) as ResolveType;
        }

        /// <summary>
        /// Attempts to resolve a named type using specified options and the supplied constructor parameters.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="parameters">User specified constructor parameters</param>
        /// <param name="options">Resolution options</param>
        /// <returns>Instance of type</returns>
        /// <exception cref="TinyIoCResolutionException">Unable to resolve the type.</exception>
        public ResolveType Resolve<ResolveType>(string name, NamedParameterOverloads parameters, ResolveOptions options)
            where ResolveType : class
        {
            return ResolveInternal(new TypeRegistration(typeof(ResolveType), name), parameters, options) as ResolveType;
        }

        /// <summary>
        /// Attempts to predict whether a given type can be resolved with default options.
        ///
        /// Note: Resolution may still fail if user defined factory registations fail to construct objects when called.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <returns>Bool indicating whether the type can be resolved</returns>
        public bool CanResolve<ResolveType>()
            where ResolveType : class
        {
            return CanResolveInternal(new TypeRegistration(typeof(ResolveType)), NamedParameterOverloads.Default, ResolveOptions.Default);
        }

        /// <summary>
        /// Attempts to predict whether a given named type can be resolved with default options.
        ///
        /// Note: Resolution may still fail if user defined factory registations fail to construct objects when called.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <returns>Bool indicating whether the type can be resolved</returns>
        public bool CanResolve<ResolveType>(string name)
            where ResolveType : class
        {
            return CanResolveInternal(new TypeRegistration(typeof(ResolveType), name), NamedParameterOverloads.Default, ResolveOptions.Default);
        }

        /// <summary>
        /// Attempts to predict whether a given type can be resolved with the specified options.
        ///
        /// Note: Resolution may still fail if user defined factory registations fail to construct objects when called.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="options">Resolution options</param>
        /// <returns>Bool indicating whether the type can be resolved</returns>
        public bool CanResolve<ResolveType>(ResolveOptions options)
            where ResolveType : class
        {
            return CanResolveInternal(new TypeRegistration(typeof(ResolveType)), NamedParameterOverloads.Default, options);
        }

        /// <summary>
        /// Attempts to predict whether a given named type can be resolved with the specified options.
        ///
        /// Note: Resolution may still fail if user defined factory registations fail to construct objects when called.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="options">Resolution options</param>
        /// <returns>Bool indicating whether the type can be resolved</returns>
        public bool CanResolve<ResolveType>(string name, ResolveOptions options)
            where ResolveType : class
        {
            return CanResolveInternal(new TypeRegistration(typeof(ResolveType), name), NamedParameterOverloads.Default, options);
        }

        /// <summary>
        /// Attempts to predict whether a given type can be resolved with the supplied constructor parameters and default options.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// 
        /// Note: Resolution may still fail if user defined factory registations fail to construct objects when called.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="parameters">User supplied named parameter overloads</param>
        /// <returns>Bool indicating whether the type can be resolved</returns>
        public bool CanResolve<ResolveType>(NamedParameterOverloads parameters)
            where ResolveType : class
        {
            return CanResolveInternal(new TypeRegistration(typeof(ResolveType)), parameters, ResolveOptions.Default);
        }

        /// <summary>
        /// Attempts to predict whether a given named type can be resolved with the supplied constructor parameters and default options.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// 
        /// Note: Resolution may still fail if user defined factory registations fail to construct objects when called.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="parameters">User supplied named parameter overloads</param>
        /// <returns>Bool indicating whether the type can be resolved</returns>
        public bool CanResolve<ResolveType>(string name, NamedParameterOverloads parameters)
            where ResolveType : class
        {
            return CanResolveInternal(new TypeRegistration(typeof(ResolveType), name), parameters, ResolveOptions.Default);
        }

        /// <summary>
        /// Attempts to predict whether a given type can be resolved with the supplied constructor parameters options.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// 
        /// Note: Resolution may still fail if user defined factory registations fail to construct objects when called.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="parameters">User supplied named parameter overloads</param>
        /// <param name="options">Resolution options</param>
        /// <returns>Bool indicating whether the type can be resolved</returns>
        public bool CanResolve<ResolveType>(NamedParameterOverloads parameters, ResolveOptions options)
            where ResolveType : class
        {
            return CanResolveInternal(new TypeRegistration(typeof(ResolveType)), parameters, options);
        }

        /// <summary>
        /// Attempts to predict whether a given named type can be resolved with the supplied constructor parameters options.
        ///
        /// Parameters are used in conjunction with normal container resolution to find the most suitable constructor (if one exists).
        /// All user supplied parameters must exist in at least one resolvable constructor of RegisterType or resolution will fail.
        /// 
        /// Note: Resolution may still fail if user defined factory registations fail to construct objects when called.
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="parameters">User supplied named parameter overloads</param>
        /// <param name="options">Resolution options</param>
        /// <returns>Bool indicating whether the type can be resolved</returns>
        public bool CanResolve<ResolveType>(string name, NamedParameterOverloads parameters, ResolveOptions options)
            where ResolveType : class
        {
            return CanResolveInternal(new TypeRegistration(typeof(ResolveType), name), parameters, options);
        }

        /// <summary>
        /// Attemps to resolve a type using the default options
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="resolvedType">Resolved type or default if resolve fails</param>
        /// <returns>True if resolved sucessfully, false otherwise</returns>
        public bool TryResolve<ResolveType>(out ResolveType resolvedType)
            where ResolveType : class
        {
            try
            {
                resolvedType = this.Resolve<ResolveType>();
                return true;
            }
            catch (TinyIoCResolutionException)
            {
                resolvedType = default(ResolveType);
                return false;
            }
        }

        /// <summary>
        /// Attemps to resolve a type using the given options
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="options">Resolution options</param>
        /// <param name="resolvedType">Resolved type or default if resolve fails</param>
        /// <returns>True if resolved sucessfully, false otherwise</returns>
        public bool TryResolve<ResolveType>(ResolveOptions options, out ResolveType resolvedType)
            where ResolveType : class
        {
            try
            {
                resolvedType = this.Resolve<ResolveType>(options);
                return true;
            }
            catch (TinyIoCResolutionException)
            {
                resolvedType = default(ResolveType);
                return false;
            }
        }

        /// <summary>
        /// Attemps to resolve a type using the default options and given name
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="resolvedType">Resolved type or default if resolve fails</param>
        /// <returns>True if resolved sucessfully, false otherwise</returns>
        public bool TryResolve<ResolveType>(string name, out ResolveType resolvedType)
            where ResolveType : class
        {
            try
            {
                resolvedType = this.Resolve<ResolveType>(name);
                return true;
            }
            catch (TinyIoCResolutionException)
            {
                resolvedType = default(ResolveType);
                return false;
            }
        }

        /// <summary>
        /// Attemps to resolve a type using the given options and name
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="options">Resolution options</param>
        /// <param name="resolvedType">Resolved type or default if resolve fails</param>
        /// <returns>True if resolved sucessfully, false otherwise</returns>
        public bool TryResolve<ResolveType>(string name, ResolveOptions options, out ResolveType resolvedType)
            where ResolveType : class
        {
            try
            {
                resolvedType = this.Resolve<ResolveType>(name, options);
                return true;
            }
            catch (TinyIoCResolutionException)
            {
                resolvedType = default(ResolveType);
                return false;
            }
        }

        /// <summary>
        /// Attemps to resolve a type using the default options and supplied constructor parameters
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="parameters">User specified constructor parameters</param>
        /// <param name="resolvedType">Resolved type or default if resolve fails</param>
        /// <returns>True if resolved sucessfully, false otherwise</returns>
        public bool TryResolve<ResolveType>(NamedParameterOverloads parameters, out ResolveType resolvedType)
            where ResolveType : class
        {
            try
            {
                resolvedType = this.Resolve<ResolveType>(parameters);
                return true;
            }
            catch (TinyIoCResolutionException)
            {
                resolvedType = default(ResolveType);
                return false;
            }
        }

        /// <summary>
        /// Attemps to resolve a type using the default options and supplied name and constructor parameters
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="parameters">User specified constructor parameters</param>
        /// <param name="resolvedType">Resolved type or default if resolve fails</param>
        /// <returns>True if resolved sucessfully, false otherwise</returns>
        public bool TryResolve<ResolveType>(string name, NamedParameterOverloads parameters, out ResolveType resolvedType)
            where ResolveType : class
        {
            try
            {
                resolvedType = this.Resolve<ResolveType>(name, parameters);
                return true;
            }
            catch (TinyIoCResolutionException)
            {
                resolvedType = default(ResolveType);
                return false;
            }
        }

        /// <summary>
        /// Attemps to resolve a type using the supplied options and constructor parameters
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="parameters">User specified constructor parameters</param>
        /// <param name="options">Resolution options</param>
        /// <param name="resolvedType">Resolved type or default if resolve fails</param>
        /// <returns>True if resolved sucessfully, false otherwise</returns>
        public bool TryResolve<ResolveType>(NamedParameterOverloads parameters, ResolveOptions options, out ResolveType resolvedType)
            where ResolveType : class
        {
            try
            {
                resolvedType = this.Resolve<ResolveType>(parameters, options);
                return true;
            }
            catch (TinyIoCResolutionException)
            {
                resolvedType = default(ResolveType);
                return false;
            }
        }

        /// <summary>
        /// Attemps to resolve a type using the supplied name, options and constructor parameters
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <param name="parameters">User specified constructor parameters</param>
        /// <param name="options">Resolution options</param>
        /// <param name="resolvedType">Resolved type or default if resolve fails</param>
        /// <returns>True if resolved sucessfully, false otherwise</returns>
        public bool TryResolve<ResolveType>(string name, NamedParameterOverloads parameters, ResolveOptions options, out ResolveType resolvedType)
            where ResolveType : class
        {
            try
            {
                resolvedType = this.Resolve<ResolveType>(name, parameters, options);
                return true;
            }
            catch (TinyIoCResolutionException)
            {
                resolvedType = default(ResolveType);
                return false;
            }
        }

        /// <summary>
        /// Returns all registrations of a type, both named and unnamed
        /// </summary>
        /// <typeparam name="ResolveType">Type to resolveAll</typeparam>
        /// <returns>IEnumerable</returns>
        public IEnumerable<ResolveType> ResolveAll<ResolveType>()
            where ResolveType : class
        {
            return ResolveAllInternal(typeof(ResolveType)).Select(o => o as ResolveType);
        }

        /// <summary>
        /// Attempts to resolve all public property dependencies on the given object.
        /// </summary>
        /// <param name="input">Object to "build up"</param>
        public void BuildUp(object input)
        {
            BuildUpInternal(input, ResolveOptions.Default);
        }

        /// <summary>
        /// Attempts to resolve all public property dependencies on the given object using the given resolve options.
        /// </summary>
        /// <param name="input">Object to "build up"</param>
        /// <param name="resolveOptions">Resolve options to use</param>
        public void BuildUp(object input, ResolveOptions resolveOptions)
        {
            BuildUpInternal(input, resolveOptions);
        }
        #endregion
        #endregion

        #region Object Factories
        private abstract class ObjectFactoryBase
        {
            /// <summary>
            /// Whether to assume this factory sucessfully constructs its objects
            /// 
            /// Generally set to true for delegate style factories as CanResolve cannot delve
            /// into the delegates they contain.
            /// </summary>
            public virtual bool AssumeConstruction { get { return false; } }

            /// <summary>
            /// The type the factory instantiates
            /// </summary>
            public abstract Type CreatesType { get; }

            /// <summary>
            /// Constructor to use, if specified
            /// </summary>
            public ConstructorInfo Constructor { get; protected set; }

            /// <summary>
            /// Create the type
            /// </summary>
            /// <param name="container">Container that requested the creation</param>
            /// <param name="parameters">Any user parameters passed</param>
            /// <returns></returns>
            public abstract object GetObject(TinyIoCContainer container, NamedParameterOverloads parameters, ResolveOptions options);

            public virtual ObjectFactoryBase SingletonVariant
            {
                get
                {
                    throw new TinyIoCRegistrationException(this.GetType(), "singleton");
                }
            }

            public virtual ObjectFactoryBase MultiInstanceVariant
            {
                get
                {
                    throw new TinyIoCRegistrationException(this.GetType(), "multi-instance");
                }
            }

            public virtual ObjectFactoryBase StrongReferenceVariant
            {
                get
                {
                    throw new TinyIoCRegistrationException(this.GetType(), "strong reference");
                }
            }

            public virtual ObjectFactoryBase WeakReferenceVariant
            {
                get
                {
                    throw new TinyIoCRegistrationException(this.GetType(), "weak reference");
                }
            }

            public virtual void SetConstructor(ConstructorInfo constructor)
            {
                this.Constructor = constructor;
            }
        }

        /// <summary>
        /// IObjectFactory that creates new instances of types for each resolution
        /// </summary>
        /// <typeparam name="RegisterType">Registered type</typeparam>
        /// <typeparam name="RegisterImplementation">Type to construct to fullful request for RegisteredType</typeparam>
        private class MultiInstanceFactory<RegisterType, RegisterImplementation> : ObjectFactoryBase
            where RegisterType : class
            where RegisterImplementation : class, RegisterType
        {
            public override Type CreatesType { get { return typeof(RegisterImplementation); } }

            public MultiInstanceFactory()
            {
                if (typeof(RegisterImplementation).IsAbstract || typeof(RegisterImplementation).IsInterface)
                    throw new TinyIoCRegistrationTypeException(typeof(RegisterImplementation), "MultiInstanceFactory");
            }

            public override object GetObject(TinyIoCContainer container, NamedParameterOverloads parameters, ResolveOptions options)
            {
                try
                {
                    return container.ConstructType(typeof(RegisterImplementation), Constructor, parameters, options);
                }
                catch (TinyIoCResolutionException ex)
                {
                    throw new TinyIoCResolutionException(typeof(RegisterImplementation), ex);
                }
            }

            public override ObjectFactoryBase SingletonVariant
            {
                get
                {
                    return new SingletonFactory<RegisterType, RegisterImplementation>();
                }
            }

            public override ObjectFactoryBase MultiInstanceVariant
            {
                get
                {
                    return this;
                }
            }
        }

        /// <summary>
        /// IObjectFactory that invokes a specified delegate to construct the object
        /// </summary>
        /// <typeparam name="RegisterType">Registered type to be constructed</typeparam>
        private class DelegateFactory<RegisterType> : ObjectFactoryBase
            where RegisterType : class
        {
            private Func<TinyIoCContainer, NamedParameterOverloads, RegisterType> _factory;

            public override bool AssumeConstruction { get { return true; } }

            public override Type CreatesType { get { return typeof(RegisterType); } }

            public override object GetObject(TinyIoCContainer container, NamedParameterOverloads parameters, ResolveOptions options)
            {
                try
                {
                    return _factory.Invoke(container, parameters);
                }
                catch (Exception ex)
                {
                    throw new TinyIoCResolutionException(typeof(RegisterType), ex);
                }
            }

            public DelegateFactory(Func<TinyIoCContainer, NamedParameterOverloads, RegisterType> factory)
            {
                if (factory == null)
                    throw new ArgumentNullException("factory");

                _factory = factory;
            }

            public override ObjectFactoryBase WeakReferenceVariant
            {
                get
                {
                    return new WeakDelegateFactory<RegisterType>(_factory);
                }
            }

            public override ObjectFactoryBase StrongReferenceVariant
            {
                get
                {
                    return this;
                }
            }

            public override void SetConstructor(ConstructorInfo constructor)
            {
                throw new TinyIoCConstructorResolutionException("Constructor selection is not possible for delegate factory registrations");
            }
        }

        /// <summary>
        /// IObjectFactory that invokes a specified delegate to construct the object
        /// 
        /// Holds the delegate using a weak reference
        /// </summary>
        /// <typeparam name="RegisterType">Registered type to be constructed</typeparam>
        private class WeakDelegateFactory<RegisterType> : ObjectFactoryBase
            where RegisterType : class
        {
            private WeakReference _factory;

            public override bool AssumeConstruction { get { return true; } }

            public override Type CreatesType { get { return typeof(RegisterType); } }

            public override object GetObject(TinyIoCContainer container, NamedParameterOverloads parameters, ResolveOptions options)
            {
                var factory = _factory.Target as Func<TinyIoCContainer, NamedParameterOverloads, RegisterType>;

                if (factory == null)
                    throw new TinyIoCWeakReferenceException(typeof(RegisterType));

                try
                {
                    return factory.Invoke(container, parameters);
                }
                catch (Exception ex)
                {
                    throw new TinyIoCResolutionException(typeof(RegisterType), ex);
                }
            }

            public WeakDelegateFactory(Func<TinyIoCContainer, NamedParameterOverloads, RegisterType> factory)
            {
                if (factory == null)
                    throw new ArgumentNullException("factory");

                _factory = new WeakReference(factory);
            }

            public override ObjectFactoryBase StrongReferenceVariant
            {
                get
                {
                    var factory = _factory.Target as Func<TinyIoCContainer, NamedParameterOverloads, RegisterType>;

                    if (factory == null)
                        throw new TinyIoCWeakReferenceException(typeof(RegisterType));

                    return new DelegateFactory<RegisterType>(factory);
                }
            }

            public override ObjectFactoryBase WeakReferenceVariant
            {
                get
                {
                    return this;
                }
            }

            public override void SetConstructor(ConstructorInfo constructor)
            {
                throw new TinyIoCConstructorResolutionException("Constructor selection is not possible for delegate factory registrations");
            }
        }

        /// <summary>
        /// Stores an particular instance to return for a type
        /// </summary>
        /// <typeparam name="RegisterType">Registered type</typeparam>
        /// <typeparam name="RegisterImplementation">Type of the instance</typeparam>
        private class InstanceFactory<RegisterType, RegisterImplementation> : ObjectFactoryBase, IDisposable
            where RegisterType : class
            where RegisterImplementation : class, RegisterType
        {
            private RegisterImplementation _instance;

            public override bool AssumeConstruction { get { return true; } }

            public InstanceFactory(RegisterImplementation instance)
            {
                this._instance = instance;
            }

            public override Type CreatesType
            {
                get { return typeof(RegisterImplementation); }
            }

            public override object GetObject(TinyIoCContainer container, NamedParameterOverloads parameters, ResolveOptions options)
            {
                return _instance;
            }

            public override ObjectFactoryBase MultiInstanceVariant
            {
                get
                {
                    return new MultiInstanceFactory<RegisterType, RegisterImplementation>();
                }
            }

            public override ObjectFactoryBase WeakReferenceVariant
            {
                get
                {
                    return new WeakInstanceFactory<RegisterType, RegisterImplementation>(_instance);
                }
            }

            public override ObjectFactoryBase StrongReferenceVariant
            {
                get
                {
                    return this;
                }
            }

            public override void SetConstructor(ConstructorInfo constructor)
            {
                throw new TinyIoCConstructorResolutionException("Constructor selection is not possible for instance factory registrations");
            }

            public void Dispose()
            {
                var disposable = _instance as IDisposable;

                if (disposable != null)
                    disposable.Dispose();
            }
        }

        /// <summary>
        /// Stores an particular instance to return for a type
        /// 
        /// Stores the instance with a weak reference
        /// </summary>
        /// <typeparam name="RegisterType">Registered type</typeparam>
        /// <typeparam name="RegisterImplementation">Type of the instance</typeparam>
        private class WeakInstanceFactory<RegisterType, RegisterImplementation> : ObjectFactoryBase, IDisposable
            where RegisterType : class
            where RegisterImplementation : class, RegisterType
        {
            private WeakReference _instance;

            public WeakInstanceFactory(RegisterImplementation instance)
            {
                this._instance = new WeakReference(instance);
            }

            public override Type CreatesType
            {
                get { return typeof(RegisterImplementation); }
            }

            public override object GetObject(TinyIoCContainer container, NamedParameterOverloads parameters, ResolveOptions options)
            {
                var instance = _instance.Target as RegisterImplementation;

                if (instance == null)
                    throw new TinyIoCWeakReferenceException(typeof(RegisterType));

                return instance;
            }

            public override ObjectFactoryBase MultiInstanceVariant
            {
                get
                {
                    return new MultiInstanceFactory<RegisterType, RegisterImplementation>();
                }
            }

            public override ObjectFactoryBase WeakReferenceVariant
            {
                get
                {
                    return this;
                }
            }

            public override ObjectFactoryBase StrongReferenceVariant
            {
                get
                {
                    var instance = _instance.Target as RegisterImplementation;

                    if (instance == null)
                        throw new TinyIoCWeakReferenceException(typeof(RegisterType));

                    return new InstanceFactory<RegisterType, RegisterImplementation>(instance);
                }
            }

            public override void SetConstructor(ConstructorInfo constructor)
            {
                throw new TinyIoCConstructorResolutionException("Constructor selection is not possible for instance factory registrations");
            }

            public void Dispose()
            {
                var disposable = _instance.Target as IDisposable;

                if (disposable != null)
                    disposable.Dispose();
            }
        }

        /// <summary>
        /// A factory that lazy instantiates a type and always returns the same instance
        /// </summary>
        /// <typeparam name="RegisterType">Registered type</typeparam>
        /// <typeparam name="RegisterImplementation">Type to instantiate</typeparam>
        private class SingletonFactory<RegisterType, RegisterImplementation> : ObjectFactoryBase, IDisposable
            where RegisterType : class
            where RegisterImplementation : class, RegisterType
        {
            private readonly object SingletonLock = new object();
            private RegisterImplementation _Current;

            public SingletonFactory()
            {
                if (typeof(RegisterImplementation).IsAbstract || typeof(RegisterImplementation).IsInterface)
                    throw new TinyIoCRegistrationTypeException(typeof(RegisterImplementation), "SingletonFactory");
            }

            public override Type CreatesType
            {
                get { return typeof(RegisterImplementation); }
            }

            public override object GetObject(TinyIoCContainer container, NamedParameterOverloads parameters, ResolveOptions options)
            {
                if (parameters.Count != 0)
                    throw new ArgumentException("Cannot specify parameters for singleton types");

                lock (SingletonLock)
                    if (_Current == null)
                        _Current = container.ConstructType(typeof(RegisterImplementation), Constructor, options) as RegisterImplementation;

                return _Current;
            }

            public override ObjectFactoryBase SingletonVariant
            {
                get
                {
                    return this;
                }
            }

            public override ObjectFactoryBase MultiInstanceVariant
            {
                get
                {
                    return new MultiInstanceFactory<RegisterType, RegisterImplementation>();
                }
            }

            public void Dispose()
            {
                if (_Current != null)
                {
                    var disposable = _Current as IDisposable;

                    if (disposable != null)
                        disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// A factory that constructs enumerations of implementations for a given interfact.
        /// </summary>
        /// <typeparam name="RegisterType">The composable interface that has multiple implementations.</typeparam>
        private class CompositionFactoryWrapper<RegisterType> : ObjectFactoryBase, IDisposable
        {
            private readonly ICollection<ObjectFactoryBase> _factories;
            private readonly IEnumerable<Type> implementations;
            private Type _factoryType;

            public CompositionFactoryWrapper(Type factoryType)
            {
                this._factoryType = factoryType;

                this._factories = new List<ObjectFactoryBase>();

#if APPDOMAIN_GETASSEMBLIES
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
#else
                var assemblies = new Assembly[] { this.GetType().Assembly };
#endif

                var allTypes = assemblies.SelectMany(assembly => assembly.GetTypes());

                implementations =
                    (from type in allTypes
                     where type.GetInterfaces().Contains(typeof(RegisterType))
                        && type.IsClass
                        && (!type.IsAbstract)
                        && (!type.IsGenericTypeDefinition)
                     select type);

                foreach (var implementation in implementations)
                {
                    var factoryTypeForImplementatio = factoryType.MakeGenericType(typeof(RegisterType), implementation);
                    _factories.Add((ObjectFactoryBase)Activator.CreateInstance(factoryTypeForImplementatio));
                }
            }

            public override bool AssumeConstruction
            {
                get
                {
                    return true;
                }
            }

            public override Type CreatesType
            {
                get
                {
                    return typeof(IEnumerable<>).MakeGenericType(typeof(RegisterType));
                }
            }

            public override object GetObject(TinyIoCContainer container, NamedParameterOverloads parameters, ResolveOptions options)
            {
                var objects = new List<RegisterType>();

                foreach (var factory in _factories)
                {
                    objects.Add((RegisterType)factory.GetObject(container, parameters, options));
                }

                return new ReadOnlyCollection<RegisterType>(objects);
            }

            public void Dispose()
            {
                foreach (var factory in _factories)
                {
                    if (factory is IDisposable)
                    {
                        ((IDisposable)factory).Dispose();
                    }
                }
            }

            public override ObjectFactoryBase MultiInstanceVariant
            {
                get
                {
                    if (this._factoryType != typeof(MultiInstanceFactory<,>))
                    {
                        return new CompositionFactoryWrapper<RegisterType>(typeof(MultiInstanceFactory<,>));
                    }

                    return this;
                }
            }

            public override ObjectFactoryBase SingletonVariant
            {
                get
                {
                    if (this._factoryType != typeof(SingletonFactory<,>))
                    {
                        return new CompositionFactoryWrapper<RegisterType>(typeof(SingletonFactory<,>));
                    }

                    return this;
                }
            }
        }
        #endregion

        #region Singleton Container
        private static readonly TinyIoCContainer _Current = new TinyIoCContainer();

        static TinyIoCContainer()
        {
        }

        /// <summary>
        /// Lazy created Singleton instance of the container for simple scenarios
        /// </summary>
        public static TinyIoCContainer Current
        {
            get
            {
                return _Current;
            }
        }
        #endregion

        #region Type Registrations
        public sealed class TypeRegistration
        {
            public Type Type { get; private set; }
            public string Name { get; private set; }

            public TypeRegistration(Type type)
                : this(type, string.Empty)
            {
            }

            public TypeRegistration(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            public override bool Equals(object obj)
            {
                var typeRegistration = obj as TypeRegistration;

                if (typeRegistration == null)
                    return false;

                if (this.Type != typeRegistration.Type)
                    return false;

                if (String.Compare(this.Name, typeRegistration.Name, StringComparison.Ordinal) != 0)
                    return false;

                return true;
            }

            public override int GetHashCode()
            {
                return String.Format("{0}|{1}", this.Type.FullName, this.Name).GetHashCode();
            }
        }
        private readonly SafeDictionary<TypeRegistration, ObjectFactoryBase> _RegisteredTypes;
        #endregion

        #region Constructors
        public TinyIoCContainer()
        {
            _RegisteredTypes = new SafeDictionary<TypeRegistration, ObjectFactoryBase>();

            RegisterDefaultTypes();
        }

        TinyIoCContainer _Parent;
        private TinyIoCContainer(TinyIoCContainer parent)
            : this()
        {
            this._Parent = parent;
        }
        #endregion

        #region Internal Methods
        private readonly object _AutoRegisterLock = new object();
        private void AutoRegisterInternal(IEnumerable<Assembly> assemblies, bool ignoreDuplicateImplementations)
        {
            lock (_AutoRegisterLock)
            {
                var defaultFactoryMethod = this.GetType().GetMethod("GetDefaultObjectFactory", BindingFlags.NonPublic | BindingFlags.Instance);

                var types = assemblies.SelectMany(a => a.GetTypes()).Where(t => !IsIgnoredType(t)).ToList();

                var concreteTypes = from type in types
                                    where (type.IsClass == true) && (type.IsAbstract == false) && (type != this.GetType() && (type.DeclaringType != this.GetType()) && (!type.IsGenericTypeDefinition))
                                    select type;

                foreach (var type in concreteTypes)
                {
                    Type[] genericTypes = { type, type };
                    var genericDefaultFactoryMethod = defaultFactoryMethod.MakeGenericMethod(genericTypes);
                    try
                    {
                        this.RegisterInternal(type, type, string.Empty, genericDefaultFactoryMethod.Invoke(this, null) as ObjectFactoryBase);
                    }
                    catch (MethodAccessException)
                    {
                        // Ignore methods we can't access - added for Silverlight
                    }
                }

                var abstractInterfaceTypes = from type in types
                                             where ((type.IsInterface == true || type.IsAbstract == true) && (type.DeclaringType != this.GetType()) && (!type.IsGenericTypeDefinition))
                                             select type;

                foreach (var type in abstractInterfaceTypes)
                {
                    var implementations = from implementationType in concreteTypes
                                          where implementationType.GetInterfaces().Contains(type) || implementationType.BaseType == type
                                          select implementationType;

                    if (!ignoreDuplicateImplementations && implementations.Count() > 1)
                        throw new TinyIoCAutoRegistrationException(type, implementations);

                    var firstImplementation = implementations.FirstOrDefault();
                    if (firstImplementation != null)
                    {
                        Type[] genericTypes = { type, firstImplementation };
                        var genericDefaultFactoryMethod = defaultFactoryMethod.MakeGenericMethod(genericTypes);
                        try
                        {
                            this.RegisterInternal(type, firstImplementation, string.Empty, genericDefaultFactoryMethod.Invoke(this, null) as ObjectFactoryBase);
                        }
                        catch (MethodAccessException)
                        {
                            // Ignore methods we can't access - added for Silverlight
                        }
                    }
                }
            }
        }

        private bool IsIgnoredAssembly(Assembly assembly)
        {
            // TODO - find a better way to remove "system" assemblies from the auto registration
            var ignoreChecks = new List<Func<Assembly, bool>>()
            {
                asm => asm.FullName.StartsWith("Microsoft.", StringComparison.InvariantCulture),
                asm => asm.FullName.StartsWith("System.", StringComparison.InvariantCulture),
                asm => asm.FullName.StartsWith("System,", StringComparison.InvariantCulture),
                asm => asm.FullName.StartsWith("CR_ExtUnitTest", StringComparison.InvariantCulture),
                asm => asm.FullName.StartsWith("mscorlib,", StringComparison.InvariantCulture),
                asm => asm.FullName.StartsWith("CR_VSTest", StringComparison.InvariantCulture),
                asm => asm.FullName.StartsWith("DevExpress.CodeRush", StringComparison.InvariantCulture),
            };

            foreach (var check in ignoreChecks)
            {
                if (check(assembly))
                    return true;
            }

            return false;
        }

        private bool IsIgnoredType(Type type)
        {
            // TODO - find a better way to remove "system" types from the auto registration
            var ignoreChecks = new List<Func<Type, bool>>()
            {
                t => t.FullName.StartsWith("System.", StringComparison.InvariantCulture),
                t => t.FullName.StartsWith("Microsoft.", StringComparison.InvariantCulture),
                t => t.IsPrimitive,
#if !UNBOUND_GENERICS_GETCONSTRUCTORS
                t => t.IsGenericTypeDefinition,
#endif
                t => (t.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Length == 0) && !(t.IsInterface || t.IsAbstract),
            };

            foreach (var check in ignoreChecks)
            {
                if (check(type))
                    return true;
            }

            return false;
        }

        private void RegisterDefaultTypes()
        {
            this.Register<TinyIoCContainer>(this);

#if TINYMESSENGER
            // Only register the TinyMessenger singleton if we are the root container
            if (this._Parent == null)
                this.Register<TinyMessenger.ITinyMessengerHub, TinyMessenger.TinyMessengerHub>();
#endif
        }

        private ObjectFactoryBase GetCurrentFactory(TypeRegistration registration)
        {
            ObjectFactoryBase current = null;

            _RegisteredTypes.TryGetValue(registration, out current);

            return current;
        }

        private RegisterOptions RegisterInternal(Type registerType, Type registerImplementation, string name, ObjectFactoryBase factory)
        {
            var typeRegistration = new TypeRegistration(registerType, name);

            return AddUpdateRegistration(typeRegistration, factory);
        }

        private RegisterOptions RegisterManyInternal(Type registerType, ObjectFactoryBase factory)
        {
            var compositeType = typeof(IEnumerable<>).MakeGenericType(registerType);

            var typeRegistration = new TypeRegistration(compositeType);

            return this.AddUpdateRegistration(typeRegistration, factory);
        }


        private RegisterOptions AddUpdateRegistration(TypeRegistration typeRegistration, ObjectFactoryBase factory)
        {
            _RegisteredTypes[typeRegistration] = factory;

            return new RegisterOptions(this, typeRegistration);
        }

        private void RemoveRegistration(TypeRegistration typeRegistration)
        {
            _RegisteredTypes.Remove(typeRegistration);
        }

        private ObjectFactoryBase GetDefaultObjectFactory<RegisterType, RegisterImplementation>()
            where RegisterType : class
            where RegisterImplementation : class, RegisterType
        {
            if (typeof(RegisterType).IsInterface || typeof(RegisterType).IsAbstract)
                return new SingletonFactory<RegisterType, RegisterImplementation>();

            return new MultiInstanceFactory<RegisterType, RegisterImplementation>();
        }

        private bool CanResolveInternal(TypeRegistration registration, NamedParameterOverloads parameters, ResolveOptions options)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            Type checkType = registration.Type;
            string name = registration.Name;

            ObjectFactoryBase factory;
            if (_RegisteredTypes.TryGetValue(new TypeRegistration(checkType, name), out factory))
            {
                if (factory.AssumeConstruction)
                    return true;

                if (factory.Constructor == null)
                    return (GetBestConstructor(factory.CreatesType, parameters, options) != null) ? true : false;
                else
                    return CanConstruct(factory.Constructor, parameters, options);
            }

            // Fail if requesting named resolution and settings set to fail if unresolved
            // Or bubble up if we have a parent
            if (!String.IsNullOrEmpty(name) && options.NamedResolutionFailureAction == NamedResolutionFailureActions.Fail)
                return (_Parent != null) ? _Parent.CanResolveInternal(registration, parameters, options) : false;

            // Attemped unnamed fallback container resolution if relevant and requested
            if (!String.IsNullOrEmpty(name) && options.NamedResolutionFailureAction == NamedResolutionFailureActions.AttemptUnnamedResolution)
            {
                if (_RegisteredTypes.TryGetValue(new TypeRegistration(checkType), out factory))
                {
                    if (factory.AssumeConstruction)
                        return true;

                    return (GetBestConstructor(factory.CreatesType, parameters, options) != null) ? true : false;
                }
            }

            // Check if type is an automatic lazy factory request
            if (IsAutomaticLazyFactoryRequest(checkType))
                return true;

            // Check if type is an IEnumerable<ResolveType>
            if (IsIEnumerableRequest(registration.Type))
                return true;

            // Attempt unregistered construction if possible and requested
            // If we cant', bubble if we have a parent
            if ((options.UnregisteredResolutionAction == UnregisteredResolutionActions.AttemptResolve) || (checkType.IsGenericType && options.UnregisteredResolutionAction == UnregisteredResolutionActions.GenericsOnly))
                return (GetBestConstructor(checkType, parameters, options) != null) ? true : (_Parent != null) ? _Parent.CanResolveInternal(registration, parameters, options) : false;

            // Bubble resolution up the container tree if we have a parent
            if (_Parent != null)
                return _Parent.CanResolveInternal(registration, parameters, options);

            return false;
        }

        private bool IsIEnumerableRequest(Type type)
        {
            if (!type.IsGenericType)
                return false;

            Type genericType = type.GetGenericTypeDefinition();

            if (genericType == typeof(IEnumerable<>))
                return true;

            return false;
        }

        private bool IsAutomaticLazyFactoryRequest(Type type)
        {
            if (!type.IsGenericType)
                return false;

            Type genericType = type.GetGenericTypeDefinition();

            // Just a func
            if (genericType == typeof(Func<>))
                return true;

            // 2 parameter func with string as first parameter (name)
            if ((genericType == typeof(Func<,>) && type.GetGenericArguments()[0] == typeof(string)))
                return true;

            // 3 parameter func with string as first parameter (name) and IDictionary<string, object> as second (parameters)
            if ((genericType == typeof(Func<,,>) && type.GetGenericArguments()[0] == typeof(string) && type.GetGenericArguments()[1] == typeof(IDictionary<String, object>)))
                return true;

            return false;
        }

        private object ResolveInternal(TypeRegistration registration, NamedParameterOverloads parameters, ResolveOptions options)
        {
            ObjectFactoryBase factory;

            // Attempt container resolution
            if (_RegisteredTypes.TryGetValue(registration, out factory))
            {
                try
                {
                    return factory.GetObject(this, parameters, options);
                }
                catch (Exception ex)
                {
                    throw new TinyIoCResolutionException(registration.Type, ex);
                }
            }

            // Fail if requesting named resolution and settings set to fail if unresolved
            if (!String.IsNullOrEmpty(registration.Name) && options.NamedResolutionFailureAction == NamedResolutionFailureActions.Fail)
            {
                // Bubble resolution up the container tree if we have a parent
                if (_Parent != null)
                    return _Parent.ResolveInternal(registration, parameters, options);
                else
                    throw new TinyIoCResolutionException(registration.Type);
            }

            // Attemped unnamed fallback container resolution if relevant and requested
            if (!String.IsNullOrEmpty(registration.Name) && options.NamedResolutionFailureAction == NamedResolutionFailureActions.AttemptUnnamedResolution)
            {
                if (_RegisteredTypes.TryGetValue(new TypeRegistration(registration.Type, string.Empty), out factory))
                {
                    try
                    {
                        return factory.GetObject(this, parameters, options);
                    }
                    catch (Exception ex)
                    {
                        throw new TinyIoCResolutionException(registration.Type, ex);
                    }
                }
            }

#if EXPRESSIONS
            // Attempt to construct an automatic lazy factory if possible
            if (IsAutomaticLazyFactoryRequest(registration.Type))
                return GetLazyAutomaticFactoryRequest(registration.Type);
#endif
            if (IsIEnumerableRequest(registration.Type))
                return GetIEnumerableRequest(registration.Type);

            // Attempt unregistered construction if possible and requested
            if ((options.UnregisteredResolutionAction == UnregisteredResolutionActions.AttemptResolve) || (registration.Type.IsGenericType && options.UnregisteredResolutionAction == UnregisteredResolutionActions.GenericsOnly))
            {
                if (!registration.Type.IsAbstract && !registration.Type.IsInterface)
                    return ConstructType(registration.Type, parameters, options);
            }

            // Bubble resolution up the container tree if we have a parent
            if (_Parent != null)
                return _Parent.ResolveInternal(registration, parameters, options);

            // Unable to resolve - throw
            throw new TinyIoCResolutionException(registration.Type);
        }

#if EXPRESSIONS
        private object GetLazyAutomaticFactoryRequest(Type type)
        {
            if (!type.IsGenericType)
                return null;

            Type genericType = type.GetGenericTypeDefinition();
            Type[] genericArguments = type.GetGenericArguments();

            // Just a func
            if (genericType == typeof(Func<>))
            {
                Type returnType = genericArguments[0];

                MethodInfo resolveMethod = typeof(TinyIoCContainer).GetMethod("Resolve", new Type[] { });
                resolveMethod = resolveMethod.MakeGenericMethod(returnType);

                var resolveCall = Expression.Call(Expression.Constant(this), resolveMethod);

                var resolveLambda = Expression.Lambda(resolveCall).Compile();

                return resolveLambda;
            }

            // 2 parameter func with string as first parameter (name)
            if ((genericType == typeof(Func<,>)) && (genericArguments[0] == typeof(string)))
            {
                Type returnType = genericArguments[1];

                MethodInfo resolveMethod = typeof(TinyIoCContainer).GetMethod("Resolve", new Type[] { typeof(String) });
                resolveMethod = resolveMethod.MakeGenericMethod(returnType);

                ParameterExpression[] resolveParameters = new ParameterExpression[] { Expression.Parameter(typeof(String), "name") };
                var resolveCall = Expression.Call(Expression.Constant(this), resolveMethod, resolveParameters);

                var resolveLambda = Expression.Lambda(resolveCall, resolveParameters).Compile();

                return resolveLambda;
            }

            // 3 parameter func with string as first parameter (name) and IDictionary<string, object> as second (parameters)
            if ((genericType == typeof(Func<,,>) && type.GetGenericArguments()[0] == typeof(string) && type.GetGenericArguments()[1] == typeof(IDictionary<string, object>)))
            {
                Type returnType = genericArguments[2];

                var name = Expression.Parameter(typeof(string), "name");
                var parameters = Expression.Parameter(typeof(IDictionary<string, object>), "parameters");

                MethodInfo resolveMethod = typeof(TinyIoCContainer).GetMethod("Resolve", new Type[] { typeof(String), typeof(NamedParameterOverloads) });
                resolveMethod = resolveMethod.MakeGenericMethod(returnType);

                var resolveCall = Expression.Call(Expression.Constant(this), resolveMethod, name, Expression.Call(typeof(NamedParameterOverloads), "FromIDictionary", null, parameters));

                var resolveLambda = Expression.Lambda(resolveCall, name, parameters).Compile();

                return resolveLambda;
            }

            throw new TinyIoCResolutionException(type);
        }
#endif
        private object GetIEnumerableRequest(Type type)
        {
            // Using MakeGenericMethod (slow) because we need to
            // cast the IEnumerable or constructing the type wil fail.
            // We may as well use the ResolveAll<ResolveType> public
            // method to do this.
            var resolveAllMethod = this.GetType().GetMethod("ResolveAll", BindingFlags.Public | BindingFlags.Instance);
            var genericResolveAllMethod = resolveAllMethod.MakeGenericMethod(type.GetGenericArguments()[0]);
            return genericResolveAllMethod.Invoke(this, new object[] { });
        }

        private bool CanConstruct(ConstructorInfo ctor, NamedParameterOverloads parameters, ResolveOptions options)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            foreach (var parameter in ctor.GetParameters())
            {
                if (string.IsNullOrEmpty(parameter.Name))
                    return false;

                var isParameterOverload = parameters.ContainsKey(parameter.Name);

                if (parameter.ParameterType.IsPrimitive && !isParameterOverload)
                    return false;

                if (!isParameterOverload && !CanResolveInternal(new TypeRegistration(parameter.ParameterType), NamedParameterOverloads.Default, options))
                    return false;
            }

            return true;
        }

        private ConstructorInfo GetBestConstructor(Type type, NamedParameterOverloads parameters, ResolveOptions options)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            if (type.IsValueType)
                return null;

            // Get constructors in reverse order based on the number of parameters
            // i.e. be as "greedy" as possible so we satify the most amount of dependencies possible
            var ctors = from ctor in type.GetConstructors()
                        orderby ctor.GetParameters().Count() descending
                        select ctor;

            foreach (var ctor in ctors)
            {
                if (CanConstruct(ctor, parameters, options))
                    return ctor;
            }

            return null;
        }

        private object ConstructType(Type type, ResolveOptions options)
        {
            return ConstructType(type, null, NamedParameterOverloads.Default, options);
        }

        private object ConstructType(Type type, ConstructorInfo constructor, ResolveOptions options)
        {
            return ConstructType(type, constructor, NamedParameterOverloads.Default, options);
        }

        private object ConstructType(Type type, NamedParameterOverloads parameters, ResolveOptions options)
        {
            return ConstructType(type, null, parameters, options);
        }

        private object ConstructType(Type type, ConstructorInfo constructor, NamedParameterOverloads parameters, ResolveOptions options)
        {
            if (constructor == null)
                constructor = GetBestConstructor(type, parameters, options);

            if (constructor == null)
                throw new TinyIoCResolutionException(type);

            var ctorParams = constructor.GetParameters();
            object[] args = new object[ctorParams.Count()];

            for (int parameterIndex = 0; parameterIndex < ctorParams.Count(); parameterIndex++)
            {
                var currentParam = ctorParams[parameterIndex];

                args[parameterIndex] = parameters.ContainsKey(currentParam.Name) ? parameters[currentParam.Name] : ResolveInternal(new TypeRegistration(currentParam.ParameterType), NamedParameterOverloads.Default, options);
            }

            try
            {
                return constructor.Invoke(args);
            }
            catch (Exception ex)
            {
                throw new TinyIoCResolutionException(type, ex);
            }
        }

        private void BuildUpInternal(object input, ResolveOptions resolveOptions)
        {
            var properties = from property in input.GetType().GetProperties()
                             where (property.GetGetMethod() != null) && (property.GetSetMethod() != null) && !property.PropertyType.IsValueType
                             select property;

            foreach (var property in properties)
            {
                if (property.GetValue(input, null) == null)
                {
                    try
                    {
                        property.SetValue(input, ResolveInternal(new TypeRegistration(property.PropertyType), NamedParameterOverloads.Default, resolveOptions), null);
                    }
                    catch (TinyIoCResolutionException)
                    {
                        // Catch any resolution errors and ignore them
                    }
                }
            }
        }

        private IEnumerable<object> ResolveAllInternal(Type resolveType)
        {
            var registrations = _RegisteredTypes.Keys.Where(tr => tr.Type == resolveType);

            foreach (var registration in registrations)
            {
                yield return ResolveInternal(registration, NamedParameterOverloads.Default, ResolveOptions.Default);
            }
        }
        #endregion

        #region IDisposable Members
        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                _RegisteredTypes.Dispose();

                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
