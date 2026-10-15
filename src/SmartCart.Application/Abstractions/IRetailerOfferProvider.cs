using SmartCart.Domain.Entities;

namespace SmartCart.Application.Abstractions;

public interface IRetailerOfferProvider
{
    string Retailer { get; }

    Task<IReadOnlyCollection<Offer>> GetOffersAsync(CancellationToken cancellationToken = default);
}
