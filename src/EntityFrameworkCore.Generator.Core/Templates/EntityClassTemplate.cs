using EntityFrameworkCore.Generator.Extensions;
using EntityFrameworkCore.Generator.Metadata.Generation;
using EntityFrameworkCore.Generator.Options;

using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace EntityFrameworkCore.Generator.Templates;

public class EntityClassTemplate : CodeTemplateBase
{
    private readonly Entity _entity;

    public EntityClassTemplate(Entity entity, GeneratorOptions options) : base(options)
    {
        _entity = entity;
    }

    public override string WriteCode()
    {
        CodeBuilder.Clear();

        CodeBuilder.Append($"namespace {_entity.EntityNamespace}");

        if (Options.Project.FileScopedNamespace)
        {
            CodeBuilder.AppendLine(";");
            CodeBuilder.AppendLine();
            GenerateClass();
        }
        else
        {
            CodeBuilder.AppendLine();
            CodeBuilder.AppendLine("{");

            using (CodeBuilder.Indent())
            {
                GenerateClass();
            }

            CodeBuilder.AppendLine("}");
        }

        return CodeBuilder.ToString();
    }

    private void GenerateClass()
    {
        var entityClass = _entity.EntityClass.ToSafeName();

        if (Options.Data.Entity.Document)
        {
            CodeBuilder.AppendLine("/// <summary>");
            CodeBuilder.AppendLine($"/// Entity class representing data for table '{_entity.TableName}'.");
            CodeBuilder.AppendLine("/// </summary>");
        }

        CodeBuilder.AppendLine($"public partial class {entityClass}");

        if (_entity.EntityBaseClass.HasValue())
        {
            var entityBaseClass = _entity.EntityBaseClass.ToSafeName();
            using (CodeBuilder.Indent())
                CodeBuilder.AppendLine($": {entityBaseClass}, {typeof(IIdentifiable<int>).ToType()}");
        }

        CodeBuilder.AppendLine("{");

        using (CodeBuilder.Indent())
        {
            GenerateConstructor();

            GenerateProperties();
            GenerateRelationshipProperties();
            GenerateIdentifiableProperties();
        }

        CodeBuilder.AppendLine("}");

    }

    private void GenerateConstructor()
    {
        var relationships = _entity.Relationships
            .Where(r => r.Cardinality == Cardinality.Many)
            .OrderBy(r => r.PropertyName)
            .ToList();

        var entityClass = _entity.EntityClass.ToSafeName();

        if (Options.Data.Entity.Document)
        {
            CodeBuilder.AppendLine("/// <summary>");
            CodeBuilder.AppendLine($"/// Initializes a new instance of the <see cref=\"{entityClass}\"/> class.");
            CodeBuilder.AppendLine("/// </summary>");
        }

        CodeBuilder.AppendLine($"public {entityClass}()");
        CodeBuilder.AppendLine("{");

        using (CodeBuilder.Indent())
        {
            CodeBuilder.AppendLine("#region Generated Constructor");
            foreach (var relationship in relationships)
            {
                var propertyName = relationship.PropertyName.ToSafeName();

                var primaryNamespace = relationship.PrimaryEntity.EntityNamespace;
                var primaryName = relationship.PrimaryEntity.EntityClass.ToSafeName();
                var primaryFullName = $"{primaryNamespace}.{primaryName}";

                CodeBuilder.AppendLine($"{propertyName} = new {("System.Collections.Generic.HashSet<"+primaryFullName+">").ToType()}();");
            }
            CodeBuilder.AppendLine("#endregion");
        }

        CodeBuilder.AppendLine("}");
        CodeBuilder.AppendLine();
    }

    private void GenerateProperties()
    {
        CodeBuilder.AppendLine("#region Generated Properties");
        foreach (var property in _entity.Properties)
        {
            var propertyType = property.SystemType.ToType();
            var propertyName = property.PropertyName.ToSafeName();

            if (Options.Data.Entity.Document)
            {
                CodeBuilder.AppendLine("/// <summary>");
                CodeBuilder.AppendLine($"/// Gets or sets the property value representing column '{property.ColumnName}'.");
                CodeBuilder.AppendLine("/// </summary>");
                CodeBuilder.AppendLine("/// <value>");
                CodeBuilder.AppendLine($"/// The property value representing column '{property.ColumnName}'.");
                CodeBuilder.AppendLine("/// </value>");
            }

            if (property.IsNullable == true && (property.SystemType.IsValueType || Options.Project.Nullable))
                CodeBuilder.AppendLine($"[{typeof(AttrAttribute).ToType()}] public {propertyType}? {propertyName} {{ get; set; }}");
            else if (Options.Project.Nullable && !property.SystemType.IsValueType)
                CodeBuilder.AppendLine($"[{typeof(AttrAttribute).ToType()}] public {propertyType} {propertyName} {{ get; set; }} = null!;");
            else
                CodeBuilder.AppendLine($"[{typeof(AttrAttribute).ToType()}] public {propertyType} {propertyName} {{ get; set; }}");
        }
        CodeBuilder.AppendLine("#endregion");
        CodeBuilder.AppendLine();
    }

    private void GenerateRelationshipProperties()
    {
        CodeBuilder.AppendLine("#region Generated Relationships");
        foreach (var relationship in _entity.Relationships.OrderBy(r => r.PropertyName))
        {
            var propertyName = relationship.PropertyName.ToSafeName();
            var primaryNamespace = relationship.PrimaryEntity.EntityNamespace;
            var primaryName = relationship.PrimaryEntity.EntityClass.ToSafeName();
            var primaryFullName = $"{primaryNamespace}.{primaryName}";

            if (relationship.Cardinality == Cardinality.Many)
            {
                if (Options.Data.Entity.Document)
                {
                    CodeBuilder.AppendLine("/// <summary>");
                    CodeBuilder.AppendLine($"/// Gets or sets the navigation collection for entity <see cref=\"{primaryFullName}\" />.");
                    CodeBuilder.AppendLine("/// </summary>");
                    CodeBuilder.AppendLine("/// <value>");
                    CodeBuilder.AppendLine($"/// The navigation collection for entity <see cref=\"{primaryFullName}\" />.");
                    CodeBuilder.AppendLine("/// </value>");
                }


                CodeBuilder.AppendLine($"[{typeof(HasManyAttribute).ToType()}] public virtual {("System.Collections.Generic.ICollection<"+primaryFullName+">").ToType()} {propertyName} {{ get; set; }}");
            }
            else
            {
                if (Options.Data.Entity.Document)
                {
                    CodeBuilder.AppendLine("/// <summary>");
                    CodeBuilder.AppendLine($"/// Gets or sets the navigation property for entity <see cref=\"{primaryFullName}\" />.");
                    CodeBuilder.AppendLine("/// </summary>");
                    CodeBuilder.AppendLine("/// <value>");
                    CodeBuilder.AppendLine($"/// The navigation property for entity <see cref=\"{primaryFullName}\" />.");
                    CodeBuilder.AppendLine("/// </value>");

                    foreach (var property in relationship.Properties)
                        CodeBuilder.AppendLine($"/// <seealso cref=\"{property.PropertyName}\" />");
                }

                if (!Options.Project.Nullable)
                    CodeBuilder.AppendLine($"[{typeof(HasOneAttribute).ToType()}] public virtual {primaryFullName.ToType()} {propertyName} {{ get; set; }}");
                else if (relationship.Cardinality == Cardinality.One)
                    CodeBuilder.AppendLine($"[{typeof(HasOneAttribute).ToType()}] public virtual {primaryFullName.ToType()} {propertyName} {{ get; set; }} = null!;");
                else
                    CodeBuilder.AppendLine($"[{typeof(HasOneAttribute).ToType()}] public virtual {primaryFullName.ToType()}? {propertyName} {{ get; set; }}");
            }
        }
        CodeBuilder.AppendLine("#endregion");
        CodeBuilder.AppendLine();
    }

    private void GenerateIdentifiableProperties()
    {
        CodeBuilder.AppendLine("#region Generated IIdentifiable Properties");

        CodeBuilder.AppendLine($"{typeof(string).ToType()} {typeof(IIdentifiable).ToType()}.StringId");
        CodeBuilder.AppendLine("{");
        using (CodeBuilder.Indent())
        {
            CodeBuilder.AppendLine("get => Id.ToString();");
            CodeBuilder.AppendLine("set { }");
        }
        CodeBuilder.AppendLine("}");

        CodeBuilder.AppendLine($"{typeof(string).ToType()} {typeof(IIdentifiable).ToType()}.LocalId");
        CodeBuilder.AppendLine("{");
        using (CodeBuilder.Indent())
        {
            CodeBuilder.AppendLine("get => null;");
            CodeBuilder.AppendLine("set { }");
        }
        CodeBuilder.AppendLine("}");

        CodeBuilder.AppendLine($"{typeof(int).ToType()} {typeof(IIdentifiable<int>).ToType()}.Id");
        CodeBuilder.AppendLine("{");
        using (CodeBuilder.Indent())
        {
            CodeBuilder.AppendLine("get => Id;");
            CodeBuilder.AppendLine("set { }");
        }
        CodeBuilder.AppendLine("}");
        CodeBuilder.AppendLine("#endregion");
        CodeBuilder.AppendLine();
    }
}
