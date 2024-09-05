using BoSai.CustomerLeaderboard.Domain.Interfaces;
using BoSai.CustomerLeaderboard.Domain.Models;
using BoSai.CustomerLeaderboard.Shared.DTOs;
using System.Collections.Concurrent;

namespace BoSai.CustomerLeaderboard.Domain.Services
{
    /// <summary>
    /// 排行榜服务类，负责管理客户分数和排名
    /// </summary>
    public class LeaderboardService : ILeaderboardService
    {
        /// <summary>
        /// 使用并发字典来存储客户数据（CustomerId -> Customer）
        /// </summary>
        private readonly ConcurrentDictionary<long, Customer> _customers = new();

        // 使用SortedList按分数和CustomerID排序，分数降序排列（双键排序：-Score -> CustomerId）
        private readonly SortedList<(decimal negativeScore, long customerId), Customer> _sortedCustomers = new();

        /// <summary>
        /// 锁用于保证多线程环境下的操作安全
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// 更新客户分数，如果客户不存在，则添加到排行榜
        /// </summary>
        /// <param name="customerId">客户id</param>
        /// <param name="scoreChange">分数变化值</param>
        /// <returns>更新之后的分数</returns>
        public decimal UpdateScore(long customerId, decimal scoreChange)
        {
            if (customerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scoreChange));
            }
            if (scoreChange > 1000 || scoreChange < -1000)
            {
                throw new ArgumentOutOfRangeException(nameof(scoreChange));
            }
            Customer customer;
            lock (_lock)
            {
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
                if (_customers.TryGetValue(customerId, out customer))
                {
                    // 从有序列表中删除旧的客户记录，使用负数分数来保持降序排列
                    if (_sortedCustomers.ContainsKey((-customer.Score, customer.CustomerId)))
                    {
                        _sortedCustomers.Remove((-customer.Score, customer.CustomerId));
                    }
                    customer.ScoreChange(scoreChange); // 更新现有客户的分数
                }
                else
                {
                    // 添加新客户
                    customer = new Customer();
                    customer.SetCustomerId(customerId);
                    customer.ScoreChange(scoreChange);
                    _customers[customerId] = customer; // 添加新客户到排行榜
                }
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。

                // 将客户重新插入到有序列表中，使用负数分数来保持降序排列,只有分数大于0才参与排名
                if (customer.Score > 0)
                {
                    _sortedCustomers.Add((-customer.Score, customer.CustomerId), customer);
                }
            }
            return customer.Score;
        }

        /// <summary>
        /// 按排名范围获取客户
        /// </summary>
        /// <param name="start">起始排名</param>
        /// <param name="end">截止排名</param>
        /// <returns>范围内客户列表</returns>
        /// <exception cref="ArgumentException"></exception>
        public List<CustomerDTO> GetCustomersByRank(int start, int end)
        {
            if (start < 0 || end < 0 || start > end)
            {
                throw new ArgumentException();
            }
            // 由于_sortedCustomers已经有序，可以直接通过索引获取指定范围的客户
            return _sortedCustomers.Values
                .Skip(start - 1)
                .Take(end - start + 1)
                .Select((customer, index) =>
                {
                    // 添加排名信息
                    var rank = start + index;
                    return new CustomerDTO
                    {
                        CustomerId = customer.CustomerId,
                        Score = customer.Score,
                        Rank = rank
                    };
                })
                .ToList();
        }

        /// <summary>
        /// 按客户ID获取客户及其邻居
        /// </summary>
        /// <param name="customerId">客户id</param>
        /// <param name="high">客户排名之前位数</param>
        /// <param name="low">客户排名之后位数</param>
        /// <returns>指定户客户及其邻居</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public List<CustomerDTO> GetCustomerAndNeighbors(long customerId, int high, int low)
        {
            if (customerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(customerId));
            }
            if (!_customers.ContainsKey(customerId))
            {
                return new List<CustomerDTO>();
            }

            // 查找客户的索引
            var index = _sortedCustomers.IndexOfKey((-_customers[customerId].Score, customerId));
            if (index == -1) return new List<CustomerDTO>();

            // 获取指定客户及其上方和下方的邻居
            int start = index - high >= 0 ? index - high : 0;
            int count = high + low + 1;
            return _sortedCustomers.Values
                .Skip(start)
                .Take(count)
                .Select((customer, idx) =>
                {
                    // 添加排名信息
                    var rank = start + idx + 1; // 索引从0开始，排名从1开始
                    return new CustomerDTO
                    {
                        CustomerId = customer.CustomerId,
                        Score = customer.Score,
                        Rank = rank
                    };
                })
                .ToList();
        }
    }
}
