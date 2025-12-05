using System.Buffers.Text;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace WSY.CertHub.Targets.SoftEther;

/// <summary>
/// VPN Server RPC Stubs
/// </summary>
public class VpnServerClient
{
    public const int DefaultTimeoutSeconds = 60;
    private readonly string base_url;
    public readonly HttpClient client;

    /// <summary>
    /// Constructor of the VpnServerClient class
    /// </summary>
    /// <param name="vpnserver_host">The hostname or IP address of the destination VPN Server. In the web browser you can specify null if you want to connect to the server itself.</param>
    /// <param name="vpnserver_port">The port number of the destination VPN Server. In the web browser you can specify null if you want to connect to the server itself.</param>
    /// <param name="admin_password">Specify the administration password. This value is valid only if vpnserver_hostname is sepcified.</param>
    /// <param name="hub_name">The name of the Virtual Hub if you want to connect to the VPN Server as a Virtual Hub Admin Mode. Specify null if you want to connect to the VPN Server as the Entire VPN Server Admin Mode.</param>
    public VpnServerClient(string vpnserver_host, int vpnserver_port, string admin_password, string? hub_name = null)
    {
        base_url = $"https://{vpnserver_host}:{vpnserver_port}/api/";

        HttpClientHandler client_handler = new() { AllowAutoRedirect = true, MaxAutomaticRedirections = 10, ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        client = new(client_handler, true) { Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds) };

        client.DefaultRequestHeaders.Add("X-VPNADMIN-HUBNAME", string.IsNullOrEmpty(hub_name) ? "" : hub_name);
        client.DefaultRequestHeaders.Add("X-VPNADMIN-PASSWORD", admin_password);
    }

    /// <summary>
    /// Set SSL Certificate and Private Key of VPN Server (Async mode).
    /// You can set the SSL certificate that the VPN Server provides to the connected client and the private key for that certificate.
    /// The certificate must be in X.509 format and the private key must be Base 64 encoded format.
    /// To call this API, you must have VPN Server administrator privileges.
    /// </summary>
    public async Task<VpnServerCertificate?> SetServerCertAsync(VpnServerCertificate t, CancellationToken cancellationToken) => await CallAsync("SetServerCert", t, cancellationToken);

    /// <summary>
    /// Set SSL Certificate and Private Key of VPN Server (Sync mode).
    /// You can set the SSL certificate that the VPN Server provides to the connected client and the private key for that certificate.
    /// The certificate must be in X.509 format and the private key must be Base 64 encoded format.
    /// To call this API, you must have VPN Server administrator privileges.
    /// </summary>
    public VpnServerCertificate? SetServerCert(VpnServerCertificate t) => SetServerCertAsync(t, CancellationToken.None).Result;

    /// <summary>
    /// Get SSL Certificate and Private Key of VPN Server (Async mode).
    /// Use this to get the SSL certificate private key that the VPN Server provides to the connected client.
    /// To call this API, you must have VPN Server administrator privileges.
    /// </summary>
    public async Task<VpnServerCertificate?> GetServerCertAsync(CancellationToken cancellationToken) => await CallAsync<VpnServerCertificate>("GetServerCert", new() { PrivateKey = null!, PublicKey = null! }, cancellationToken: cancellationToken);

    /// <summary>
    /// Get SSL Certificate and Private Key of VPN Server (Sync mode).
    /// Use this to get the SSL certificate private key that the VPN Server provides to the connected client.
    /// To call this API, you must have VPN Server administrator privileges.
    /// </summary>
    public VpnServerCertificate? GetServerCert() => GetServerCertAsync(CancellationToken.None).Result;

    /// <summary>
    /// Call a RPC procedure
    /// </summary>
    public async Task<T?> CallAsync<T>(string methodName, T? request = default, CancellationToken cancellationToken = default)
    {
        string id = DateTime.Now.Ticks.ToString();

        JsonRpcRequest<T> req = new(methodName, request, id);
        string req_string = JsonSerializer.Serialize(req);
        //Console.WriteLine($"req: {req_string}");

        HttpContent requestPayload = new StringContent(req_string, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(base_url, requestPayload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new JsonRpcException(new((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken)));
        }
        
        JsonRpcResponse<T> ret = await response.Content.ReadFromJsonAsync<JsonRpcResponse<T>>(cancellationToken) ?? throw new InvalidOperationException("Json Deserialize returned null value!");

        ret.ThrowIfError();

        return ret.Result;
    }

}

/// <summary>
/// JSON-RPC request class. See https://www.jsonrpc.org/specification
/// </summary>
internal record JsonRpcRequest<TParam>
{
    [JsonPropertyName("jsonrpc")]
    [JsonPropertyOrder(1)]
    public string Version { get; set; } = "2.0";

    [JsonPropertyName("id")]
    [JsonPropertyOrder(2)]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    [JsonPropertyOrder(3)]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    [JsonPropertyOrder(4)]
    public TParam? Params { get; set; } = default;

    public JsonRpcRequest(string method, TParam? param, string id)
    {
        this.Method = method;
        this.Params = param;
        this.Id = id;
    }
}

/// <summary>
/// JSON-RPC response class with generics
/// </summary>
/// <typeparam name="TResult"></typeparam>
internal record JsonRpcResponse<TResult>
{
    [JsonPropertyName("jsonrpc")]
    [JsonPropertyOrder(1)]
    public string Version { get; set; } = "2.0";

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonPropertyOrder(2)]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    [JsonPropertyOrder(3)]
    public TResult? Result { get; set; } = default;

    [JsonPropertyName("error")]
    [JsonPropertyOrder(4)]
    public JsonRpcError? Error { get; set; } = null;

    public void ThrowIfError()
    {
        if (this.Error != null)
        {
            throw new JsonRpcException(this.Error);
        }
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
