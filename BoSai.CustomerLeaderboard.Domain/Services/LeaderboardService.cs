using BoSai.CustomerLeaderboard.Domain.Interfaces;
using BoSai.CustomerLeaderboard.Domain.Models;
using BoSai.CustomerLeaderboard.Shared.DTOs;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BoSai.CustomerLeaderboard.Domain.Services
{
    /// <summary>
    /// 排行榜服务类，负责管理客户分数和排名
    /// </summary>
    public class LeaderboardService : ILeaderboardService
    {
        // 使用ConcurrentDictionary来存储客户信息，确保线程安全
        private readonly ConcurrentDictionary<long, Customer> _customers = new();

        // 定义分片大小，假设每个分片包含100分数区间
        private const int ShardSize = 100;

        // 使用多个分片存储sortedCustomers，每个分片用SortedDictionary存储
        private readonly SortedDictionary<int, SortedDictionary<(decimal, long), Customer>> _shardedCustomers
            = new();

        // Helper method: 根据分数确定分片
        private int GetShardIndex(decimal score)
        {
            return (int)(-score / ShardSize);
        }

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
            lock (_lock)
            {
                // Validate score change within the range
                if (scoreChange < -1000 || scoreChange > 1000)
                {
                    throw new ArgumentOutOfRangeException(nameof(scoreChange), "Score change must be within the range [-1000, 1000].");
                }

                // 查找或者创建客户
                var customer = _customers.GetOrAdd(customerId, id =>
                {
                    var tempCustomer = new Customer();
                    tempCustomer.SetCustomerId(customerId);
                    return tempCustomer;
                });

                decimal previousScore = customer.Score;

                customer.ScoreChange(scoreChange);

                int oldShardIndex = GetShardIndex(previousScore);

                int newShardIndex = GetShardIndex(customer.Score);

                // 如果客户分数发生了变化，需要更新分片
                if (oldShardIndex != newShardIndex)
                {
                    // 从旧分片中移除客户
                    if (_shardedCustomers.ContainsKey(oldShardIndex))
                    {
                        var oldShard = _shardedCustomers[oldShardIndex];
                        oldShard.Remove((-previousScore, customerId));
                    }
                    if (customer.Score <= 0)
                    {
                        // 分数不大于0时不参与排名
                        return customer.Score;
                    }
                    SortedDictionary<(decimal, long), Customer>? newShard = null;
                    // 查找或者创建新的片区
                    if (_shardedCustomers.ContainsKey(newShardIndex))
                    {
                        newShard = _shardedCustomers[newShardIndex];
                    }
                    else
                    {
                        newShard = new SortedDictionary<(decimal, long), Customer>();
                        _shardedCustomers.Add(newShardIndex, newShard);
                    }

                    newShard.Add((-customer.Score, customer.CustomerId), customer);
                }
                else
                {

                    // 如果客户仍在同一个分片内，更新当前分片内的记录
                    if (_shardedCustomers.ContainsKey(newShardIndex))
                    {
                        var shard = _shardedCustomers[newShardIndex];
                        shard.Remove((-previousScore, customerId));
                        if (customer.Score <= 0)
                        {
                            // 分数不大于0时不参与排名
                            return customer.Score;
                        }
                        shard.Add((-customer.Score, customerId), customer);
                    }
                    else
                    {
                        if (customer.Score <= 0)
                        {
                            // 分数不大于0时不参与排名
                            return customer.Score;
                        }
                        var newShard = new SortedDictionary<(decimal, long), Customer>();
                        _shardedCustomers.Add(newShardIndex, newShard);
                        newShard.Add((-customer.Score, customer.CustomerId), customer);
                    }
                }
                return customer.Score;
            }
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
            List<CustomerDTO> result = new();
            long currentRank = 0;
            int totalShardCount = 0;
            // 按照分片的顺序遍历所有分片，并确保是按分数降序排列
            foreach (var shardKeyValuePair in _shardedCustomers)
            {
                totalShardCount += shardKeyValuePair.Value.Count();
                if (totalShardCount >= start && totalShardCount <= end)
                {
                    // 前面n个片区的客户总数大于等于起始排名，从该片区开始筛选数据
                    foreach (var customerKeyValuePair in shardKeyValuePair.Value)
                    {
                        currentRank += 1;
                        if (currentRank >= start && currentRank <= end)
                        {
                            result.Add(new CustomerDTO()
                            {
                                CustomerId = customerKeyValuePair.Value.CustomerId,
                                Score = customerKeyValuePair.Value.Score,
                                Rank = currentRank
                            });
                        }
                    }
                }
                else
                {
                    // 不在排名范围内的片区，不进行数据遍历，只取总数。
                    currentRank += shardKeyValuePair.Value.Count();
                }
                // 已获取全部排名，停止遍历数据
                if (currentRank >= end)
                {
                    break;
                }
            }
            return result;
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
            List<CustomerDTO> result = new();
            if (_customers.TryGetValue(customerId, out var targetCustomer))
            {
                if (targetCustomer.Score < 0)
                {
                    // 分数小于0是不参与排名
                    return result;
                }

                // 获取目标客户所在分片的索引
                int targetShardIndex = GetShardIndex(targetCustomer.Score);

                // 获取目标客户以前片区的客户总量
                long preShardCustomerCount = 0;
                foreach (var preShard in _shardedCustomers.Where(c => c.Key < targetShardIndex))
                {
                    preShardCustomerCount += preShard.Value.Count;
                }

                var targetShard = _shardedCustomers[targetShardIndex];

                // 获取目标客户在当前分数片区的排名
                long currentShardRank = targetShard.Count(c => c.Key.Item1 * -1 > targetCustomer.Score ||// 分数小于当前客户
                                                             c.Key.Item1 * -1 == targetCustomer.Score && c.Key.Item2 < targetCustomer.CustomerId) + 1;// 分数等于当前客户但客户ID小于当前客户

                // 当前用户排名 = 当前用户所在分数片区之前所有片区的用户总数 + 用户在当前片区的排名
                long targetCustomerRank = preShardCustomerCount + currentShardRank;

                var sortedCustomer = new SortedDictionary<long, CustomerDTO>();

                // 添加当前用户
                sortedCustomer.Add(targetCustomerRank, new CustomerDTO()
                {
                    CustomerId = targetCustomer.CustomerId,
                    Score = targetCustomer.Score,
                    Rank = targetCustomerRank
                });

                // 添加排名在当前用户前high位邻居
                int addedHigherCount = 0;
                // 取当前片区排名在前的邻居

                var reversedHigherRankShard = targetShard.Where(c => c.Key.Item1 * -1 > targetCustomer.Score ||
                                                          (c.Key.Item1 * -1 == targetCustomer.Score && c.Key.Item2 < targetCustomer.CustomerId)).Reverse();
                foreach (var customerKeyValuePair in reversedHigherRankShard)
                {
                    if (addedHigherCount >= high)
                    {
                        break;
                    }

                    addedHigherCount += 1;
                    var neighborRank = targetCustomerRank - addedHigherCount;
                    var neighbor = customerKeyValuePair.Value;

                    sortedCustomer.Add(neighborRank, new CustomerDTO()
                    {
                        CustomerId = neighbor.CustomerId,
                        Score = neighbor.Score,
                        Rank = neighborRank
                    });
                }

                // 当前用户片区未找到足够的排名在客户前面的邻居，从前面的片区继续查找。
                if (addedHigherCount < high)
                {
                    var reversedSorted = _shardedCustomers.Where(c => c.Key < targetShardIndex).Reverse();
                    foreach (var shardKeyValuePair in reversedSorted)
                    {
                        var shard = shardKeyValuePair.Value;
                        addedHigherCount = AddHigherRankNeighbor(high, targetCustomerRank, sortedCustomer, addedHigherCount, shard);
                        if (addedHigherCount >= high)
                        {
                            break;
                        }
                    }
                }

                //添加排名在当前用户后low位邻居
                int addedLowerCount = 0;
                var lowerRankShard = targetShard.Where(c => c.Key.Item1 * -1 < targetCustomer.Score ||
                                          (c.Key.Item1 * -1 == targetCustomer.Score && c.Key.Item2 > targetCustomer.CustomerId));
                foreach (var customerKeyValuePair in lowerRankShard)
                {
                    if (addedLowerCount >= high)
                    {
                        break;
                    }
                    var neighbor = customerKeyValuePair.Value;
                    if (neighbor.Score < 0)
                    {
                        break;
                    }
                    addedLowerCount += 1;
                    var neighborRank = targetCustomerRank + addedLowerCount;


                    sortedCustomer.Add(neighborRank, new CustomerDTO()
                    {
                        CustomerId = neighbor.CustomerId,
                        Score = neighbor.Score,
                        Rank = neighborRank
                    });
                }
                // 当前用户片区未找到足够的排名在客户后面的邻居，从后面的片区继续查找。
                if (addedLowerCount < low)
                {
                    foreach (var shardKeyValuePair in _shardedCustomers.Where(c => c.Key > targetShardIndex))
                    {
                        var shard = shardKeyValuePair.Value;
                        addedLowerCount = AddLowerRankNeighbor(low, targetCustomerRank, sortedCustomer, addedLowerCount, shard);
                        if (addedHigherCount >= low)
                        {
                            break;
                        }
                    }
                }
                result = (from ctm in sortedCustomer select ctm.Value).ToList();
            }

            return result; // 未找到客户
        }

        private int AddHigherRankNeighbor(int high, long targetCustomerRank, SortedDictionary<long, CustomerDTO> sortedCustomer, int addedHigherCount, SortedDictionary<(decimal, long), Customer> shard)
        {
            foreach (var customerKeyValuePair in shard.Reverse())
            {
                if (addedHigherCount >= high)
                {
                    break;
                }
                var neighbor = customerKeyValuePair.Value;
                addedHigherCount += 1;
                var neighborRank = targetCustomerRank - addedHigherCount;
                sortedCustomer.Add(neighborRank, new CustomerDTO()
                {
                    CustomerId = neighbor.CustomerId,
                    Score = neighbor.Score,
                    Rank = neighborRank
                });
            }

            return addedHigherCount;
        }

        private int AddLowerRankNeighbor(int low, long currentShardRank, SortedDictionary<long, CustomerDTO> sortedCustomer, int addedLowerCount, SortedDictionary<(decimal, long), Customer> shard)
        {
            if (addedLowerCount >= low)
            {
                return addedLowerCount;
            }
            foreach (var customerKeyValuePair in shard)
            {
                if (addedLowerCount >= low)
                {
                    break;
                }
                var neighbor = customerKeyValuePair.Value;
                if (neighbor.Score < 0)
                {
                    //分数小于0不参与排名
                    break;
                }
                addedLowerCount += 1;
                var neighborRank = currentShardRank + addedLowerCount;
                sortedCustomer.Add(neighborRank, new CustomerDTO()
                {
                    CustomerId = neighbor.CustomerId,
                    Score = neighbor.Score,
                    Rank = neighborRank
                });
            }
            return addedLowerCount;
        }
    }
}
