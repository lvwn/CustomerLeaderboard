using BoSai.CustomerLeaderboard.API.Controllers;
using BoSai.CustomerLeaderboard.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoSai.CustomerLeaderboard.API.Test
{

    /// <summary>
    /// 客户单元测试
    /// </summary>
    public class CustomerControllerTests
    {
        private readonly Mock<ILeaderboardService> _mockService;
        private readonly CustomerController _ctController;
        public CustomerControllerTests()
        {
            _mockService = new Mock<ILeaderboardService>();
            _ctController = new CustomerController(_mockService.Object);
        }
       
        /// <summary>
        /// 测试正常数据得分更新，预期得到200返回码和更新后的分数
        /// </summary>
        /// <param name="scoreChange"></param>
        /// <param name="p"></param>
        [Theory]
        [InlineData(1, 500)]
        [InlineData(2, -500)]
        public void UpdateScore_ValidScoreChange_ShouldReturnOk(decimal scoreChange, int p)
        {
            // Arrange
            _mockService.Setup(s => s.UpdateScore(It.IsAny<long>(), It.IsAny<decimal>())).Returns(scoreChange);

            // Act
            var result = _ctController.UpdateScore(1, scoreChange);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(scoreChange, okResult.Value);
        }

        /// <summary>
        /// 分别客户id为负值、得分超过1000，得分低于-1000的情况。预期得到400返回码
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="score"></param>
        [Theory]
        [InlineData(-1, 90)]
        [InlineData(100, -1001)]
        [InlineData(100, 1001)]
        public void UpdateScore_InvalidRange_ShouldReturnBadRequest(int customerId, int score)
        {
            // Act
            var result = _ctController.UpdateScore(customerId, score);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }
    }
}
