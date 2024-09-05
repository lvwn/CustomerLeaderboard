using BoSai.CustomerLeaderboard.API.Controllers;
using BoSai.CustomerLeaderboard.Domain.Interfaces;
using BoSai.CustomerLeaderboard.Domain.Models;
using BoSai.CustomerLeaderboard.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BoSai.CustomerLeaderboard.API.Test
{
    public class LeaderboardControllerTests
    {
        private readonly Mock<ILeaderboardService> _mockService;
        private readonly LeaderboardController _lbdController;
        private readonly CustomerController _ctController;

        /// <summary>
        /// 排行榜单元测试
        /// </summary>
        public LeaderboardControllerTests()
        {
            _mockService = new Mock<ILeaderboardService>();
            _lbdController = new LeaderboardController(_mockService.Object);
            _ctController = new CustomerController(_mockService.Object);
        }

        /// <summary>
        /// 测试得分超出边界值1000,-1000的情况，预期返回400
        /// </summary>
        /// <param name="scoreChange"></param>
        [Theory]
        [InlineData(1001)]
        [InlineData(-1001)]
        public void UpdateScore_InvalidScoreChange_ShouldReturnBadRequest(decimal scoreChange)
        {
            // Arrange
            _mockService.Setup(s => s.UpdateScore(It.IsAny<long>(), scoreChange)).Throws(new ArgumentOutOfRangeException());

            // Act
            var result = _ctController.UpdateScore(1, scoreChange);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        /// <summary>
        /// 测试正常范围数据返回
        /// </summary>
        [Fact]
        public void GetCustomersByRank_ValidRange_ShouldReturnOk()
        {
            // Arrange
            var mockCustomers = new List<CustomerDTO>
            {
                new CustomerDTO
                {
                    CustomerId= 1,
                    Score=500,
                    Rank=1
                },
                new CustomerDTO
                {
                    CustomerId= 2,
                    Score=400,
                    Rank=2
                },
            };
            _mockService.Setup(s => s.GetCustomersByRank(1, 2)).Returns(mockCustomers);

            // Act
            var result = _lbdController.GetCustomersByRank(1, 2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            var returnedCustomers = Assert.IsType<List<CustomerDTO>>(okResult.Value);
            Assert.Equal(2, returnedCustomers.Count);
            Assert.Equal(1, returnedCustomers[0].Rank);
            Assert.Equal(2, returnedCustomers[1].Rank);
        }



        /// <summary>
        /// 测试空数据返回，预期返回200
        /// </summary>
        [Fact]
        public void GetCustomersByRank_InvalidRange_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            _mockService.Setup(s => s.GetCustomersByRank(100, 101)).Returns(new List<CustomerDTO>());

            // Act
            var result = _lbdController.GetCustomersByRank(100, 101);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            var returnedCustomers = Assert.IsType<List<CustomerDTO>>(okResult.Value);
            Assert.Empty(returnedCustomers);
        }


        /// <summary>
        /// 测试合法用户返回，预期得到200返回码和正确的数据。
        /// </summary>
        [Fact]
        public void GetCustomerAndNeighbors_ValidCustomer_ShouldReturnOk()
        {
            // Arrange
            var mockCustomers = new List<CustomerDTO>
            {
                new CustomerDTO
                {
                    CustomerId=2,
                    Score=400,
                    Rank=1
                },
                new CustomerDTO
                {
                    CustomerId= 3,
                    Score=300,
                    Rank=2
                },
                new CustomerDTO
                {
                    CustomerId= 4,
                    Score=200,
                    Rank=3
                }
            };
            _mockService.Setup(s => s.GetCustomerAndNeighbors(3, 1, 1)).Returns(mockCustomers);

            // Act
            var result = _lbdController.GetCustomerAndNeighbors(3, 1, 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            var returnedCustomers = Assert.IsType<List<CustomerDTO>>(okResult.Value);
            Assert.Equal(3, returnedCustomers.Count);
            Assert.Equal(1, returnedCustomers[0].Rank);
            Assert.Equal(2, returnedCustomers[1].Rank);
            Assert.Equal(3, returnedCustomers[2].Rank);
        }

        /// <summary>
        /// 测试非法用户获取排名和前后邻居，预期得到200返回码和空数据
        /// </summary>
        [Fact]
        public void GetCustomerAndNeighbors_InvalidCustomer_ShouldReturnOkWithEmptyList()
        {
            _mockService.Setup(s => s.GetCustomerAndNeighbors(999, 1, 1)).Returns(new List<CustomerDTO>());

            var result = _lbdController.GetCustomerAndNeighbors(999, 1, 1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            var returnedCustomers = Assert.IsType<List<CustomerDTO>>(okResult.Value);
            Assert.Empty(returnedCustomers);
        }
    }
}