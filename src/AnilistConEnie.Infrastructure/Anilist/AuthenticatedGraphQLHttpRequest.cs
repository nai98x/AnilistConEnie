using System.Net.Http.Headers;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;

namespace AnilistConEnie.Infrastructure.Anilist;

/// <summary>
/// Request de GraphQL que adjunta el token OAuth del usuario en el header <c>Authorization</c>.
/// El header se agrega por request (no en el cliente compartido), por lo que es seguro usarlo
/// con el <see cref="GraphQLHttpClient"/> singleton del executor.
/// </summary>
internal sealed class AuthenticatedGraphQLHttpRequest(GraphQLRequest request, string accessToken) : GraphQLHttpRequest(request)
{
    public override HttpRequestMessage ToHttpRequestMessage(GraphQLHttpClientOptions options, IGraphQLJsonSerializer serializer)
    {
        HttpRequestMessage message = base.ToHttpRequestMessage(options, serializer);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return message;
    }
}
