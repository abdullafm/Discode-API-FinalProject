using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Discode_API_FinalProject.Helpers
{
    public class SwaggerExamplesFilter : ISchemaFilter
    {
        private static readonly Dictionary<string, IOpenApiAny> Examples = new(StringComparer.OrdinalIgnoreCase)
        {
            ["email"] = new OpenApiString("user@example.com"),
            ["password"] = new OpenApiString("Passw0rd!x"),
            ["currentPassword"] = new OpenApiString("Passw0rd!x"),
            ["newPassword"] = new OpenApiString("Passw0rd!y"),
            ["confirmPassword"] = new OpenApiString("Passw0rd!y"),
            ["userName"] = new OpenApiString("username"),
            ["displayName"] = new OpenApiString("Display Name"),
            ["birthDay"] = new OpenApiDateTime(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            ["code"] = new OpenApiString("1234"),
            ["token"] = new OpenApiString("token-from-the-email"),
            ["resetPageUrl"] = new OpenApiString("https://localhost:7208/Account/ResetPassword"),
            ["avatarUrl"] = new OpenApiString(""),
            ["imageUrl"] = new OpenApiString(""),
            ["customStatus"] = new OpenApiString("Hello!"),
            ["pronouns"] = new OpenApiString("they/them"),
            ["bio"] = new OpenApiString("About me"),
            ["isPublic"] = new OpenApiBoolean(true),
            ["text"] = new OpenApiString("Hello"),
            ["emoji"] = new OpenApiString("\u2764\uFE0F"),
            ["pinned"] = new OpenApiBoolean(true),
            ["userId"] = new OpenApiNull(),
            ["replyToMessageId"] = new OpenApiNull()
        };

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties is null || !context.Type.Name.EndsWith("Dto", StringComparison.Ordinal))
            {
                return;
            }

            foreach (var property in schema.Properties)
            {
                if (context.Type.Name == "CreateStoryDto" && property.Key == "imageUrl")
                {
                    property.Value.Example = new OpenApiString("/uploads/stories/0123456789abcdef0123456789abcdef.jpg");
                }
                else if (Examples.TryGetValue(property.Key, out var example))
                {
                    property.Value.Example = example;
                }
            }
        }
    }
}
