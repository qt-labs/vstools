/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using QtVsTools.Core;

/// <summary>
/// The classes in this namespace provide support to the serialization and deserialization of
/// .NET objects using the JavaScript Object Notation (JSON) format. The transformation of
/// objects to and from JSON data is based on the DataContractJsonSerializer class provided
/// by the .NET framework, as documented in the following page:
///
///     https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-serialize-and-deserialize-json-data
///
/// To support the deserialization of polymorphic types, the concept of deferred deserialization
/// is introduced: if a field is marked for deferred deserialization, the corresponding JSON data
/// is not interpreted right way, but is rather stored for later processing, e.g. when the actual
/// type of the field can be determined.
/// </summary>
///
namespace QtVsTools.Json
{
    /// <summary>
    /// Public interface of objects representing JSON serialized data
    /// </summary>
    ///
    public interface IJsonData : IDisposable
    {
        bool IsEmpty();
        byte[] GetBytes();
        string GetString();
    }

    /// <summary>
    /// Public interface of types providing deferred deserialization.
    /// </summary>
    ///
    public interface IDeferredObject
    {
        object Object { get; }
        bool HasData { get; }
        void Deserialize();
    }

    /// <summary>
    /// Public interface of types containing deferred-deserialized objects
    /// </summary>
    ///
    public interface IDeferredObjectContainer
    {
        void Add(IDeferredObject defObj);
        IEnumerable<IDeferredObject> PendingObjects { get; }
    }

    /// <summary>
    /// A Serializer object allows the serialization and deserialization of objects using the JSON
    /// format, by extending the services provided by the DataContractJsonSerializer class.
    /// </summary>
    ///
    public class Serializer : Concurrent
    {
        private DataContractJsonSerializer serializer;

        public static Serializer Create(Type type)
        {
            var _this = new Serializer();
            return _this.Initialize(type) ? _this : null;
        }

        private Serializer()
        { }

        private bool Initialize(Type type)
        {
            var settings = new DataContractJsonSerializerSettings();
            settings.DataContractSurrogate = new DataContractSurrogate { Serializer = this };
            settings.EmitTypeInformation = EmitTypeInformation.Never;
            settings.UseSimpleDictionaryFormat = true;

            serializer = new DataContractJsonSerializer(type, settings);
            return serializer != null;
        }

        public IJsonData Serialize(object obj, bool indent = false)
        {
            var stream = new MemoryStream();
            using (var writer = JsonReaderWriterFactory
                .CreateJsonWriter(stream, Encoding.UTF8, true, indent)) {
                try {
                    serializer.WriteObject(writer, obj);
                    writer.Close();
                    return new JsonData { Stream = stream };
                } catch (Exception exception) {
                    exception.Log();
                    if (stream is { CanRead: true, Length: > 0 })
                        stream.Dispose();
                    return null;
                }
            }
        }

        public object Deserialize(IJsonData jsonData)
        {
            if (jsonData is not JsonData data)
                return null;

            if (data.XmlStream == null && !Parse(data))
                return null;
            if (data.XmlStream == null)
                return null;

            lock (CriticalSection) {
                try {
                    using (reader = XmlReader.Create(data.XmlStream)) {
                        var obj = serializer.ReadObject(reader, false);

                        if (obj is IDeferredObjectContainer container)
                            deferredObjects.ForEach(x => container.Add(x));

                        return obj;
                    }

                } catch (Exception exception) {
                    exception.Log();
                    return null;

                } finally {
                    reader = null;
                    deferredObjects.Clear();
                    data.XmlStream.Position = 0;
                }
            }
        }

        /// <summary>
        /// Parses raw JSON data and returns the corresponding IJsonData object.
        /// </summary>
        /// <param name="rawJsonData">Raw JSON data</param>
        /// <returns>IJsonData object corresponding to the data provided</returns>
        ///
        public static IJsonData Parse(byte[] rawJsonData)
        {
            rawJsonData ??= Array.Empty<byte>();

            var data = new JsonData
            {
                Stream = new MemoryStream(rawJsonData)
            };

            if (!Parse(data)) {
                data.Dispose();
                return null;
            }

            return data;
        }

        private static bool Parse(JsonData data)
        {
            try {
                var q = new XmlDictionaryReaderQuotas();
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(data.Stream, q)) {
                    reader.Read();
                    var xmlData = Encoding.UTF8.GetBytes(reader.ReadOuterXml());
                    reader.Close();
                    data.XmlStream = new MemoryStream(xmlData);
                }
                return true;
            } catch (Exception exception) {
                exception.Log();
                return false;
            }
        }

        #region //////////////////// JsonData /////////////////////////////////////////////////////

        private class JsonData : Disposable, IJsonData
        {
            public MemoryStream Stream { get; set; }
            public MemoryStream XmlStream { get; set; }

            byte[] IJsonData.GetBytes()
            {
                return Stream.ToArray();
            }

            string IJsonData.GetString()
            {
                return Encoding.UTF8.GetString(((IJsonData)this).GetBytes());
            }

            bool IJsonData.IsEmpty()
            {
                return Stream is not {CanRead: true, Length: not 0}
                    && XmlStream is not {CanRead: true, Length: not 0};
            }

            protected override void DisposeManaged()
            {
                Stream?.Dispose();
                XmlStream?.Dispose();
            }
        }

        #endregion //////////////////// JsonData //////////////////////////////////////////////////


        #region //////////////////// Data Contract Surrogate //////////////////////////////////////

        static readonly Exclusive<Serializer> sharedInstance = new();
        private XmlReader reader;
        private readonly List<IDeferredObject> deferredObjects = new();

        public static IJsonData GetCurrentJsonData()
        {
            Serializer _this = sharedInstance;
            try {
                var root = new StringBuilder();
                root.Append("<root type=\"object\">");
                while (_this.reader.IsStartElement())
                    root.Append(_this.reader.ReadOuterXml());
                root.Append("</root>");
                var xmlData = Encoding.UTF8.GetBytes(root.ToString());
                return new JsonData { XmlStream = new MemoryStream(xmlData) };
            } catch (Exception exception) {
                exception.Log();
                return null;
            }
        }

        class DataContractSurrogate : IDataContractSurrogate
        {
            public Serializer Serializer { get; set; }

            Type IDataContractSurrogate.GetDataContractType(Type type)
            {
                if (typeof(IDeferredObject).IsAssignableFrom(type)) {
                    // About to process a deferred object: lock shared serializer
                    sharedInstance.Set(Serializer);
                }
                return type;
            }

            object IDataContractSurrogate.GetDeserializedObject(object obj, Type targetType)
            {
                if (typeof(IDeferredObject).IsAssignableFrom(targetType)) {
                    // Deferred object deserialized: add to list of deferred objects...
                    Serializer.deferredObjects.Add(obj as IDeferredObject);

                    // ...and release shared serializer
                    sharedInstance.Release();
                }
                return obj;
            }

            object IDataContractSurrogate.GetObjectToSerialize(object obj, Type targetType)
            {
                if (obj is IDeferredObject deferredObject) {
                    // Deferred object serialized: release shared serializer
                    sharedInstance.Release();

                    return deferredObject.Object;
                }
                return obj;
            }

            object IDataContractSurrogate.GetCustomDataToExport(
                MemberInfo memberInfo,
                Type dataContractType)
            { throw new NotImplementedException(); }

            object IDataContractSurrogate.GetCustomDataToExport(
                Type clrType,
                Type dataContractType)
            { throw new NotImplementedException(); }

            Type IDataContractSurrogate.GetReferencedTypeOnImport(
                string typeName,
                string typeNamespace,
                object customData)
            { throw new NotImplementedException(); }

            CodeTypeDeclaration IDataContractSurrogate.ProcessImportedType(
                CodeTypeDeclaration typeDeclaration,
                CodeCompileUnit compileUnit)
            { throw new NotImplementedException(); }

            void IDataContractSurrogate.GetKnownCustomDataTypes(Collection<Type> customDataTypes)
            { throw new NotImplementedException(); }
        }

        #endregion //////////////////// Data Contract Surrogate ///////////////////////////////////

    }
}
