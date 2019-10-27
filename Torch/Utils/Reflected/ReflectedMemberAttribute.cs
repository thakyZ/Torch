using System;

namespace Torch.Utils
{
    public abstract class ReflectedMemberAttribute : Attribute
    {
        /// <summary>
        /// Name of the member to access.  If null, the tagged field's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Declaring type of the member to access.  If null, inferred from the instance argument type.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Assembly qualified name of <see cref="Type"/>
        /// </summary>
        public string TypeName
        {
            get => Type?.AssemblyQualifiedName;
            set => Type = value == null ? null : Type.GetType(value, true);
        }
    }
}