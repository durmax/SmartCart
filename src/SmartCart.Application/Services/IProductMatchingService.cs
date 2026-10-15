using SmartCart.Domain.Entities;

namespace SmartCart.Application.Services;

public interface IProductMatchingService
{
    Task<IReadOnlyCollection<Offer>> MatchOffersAsync(Guid productId, CancellationToken cancellationToken = default);
}
