using BoSai.CustomerLeaderboard.Shared.DTOs;

namespace BoSai.CustomerLeaderboard.Domain.Interfaces
{
    public interface ILeaderboardService
    {
        decimal UpdateScore(long customerId, decimal scoreChange);

        List<CustomerDTO> GetCustomersByRank(int start, int end);

        List<CustomerDTO> GetCustomerAndNeighbors(long customerId, int high, int low);
    }
}
