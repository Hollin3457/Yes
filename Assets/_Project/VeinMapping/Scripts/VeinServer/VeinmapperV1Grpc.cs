// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: veinmapper_v1.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace NUHS.VeinServer {
  public static partial class VeinMapperServiceV1
  {
    static readonly string __ServiceName = "VeinMapperServiceV1";

    static readonly grpc::Marshaller<global::NUHS.VeinServer.VeinMapperSetupRequestV1> __Marshaller_VeinMapperSetupRequestV1 = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::NUHS.VeinServer.VeinMapperSetupRequestV1.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::NUHS.VeinServer.VeinMapperSetupResponseV1> __Marshaller_VeinMapperSetupResponseV1 = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::NUHS.VeinServer.VeinMapperSetupResponseV1.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::NUHS.VeinServer.VeinMapperStreamRequestV1> __Marshaller_VeinMapperStreamRequestV1 = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::NUHS.VeinServer.VeinMapperStreamRequestV1.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::NUHS.VeinServer.VeinMapperStreamResponseV1> __Marshaller_VeinMapperStreamResponseV1 = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::NUHS.VeinServer.VeinMapperStreamResponseV1.Parser.ParseFrom);

    static readonly grpc::Method<global::NUHS.VeinServer.VeinMapperSetupRequestV1, global::NUHS.VeinServer.VeinMapperSetupResponseV1> __Method_Setup = new grpc::Method<global::NUHS.VeinServer.VeinMapperSetupRequestV1, global::NUHS.VeinServer.VeinMapperSetupResponseV1>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Setup",
        __Marshaller_VeinMapperSetupRequestV1,
        __Marshaller_VeinMapperSetupResponseV1);

    static readonly grpc::Method<global::NUHS.VeinServer.VeinMapperStreamRequestV1, global::NUHS.VeinServer.VeinMapperStreamResponseV1> __Method_Stream = new grpc::Method<global::NUHS.VeinServer.VeinMapperStreamRequestV1, global::NUHS.VeinServer.VeinMapperStreamResponseV1>(
        grpc::MethodType.DuplexStreaming,
        __ServiceName,
        "Stream",
        __Marshaller_VeinMapperStreamRequestV1,
        __Marshaller_VeinMapperStreamResponseV1);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::NUHS.VeinServer.VeinmapperV1Reflection.Descriptor.Services[0]; }
    }

    /// <summary>Client for VeinMapperServiceV1</summary>
    public partial class VeinMapperServiceV1Client : grpc::ClientBase<VeinMapperServiceV1Client>
    {
      /// <summary>Creates a new client for VeinMapperServiceV1</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public VeinMapperServiceV1Client(grpc::ChannelBase channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for VeinMapperServiceV1 that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public VeinMapperServiceV1Client(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected VeinMapperServiceV1Client() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected VeinMapperServiceV1Client(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      /// <summary>
      /// Set up the device for vein mapping by sending in device parameters such as LUT
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::NUHS.VeinServer.VeinMapperSetupResponseV1 Setup(global::NUHS.VeinServer.VeinMapperSetupRequestV1 request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Setup(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// Set up the device for vein mapping by sending in device parameters such as LUT
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::NUHS.VeinServer.VeinMapperSetupResponseV1 Setup(global::NUHS.VeinServer.VeinMapperSetupRequestV1 request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_Setup, null, options, request);
      }
      /// <summary>
      /// Set up the device for vein mapping by sending in device parameters such as LUT
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::NUHS.VeinServer.VeinMapperSetupResponseV1> SetupAsync(global::NUHS.VeinServer.VeinMapperSetupRequestV1 request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return SetupAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// Set up the device for vein mapping by sending in device parameters such as LUT
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::NUHS.VeinServer.VeinMapperSetupResponseV1> SetupAsync(global::NUHS.VeinServer.VeinMapperSetupRequestV1 request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_Setup, null, options, request);
      }
      /// <summary>
      /// Stream images to be processed and returned as a point cloud
      /// </summary>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncDuplexStreamingCall<global::NUHS.VeinServer.VeinMapperStreamRequestV1, global::NUHS.VeinServer.VeinMapperStreamResponseV1> Stream(grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Stream(new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// Stream images to be processed and returned as a point cloud
      /// </summary>
      /// <param name="options">The options for the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncDuplexStreamingCall<global::NUHS.VeinServer.VeinMapperStreamRequestV1, global::NUHS.VeinServer.VeinMapperStreamResponseV1> Stream(grpc::CallOptions options)
      {
        return CallInvoker.AsyncDuplexStreamingCall(__Method_Stream, null, options);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override VeinMapperServiceV1Client NewInstance(ClientBaseConfiguration configuration)
      {
        return new VeinMapperServiceV1Client(configuration);
      }
    }

  }
}
#endregion
