namespace DhrMaes.Storage.Core.Providers
{
	using System;

	/// <summary>
	/// Specifies a unique identifier for a provider implementation.
	/// </summary>
	/// <remarks>
	/// Apply this attribute to provider classes to associate them with a unique identifier.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ProviderIdentifierAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProviderIdentifierAttribute"/> class with the specified identifier.
		/// </summary>
		/// <param name="id">The unique identifier for the provider.</param>
		public ProviderIdentifierAttribute(string id)
		{
			Id = id;
		}

		/// <summary>
		/// Gets the unique identifier associated with the provider.
		/// </summary>
		public string Id { get; }
	}
}
