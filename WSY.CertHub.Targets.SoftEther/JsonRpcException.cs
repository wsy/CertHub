
// SoftEther VPN Server JSON-RPC Stub code for C#
// 
// JsonRpc.cs - JSON-RPC Client Utility Functions
//
// Automatically generated at 2023-05-10 14:43:37 by vpnserver-jsonrpc-codegen
//
// Licensed under the Apache License 2.0
// Copyright (c) 2014-2023 SoftEther VPN Project

using System.Text.Json.Serialization;

namespace WSY.CertHub.Targets.SoftEther;

/// <summary>
/// JSON-RPC exception class
/// </summary>
public class JsonRpcException(JsonRpcError err) : Exception($"Code={err.Code}, Message={err.Message}, Data={JsonSerializer.Serialize(err?.Data)}")
{
    public JsonRpcError RpcError { get; } = err;
}

/// <summary>
/// JSON-RPC error class. See https://www.jsonrpc.org/specification
/// </summary>
public record JsonRpcError
{
    public JsonRpcError() { }
    public JsonRpcError(int code, string message, object? data = null)
    {
        this.Code = code;
        this.Message = message;
        if (string.IsNullOrWhiteSpace(this.Message))
        {
            this.Message = $"JSON-RPC Error {code}";
        }
        this.Data = data;
    }

    [JsonPropertyName("code")]
    public int Code { get; set; } = 0;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; } = null;
}
