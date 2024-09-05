using BoSai.CustomerLeaderboard.Domain.Interfaces;
using BoSai.CustomerLeaderboard.Domain.Models;
using BoSai.CustomerLeaderboard.Shared;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BoSai.CustomerLeaderboard.API.Controllers
{
    [ApiController]
    [Route("leaderboard")]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;

        public LeaderboardController(ILeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        /// <summary>
        /// 按排名范围获取客户的排名
        /// </summary>
        /// <param name="start">起始排名</param>
        /// <param name="end">截止排名</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<List<Customer>> GetCustomersByRank([FromQuery] int start, [FromQuery] int end)
        {
            if (start > end || start < 0 || end < 0)
            {
                return new BadRequestObjectResult(new ErrorInfo("InvalidParam", "非法参数"));
            }
            var customers = _leaderboardService.GetCustomersByRank(start, end);
            return Ok(customers);  // 返回指定范围内的客户信息
        }

        /// <summary>
        /// 按客户ID获取客户及其邻居的排名
        /// </summary>
        /// <param name="customerId">客户ID</param>
        /// <param name="high">排名在客户id签名前面的邻居数量</param>
        /// <param name="low">排名在客户id签名后面的邻居数量</param>
        /// <returns></returns>
        [HttpGet("{customerid}")]
        public ActionResult<List<Customer>> GetCustomerAndNeighbors(
            [Required] long customerId,
            [FromQuery] int high = 0,
            [FromQuery] int low = 0)
        {
            if (customerId < 1 || high < 0 || low < 0)
            {
                return new BadRequestObjectResult(new ErrorInfo("InvalidParam", "非法参数"));
            }
            var customers = _leaderboardService.GetCustomerAndNeighbors(customerId, high, low);
            return Ok(customers);  // 返回客户及其邻居的信息
        }
    }
}
