﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Common;

    public delegate TypeSchema SchemaGenerator(Type type);

    /// <summary>
    /// Flags indicating type.
    /// </summary>
    public enum TypeFlags : uint
    {
        /// <summary>
        /// Type is a class (reference).
        /// </summary>
        IsClass = 0x01,

        /// <summary>
        /// Type is a struct (value).
        /// </summary>
        IsStruct = 0x02,

        /// <summary>
        /// Type is a contract (interface).
        /// </summary>
        IsContract = 0x04,

        /// <summary>
        /// Type is a collection (enumerable).
        /// </summary>
        IsCollection = 0x08
    }

    /// <summary>
    /// The schema definition used when serializing and deserializing a type
    /// </summary>
    public sealed class TypeSchema : Metadata
    {
        private static readonly XsdDataContractExporter DcInspector = new XsdDataContractExporter();
        private Dictionary<string, TypeMemberSchema> map;
        private TypeFlags flags;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeSchema"/> class.
        /// </summary>
        /// <param name="name">The contract name</param>
        /// <param name="id">The id, as generated by <see cref="GetId(string)"/></param>
        /// <param name="typeName">The assembly-qualified type name</param>
        /// <param name="flags">The type flags</param>
        /// <param name="members">The serializable members of the type, in the correct order</param>
        /// <param name="version">The schema version, usually representing the version of the code that generated this schema</param>
        /// <param name="serializerTypeName">The name of the serializer that produced the schema</param>
        /// <param name="serializerVersion">The version of the serializer that produced the schema</param>
        public TypeSchema(string name, int id, string typeName, TypeFlags flags, IEnumerable<TypeMemberSchema> members, int version, string serializerTypeName = null, int serializerVersion = 0)
            : this(name, id, typeName, version, serializerTypeName, serializerVersion)
        {
            this.flags = flags;
            this.Members = members.ToArray();
            this.map = new Dictionary<string, TypeMemberSchema>(this.Members.Length);
            foreach (var m in members)
            {
                this.map[m.Name] = m;
            }
        }

        internal TypeSchema(string name, int id, string typeName, int version, string serializerTypeName = null, int serializerVersion = 0)
           : base(MetadataKind.TypeSchema, name, id, typeName, version, serializerTypeName, serializerVersion, 0)
        {
        }

        /// <summary>
        /// Gets the type flags.
        /// </summary>
        public TypeFlags Flags => this.flags;

        /// <summary>
        /// Gets a value indicating whether type is partial.
        /// </summary>
        public bool IsPartial => this.Members == null;

        /// <summary>
        /// Gets the serializable members of the type.
        /// </summary>
        public TypeMemberSchema[] Members { get; private set; }

        /// <summary>
        /// Generates a schema for the specified type.
        /// If the type is DataContract-compatible (and version > 0), the schema is based on DataContract rules (see https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/serializable-types?view=netframework-4.7)
        /// If not, the schema is based on binary serialization rules (see https://docs.microsoft.com/en-us/dotnet/api/system.serializableattribute?view=netframework-4.7)
        /// </summary>
        /// <param name="type">The type to generate the schema for.</param>
        /// <param name="runtimeVersion">The version of the schema generation rules to use (same as <see cref="KnownSerializers.RuntimeVersion"/>).</param>
        /// <param name="serializer">The type of the serializer that will use this schema</param>
        /// <param name="serializerVersion">The version of the serializer that will use this schema</param>
        /// <returns>A schema describing the serialization information for the specified type</returns>
        public static TypeSchema FromType(Type type, RuntimeInfo runtimeVersion, Type serializer, int serializerVersion)
        {
            // can't support the implicit DataContract model yet (would need to parse the DCInspector schema).
            // bool hasDataContract = DcInspector.CanExport(type);
            bool hasDataContract = Attribute.IsDefined(type, typeof(DataContractAttribute));
            string name = GetContractName(type, runtimeVersion);
            var members = new List<TypeMemberSchema>();
            TypeFlags flags = type.IsValueType ? TypeFlags.IsStruct : TypeFlags.IsClass;

            // version 0 didn't use DataContract
            if (runtimeVersion.SerializerVersion == 0)
            {
                // code from old version of Generator.cs.
                // It incorrectly includes inherited fields multiple times, but needs to remain this way for compatibility with v0 stores.
                var allFields = type
                    .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => !f.FieldType.IsPointer);

                for (var t = type.BaseType; t != null && t != typeof(object); t = t.BaseType)
                {
                    // add all private fields from all base classes up the inheritance chain
                    var privateInherited = t
                     .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                     .Where(f => !f.FieldType.IsPointer);
                    allFields = allFields.Union(privateInherited);
                }

                members.AddRange(
                    allFields
                    .Select(f => new TypeMemberSchema(f.Name, f.FieldType.AssemblyQualifiedName, !Attribute.IsDefined(f, typeof(OptionalFieldAttribute)), f)));
            }
            else if (!hasDataContract)
            {
                // use binary serialization approach (all fields and only fields) when the type is not data contract capable
                members.AddRange(
                    Generator
                    .GetClonableFields(type)
                    .Select(f => new TypeMemberSchema(f.Name, f.FieldType.AssemblyQualifiedName, !Attribute.IsDefined(f, typeof(OptionalFieldAttribute)), f)));
            }
            else
            {
                // DataContract-capable (>=v1)
                flags |= TypeFlags.IsContract;

                // if [DataContract] is not present, collect all public fields and properties
                // else collect all [DataMember] fields and properties
                // see https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/serializable-types?view=netframework-4.7
                if (!Attribute.IsDefined(type, typeof(DataContractAttribute)))
                {
                    // DataContract-capable (>=v2) - this branch is not yet active
                    // all public fields (including inherited)
                    members.AddRange(
                        type
                        .GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Where(Generator.NonSerializedFilter)
                        .Select(f => new TypeMemberSchema(f.Name, f.FieldType.AssemblyQualifiedName, false, f)));

                    // all public props  (including inherited) with public get & set
                    members.AddRange(
                        type
                        .GetProperties(BindingFlags.FlattenHierarchy & ~BindingFlags.Static)
                        .Where(p => p.CanRead && p.CanWrite && p.GetMethod.IsPublic && p.SetMethod.IsPublic)
                        .Select(p => new TypeMemberSchema(p.Name, p.PropertyType.AssemblyQualifiedName, false, p)));

                    // sort alphabetically, ignoring case
                    members.Sort((v1, v2) => string.Compare(v1.Name, v2.Name, true));
                }
                else
                {
                    // all fields and props with [DataMember], public or private, inherited
                    // the ordering is base-class first, and within each set, Order value first, then alphabetical
                    var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                    var typeHierarchy = new Stack<Type>();
                    for (var bt = type; bt != null && bt != typeof(object) && !bt.IsInterface; bt = bt.BaseType)
                    {
                        typeHierarchy.Push(bt);
                    }

                    foreach (var currentType in typeHierarchy)
                    {
                        var localMembers = new List<(string name, string type, bool required, int order, MemberInfo info)>();
                        localMembers.AddRange(
                            currentType
                            .GetFields(bindingFlags)
                            .Where(f => Attribute.IsDefined(f, typeof(DataMemberAttribute)))
                            .Select(f => (f, f.GetCustomAttribute<DataMemberAttribute>()))
                            .Select(t => (t.Item1.Name, t.Item1.FieldType.AssemblyQualifiedName, t.Item2.IsRequired, t.Item2.Order, (MemberInfo)t.Item1)));
                        localMembers.AddRange(
                        currentType
                            .GetProperties(bindingFlags)
                            .Where(p => p.CanRead && p.CanWrite) // include private setters
                            .Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)))
                            .Select(p => (p, p.GetCustomAttribute<DataMemberAttribute>()))
                            .Select(t => (t.Item1.Name, t.Item1.PropertyType.AssemblyQualifiedName, t.Item2.IsRequired, t.Item2.Order, (MemberInfo)t.Item1)));

                        // sort by order first, then alphabetically ignoring case
                        localMembers.Sort((v1, v2) => v1.order == v2.order ? string.Compare(v1.name, v2.name, true) : Math.Sign(v1.order - v2.order));
                        members.AddRange(localMembers.Select(t => new TypeMemberSchema(t.name, t.type, t.required, t.info)));
                    }
                }
            }

            return new TypeSchema(name, GetId(name), type.AssemblyQualifiedName, flags, members, runtimeVersion.SerializerVersion, serializer.AssemblyQualifiedName, serializerVersion);
        }

        /// <summary>
        /// Returns the contract name for a given type, which is either the DataContract name, if available, or the assembly-qualified type name
        /// </summary>
        /// <param name="type">The type to generate the name for</param>
        /// <param name="runtimeVersion">The version of the schema generation rules to use (same as <see cref="KnownSerializers.RuntimeVersion"/>).</param>
        /// <returns>The DataContract name, if available, or the assembly-qualified type name</returns>
        public static string GetContractName(Type type, RuntimeInfo runtimeVersion)
        {
            // v2 will use DcInspector.CanExport(type)
            if (Attribute.IsDefined(type, typeof(DataContractAttribute)) && runtimeVersion.SerializerVersion > 0)
            {
                var name = DcInspector.GetSchemaTypeName(type);
                if (name != null)
                {
                    return name.ToString();
                }
            }

            return type.AssemblyQualifiedName;
        }

        /// <summary>
        /// Generates a unique ID for the type, based on the type's contract name
        /// (DataContract name, if available, or the assembly-qualified type name)
        /// </summary>
        /// <param name="type">The type to generate an ID for</param>
        /// <param name="runtimeVersion">The version of the schema generation rules to use (same as <see cref="KnownSerializers.RuntimeVersion"/>).</param>
        /// <returns>A hash of the type's contract name</returns>
        public static int GetId(Type type, RuntimeInfo runtimeVersion)
        {
            return GetId(GetContractName(type, runtimeVersion));
        }

        /// <summary>
        /// Returns a unique ID for the given contract name.
        /// </summary>
        /// <param name="contractName">The contract name</param>
        /// <returns>A hash of the contract name</returns>
        public static int GetId(string contractName)
        {
            // inspired by string.GetHashCode()
            // see https://referencesource.microsoft.com/#mscorlib/system/string.cs,0a17bbac4851d0d4
            // we are not using string.GetHashCode because 1) it promises to not be stable between versions and 2) we only have room for 30 bits when storing the id
            unsafe
            {
                fixed (char* src = contractName)
                {
                    int hash1 = (5381 << 16) + 5381;
                    int hash2 = hash1;
                    int* pint = (int*)src;
                    int len = contractName.Length;
                    while (len > 2)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                        hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ pint[1];
                        pint += 2;
                        len -= 4;
                    }

                    if (len > 0)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                    }

                    return (hash1 + (hash2 * 1566083941)) & 0x3FFFFFFF; // persistence needs two bits, so restrict the id to 30.
                }
            }
        }

        /// <summary>
        /// Sets the serializer name and version, which a serializer needs to be able to interpret
        /// when initialized with an older schema.
        /// </summary>
        /// <param name="serializerType">The serialize type name</param>
        /// <param name="serializerVersion">The serializer version</param>
        public void SetSerializerInfo(string serializerType, int serializerVersion)
        {
            this.SerializerTypeName = serializerType;
            this.SerializerVersion = serializerVersion;
        }

        /// <summary>
        /// Validate whether two schemas are compatible.
        /// </summary>
        /// <remarks>Schemas are compatible if all required fields are present in both (regardless of type).</remarks>
        /// <param name="other">Other type schema.</param>
        public void ValidateCompatibleWith(TypeSchema other)
        {
            if (this.IsPartial || other == null || other.IsPartial)
            {
                return;
            }

            if ((this.flags & TypeFlags.IsClass) != 0 && (other.flags & TypeFlags.IsStruct) != 0)
            {
                throw new SerializationException($"The type {this.TypeName} changed between versions from struct to class, which is not supported.");
            }
            else if ((this.flags & TypeFlags.IsStruct) != 0 && (other.flags & TypeFlags.IsClass) != 0)
            {
                throw new SerializationException($"The type {this.TypeName} changed between versions from class to struct, which is not supported.");
            }

            // required members in this schema must be present in the other schema
            var requiredAndMissing = this.Members.Where(mbr => mbr.IsRequired && !other.map.ContainsKey(mbr.Name));
            if (requiredAndMissing.Count() > 0)
            {
                if (other.TypeName != this.TypeName)
                {
                    throw new SerializationException($"The schema {other.Name} version {other.Version} (implemented by {other.TypeName}) is missing the following members required in the current version of {this.TypeName}: {string.Join(",", requiredAndMissing)}");
                }
                else
                {
                    throw new SerializationException($"The type {this.TypeName} appears to have changed in a way that makes it incompatible with previous versions. The following members required by the new version are missing: {string.Join(",", requiredAndMissing)}");
                }
            }

            // all members in the other schema need to be present in this schema
            requiredAndMissing = other.Members.Where(o => !this.map.ContainsKey(o.Name));
            if (requiredAndMissing.Count() > 0)
            {
                if (other.TypeName != this.TypeName)
                {
                    throw new SerializationException($"The schema {other.Name} version {other.Version} (implemented by {other.TypeName}) contains the following members which are not present in the current version of {this.TypeName}: {string.Join(",", requiredAndMissing)}");
                }
                else
                {
                    throw new SerializationException($"The type {this.TypeName} appears to have changed in a way that makes it incompatible with previous versions. The following members required by the old version are missing in the new version: {string.Join(",", requiredAndMissing)}");
                }
            }
        }

        /// <summary>
        /// Retrieves the <see cref="MemberInfo"/> information for each member of the type schema, based on a target schema specification.
        /// </summary>
        /// <param name="targetSchema">The schema specification describing which members and in which order to enumerate.
        /// If null, all members are returned in their original order.</param>
        /// <returns>A collection of <see cref="MemberInfo"/> objects.</returns>
        public IEnumerable<MemberInfo> GetCompatibleMemberSet(TypeSchema targetSchema = null)
        {
            if (targetSchema == null || targetSchema.IsPartial)
            {
                return this.Members.Select(t => t.MemberInfo);
            }

            this.ValidateCompatibleWith(targetSchema);

            var set = new List<MemberInfo>(targetSchema.Members.Length);
            foreach (var o in targetSchema.Members)
            {
                if (this.map.TryGetValue(o.Name, out TypeMemberSchema ms))
                {
                    set.Add(ms.MemberInfo);
                }
            }

            return set;
        }

        internal new void Deserialize(BufferReader metadataBuffer)
        {
            var memberCount = metadataBuffer.ReadInt32();
            if (memberCount == 0)
            {
                return;
            }

            this.Members = new TypeMemberSchema[memberCount];
            this.map = new Dictionary<string, TypeMemberSchema>(memberCount);
            for (int i = 0; i < memberCount; i++)
            {
                var m = new TypeMemberSchema(metadataBuffer.ReadString(), metadataBuffer.ReadString(), metadataBuffer.ReadBool(), null);
                this.Members[i] = m;
                this.map.Add(m.Name, m);
            }

            if (this.Version >= 2)
            {
                this.flags = (TypeFlags)metadataBuffer.ReadUInt32();
            }
        }

        internal override void Serialize(BufferWriter metadataBuffer)
        {
            base.Serialize(metadataBuffer);
            if (this.Members == null)
            {
                metadataBuffer.Write(0);
                return;
            }

            metadataBuffer.Write(this.Members.Length);
            foreach (var m in this.Members)
            {
                metadataBuffer.Write(m.Name);
                metadataBuffer.Write(m.Type);
                metadataBuffer.Write(m.IsRequired);
            }

            if (this.Version >= 2)
            {
                metadataBuffer.Write((uint)this.flags);
            }
        }
    }
}
