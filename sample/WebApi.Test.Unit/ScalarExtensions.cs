namespace WebApiScalar;

/// <summary>
/// Provides extension methods for mapping Scalar UI endpoints.
/// </summary>
public static class ScalarExtensions
{
    /// <summary>
    /// Maps the Scalar UI endpoint to the specified route.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>An endpoint convention builder for the mapped endpoint.</returns>
    public static IEndpointConventionBuilder MapScalarUi(this IEndpointRouteBuilder endpoints)
    {
        // ReSharper disable once StringLiteralTypo
        return endpoints.MapGet("/scalar/{documentName}", (string documentName) => Results.Content($$"""
                                                                                                     <!doctype html>
                                                                                                     <html>

                                                                                                     <head>
                                                                                                         <title>Scalar API Reference -- {{documentName}}</title>
                                                                                                         <meta charset="utf-8" />
                                                                                                         <meta name="viewport" content="width=device-width, initial-scale=1" />
                                                                                                     </head>

                                                                                                     <body>
                                                                                                         <script id="api-reference" data-url="/openapi/{{documentName}}.json"></script>
                                                                                                         <script>
                                                                                                             var configuration = {
                                                                                                                 theme: 'purple',
                                                                                                                 defaultHttpClient: {
                                                                                                                     targetKey: 'csharp',
                                                                                                                     clientKey: 'httpclient',
                                                                                                                 }
                                                                                                             }
                                                                                                     
                                                                                                             document.getElementById('api-reference').dataset.configuration = JSON.stringify(configuration)
                                                                                                         </script>
                                                                                                         <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
                                                                                                     </body>

                                                                                                     </html>
                                                                                                     """, "text/html")).ExcludeFromDescription();
    }
}