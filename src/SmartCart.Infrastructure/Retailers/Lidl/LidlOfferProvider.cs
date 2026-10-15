using SmartCart.Application.Abstractions;
using SmartCart.Domain.Entities;

namespace SmartCart.Infrastructure.Retailers.Lidl;

public class LidlOfferProvider : IRetailerOfferProvider
{
    public string Retailer => "Lidl";

    public Task<IReadOnlyCollection<Offer>> GetOffersAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
