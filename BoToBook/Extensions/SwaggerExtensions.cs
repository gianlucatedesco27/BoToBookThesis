using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace BoToBook.Extensions
{
    public class SwaggerIgnoreSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null)
            {
                return;
            }

            var ignoreDataMemberProperties = context.Type.GetProperties()
                .Where(t => t.GetCustomAttribute<SwaggerIgnore>() != null);

            foreach (var ignoreDataMemberProperty in ignoreDataMemberProperties)
            {
                var propertyToHide = schema.Properties.Keys
                    .SingleOrDefault(x => x.ToLower() == ignoreDataMemberProperty.Name.ToLower());

                if (propertyToHide != null)
                {
                    schema.Properties.Remove(propertyToHide);
                }
            }
        }
    }

    public class SwaggerIgnoreOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null || context == null || context.ApiDescription?.ParameterDescriptions == null)
                return;

            var parametersToHide = context.ApiDescription.ParameterDescriptions
                .Where(parameterDescription => ParameterHasIgnoreAttribute(parameterDescription))
                .ToList();

            if (parametersToHide.Count == 0)
                return;

            foreach (var parameterToHide in parametersToHide)
            {
                var parameter = operation.Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, parameterToHide.Name, System.StringComparison.Ordinal));
                if (parameter != null)
                    operation.Parameters.Remove(parameter);
            }
        }

        private static bool ParameterHasIgnoreAttribute(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription parameterDescription)
        {
            if (parameterDescription.ModelMetadata is Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelMetadata metadata)
            {
                if (metadata?.Attributes?.ParameterAttributes != null)
                    return metadata.Attributes.ParameterAttributes.Any(attribute => attribute.GetType() == typeof(SwaggerIgnore));

                if (metadata?.Attributes?.Attributes != null)
                    return metadata.Attributes.Attributes.Any(attribute => attribute.GetType() == typeof(SwaggerIgnore));

            }

            return false;
        }
    }
}
