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
        /// �ֱ���Ա߽�ֵ1000��-1000�Լ��÷�0������£�Ԥ�ڶ��ܳɹ�
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
        /// �����û��ķ�֧��Ҫ�ۼƣ�����ֱ�Ӹ�ֵΪ��ǰ��ֵ
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
        /// �÷�Ϊ��ֵ����������
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
            Assert.Equal(4, customers.Count);// Ԥ�ڲ���������ֻ��4λ
            foreach (var item in customers)
            {
                Assert.NotEqual(4, item.CustomerId);// �ͻ�4�÷�С��0��Ԥ�ڲ���������
                Assert.NotEqual(6, item.CustomerId);// �ͻ�6�ۼƵ÷ֺ�С��0��Ԥ�ڲ���������
            }
        }

        /// <summary>
        /// �ֱ���Կͻ�idΪ�������÷ִ���1000���÷�С��-1000�������Ԥ���׳�ArgumentOutOfRangeException
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
        /// ���������Ƿ����Ԥ�ڣ���ͬ��ֵ������¿ͻ�idС��Ӧ������ǰ��
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
        /// ���Ի�ȡһ�������ڵ��������䣬Ԥ�ڵõ�������
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
        /// �ֱ������ʼ����Ϊ��������ֹ����Ϊ��������ʼ����С�ڽ�ֹ�����������Ԥ���׳�ArgumentException
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
        /// ������������»�ȡָ���û�����ǰ���ھ�
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
        /// ����һ�������ڵĿͻ�id,Ԥ�ڷ��ؿ�����
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
        /// �ֱ���Կͻ�idΪ0����δ�����������Ԥ���׳�ArgumentOutOfRangeException�쳣
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