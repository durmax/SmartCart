using SmartCart.Domain.Entities;

namespace SmartCart.Application.Services;

public interface IOfferService
{
    Task<IReadOnlyCollection<Offer>> GetCurrentOffersAsync(CancellationToken cancellationToken = default);
}
