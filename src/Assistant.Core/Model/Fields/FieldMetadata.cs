using System;
/// <summary>
/// Holds metadata for a field:
/// - binding (parser result)
/// - descriptor (definition)
/// Keeps runtime state (FieldState) clean and focused.
/// </summary>
namespace Assistant.Core.Model.Fields
{
    public sealed class FieldMetadata
    {
        public FieldBinding? Binding { get; private set; }
        public FieldDescriptor? Descriptor { get; private set; }

        public FieldMetadata(FieldAlias alias, FieldBinding? binding, FieldDescriptor? descriptor)
        {
            SetBinding(alias, binding);
            SetDescriptor(alias, descriptor);
        }

        public void SetBinding(FieldAlias alias, FieldBinding? binding)
        {
            if (binding is not null && !binding.Alias.Equals(alias))
            {
                throw new InvalidOperationException("Binding alias does not match field alias.");
            }

            Binding = binding;
        }

        public void SetDescriptor(FieldAlias alias, FieldDescriptor? descriptor)
        {
            if (descriptor is not null && !descriptor.Alias.Equals(alias))
            {
                throw new InvalidOperationException("Descriptor alias does not match field alias.");
            }

            Descriptor = descriptor;
        }
    }
}