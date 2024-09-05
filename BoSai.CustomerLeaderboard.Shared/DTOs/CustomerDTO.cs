using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoSai.CustomerLeaderboard.Shared.DTOs
{
    public class CustomerDTO
    {
        public CustomerDTO() { }
        //public CustomerDTO(long customerId, decimal score, decimal rank)
        //{
        //    CustomerId = customerId;
        //    Score = score;
        //    Rank = rank;
        //}
        /// <summary>
        /// 客户唯一标识符
        /// </summary>
        public long CustomerId { get; set; }

        /// <summary>
        /// 客户分数
        /// </summary>
        public decimal Score { get; set; }

        /// <summary>
        /// 排名
        /// </summary>
        public decimal Rank { get; set; }
    }
}
