using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoSai.CustomerLeaderboard.Domain.Models
{
    /// <summary>
    /// 客户得分实体
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// 客户唯一标识符
        /// </summary>
        public long CustomerId { get; private set; }

        /// <summary>
        /// 客户分数
        /// </summary>
        public decimal Score { get; private set; }

        public Customer() { }

        /// <summary>
        /// 设置客户id
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Customer SetCustomerId(long customerId)
        {
            if (customerId < 1)
                throw new ArgumentOutOfRangeException(nameof(customerId), $"{nameof(customerId)}必须为正整数");
            this.CustomerId = customerId;
            return this;
        }

        /// <summary>
        /// 分数变化值
        /// </summary>
        /// <param name="scoreChange"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Customer ScoreChange(decimal scoreChange)
        {
            if (scoreChange > 1000 || scoreChange < -1000)
                throw new ArgumentOutOfRangeException("score", "score分数不能大于1000小于-1000");
            this.Score += scoreChange;
            return this;
        }
    }
}
