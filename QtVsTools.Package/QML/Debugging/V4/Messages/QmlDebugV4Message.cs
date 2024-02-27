/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only WITH Qt-GPL-exception-1.0
***************************************************************************************************/

using System;
using System.Runtime.Serialization;

namespace QtVsTools.Qml.Debug.V4
{
    using Json;

    [DataContract]
    class Message : Serializable<Message>
    {
        //  <message type>
        //  ...
        protected Message()
        { }

        public static T Create<T>(ProtocolDriver driver)
            where T : Message, new()
        {
            return new T { Driver = driver };
        }

        public static bool Send<T>(ProtocolDriver driver, Action<T> initMsg = null)
            where T : Message, new()
        {
            T _this = Create<T>(driver);
            initMsg?.Invoke(_this);
            return _this.Send();
        }

        public string Type { get; set; }

        protected sealed override void InitializeObject(object initArgs)
        {
            if (initArgs is string args)
                Type = args;
        }

        protected override bool? IsCompatible(Message that)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (that == null)
                return false;
            if (string.IsNullOrEmpty(Type))
                return null;
            return this.Type == that.Type;
        }

        protected ProtocolDriver Driver { get; private set; }

        public virtual bool Send()
        {
            return Driver.SendMessage(this);
        }
    }

    [DataContract]
    class Request : Message
    {
        //  "v8request"
        //  { "seq"     : <number>,
        //    "type"    : "request",
        //    "command" : <command>
        //    ...
        //  }
        public const string MSG_TYPE = "v8request";
        public const string MSG_SUBTYPE = "request";
        protected Request()
        {
            Type = MSG_TYPE;
            SubType = MSG_SUBTYPE;
        }

        [DataMember(Name = "type")]
        public string SubType { get; set; }

        [DataMember(Name = "command")]
        public string Command { get; set; }

        [DataMember(Name = "seq")]
        public int SequenceNum { get; set; }

        protected override bool? IsCompatible(Message msg)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(msg) == false)
                return false;

            if (string.IsNullOrEmpty(SubType) || string.IsNullOrEmpty(Command))
                return null;
            if (msg is Request that)
                return this.SubType == that.SubType && this.Command == that.Command;
            return null;
        }

        Response response;
        public Response Response
        {
            get => response;
            set => Atomic(() => response == null, () => response = value);
        }

        object tag;
        public object Tag
        {
            get => tag;
            set => Atomic(() => tag == null, () => tag = value);
        }

        public virtual ProtocolDriver.PendingRequest SendRequest()
        {
            return Driver.SendRequest(this);
        }

        public new Response Send()
        {
            return SendRequest().WaitForResponse();
        }

        public static new Response Send<T>(ProtocolDriver driver, Action<T> initMsg = null)
            where T : Request, new()
        {
            T _this = Create<T>(driver);
            initMsg?.Invoke(_this);
            return _this.Send();
        }
    }

    [DataContract]
    [SkipDeserialization]
    class Request<TResponse> : Request
        where TResponse : Response
    {
        public new TResponse Response => base.Response as TResponse;

        public new virtual TResponse Send()
        {
            var pendingRequest = SendRequest();
            if (!pendingRequest.RequestSent)
                return null;

            return pendingRequest.WaitForResponse() == null ? null : Response;
        }
    }

    [DataContract]
    [SkipDeserialization]
    class Request<TResponse, TArgs> : Request<TResponse>
        where TResponse : Response
        where TArgs : class, new()
    {
        [DataMember(Name = "arguments", EmitDefaultValue = false)]
        TArgs args;

        public virtual TArgs Arguments
        {
            get => ThreadSafe(() => args ??= new TArgs());
            set => args = value;
        }
    }

    [DataContract]
    class ServerMessage : Message
    {
        //  "v8message"
        //  { "seq"  : <number>,
        //    "type" : <string>
        //    ...
        //  }
        public const string MSG_TYPE = "v8message";
        protected ServerMessage()
        {
            Type = MSG_TYPE;
        }

        [DataMember(Name = "type")]
        public string SubType { get; set; }

        [DataMember(Name = "seq")]
        public int SequenceNum { get; set; }

        protected override bool? IsCompatible(Message msg)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(msg) == false)
                return false;

            if (!string.IsNullOrEmpty(SubType) && msg is ServerMessage that)
                return this.SubType == that.SubType;
            return null;
        }
    }

    [DataContract]
    class Response : ServerMessage
    {
        //  "v8message"
        //  { "seq"         : <number>,
        //    "type"        : "response",
        //    "request_seq" : <number>,
        //    "command"     : <command>,
        //    "running"     : <is the VM running after sending this response>,
        //    "success"     : <success?>,
        //    "message"     : [error message]
        //    ...
        //  }
        public const string MSG_SUBTYPE = "response";
        protected Response()
        {
            SubType = MSG_SUBTYPE;
        }

        [DataMember(Name = "command")]
        public string Command { get; set; }

        [DataMember(Name = "request_seq")]
        public int RequestSeq { get; set; }

        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "running")]
        public bool Running { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        protected override bool? IsCompatible(Message msg)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(msg) == false)
                return false;

            if (msg is Response that) {
                // If response is unsuccessful, no need to continue searching, just use this class,
                // it already has all the data needed for error processing (i.e. the error message)
                if (!that.Success)
                    return true;

                if (string.IsNullOrEmpty(Command))
                    return null;

                return this.Command == that.Command;
            }
            return null;
        }
    }

    [DataContract]
    [SkipDeserialization]
    class Response<TBody> : Response
        where TBody : class, new()
    {
        [DataMember(Name = "body", EmitDefaultValue = false)]
        TBody body;

        public TBody Body
        {
            get => ThreadSafe(() => body ??= new TBody());
            set => body = value;
        }
    }

    [DataContract]
    class Event : ServerMessage
    {
        //  "v8message"
        //  { "seq"   : <number>,
        //    "type"  : "event",
        //    "event" : <string>,
        //    ...
        //  }
        public const string MSG_SUBTYPE = "event";
        protected Event()
        {
            SubType = MSG_SUBTYPE;
        }

        [DataMember(Name = "event")]
        public string EventType { get; set; }

        protected override bool? IsCompatible(Message msg)
        {
            System.Diagnostics.Debug.Assert(IsPrototype);

            if (base.IsCompatible(msg) == false)
                return false;

            if (!string.IsNullOrEmpty(EventType) && msg is Event that)
                return this.EventType == that.EventType;
            return null;
        }
    }

    [DataContract]
    [SkipDeserialization]
    class Event<TBody> : Event
        where TBody : class, new()
    {
        [DataMember(Name = "body", EmitDefaultValue = false)]
        private TBody body;

        public TBody Body
        {
            get => ThreadSafe(() => body ??= new TBody());
            set => body = value;
        }
    }
}
