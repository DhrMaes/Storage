namespace System.Runtime.CompilerServices
{
      [AttributeUsage(AttributeTargets.All, Inherited = false)]
      internal sealed class RequiredMemberAttribute : Attribute
      {
      }

      [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
      internal sealed class CompilerRequiredAttribute : Attribute
      {
            public CompilerRequiredAttribute()
            {
            }
      }

      [AttributeUsage(AttributeTargets.All, Inherited = false)]
      internal sealed class CompilerFeatureRequiredAttribute : Attribute
      {
            public CompilerFeatureRequiredAttribute(string featureName)
            {
                  FeatureName = featureName;
            }

            public string FeatureName { get; }

            public bool IsOptional { get; init; }
      }

      internal static class IsExternalInit {}
}

namespace System.Diagnostics.CodeAnalysis
{
      [AttributeUsage(AttributeTargets.Constructor, Inherited = false)]
      internal sealed class SetsRequiredMembersAttribute : Attribute
      {
            public SetsRequiredMembersAttribute()
            {
            }
      }
}