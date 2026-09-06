namespace ProviderHub.Api.Contracts;

/// <summary>
/// The request bodies accepted by the API.
/// <para>
/// They are separate types from the commands of the application layer, even where the fields
/// look identical, because they answer to different pressures. A command carries the identifier
/// of the thing being edited; the body must not, since that identifier belongs to the URL and
/// accepting it twice invites the two to disagree. And an HTTP contract has to stay stable for
/// clients long after an internal command has been refactored.
/// </para>
/// </summary>
/// <param name="Nit">Colombian tax identifier, for example <c>890903938-8</c>.</param>
/// <param name="Name">Trade name of the company.</param>
/// <param name="Website">Absolute http or https address.</param>
/// <param name="Email">Contact e-mail address.</param>
public sealed record SaveProviderRequest(string Nit, string Name, string Website, string Email);

/// <param name="ServiceId">Identifier of the catalogue service being enabled.</param>
/// <param name="Countries">ISO 3166-1 alpha-2 codes, for example <c>["CO", "MX"]</c>. At least one.</param>
public sealed record OfferServiceRequest(int ServiceId, IReadOnlyList<string> Countries);

/// <param name="Countries">The new set of countries. At least one.</param>
public sealed record ChangeCountriesRequest(IReadOnlyList<string> Countries);

/// <param name="Name">Name of the service, unique across the catalogue.</param>
/// <param name="HourlyRate">Price of one hour, in US dollars.</param>
public sealed record SaveServiceRequest(string Name, decimal HourlyRate);
