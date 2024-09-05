using System;
using System.Collections.Generic;
using System.Linq;
using BoSai.CustomerLeaderboard.Domain.Services;
using Xunit;
namespace BoSai.CustomerLeaderboard.Domain.Tests
{
    public class LeaderboardServiceTests
    {
        private readonly LeaderboardService _leaderboardService;

        public LeaderboardServiceTests()
        {
            _leaderboardService = new LeaderboardService();
        }

        /// <summary>
        /// 分别测试边界值1000、-1000以及得分0的情况下，预期都能成功
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="scoreChange"></param>
        [Theory]
        [InlineData(1, 1000)]
        [InlineData(2, -1000)]
        [InlineData(3, 0)]
        public void UpdateScore_ValidScoreChange_ShouldUpdateScore(long customerId, decimal scoreChange)
        {
            // Act
            var updatedScore = _leaderboardService.UpdateScore(customerId, scoreChange);

            // Assert
            Assert.Equal(scoreChange, updatedScore);
        }

        /// <summary>
        /// 测试用户的分支需要累计，不能直接赋值为当前分值
        /// </summary>
        [Fact]
        public void UpdateScore_ValidScoreChange_ShouldAccumulateScore()
        {
            var scores = new decimal[] { 400, 100, -200, 700, -10, 0 };
            decimal currentScore = 0;
            int customerId = 1000;

            foreach (var score in scores)
            {
                currentScore += score;

                // Act 
                var updatedScore = _leaderboardService.UpdateScore(customerId, score);

                // Assert
                Assert.Equal(currentScore, updatedScore);
            }
        }

        /// <summary>
        /// 得分为负值不参与排名
        /// </summary>
        [Fact]
        public void GetCustomersByRank_NegativeScore_ShouldContainInLeaderboard()
        {
            _leaderboardService.UpdateScore(1, 200);
            _leaderboardService.UpdateScore(2, 450);
            _leaderboardService.UpdateScore(3, 450);
            _leaderboardService.UpdateScore(4, -100);
            _leaderboardService.UpdateScore(5, 100);
            _leaderboardService.UpdateScore(6, 100);
            _leaderboardService.UpdateScore(6, -200);

            var customers = _leaderboardService.GetCustomersByRank(1, 7);
            Assert.Equal(4, customers.Count);// 预期参与排名的只有4位
            foreach (var item in customers)
            {
                Assert.NotEqual(4, item.CustomerId);// 客户4得分小于0，预期不存于排名
                Assert.NotEqual(6, item.CustomerId);// 客户6累计得分后小于0，预期不参与排名
            }
        }

        /// <summary>
        /// 分别测试客户id为负数、得分大于1000，得分小于-1000的情况，预期抛出ArgumentOutOfRangeException
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="scoreChange"></param>
        [Theory]
        [InlineData(-1, 50)]
        [InlineData(1, 1001)]
        [InlineData(2, -1001)]
        public void UpdateScore_InvalidScoreChange_ShouldThrowArgumentOutOfRangeException(long customerId, decimal scoreChange)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _leaderboardService.UpdateScore(customerId, scoreChange));
        }

        /// <summary>
        /// 测试排名是否符合预期，相同分值的情况下客户id小的应该排在前面
        /// </summary>
        [Fact]
        public void GetCustomersByRank_ValidRange_ShouldReturnCorrectCustomers()
        {
            // Arrange
            _leaderboardService.UpdateScore(1, 200);
            _leaderboardService.UpdateScore(322, 450);
            _leaderboardService.UpdateScore(2, 450);
            _leaderboardService.UpdateScore(3, 500);
            _leaderboardService.UpdateScore(4, 100);
            _leaderboardService.UpdateScore(5, 600);

            // Act
            var customers = _leaderboardService.GetCustomersByRank(1, 4);

            // Assert
            Assert.Equal(4, customers.Count);
            Assert.Equal(1, customers[0].Rank);
            Assert.Equal(5, customers[0].CustomerId);
            Assert.Equal(500, customers[1].Score);
            Assert.Equal(2, customers[2].CustomerId);
            Assert.Equal(450, customers[3].Score);
            Assert.Equal(322, customers[3].CustomerId);
        }

        /// <summary>
        /// 测试获取一个不存在的排名区间，预期得到空数据
        /// </summary>
        [Fact]
        public void GetCustomersByRank_InvalidRange_ShouldReturnEmptyList()
        {
            // Arrange
            _leaderboardService.UpdateScore(1004, 500);

            // Act
            var customers = _leaderboardService.GetCustomersByRank(10002, 10003);

            // Assert
            Assert.Empty(customers);
        }

        /// <summary>
        /// 分别测试起始排名为负数、截止排名为负数、起始排名小于截止排名的情况，预期抛出ArgumentException
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        [Theory]
        [InlineData(-1, 10)]
        [InlineData(10, -2)]
        [InlineData(10, 5)]
        public void GetCustomersByRank_InvaildParam_ShouldThrowArgumentException(int start, int end)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _leaderboardService.GetCustomersByRank(start, end));
        }

        /// <summary>
        /// 测试正常情况下获取指定用户及其前后邻居
        /// </summary>
        [Fact]
        public void GetCustomerAndNeighbors_ValidCustomer_ShouldReturnCorrectNeighbors()
        {
            // Arrange
            _leaderboardService.UpdateScore(1, 500);
            _leaderboardService.UpdateScore(2, 400);
            _leaderboardService.UpdateScore(3, 300);
            _leaderboardService.UpdateScore(4, 200);
            _leaderboardService.UpdateScore(5, 100);

            // Act
            var neighbors = _leaderboardService.GetCustomerAndNeighbors(3, 1, 1);

            // Assert
            Assert.Equal(3, neighbors.Count);
            Assert.Equal(2, neighbors[0].CustomerId); // Higher neighbor
            Assert.Equal(3, neighbors[1].CustomerId); // The target customer
            Assert.Equal(4, neighbors[2].CustomerId); // Lower neighbor
        }

        /// <summary>
        /// 测试一个不存在的客户id,预期返回空数据
        /// </summary>
        [Fact]
        public void GetCustomerAndNeighbors_InvalidCustomer_ShouldReturnEmptyList()
        {
            // Act
            var neighbors = _leaderboardService.GetCustomerAndNeighbors(999, 1, 1); // Non-existent customer

            // Assert
            Assert.Empty(neighbors);
        }

        /// <summary>
        /// 分别测试客户id为0或者未负数的情况，预期抛出ArgumentOutOfRangeException异常
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="high"></param>
        /// <param name="low"></param>
        [Theory]
        [InlineData(0, 50, 11)]
        [InlineData(-100, 50, 11)]
        public void GetCustomerAndNeighbors_ValidCustomer_CustomerIdShouldBePositive(long customerId, int high, int low)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _leaderboardService.GetCustomerAndNeighbors(customerId, high, low));
        }
    }
}