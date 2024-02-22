// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: veinserver_core_v1.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace NUHS.VeinServer {
  public static partial class VeinServerCoreServiceV1
  {
    static readonly string __ServiceName = "VeinServerCoreServiceV1";

    static readonly grpc::Marshaller<global::NUHS.VeinServer.VeinServerPingRequestV1> __Marshaller_VeinServerPingRequestV1 = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::NUHS.VeinServer.VeinServerPingRequestV1.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::NUHS.VeinServer.VeinServerPingResponseV1> __Marshaller_VeinServerPingResponseV1 = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::NUHS.VeinServer.VeinServerPingResponseV1.Parser.ParseFrom);

    static readonly grpc::Method<global::NUHS.VeinServer.VeinServerPingRequestV1, global::NUHS.VeinServer.VeinServerPingResponseV1> __Method_Ping = new grpc::Method<global::NUHS.VeinServer.VeinServerPingRequestV1, global::NUHS.VeinServer.VeinServerPingResponseV1>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Ping",
        __Marshaller_VeinServerPingRequestV1,
        __Marshaller_VeinServerPingResponseV1);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::NUHS.VeinServer.VeinserverCoreV1Reflection.Descriptor.Services[0]; }
    }

    /// <summary>Client for VeinServerCoreServiceV1</summary>
    public partial class VeinServerCoreServiceV1Client : grpc::ClientBase<VeinServerCoreServiceV1Client>
    {
      /// <summary>Creates a new client for VeinServerCoreServiceV1</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public VeinServerCoreServiceV1Client(grpc::ChannelBase channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for VeinServerCoreServiceV1 that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public VeinServerCoreServiceV1Client(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected VeinServerCoreServiceV1Client() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected VeinServerCoreServiceV1Client(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      /// <summary>
      /// Ping request to check if server is alive
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::NUHS.VeinServer.VeinServerPingResponseV1 Ping(global::NUHS.VeinServer.VeinServerPingRequestV1 request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Ping(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// Ping request to check if server is alive
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::NUHS.VeinServer.VeinServerPingResponseV1 Ping(global::NUHS.VeinServer.VeinServerPingRequestV1 request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_Ping, null, options, request);
      }
      /// <summary>
      /// Ping request to check if server is alive
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::NUHS.VeinServer.VeinServerPingResponseV1> PingAsync(global::NUHS.VeinServer.VeinServerPingRequestV1 request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return PingAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// Ping request to check if server is alive
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::NUHS.VeinServer.VeinServerPingResponseV1> PingAsync(global::NUHS.VeinServer.VeinServerPingRequestV1 request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_Ping, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override VeinServerCoreServiceV1Client NewInstance(ClientBaseConfiguration configuration)
      {
        return new VeinServerCoreServiceV1Client(configuration);
      }
    }

  }
}
#endregion