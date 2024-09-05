using BoSai.CustomerLeaderboard.Domain.Interfaces;
using BoSai.CustomerLeaderboard.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BoSai.CustomerLeaderboard.API.Controllers
{
    [Route("customer")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;

        public CustomerController(ILeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        /// <summary>
        /// 更新客户分数的API
        /// </summary>
        /// <param name="customerid">客户id</param>
        /// <param name="score">变化分数</param>
        /// <returns>客户更新后的分数</returns>
        [HttpPost("{customerid}/score/{score}")]
        public ActionResult<decimal> UpdateScore([Required] long customerid, [Required] decimal score)
        {
            if (customerid < 0 || score > 1000 || score < -1000)
            {
                return new BadRequestObjectResult(new ErrorInfo("InvalidParam", "非法参数"));
            }
            try
            {
                var updatedScore = _leaderboardService.UpdateScore(customerid, score);
                return Ok(updatedScore);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return new BadRequestObjectResult(new { ErrorCode = "InvalidParam", ErrorMsg = $"{ex.ParamName}{ex.Message}" });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { ErrorCode = "SystemError", ErrorMsg = ex.Message });
            }
        }
    }
}
