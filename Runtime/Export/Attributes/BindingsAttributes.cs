// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Bindings
{
    interface IBindingsAttribute
    {
    }

    interface IBindingsNameProviderAttribute : IBindingsAttribute
    {
        string Name { get; set; }
    }

    interface IBindingsHeaderProviderAttribute : IBindingsAttribute
    {
        string Header { get; set; }
    }

    interface IBindingsIsThreadSafeProviderAttribute : IBindingsAttribute
    {
        bool IsThreadSafe { get; set; }
    }

    interface IBindingsIsFreeFunctionProviderAttribute : IBindingsAttribute
    {
        bool IsFreeFunction { get; set; }
        bool HasExplicitThis { get; set; }
    }

    interface IBindingsThrowsProviderAttribute : IBindingsAttribute
    {
        bool ThrowsException { get; set; }
    }

    interface IBindingsGenerateMarshallingTypeAttribute : IBindingsAttribute
    {
        CodegenOptions CodegenOptions { get; set; }
    }

    // This is a set of attributes used to override conventional behaviour in the bindings generator.
    // Please refer to bindings generator documentation.


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
    class NativeConditionalAttribute : Attribute, IBindingsAttribute
    {
        public string Condition { get; set; }
        public bool Enabled { get; set; }

        public NativeConditionalAttribute()
        {
        }

        public NativeConditionalAttribute(string condition)
        {
            Condition = condition;
            Enabled = true;
        }

        public NativeConditionalAttribute(bool enabled)
        {
            Enabled = enabled;
        }

        public NativeConditionalAttribute(string condition, bool enabled) : this(condition)
        {
            Enabled = enabled;
        }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    class NativeHeaderAttribute : Attribute, IBindingsHeaderProviderAttribute
    {
        public string Header { get; set; }

        public NativeHeaderAttribute()
        {
        }

        public NativeHeaderAttribute(string header)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (header == "") throw new ArgumentException("header cannot be empty", "header");

            Header = header;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    class NativeNameAttribute : Attribute, IBindingsNameProviderAttribute
    {
        public string Name { get; set; }

        public NativeNameAttribute()
        {
        }

        public NativeNameAttribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == "") throw new ArgumentException("name cannot be empty", "name");

            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    class NativeMethodAttribute : Attribute, IBindingsNameProviderAttribute, IBindingsIsThreadSafeProviderAttribute, IBindingsIsFreeFunctionProviderAttribute, IBindingsThrowsProviderAttribute
    {
        public string Name { get; set; }
        public bool IsThreadSafe { get; set; }
        public bool IsFreeFunction { get; set; }
        public bool ThrowsException { get; set; }
        public bool HasExplicitThis { get; set; }

        public NativeMethodAttribute()
        {
        }

        public NativeMethodAttribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == "") throw new ArgumentException("name cannot be empty", "name");

            Name = name;
        }

        public NativeMethodAttribute(string name, bool isFreeFunction) : this(name)
        {
            IsFreeFunction = isFreeFunction;
        }

        public NativeMethodAttribute(string name, bool isFreeFunction, bool isThreadSafe) : this(name, isFreeFunction)
        {
            IsThreadSafe = isThreadSafe;
        }

        public NativeMethodAttribute(string name, bool isFreeFunction, bool isThreadSafe, bool throws) : this(name, isFreeFunction, isThreadSafe)
        {
            ThrowsException = throws;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    class NativePropertyAttribute : NativeMethodAttribute
    {
        public NativePropertyAttribute()
        {
        }

        public NativePropertyAttribute(string name) : base(name)
        {
        }

        public NativePropertyAttribute(string name, bool isFreeFunction) : base(name, isFreeFunction)
        {
        }

        public NativePropertyAttribute(string name, bool isFreeFunction, bool isThreadSafe) : base(name, isFreeFunction, isThreadSafe)
        {
        }
    }

    enum CodegenOptions
    {
        Auto,
        Custom,
        Force
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    class NativeTypeAttribute : Attribute, IBindingsHeaderProviderAttribute, IBindingsGenerateMarshallingTypeAttribute
    {
        public string Header { get; set; }

        public string IntermediateScriptingStructName { get; set; }

        public CodegenOptions CodegenOptions { get; set; }

        public NativeTypeAttribute()
        {
            CodegenOptions = CodegenOptions.Auto;
        }

        public NativeTypeAttribute(CodegenOptions codegenOptions)
        {
            CodegenOptions = codegenOptions;
        }

        public NativeTypeAttribute(string header)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (header == "") throw new ArgumentException("header cannot be empty", "header");

            CodegenOptions = CodegenOptions.Auto;
            Header = header;
        }

        public NativeTypeAttribute(string header, CodegenOptions codegenOptions) : this(header)
        {
            CodegenOptions = codegenOptions;
        }

        public NativeTypeAttribute(CodegenOptions codegenOptions, string intermediateStructName) : this(codegenOptions)
        {
            IntermediateScriptingStructName = intermediateStructName;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    class NotNullAttribute : Attribute, IBindingsAttribute
    {
        public NotNullAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    class UnmarshalledAttribute : Attribute, IBindingsAttribute
    {
        public UnmarshalledAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    class FreeFunctionAttribute : NativeMethodAttribute
    {
        public FreeFunctionAttribute()
        {
            IsFreeFunction = true;
        }

        public FreeFunctionAttribute(string name) : base(name, true)
        {
        }

        public FreeFunctionAttribute(string name, bool isThreadSafe) : base(name, true, isThreadSafe)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    class ThreadSafeAttribute : NativeMethodAttribute
    {
        public ThreadSafeAttribute()
        {
            IsThreadSafe = true;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
    class StaticAccessorAttribute : Attribute, IBindingsAttribute
    {
        public string Name { get; set; }
        public bool Pointer { get; set; }

        public StaticAccessorAttribute()
        {
        }

        internal StaticAccessorAttribute(string name)
        {
            Name = name;
        }

        public StaticAccessorAttribute(bool pointer)
        {
            Pointer = pointer;
        }

        public StaticAccessorAttribute(string name, bool pointer)
        {
            Name = name;
            Pointer = pointer;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    class NativeThrowsAttribute : Attribute, IBindingsThrowsProviderAttribute
    {
        public bool ThrowsException { get; set; }

        public NativeThrowsAttribute()
        {
            ThrowsException = true;
        }

        public NativeThrowsAttribute(bool throwsException)
        {
            ThrowsException = throwsException;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    class IgnoreAttribute : Attribute, IBindingsAttribute
    {
    }
}
